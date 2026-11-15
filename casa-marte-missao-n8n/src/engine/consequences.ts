import { BALANCE } from "@/config/balance";
import type { Allocation, SystemId } from "./demand";
import type { SolOutcome } from "./outcome";
import { roundTo5 } from "./rounding";

export type ConsequenceResource = SystemId | "lifeSupport" | "solar" | "dose" | "observatory";
export type ConsequenceSeverity = "minor" | "moderate" | "severe" | "positive" | "info";
export interface ApiConsequenceFacts {
  flareClass: string | null;
  cmeSpeed: number | null;
  airTempMean: number | null;
  windSpeed: number | null;
}
export interface CauseFacts {
  allocated?: number; demand?: number; missing?: number;
  flareClass?: string; cmeSpeed?: number; airTempMean?: number; windSpeed?: number;
  dust?: number; energyLossPct?: number; alarmPenalty?: number;
}
export interface ConsequenceEffects {
  integrity?: number; oxygen?: number; dose?: number; nextEnergyLossPct?: number; bonusStars?: number;
}
export interface ConsequenceEvent {
  id: string; resource: ConsequenceResource; severity: ConsequenceSeverity;
  simulated: boolean; cause: CauseFacts; effects: ConsequenceEffects;
}
export interface DustStorm {
  active: boolean; simulated: true; source: "insight" | "training" | "none";
  windSpeed: number | null;
}

/** Real wind may trigger a labeled simulation; missing wind uses a fixed training schedule. */
export function stormForSol(solIdx: number, windSpeed: number | null): DustStorm {
  const wind = windSpeed !== null && Number.isFinite(windSpeed) ? windSpeed : null;
  if (wind !== null) return { active: BALANCE.marsWeatherModifier && wind > BALANCE.weather.windStormThreshold, simulated: true, source: "insight", windSpeed: wind };
  const active = BALANCE.marsWeatherModifier && BALANCE.weather.scriptedStormSols.includes(solIdx + 1);
  return { active, simulated: true, source: active ? "training" : "none", windSpeed: null };
}

export function panelDustAfterStorm(current: readonly number[], storm: DustStorm): number[] {
  return Array.from({ length: 4 }, (_, i) => Math.max(0, Math.min(100, (current[i] ?? 0) + (storm.active ? BALANCE.weather.dustGain[i] ?? 0 : 0))));
}

/** A successful complete cleaning run clears every array; local previews never call this. */
export function panelDustAfterCleaning(current: readonly number[], storm: DustStorm, cleaned: boolean): number[] {
  return cleaned ? [0, 0, 0, 0] : panelDustAfterStorm(current, storm);
}

export function cleaningPasses(dust: number): number {
  if (!Number.isFinite(dust) || dust <= 10) return 0;
  if (dust <= BALANCE.cleaning.lightMax) return BALANCE.cleaning.lightPasses;
  if (dust <= BALANCE.cleaning.heavyMax) return BALANCE.cleaning.heavyPasses;
  return BALANCE.cleaning.buriedPasses;
}

export function energyLossPct(panelDust: readonly number[]): number {
  const average = panelDust.slice(0, 4).reduce((sum, value) => sum + Math.max(0, Math.min(100, value)), 0) / 4;
  return Math.min(BALANCE.miniGames.weatherDustCap, Math.round(average * BALANCE.miniGames.weatherDustCap) / 100);
}

/** Five-kWh allocation steps are retained; a fully buried base loses at most 20 kWh. */
export function effectiveEnergyPool(panelDust: readonly number[]): number {
  return Math.max(BALANCE.energyPool - BALANCE.miniGames.weatherDustCap, roundTo5(BALANCE.energyPool - energyLossPct(panelDust)));
}

export function missingSeverity(missing: number, demand: number): ConsequenceSeverity {
  const ratio = demand > 0 ? missing / demand : 0;
  return ratio >= BALANCE.consequences.moderateMissingRatio ? "severe" : ratio >= BALANCE.consequences.minorMissingRatio ? "moderate" : "minor";
}

export function doseGain(shieldMissing: number, shelterMissed: boolean): number {
  return Math.max(0, shieldMissing) * BALANCE.consequences.dosePerShieldKwh + (shelterMissed ? BALANCE.consequences.shelterMissedDose : 0);
}

