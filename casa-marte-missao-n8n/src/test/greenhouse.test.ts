import { describe, expect, it } from "vitest";
import { gardenVisuals, collectApple, useGarden } from "@/game/greenhouse";
import { useCampaign } from "@/game/campaign";
import { ROOM_BY_ID } from "@/config/rooms";
import { isWalkable, routeBetween } from "@/game/sim/world";

describe("Greenhouse presentation", () => {
  it("uses existing allocation for bright ideal and surplus gardens", () => {
    expect(gardenVisuals(20, 20)).toMatchObject({ state: "lush", mist: true, ripe: 24 });
    expect(gardenVisuals(40, 20)).toMatchObject({ state: "lush", mist: true, ripe: 24 });
  });
  it("dims moderate gardens without adding a new campaign status", () => {
    expect(gardenVisuals(10, 20)).toMatchObject({ state: "moderate", mist: false, ripe: 12 });
  });
  it("stops mist and reduces ripe apples on deficit", () => {
    expect(gardenVisuals(0, 20)).toMatchObject({ state: "deficit", mist: false, ripe: 4, warmth: 0 });
  });
  it("collects each ripe nearby apple once and rejects unripe or distant apples", () => {
    expect(collectApple([], 0, 4, 7)).toEqual([0]);
    expect(collectApple([0], 0, 4, 2)).toEqual([0]);
    expect(collectApple([], 4, 4, 2)).toEqual([]);
    expect(collectApple([], 0, 4, 7.1)).toEqual([]);
  });
  it("keeps cosmetic harvest separate from campaign resources and resets the basket", () => {
    const before = useCampaign.getState();
    useGarden.getState().pick(0, 24, 2);
    expect(useGarden.getState().collected).toEqual([0]);
    expect(useCampaign.getState()).toBe(before);
    useGarden.getState().reset();
    expect(useGarden.getState().collected).toEqual([]);
  });
  it("expands the greenhouse while retaining the west tunnel and walkable orchard station", () => {
    expect(ROOM_BY_ID.greenhouse).toMatchObject({ x: 52, width: 40, depth: 32, ports: [[32, 0]], entrance: [48, 0] });
    expect(isWalkable(32, 0)).toBe(true);
    expect(isWalkable(48, 0)).toBe(true);
    expect(routeBetween(0, 4, 48, 0).length).toBeGreaterThan(0);
  });
});