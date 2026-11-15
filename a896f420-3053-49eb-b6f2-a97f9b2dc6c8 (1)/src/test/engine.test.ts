import { describe, it, expect } from "vitest";
import { roundTo5 } from "@/engine/rounding";
import { parseFlareClass, flareFlux } from "@/engine/flare";
import { shieldDemand, thermalDemand } from "@/engine/demand";
import { classify, evaluateSol } from "@/engine/outcome";
import { cmeTravelHours } from "@/engine/cme";
import { SOLS, DERIVED } from "@/data/campaign";

describe("engine", () => {
  it("rounds to 5 with halves going up", () => {
    expect(roundTo5(32.5)).toBe(35);
    expect(roundTo5(37.5)).toBe(40);
    expect(roundTo5(42.5)).toBe(45);
    expect(roundTo5(31)).toBe(30);
  });
  it("X9.3 with a 1570 km/s CME needs 75 kWh (I-01)", () => {
    expect(shieldDemand(parseFlareClass("X9.3"), 1570)).toBe(75);
  });
  it("a CME without speed adds nothing (never assume 400 km/s)", () => {
    expect(shieldDemand(parseFlareClass("M1.0"), null)).toBe(25);
    expect(shieldDemand(parseFlareClass("M1.0"), 400)).toBe(30);
    expect(shieldDemand(null, null)).toBe(10);
  });
  it("each flare letter is 10x the previous; M9.9 is weaker than X1.0", () => {
    expect(flareFlux(parseFlareClass("X1.0")!) / flareFlux(parseFlareClass("M1.0")!)).toBeCloseTo(10);
    expect(flareFlux(parseFlareClass("M9.9")!)).toBeLessThan(flareFlux(parseFlareClass("X1.0")!));
    expect(parseFlareClass("X28")?.magnitude).toBe(28);
  });
  it("thermal demand grows with the cold and is null without a reading", () => {
    expect(thermalDemand(-62)).toBe(20); // 10 + 0.15*82 = 22.3 -> 20
    expect(thermalDemand(-80)).toBe(25); // 10 + 15 = 25
    expect(thermalDemand(null)).toBeNull();
  });
  it("margin of 5 is ideal, beyond is deficit or surplus", () => {
    expect(classify(25, 30).status).toBe("ideal");
    expect(classify(20, 30).status).toBe("deficit");
    expect(classify(40, 30).status).toBe("surplus");
    const o = evaluateSol({ shield: 20, life: 40, greenhouse: 40 }, { shield: 50, life: 20, greenhouse: 20 });
    expect(o.integrityDelta).toBe(-30);
  });
  it("CME travel time uses 2.28e8 km at constant speed", () => {
    expect(cmeTravelHours(1000)).toBeCloseTo(63.33, 1);
    expect(cmeTravelHours(null)).toBeNull();
  });
  it("snapshots keep missing InSight data as null (no interpolation)", () => {
    expect(SOLS[2]!.airTemp.value).toBeNull();
    expect(DERIVED[2]!.thermalMissing).toBe(true);
    expect(SOLS[2]!.cme.value).toBeNull(); // CME with no speed = no CME
    expect(DERIVED[3]!.alarm).toBe("sep");
  });
});
