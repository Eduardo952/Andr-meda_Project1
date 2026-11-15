import { BALANCE } from "@/config/balance";
import type { DerivedSol, NormalizedSol } from "@/engine/normalize";
import type { Allocation } from "@/engine/demand";
import type { SolOutcome } from "@/engine/outcome";
import type { Lang } from "@/i18n";
import type { NarratorFacts, NarratorPhase, NarratorRequest, QuestionId, Severity } from "./types";
import { assignTasks } from "@/tasks/assignments";

export const FORMULA_VERSION = "1.0";

export function severityOf(sol: NormalizedSol, d: DerivedSol): Severity {
  if (d.alarm) return "alert";
  if (sol.cme.value || (sol.flare.value && BALANCE.alarmFlareLetters.includes(sol.flare.value.letter))) return "attention";
  return "calm";
}

/** Engine facts -> narrator facts (only fields that exist; absent = null). */
export function buildFacts(sol: NormalizedSol, d: DerivedSol): NarratorFacts {
  const imp = sol.marsImpact.value;
  return {
    flare: sol.flare.value ? { class: sol.flare.value.letter, magnitude: sol.flare.value.magnitude, source: "DONKI/FLR" } : null,
    cme: sol.cme.value ? { speed_kms: sol.cme.value.speed, source: "DONKI/CMEAnalysis" } : null,
    mars_impact: imp ? { level: !imp.impact ? "none" : imp.glancing ? "glancing_blow" : "direct", source: "DONKI/WSAEnlilSimulations" } : null,
    weather: sol.airTemp.value ? { temp_avg_c: sol.airTemp.value.av, source: "InSight/AT" } : null,
    severity: severityOf(sol, d),
  };
}

export function buildRequest(args: {
  lang: Lang; sol: NormalizedSol; derived: DerivedSol; alloc: Allocation; outcome: SolOutcome | null;
  phase: NarratorPhase; questionId?: QuestionId | null;
  taskResults?: NarratorRequest["task_results"];
}): NarratorRequest {
  const o = args.outcome;
  return {
    lang: args.lang,
    sol: args.sol.sol,
    facts: buildFacts(args.sol, args.derived),
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
