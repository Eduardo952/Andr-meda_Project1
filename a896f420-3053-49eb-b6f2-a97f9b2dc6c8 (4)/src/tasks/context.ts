import { BALANCE } from "@/config/balance";
import { deriveSol, type NormalizedSol } from "@/engine/normalize";
import { applyDelta } from "@/engine/outcome";

/** Educational budgets are puzzle state, never campaign resources or NASA sensors. */
export const IRRIGATION = { moisture: [1, 3, 2], target: 3, water: 3 } as const;
export function irrigationNeeds(moisture: readonly number[] = IRRIGATION.moisture) {
  return moisture.map(value => Math.max(0, IRRIGATION.target - value));
}
export function irrigationCorrect(water: readonly number[]) {
  const needs = irrigationNeeds();
  return water.length === needs.length && water.every((v, i) => v === needs[i]);
}
export function priorityFor(oxygen: number, sol: NormalizedSol): "air" | "heat" | "water" {
  if (oxygen < BALANCE.initialOxygen) return "air";
  return deriveSol(sol).demand.life >= deriveSol(sol).demand.greenhouse ? "heat" : "water";
}
export function rewardPreview(integrity: number, oxygen: number) {
  return {
    integrity: { before: integrity, after: applyDelta(integrity, BALANCE.taskReward.integrity) },
    oxygen: { before: oxygen, after: applyDelta(oxygen, BALANCE.taskReward.oxygen) },
  };
}
export function observationImpact(sol: NormalizedSol) {
  return sol.marsImpact.value === null ? "unknown" : sol.marsImpact.value.impact ? "yes" : "no";
}