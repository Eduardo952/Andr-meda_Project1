import { BALANCE } from "@/config/balance";
import type { DerivedSol, NormalizedSol } from "@/engine/normalize";
import type { Allocation } from "@/engine/demand";
import type { SolOutcome } from "@/engine/outcome";
import type { Lang } from "@/i18n";
import type { NarratorFacts, NarratorPhase, NarratorRequest, QuestionId, Severity } from "./types";
import { assignTasks } from "@/tasks/assignments";
import { effectiveEnergyPool, stormForSol, type ConsequenceEvent } from "@/engine/consequences";

export const FORMULA_VERSION = "1.1";

export function severityOf(sol: NormalizedSol, d: DerivedSol): Severity {
  if (d.alarm) return "alert";
  if (sol.cme.value || (sol.flare.value && BALANCE.alarmFlareLetters.includes(sol.flare.value.letter))) return "attention";
  return "calm";
}

/** Engine facts -> narrator facts (only fields that exist; absent = null). */
export function buildFacts(sol: NormalizedSol, d: DerivedSol, game?: { dose: number; panelDust: readonly number[]; cleaned: boolean; shelterMissed: boolean; consequences: readonly ConsequenceEvent[] }): NarratorFacts {
  const imp = sol.marsImpact.value, wind = sol.windSpeed.value?.av ?? null, storm = stormForSol(sol.sol - 1, wind);
  return {
    flare: sol.flare.value ? { class: sol.flare.value.letter, magnitude: sol.flare.value.magnitude, source: "DONKI/FLR" } : null,
    cme: sol.cme.value ? { speed_kms: sol.cme.value.speed, source: "DONKI/CMEAnalysis" } : null,
    mars_impact: imp ? { level: !imp.impact ? "none" : imp.glancing ? "glancing_blow" : "direct", source: "DONKI/WSAEnlilSimulations" } : null,
    weather: sol.airTemp.value ? { temp_avg_c: sol.airTemp.value.av, source: "InSight/AT" } : null,
    wind: wind !== null ? { speed_avg_ms: wind, source: "InSight/HWS" } : null,
    game: {
      dose: game?.dose ?? 0, energy_pool_kwh: effectiveEnergyPool(game?.panelDust ?? [0, 0, 0, 0]),
      panel_dust_pct: Math.round((game?.panelDust ?? [0, 0, 0, 0]).reduce((a, b) => a + b, 0) / 4),
      cleaned: game?.cleaned ?? false, shelter_missed: game?.shelterMissed ?? false,
      simulated_storm: storm.active, storm_source: storm.source,
      consequences: (game?.consequences ?? []).map(event => ({ id: event.id, resource: event.resource, severity: event.severity,
        allocated: event.cause.allocated ?? null, demand: event.cause.demand ?? null, missing: event.cause.missing ?? null,
        dose: event.effects.dose ?? 0, energy_loss_pct: event.effects.nextEnergyLossPct ?? 0 })),
    },
    severity: severityOf(sol, d),
  };
}

export function buildRequest(args: {
  lang: Lang; sol: NormalizedSol; derived: DerivedSol; alloc: Allocation; outcome: SolOutcome | null;
  phase: NarratorPhase; questionId?: QuestionId | null;
  taskResults?: NarratorRequest["task_results"];
  game?: { dose: number; panelDust: readonly number[]; cleaned: boolean; shelterMissed: boolean; consequences: readonly ConsequenceEvent[] };
}): NarratorRequest {
  const o = args.outcome;
  return {
    lang: args.lang,
    sol: args.sol.sol,
    facts: buildFacts(args.sol, args.derived, args.game),
    demand_kwh: { ...args.derived.demand },
    player_kwh: { ...args.alloc },
    result: o ? { shield: o.systems.shield.status, life: o.systems.life.status, greenhouse: o.systems.greenhouse.status } : null,
    formula_version: FORMULA_VERSION,
    phase: args.phase,
    question_id: args.questionId ?? null,
    assigned_tasks: assignTasks(args.sol.sol).map(t => t.id),
    task_results: args.taskResults ?? [],
  };
}
