import type { NarratorRequest } from "./types";
import { assignableTasks } from "@/tasks/assignments";
import { translate } from "@/i18n";

/** Pure post-LLM validator. Returns null when valid, otherwise a reason. */
export function validateNarration(text: unknown, req: NarratorRequest): string | null {
  if (typeof text !== "string" || !text.trim()) return "empty";
  const t = text.trim();
  if (t.length > 400) return "too_long";
  const sentences = t.split(/[.!?…]+(?=\s|$)/).map((s) => s.trim()).filter((s) => s.length > 1);
  if (sentences.length > 3) return "too_many_sentences";
  if (detectLang(t) !== req.lang && detectLang(t) !== "unknown") return "wrong_language";

  const f = req.facts;
  const taskResults = req.task_results ?? [];
  for (const task of assignableTasks()) {
    const names = [task.id, translate("en", `mini.${task.id}.name`), translate("pt-BR", `mini.${task.id}.name`)];
    if (names.some(name => t.toLowerCase().includes(name.toLowerCase())) && !taskResults.some(r => r.id === task.id)) return "mentions_incomplete_task";
  }
  if (!taskResults.length && /\b(completed|finished|mistakes?|seconds?|stars?)\b|conclu[ií]|terminou|\berros?\b|segundos|estrelas/i.test(t)) return "mentions_absent_task_result";
  // Metrics must match the mentioned task, not merely some unrelated number in the request.
  const mentioned = taskResults.filter(r => [r.id, translate("en", `mini.${r.id}.name`), translate("pt-BR", `mini.${r.id}.name`)].some(name => t.toLowerCase().includes(name.toLowerCase())));
  for (const [pattern, key] of [[/(-?\d+(?:[.,]\d+)?)\s*(?:mistakes?|erros?)\b/gi, "mistakes"], [/(-?\d+(?:[.,]\d+)?)\s*(?:seconds?|segundos)\b/gi, "seconds"], [/(-?\d+(?:[.,]\d+)?)\s*(?:stars?|estrelas)\b/gi, "stars"]] as const) {
    for (const match of t.matchAll(pattern)) {
      const value = Number(match[1]?.replace(",", "."));
      if (!(mentioned.length ? mentioned : taskResults).some(r => round1(r[key]) === round1(value))) return "unknown_task_metric";
    }
  }
  // Events that are absent must not be mentioned.
  if (!f.flare && /\bflares?\b|erup[cç][aã]o|erup[cç][oõ]es/i.test(t)) return "mentions_absent_flare";
  if (!f.cme && /\bCMEs?\b|coronal|eje[cç][aã]o/i.test(t)) return "mentions_absent_cme";
  if (!f.weather && /temperatur|°\s?C|graus|degrees/i.test(t)) return "mentions_absent_weather";
  if ((!f.mars_impact || f.mars_impact.level === "none") && /\b(hit|hits|reach(es)?|atinge|atingir|chega(r)?) (mars|marte)\b/i.test(t) && !/\b(not|won't|n[aã]o)\b/i.test(t)) return "mentions_absent_impact";

  // Flare classes must match the facts.
  const classes = t.match(/\b[ABCMX]\d+(?:[.,]\d+)?\b/g) ?? [];
  for (const c of classes) {
    const norm = c.replace(/\s/g, "").replace(",", ".");
    const expected = f.flare ? `${f.flare.class}${f.flare.magnitude}` : null;
    if (!expected || !f.flare || Number(norm.slice(1)) !== f.flare.magnitude || norm[0] !== f.flare.class) return "unknown_flare_class";
  }
  // Every number must exist in the request.
  const allowed = allowedNumbers(req);
  const textNoClasses = t.replace(/\b[ABCMX]\d+(?:[.,]\d+)?\b/g, " ");
  for (const tok of textNoClasses.match(/-?\d[\d.,]*/g) ?? []) {
    const cands = numberCandidates(tok.replace(/[.,]$/, ""));
    if (!cands.some((n) => allowed.has(round1(n)))) return `unknown_number:${tok}`;
  }
  return null;
}

function round1(n: number) { return Math.round(n * 10) / 10; }

function numberCandidates(tok: string): number[] {
  const out = [Number(tok.replace(",", ".")), Number(tok.replace(/[.,](?=\d{3}\b)/g, "").replace(",", ".")), Number(tok.replace(/[.,]/g, ""))];
  return out.filter((n) => Number.isFinite(n)).flatMap((n) => [n, Math.abs(n)]);
}

function allowedNumbers(req: NarratorRequest): Set<number> {
  const s = new Set<number>([req.sol]);
  const f = req.facts;
  for (const r of req.task_results ?? []) { s.add(round1(r.seconds)); s.add(r.mistakes); s.add(r.stars); }
  if (f.flare) s.add(round1(f.flare.magnitude));
  if (f.cme) s.add(f.cme.speed_kms);
  if (f.weather) { s.add(round1(f.weather.temp_avg_c)); s.add(Math.round(f.weather.temp_avg_c)); s.add(Math.abs(Math.round(f.weather.temp_avg_c))); s.add(round1(Math.abs(f.weather.temp_avg_c))); }
  for (const k of ["shield", "life", "greenhouse"] as const) { s.add(req.demand_kwh[k]); s.add(req.player_kwh[k]); }
  return s;
}

const EN = /\b(the|and|is|are|was|you|we|our|to|of|at|today|energy|shield|needs?|let's)\b/gi;
const PT = /(?<![\p{L}])(é|são|está|foi|uma|um|você|nós|nosso|nossa|para|de|do|da|hoje|energia|escudo|não|mas|vamos|precisa)(?![\p{L}])/giu;
export function detectLang(t: string): "en" | "pt-BR" | "unknown" {
  const en = (t.match(EN) ?? []).length, pt = (t.match(PT) ?? []).length;
  if (en === pt) return "unknown";
  return en > pt ? "en" : "pt-BR";
}
