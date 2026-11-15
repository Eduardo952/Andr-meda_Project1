import { describe, it, expect } from "vitest";
import { TELESCOPE } from "@/config/telescope";
import { ROOM_BY_ID } from "@/config/rooms";

describe("observatory telescope", () => {
  const m = ROOM_BY_ID.observatory;
  it("stands inside the observatory floor", () => {
    expect(Math.abs(TELESCOPE.x - m.x)).toBeLessThan(m.width / 2);
    expect(Math.abs(TELESCOPE.z - m.z)).toBeLessThan(m.depth / 2);
  });
  it("has an eyepiece standing spot inside its interaction radius", () => {
    expect(Math.hypot(TELESCOPE.standX - TELESCOPE.x, TELESCOPE.standZ - TELESCOPE.z)).toBeLessThanOrEqual(TELESCOPE.useRadius);
  });
});