/** Pure evaluation. Existing SolOutcome damage remains unchanged; these are explicit new rules. */
export function evaluateConsequences(args: {
  outcome: SolOutcome; allocation: Allocation; demand: Allocation; facts: ApiConsequenceFacts;
  shelterMissed: boolean; currentDose: number; panelDust: readonly number[]; storm: DustStorm; cleaned: boolean;
}): ConsequenceEvent[] {
  const { outcome, facts } = args;
  const events: ConsequenceEvent[] = [];
  const shield = outcome.systems.shield, life = outcome.systems.life, greenhouse = outcome.systems.greenhouse;
  const dose = doseGain(shield.missing, args.shelterMissed);
  if (shield.missing > 0 || args.shelterMissed) events.push({
    id: "shield-deficit", resource: "shield",
    severity: args.currentDose + dose >= BALANCE.consequences.severeDose ? "severe" : missingSeverity(shield.missing, shield.demand),
    simulated: false,
    cause: { allocated: shield.allocated, demand: shield.demand, missing: shield.missing, ...(facts.flareClass ? { flareClass: facts.flareClass } : {}), ...(facts.cmeSpeed !== null ? { cmeSpeed: facts.cmeSpeed } : {}), ...(args.shelterMissed ? { alarmPenalty: BALANCE.consequences.shelterMissedDose } : {}) },
    effects: { integrity: outcome.integrityDelta, dose },
  });
  if (life.missing > 0) events.push({
    id: "life-deficit", resource: "lifeSupport", severity: missingSeverity(life.missing, life.demand), simulated: false,
    cause: { allocated: life.allocated, demand: life.demand, missing: life.missing, ...(facts.airTempMean !== null ? { airTempMean: facts.airTempMean } : {}) },
    effects: { oxygen: -life.missing * BALANCE.damage.oxygenPerLifeKwh },
  });
  const nextDust = panelDustAfterCleaning(args.panelDust, args.storm, args.cleaned);
  const nextLoss = energyLossPct(nextDust);
  if (args.storm.active || args.cleaned) events.push({
    id: args.cleaned ? "solar-cleaned" : "solar-dust", resource: "solar", severity: args.cleaned ? "positive" : nextLoss >= 15 ? "severe" : nextLoss >= 8 ? "moderate" : "minor", simulated: true,
    cause: { dust: Math.round(nextDust.reduce((a, b) => a + b, 0) / 4), energyLossPct: nextLoss, ...(args.storm.windSpeed !== null ? { windSpeed: args.storm.windSpeed } : {}) },
    effects: { nextEnergyLossPct: nextLoss },
  });
  if (greenhouse.missing > 0) events.push({
    id: "greenhouse-deficit", resource: "greenhouse", severity: missingSeverity(greenhouse.missing, greenhouse.demand), simulated: false,
    cause: { allocated: greenhouse.allocated, demand: greenhouse.demand, missing: greenhouse.missing },
    effects: { oxygen: -greenhouse.missing * BALANCE.damage.oxygenPerGreenhouseKwh },
  });
  const surpluses = (Object.keys(outcome.systems) as SystemId[]).filter(k => outcome.systems[k].status === "surplus");
  if (surpluses.length) events.push({ id: "energy-surplus", resource: "observatory", severity: "info", simulated: false, cause: {}, effects: {} });
  if ((Object.keys(outcome.systems) as SystemId[]).every(k => outcome.systems[k].status === "ideal")) events.push({
    id: "perfect-sol", resource: "observatory", severity: "positive", simulated: false, cause: {},
    effects: { integrity: BALANCE.taskReward.integrity, oxygen: BALANCE.taskReward.oxygen, bonusStars: BALANCE.consequences.perfectBonusStars },
  });
  return events;
}

export function successTier(integrity: number, oxygen: number, dose: number): "gold" | "silver" | "bronze" {
  if (integrity >= BALANCE.consequences.goldResourceMin && oxygen >= BALANCE.consequences.goldResourceMin && dose < BALANCE.consequences.goldDoseMax) return "gold";
  if (integrity >= BALANCE.consequences.silverResourceMin && oxygen >= BALANCE.consequences.silverResourceMin && dose < BALANCE.consequences.silverDoseMax) return "silver";
  return "bronze";
}

export function consequenceTotals(events: readonly ConsequenceEvent[]) {
  return events.reduce((total, event) => ({
    integrity: total.integrity + (event.id === "perfect-sol" ? event.effects.integrity ?? 0 : 0),
    oxygen: total.oxygen + (event.id === "perfect-sol" ? event.effects.oxygen ?? 0 : 0),
    dose: total.dose + (event.effects.dose ?? 0),
    bonusStars: total.bonusStars + (event.effects.bonusStars ?? 0),
  }), { integrity: 0, oxygen: 0, dose: 0, bonusStars: 0 });
}
