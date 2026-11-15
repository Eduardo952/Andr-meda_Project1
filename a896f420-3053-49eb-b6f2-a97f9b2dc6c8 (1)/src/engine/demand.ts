import { BALANCE } from "@/config/balance";
import type { FlareClass } from "./flare";
import { clamp, roundTo5 } from "./rounding";

export type SystemId = "shield" | "life" | "greenhouse";
export const SYSTEMS: SystemId[] = ["shield", "life", "greenhouse"];
export type Allocation = Record<SystemId, number>;

/**
 * Shield demand — GDD v1 formula (✅): clamp(F_base + 2.5·magnitude + (v/400)²·4, 10, 75), rounded to 5.
 * A CME without a speed means "no CME": the CME term is 0 (never assume 400 km/s, I-06).
 */
export function shieldDemand(flare: FlareClass | null, cmeSpeed: number | null): number {
  const S = BALANCE.shield;
  const flareTerm = flare ? S.flareBase[flare.letter] + S.flareMagnitudeFactor * flare.magnitude : 0;
  const cmeTerm = cmeSpeed && cmeSpeed > 0 ? (cmeSpeed / S.cmeReferenceSpeed) ** 2 * S.cmeFactor : 0;
  return roundTo5(clamp(flareTerm + cmeTerm, S.min, S.max));
}

/**
 * Thermal (life support) demand: base + k · max(0, T_internal − T_external_mean).
 * T_external is the InSight AT mean. Kp is NOT used (it is Earth-based, I-02).
 * Missing AT returns null: the caller shows "no reading this Sol" and uses the labelled fallback.
 */
export function thermalDemand(airTempMean: number | null): number | null {
  if (airTempMean === null || !Number.isFinite(airTempMean)) return null;
  const T = BALANCE.thermal;
  return roundTo5(T.base + T.k * Math.max(0, T.internalC - airTempMean));
}

export function greenhouseDemand(): number {
  return roundTo5(BALANCE.greenhouse.demand);
}

export function freeEnergy(a: Allocation): number {
  return BALANCE.energyPool - (a.shield + a.life + a.greenhouse);
}
