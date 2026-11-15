import { describe, expect, it } from "vitest";
import { initialTrainer, runTrainerCycle, focusedCrop, lightForCycle, adjustmentsFor, hasLowWater, useGreenhouseTrainer } from "@/game/greenhouseTrainer";
import { useCampaign } from "@/game/campaign";

describe("Mestre da Estufa sandbox", () => {
  it("starts with 11 L and moderate soil and light", () => { expect(initialTrainer()).toMatchObject({ waterMl: 11000, cycle: 1, soil: "medium", light: "medium" }); });
  it.each([["low", 9640], ["medium", 8960], ["high", 8680]] as const)("subtracts the exact %s irrigation volume", (level, waterMl) => {
    expect(runTrainerCycle(initialTrainer(), level).state.waterMl).toBe(waterMl);
  });
  it("rotates fruit, vegetable and greens", () => { expect([1, 2, 3, 4].map(focusedCrop)).toEqual(["fruit", "vegetable", "greens", "fruit"]); });
  it("keeps moderate soil with medium irrigation", () => { expect(runTrainerCycle(initialTrainer(), "medium").result?.soil).toBe("medium"); });
  it("moves dry soil toward moderate or wet with medium or high irrigation", () => {
    const dry = { ...initialTrainer(), soil: "low" as const };
    expect(runTrainerCycle(dry, "medium").result?.soil).toBe("medium");
    expect(runTrainerCycle(dry, "low").result?.soil).toBe("low");
    expect(runTrainerCycle(dry, "high").result?.soil).toBe("high");
  });
  it("alerts only below 4 L", () => { expect(hasLowWater(3999)).toBe(true); expect(hasLowWater(4000)).toBe(false); });
  it("rejects unaffordable irrigation without consuming water", () => {
    const state = { ...initialTrainer(), waterMl: 2000 };
    expect(runTrainerCycle(state, "medium")).toMatchObject({ state, error: "water", result: null });
    expect(runTrainerCycle(state, "low").state.waterMl).toBe(640);
  });
  it("ends when water reaches zero", () => { expect(runTrainerCycle({ ...initialTrainer(), waterMl: 1360 }, "low").state.ended).toBe(true); });
  it("classifies ideal, one deviation and two deviations", () => {
    expect(runTrainerCycle(initialTrainer(), "medium").result?.health).toBe("healthy");
    expect(runTrainerCycle(initialTrainer(), "low").result?.health).toBe("stressed");
    expect(runTrainerCycle({ ...initialTrainer(), light: "low" }, "low").result?.health).toBe("risk");
  });
  it("marks repeated problems as risk after three cycles", () => { expect(runTrainerCycle({ ...initialTrainer(), problemStreak: 2 }, "low").result?.health).toBe("risk"); });
  it("offers only situation-appropriate adjustments", () => {
    expect(adjustmentsFor(initialTrainer())).toEqual(["none"]);
    expect(adjustmentsFor({ ...initialTrainer(), soil: "low", light: "high" })).toEqual(["none", "moreWater", "lessLight"]);
  });
  it("corrects low light without adding reservoir water", () => {
    const next = runTrainerCycle({ ...initialTrainer(), light: "low" }, "medium", "moreLight");
    expect(next.result?.light).toBe("medium"); expect(next.state.waterMl).toBe(8960);
  });
  it("varies environment deterministically", () => { expect([1, 2, 3, 4].map(lightForCycle)).toEqual(["medium", "low", "high", "medium"]); });
  it("never changes campaign resources or rewards", () => {
    const campaign = useCampaign.getState(); useGreenhouseTrainer.getState().reset();
    useGreenhouseTrainer.getState().decide("medium", "none"); expect(useCampaign.getState()).toBe(campaign);
  });
});