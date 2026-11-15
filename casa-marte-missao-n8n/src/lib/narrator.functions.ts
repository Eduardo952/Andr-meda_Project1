import { createServerFn } from "@tanstack/react-start";
import { z } from "zod";
import { parseWebhookResponse } from "./webhook-response";
import { N8N_NARRATOR_TIMEOUT_MS, n8nHeaders, resolveN8n } from "./n8n";

/**
 * "narrator-proxy": forwards a narrator request to the n8n webhook (POST /bubble-narrator).
 * N8N_BASE_URL (or N8N_WEBHOOK_URL) and N8N_WEBHOOK_SECRET are server secrets; they never reach
 * the browser. URL resolution lives in ./n8n.ts.
 * Returns { ok: false } whenever n8n is not configured or fails, so the client uses local templates.
 */
const kwh = z.object({ shield: z.number(), life: z.number(), greenhouse: z.number() });
const status = z.enum(["deficit", "ideal", "surplus"]);
const RequestSchema = z.object({
  deviceId: z.string().min(8).max(64),
  request: z.object({
    lang: z.enum(["en", "pt-BR"]),
    sol: z.number().int().min(1).max(10),
    facts: z.object({
      flare: z.object({ class: z.string().max(1), magnitude: z.number(), source: z.literal("DONKI/FLR") }).nullable(),
      cme: z.object({ speed_kms: z.number(), source: z.literal("DONKI/CMEAnalysis") }).nullable(),
      mars_impact: z.object({ level: z.enum(["none", "minor", "glancing_blow", "direct"]), source: z.literal("DONKI/WSAEnlilSimulations") }).nullable(),
      weather: z.object({ temp_avg_c: z.number(), source: z.literal("InSight/AT") }).nullable(),
      wind: z.object({ speed_avg_ms: z.number(), source: z.literal("InSight/HWS") }).nullable().optional(),
      game: z.object({ dose: z.number().min(0).max(100), energy_pool_kwh: z.number().min(80).max(100), panel_dust_pct: z.number().min(0).max(100), cleaned: z.boolean(), shelter_missed: z.boolean(), simulated_storm: z.boolean(), storm_source: z.enum(["insight", "training", "none"]), consequences: z.array(z.object({ id: z.string().max(40), resource: z.string().max(30), severity: z.string().max(20), allocated: z.number().nullable(), demand: z.number().nullable(), missing: z.number().nullable(), dose: z.number(), energy_loss_pct: z.number() })).max(8) }).optional(),
      severity: z.enum(["calm", "attention", "alert"]),
    }),
    demand_kwh: kwh,
    player_kwh: kwh,
    result: z.object({ shield: status, life: status, greenhouse: status }).nullable(),
    formula_version: z.string().max(10),
    phase: z.enum(["sol_start", "alert", "sol_summary", "hint", "answer", "task_complete"]),
    question_id: z.enum(["what_is_a_flare", "why_shelter", "why_cold_energy"]).nullable(),
    assigned_tasks: z.array(z.enum(["SortFlares", "PhotoDetective", "AlignCoils", "WireShield", "CmeCountdown", "PipeFlow", "ThermostatDial", "SmartFarmCare", "CleanPanels", "TiltSun", "IrrigationPlan", "ResourcePriority", "PowerBalance", "WindReading"])).max(5).optional(),
    task_results: z.array(z.object({ id: z.enum(["SortFlares", "PhotoDetective", "AlignCoils", "WireShield", "CmeCountdown", "PipeFlow", "ThermostatDial", "SmartFarmCare", "CleanPanels", "TiltSun", "IrrigationPlan", "ResourcePriority", "PowerBalance", "WindReading"]), seconds: z.number().finite().min(0).max(60), mistakes: z.number().int().min(0).max(10000), stars: z.number().int().min(1).max(3) })).max(5).optional(),
  }),
});

const TIMEOUT_MS = N8N_NARRATOR_TIMEOUT_MS;
const MAX_PER_MINUTE = 12;
// Best-effort, per server instance (there is no shared rate-limit store).
const hits = new Map<string, number[]>();

function rateLimited(deviceId: string): boolean {
  const now = Date.now();
  const recent = (hits.get(deviceId) ?? []).filter((t) => now - t < 60_000);
  if (recent.length >= MAX_PER_MINUTE) { hits.set(deviceId, recent); return true; }
  recent.push(now);
  hits.set(deviceId, recent);
  if (hits.size > 5000) hits.clear();
  return false;
}

export const narratorProxy = createServerFn({ method: "POST" })
  .inputValidator((input: unknown) => RequestSchema.parse(input))
  .handler(async ({ data }) => {
    const { narratorUrl: url, secret } = resolveN8n(process.env);
    if (!url) return { ok: false as const, reason: "not_configured" };
    if (rateLimited(data.deviceId)) return { ok: false as const, reason: "rate_limited" };
    const ctrl = new AbortController();
    const timer = setTimeout(() => ctrl.abort(), TIMEOUT_MS);
    try {
      const res = await fetch(url, {
        method: "POST",
        headers: n8nHeaders(secret, true),
        body: JSON.stringify(data.request),
        signal: ctrl.signal,
      });
      if (!res.ok) {
        console.error(`n8n webhook failed [${res.status}]: ${(await res.text()).slice(0, 300)}`);
        return { ok: false as const, reason: `http_${res.status}` };
      }
      const body = parseWebhookResponse(await res.json());
      if (!body.text && !body.image) return { ok: false as const, reason: "bad_response" };
      return { ok: true as const, text: body.text ?? "", mood: body.mood, image: body.image };
    } catch (e) {
      console.error("n8n webhook error", e instanceof Error ? e.message : e);
      return { ok: false as const, reason: ctrl.signal.aborted ? "timeout" : "network" };
    } finally {
      clearTimeout(timer);
    }
  });
