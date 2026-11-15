import { describe, it, expect } from "vitest";
import { SOL_SECONDS, marsClock } from "@/game/sim/solClock";
import { translate } from "@/i18n";
import { isWalkable } from "@/game/sim/world";

describe("game rules", () => {
  it("one Sol lasts 24 h 39 min 35.244 s", () => {
    expect(SOL_SECONDS).toBeCloseTo(24 * 3600 + 39 * 60 + 35.244, 3);
    expect(marsClock(0.9999999)).toEqual({ h: 24, m: 39 });
  });
  it("English is the fallback for missing pt-BR keys", () => {
    expect(translate("pt-BR", "title.play")).toBe("Jogar");
    expect(translate("pt-BR", "does.not.exist")).toBe("does.not.exist");
    expect(translate("en", "hud.sol", { n: 2, total: 5 })).toBe("Sol 2 of 5");
  });
  it("player cannot walk outside the base", () => {
    expect(isWalkable(0, 0)).toBe(true); // walkable command hub
    expect(isWalkable(0, 4)).toBe(true); // interior command station
    expect(isWalkable(60, 60)).toBe(false);
  });
});
