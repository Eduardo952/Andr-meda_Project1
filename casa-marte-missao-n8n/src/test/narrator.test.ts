import { describe, it, expect } from "vitest";
import { validateNarration } from "@/services/narrator/validator";
import { templateFor } from "@/services/narrator/templates";
import type { NarratorRequest } from "@/services/narrator/types";

const base: NarratorRequest = {
  lang: "en", sol: 3,
  facts: { flare: { class: "X", magnitude: 9.3, source: "DONKI/FLR" }, cme: { speed_kms: 1570, source: "DONKI/CMEAnalysis" }, mars_impact: { level: "glancing_blow", source: "DONKI/WSAEnlilSimulations" }, weather: null, severity: "alert" },
  demand_kwh: { shield: 75, life: 35, greenhouse: 30 }, player_kwh: { shield: 60, life: 20, greenhouse: 20 },
  result: { shield: "deficit", life: "deficit", greenhouse: "deficit" }, formula_version: "1.0", phase: "sol_summary", question_id: null,
};

describe("narrator validator", () => {
  it("accepts text using only known facts", () => {
    expect(validateNarration("An X9.3 flare and a CME at 1570 km/s! Our shield got 60 but needed 75.", base)).toBeNull();
  });
  it("rejects invented numbers and flare classes", () => {
    expect(validateNarration("The shield needed 80 kWh today.", base)).toMatch(/unknown_number/);
    expect(validateNarration("An M2.0 flare happened today.", base)).toBe("unknown_flare_class");
  });
  it("rejects events that are absent from the facts", () => {
    const calm = { ...base, facts: { ...base.facts, flare: null, cme: null } };
    expect(validateNarration("A big flare happened today.", calm)).toBe("mentions_absent_flare");
    expect(validateNarration("The temperature is very low today.", base)).toBe("mentions_absent_weather");
  });
  it("rejects more than 3 sentences and the wrong language", () => {
    expect(validateNarration("One. Two. Three. Four.", base)).toBe("too_many_sentences");
    expect(validateNarration("Hoje o escudo precisa de mais energia para nós.", base)).toBe("wrong_language");
  });
  it("accepts consequence numbers only when they match game facts", () => {
    const req: NarratorRequest = { ...base, facts: { ...base.facts, game: { dose: 32, energy_pool_kwh: 90, panel_dust_pct: 50, cleaned: false, shelter_missed: false, simulated_storm: true, storm_source: "training", consequences: [{ id: "shield-deficit", resource: "shield", severity: "severe", allocated: 15, demand: 55, missing: 40, dose: 32, energy_loss_pct: 0 }] } } };
    expect(validateNarration("The shield got 15 but needed 55, so dose rose 32 game units.", req)).toBeNull();
    expect(validateNarration("Dose rose 33 game units.", req)).toMatch(/unknown_number/);
    expect(validateNarration("A dust storm is coming.", { ...base, facts: { ...base.facts, game: undefined } })).toBe("mentions_absent_dust_storm");
  });
  it("local templates always pass the validator", () => {
    for (const phase of ["sol_start", "alert", "sol_summary", "hint"] as const) {
      for (const lang of ["en", "pt-BR"] as const) {
        const req = { ...base, phase, lang };
        expect(validateNarration(templateFor(req).text, req)).toBeNull();
      }
    }
  });
});
