import { BALANCE } from "@/config/balance";
import { translate, formatNumber } from "@/i18n";
import type { Mood, NarratorRequest, NarratorResponse } from "./types";

/** Local templates, parameterized by phase, severity and facts. Used whenever n8n is unavailable or rejected. */
export function templateFor(req: NarratorRequest): NarratorResponse {
  const { lang, facts: f } = req;
  const tr = (k: string, v?: Record<string, string | number>) => translate(lang, `narrator.${k}`, v);
  const n = (x: number) => formatNumber(lang, x);
  const parts: string[] = [];
  let mood: Mood = "happy";
  switch (req.phase) {
    case "task_complete": {
      const task = req.task_results?.at(-1);
      if (!task) return { text: translate(lang, "bubble.taskTry"), mood: "happy" };
      return { text: translate(lang, "mini.taskNarration", { task: translate(lang, `mini.${task.id}.name`), seconds: n(task.seconds), mistakes: task.mistakes }), mood: "proud" };
    }
    case "answer":
      return { text: tr(`answers.${req.question_id ?? "what_is_a_flare"}`), mood: "excited" };
    case "hint": {
      const big = (["shield", "life", "greenhouse"] as const).reduce((a, b) => (req.demand_kwh[a] >= req.demand_kwh[b] ? a : b));
      return { text: tr("hint", { system: translate(lang, `sys.${big}`) }), mood: "excited" };
    }
    case "sol_summary": {
      const r = req.result;
      const primary = f.game?.consequences.find(event => !["perfect-sol", "energy-surplus"].includes(event.id));
      if (primary) parts.push(tr(`consequences.${primary.id}`, { a: primary.allocated ?? 0, d: primary.demand ?? 0, m: primary.missing ?? 0, dose: primary.dose, loss: primary.energy_loss_pct }));
      const bad = r ? (["shield", "life", "greenhouse"] as const).filter((k) => r[k] === "deficit") : [];
      if (!bad.length) { parts.push(tr("summary.good")); mood = "proud"; }
      else { parts.push(tr("summary.bad", { systems: bad.map((k) => translate(lang, `sys.${k}`)).join(", ") })); mood = "worried"; }
      if (BALANCE.REVEAL_ANSWER_AT_SUMMARY) parts.push(tr("summary.reveal", { s: req.demand_kwh.shield, l: req.demand_kwh.life, g: req.demand_kwh.greenhouse }));
      return { text: parts.join(" "), mood };
    }
    default: {
      // sol_start and alert
      parts.push(tr(`start.${f.severity}`, { sol: req.sol }));
      if (f.flare) parts.push(tr("flare", { c: `${f.flare.class}${n(f.flare.magnitude)}` }));
      if (f.cme) parts.push(f.mars_impact?.level === "none" ? tr("cmeNoImpact", { v: n(f.cme.speed_kms) }) : tr("cme", { v: n(f.cme.speed_kms) }));
      else if (f.weather) parts.push(tr("weather", { t: n(Math.round(f.weather.temp_avg_c)) }));
      if (f.game?.simulated_storm) parts.push(tr("dustStorm", { v: f.wind ? n(f.wind.speed_avg_ms) : tr("trainingWind") }));
      if (req.phase === "alert") parts.splice(1, parts.length - 1, tr("alert"));
      mood = f.severity === "alert" ? "worried" : f.severity === "attention" ? "excited" : "happy";
      return { text: parts.slice(0, 3).join(" "), mood };
    }
  }
}
