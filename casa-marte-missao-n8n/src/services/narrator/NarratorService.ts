import { narratorProxy } from "@/lib/narrator.functions";
import { validateNarration } from "./validator";
import { templateFor } from "./templates";
import type { Mood, NarratorLine, NarratorRequest } from "./types";
import { useWebhookMedia } from "./media";

/**
 * Single entry point for Bubble's words.
 * Flow: engine facts -> NarratorService -> (n8n via server proxy) -> validator -> UI.
 * Any failure falls back to local templates, so the game works 100% without n8n.
 */
const cache = new Map<string, NarratorLine>();

function hash(s: string): string {
  let h = 5381;
  for (let i = 0; i < s.length; i++) h = ((h << 5) + h + s.charCodeAt(i)) | 0;
  return (h >>> 0).toString(36);
}

function deviceId(): string {
  try {
    let id = localStorage.getItem("mmt-device");
    if (!id) { id = crypto.randomUUID(); localStorage.setItem("mmt-device", id); }
    return id;
  } catch { return "anonymous-device"; }
}

export async function narrate(req: NarratorRequest): Promise<NarratorLine> {
  const key = `${req.sol}|${req.lang}|${req.phase}|${req.question_id ?? ""}|${hash(JSON.stringify([req.result, req.player_kwh, req.facts, req.task_results, req.assigned_tasks]))}`;
  const hit = cache.get(key);
  if (hit) return hit;
  let line: NarratorLine = { ...templateFor(req), source: "template" };
  try {
    const r = await narratorProxy({ data: { deviceId: deviceId(), request: req } });
    if (r.ok && r.image) useWebhookMedia.getState().accept(req.sol, r.image);
    if (r.ok && r.text.trim() && validateNarration(r.text, req) === null) line = { text: r.text.trim(), mood: r.mood as Mood, source: "n8n" };
  } catch {
    // keep template
  }
  cache.set(key, line);
  return line;
}
