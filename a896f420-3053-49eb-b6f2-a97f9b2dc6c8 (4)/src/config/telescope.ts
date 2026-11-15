import { ROOM_BY_ID } from "./rooms";
const m = ROOM_BY_ID.observatory;
/** Telescope pier (matches the model built in scene/AresInteriors.ts). Artistic placement, not an engineering measurement. */
export const TELESCOPE = {
  x: m.x + 7,
  z: m.z - 2,
  /** Player must be within this radius (metres) of the pier to see the "View image" prompt. */
  useRadius: 4.5,
  clearance: 1.4, arrivalRadius: .15, leanSeconds: .65,
  zoomMin: 1, zoomMax: 2, zoomStep: .1,
  /** Where the astronaut stands to look through the eyepiece (behind the pier, facing the tube). */
  standX: m.x + 7,
  standZ: m.z + 0.2,
} as const;
