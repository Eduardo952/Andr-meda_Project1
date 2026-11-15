import { BALANCE } from "@/config/balance";
import { SYSTEMS, type Allocation, type SystemId } from "./demand";

export type Status = "ideal" | "deficit" | "surplus";
export interface SystemResult { allocated: number; demand: number; status: Status; missing: number }
export interface SolOutcome {
  systems: Record<SystemId, SystemResult>;
  integrityDelta: number;
  oxygenDelta: number;
}

/** GDD v1 margin rule: |given − needed| ≤ margin is ideal; below is a deficit; above is surplus (wasted, no reserves yet). */
export function classify(allocated: number, demand: number): SystemResult {
  const diff = allocated - demand;
  const status: Status = Math.abs(diff) <= BALANCE.margin ? "ideal" : diff < 0 ? "deficit" : "surplus";
  return { allocated, demand, status, missing: status === "deficit" ? demand - allocated : 0 };
}

export function evaluateSol(alloc: Allocation, demand: Allocation): SolOutcome {
  const systems = Object.fromEntries(SYSTEMS.map((s) => [s, classify(alloc[s], demand[s])])) as Record<SystemId, SystemResult>;
  const D = BALANCE.damage;
  return {
    systems,
    integrityDelta: -systems.shield.missing * D.integrityPerShieldKwh,
    oxygenDelta: -(systems.life.missing * D.oxygenPerLifeKwh + systems.greenhouse.missing * D.oxygenPerGreenhouseKwh),
  };
}

export function applyDelta(value: number, delta: number): number {
  return Math.max(0, Math.min(100, Math.round(value + delta)));
}
