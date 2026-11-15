import { BALANCE } from "@/config/balance";
import { createCameraRig } from "@/game/sim/camera";

/** Per-frame mutable state, written by the single stepper. Never put this in React state. */
export const runtime = {
  x: 0, z: 2, vx: 0, vz: 0, heading: Math.PI, speed: 0,
  solTime: BALANCE.startSolFraction as number,
  sunDir: [0, 1, 0] as [number, number, number],
  day: 1,
  walkPhase: 0,
  /** 0..1 smoothed alarm level for visuals. */
  alarm: 0,
  camera: createCameraRig(),
  metrics: { calls: 0, triangles: 0, fps: 0 },
  interiorId: null as string | null,
  ambienceTime: 0,
  jump: 0,
  celebration: 0,
  interaction: 0,
  /** Confirmed cleaning drives a presentation-only robot and row-by-row dust fade. */
  cleaning: 0,
  cleaningSol: -1,
  cleaningFrom: [0, 0, 0, 0] as number[],
  emergencyLevel: 0,
  emergencyKind: "none" as "none" | "solar" | "lifeSupport" | "dust" | "greenhouse",
  /** True while the player is looking through the telescope (freezes walking). */
  observing: false,
  observePath: [] as { x: number; z: number }[],
  observeArrived: false, observeWait: 0,
  /** 0..1 smoothed blend used by the astronaut "look through the telescope" animation. */
  observe: 0,
  mood: "happy" as "happy" | "worried" | "excited" | "proud",
};

export const input = { keys: new Set<string>(), joyX: 0, joyY: 0 };
