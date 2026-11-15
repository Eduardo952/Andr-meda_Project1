import { PALETTE } from "./palette";

/** Original artistic PROPOSAL, not a historical mission crew or NASA data. */
export const CREW = [
  { id: "junior", nameKey: "crew.junior", bedroom: "crew1", player: true, color: PALETTE.turquoise, keepsake: "rocket" },
  { id: "mira", nameKey: "crew.mira", bedroom: "crew2", player: false, color: PALETTE.pink, keepsake: "flower" },
  { id: "theo", nameKey: "crew.theo", bedroom: "crew3", player: false, color: PALETTE.sunYellow, keepsake: "planet" },
  { id: "nova", nameKey: "crew.nova", bedroom: "crew4", player: false, color: PALETTE.techPurple, keepsake: "kite" },
] as const;