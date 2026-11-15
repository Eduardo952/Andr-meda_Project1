import { describe, expect, it } from "vitest";
import { BALANCE } from "@/config/balance";
import { evaluateSol } from "@/engine/outcome";
import {
  cleaningPasses, consequenceTotals, doseGain, effectiveEnergyPool, energyLossPct,
  evaluateConsequences, missingSeverity, panelDustAfterCleaning, panelDustAfterStorm, stormForSol, successTier,
  type ApiConsequenceFacts,
} from "@/engine/consequences";

const facts: ApiConsequenceFacts = { flareClass: "X1.2", cmeSpeed: 800, airTempMean: -60, windSpeed: 18 };
const base = { outcome: evaluateSol({ shield: 55, life: 25, greenhouse: 20 }, { shield: 55, life: 25, greenhouse: 20 }), allocation: { shield: 55, life: 25, greenhouse: 20 }, demand: { shield: 55, life: 25, greenhouse: 20 }, facts, shelterMissed: false, currentDose: 0, panelDust: [0, 0, 0, 0], storm: stormForSol(0, null), cleaned: false };

describe("simulated dust storms and cleaning", () => {
  it("uses real InSight wind only above the provisional threshold", () => {
    expect(stormForSol(0, 12).active).toBe(false);
    expect(stormForSol(0, 12.1)).toMatchObject({ active: true, source: "insight", windSpeed: 12.1 });
    expect(stormForSol(1, 2).active).toBe(false);
  });
  it("uses the deterministic training schedule only when wind is missing", () => {
    expect(stormForSol(0, null).active).toBe(false);
    expect(stormForSol(1, null)).toMatchObject({ active: true, source: "training" });
    expect(stormForSol(2, null).active).toBe(false);
    expect(stormForSol(3, null).active).toBe(true);
  });
  it("accumulates bounded dust and successful cleaning clears every array", () => {
    const storm = stormForSol(1, null);
    expect(panelDustAfterStorm([90, 0, 20, 100], storm)).toEqual([100, 45, 55, 100]);
    expect(panelDustAfterCleaning([90, 80, 70, 60], storm, true)).toEqual([0, 0, 0, 0]);
  });
  it("requires one, two, or three passes at the documented dust bands", () => {
    expect([10, 11, 35, 36, 70, 71, 100].map(cleaningPasses)).toEqual([0, 1, 1, 2, 2, 3, 3]);
  });
  it("caps the next-Sol energy loss at exactly 20 percent", () => {
    expect(energyLossPct([100, 100, 100, 100])).toBe(20);
    expect(effectiveEnergyPool([100, 100, 100, 100])).toBe(80);
    expect(effectiveEnergyPool([0, 0, 0, 0])).toBe(100);
  });
});

describe("crew dose and Sol consequences", () => {
  it("never adds dose for surplus and combines deficit with shelter exposure", () => {
    expect(doseGain(0, false)).toBe(0);
    expect(doseGain(40, true)).toBe(67);
  });
  it("cannot reach a fatal dose in one worst-case first Sol", () => {
    expect(doseGain(75, true)).toBeLessThan(100);
    expect(doseGain(75, true)).toBe(95);
  });
  it("uses the real allocation, demand and flare facts in the shield cause", () => {
    const outcome = evaluateSol({ shield: 15, life: 25, greenhouse: 20 }, { shield: 55, life: 25, greenhouse: 20 });
    const events = evaluateConsequences({ ...base, outcome, allocation: { shield: 15, life: 25, greenhouse: 20 }, demand: { shield: 55, life: 25, greenhouse: 20 } });
    const shield = events.find(event => event.id === "shield-deficit");
    expect(shield?.cause).toMatchObject({ allocated: 15, demand: 55, missing: 40, flareClass: "X1.2", cmeSpeed: 800 });
    expect(shield?.effects.dose).toBe(32);
    expect(shield?.severity).toBe("severe");
  });
  it("keeps original damage unchanged and adds a perfect-Sol reward only once", () => {
    const events = evaluateConsequences(base);
    const perfect = events.find(event => event.id === "perfect-sol");
    expect(base.outcome.integrityDelta === 0).toBe(true);
    expect(perfect?.effects).toEqual({ integrity: BALANCE.taskReward.integrity, oxygen: BALANCE.taskReward.oxygen, bonusStars: 3 });
    expect(consequenceTotals(events)).toEqual({ integrity: 2, oxygen: 2, dose: 0, bonusStars: 3 });
    expect(consequenceTotals(events).bonusStars).toBe(3);
  });
  it("classifies missing-energy ratios at stable thresholds", () => {
    expect(missingSeverity(5, 20)).toBe("moderate");
    expect(missingSeverity(4, 20)).toBe("minor");
    expect(missingSeverity(12, 20)).toBe("severe");
  });
  it("awards success tiers from integrity, oxygen and cumulative dose", () => {
    expect(successTier(80, 80, 24)).toBe("gold");
    expect(successTier(80, 80, 25)).toBe("silver");
    expect(successTier(50, 50, 59)).toBe("silver");
    expect(successTier(49, 100, 0)).toBe("bronze");
  });
});
