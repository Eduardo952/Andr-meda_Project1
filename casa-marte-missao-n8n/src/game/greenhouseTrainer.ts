import { create } from "zustand";

/** Educational sandbox only: volumes are game adaptations, not official NASA levels. */
export const IRRIGATION_ML = { low: 1360, medium: 2040, high: 2320 } as const;
export type Irrigation = keyof typeof IRRIGATION_ML;
export type Band = "low" | "medium" | "high";
export type Crop = "fruit" | "vegetable" | "greens";
export type Health = "healthy" | "stressed" | "risk";
export type Adjustment = "none" | "moreWater" | "lessWater" | "moreLight" | "lessLight";
export const CROPS: Crop[] = ["fruit", "vegetable", "greens"];
export interface TrainerState {
  waterMl: number; cycle: number; soil: Band; light: Band;
  crops: Record<Crop, Health>; problemStreak: number; ended: boolean;
}
export interface CycleResult { crop: Crop; health: Health; soil: Band; light: Band; usedMl: number }
export function initialTrainer(): TrainerState {
  return { waterMl: 11000, cycle: 1, soil: "medium", light: "medium", crops: { fruit: "healthy", vegetable: "healthy", greens: "healthy" }, problemStreak: 0, ended: false };
}
export function focusedCrop(cycle: number): Crop { return CROPS[(cycle - 1) % CROPS.length] ?? "fruit"; }
export function hasLowWater(waterMl: number): boolean { return waterMl < 4000; }
export function lightForCycle(cycle: number): Band {
  return (["medium", "low", "high", "medium", "low", "medium"] as const)[(cycle - 1) % 6] ?? "medium";
}
export function adjustmentsFor(state: TrainerState): Adjustment[] {
  return ["none", ...(state.soil === "low" ? ["moreWater" as const] : state.soil === "high" ? ["lessWater" as const] : []),
    ...(state.light === "low" ? ["moreLight" as const] : state.light === "high" ? ["lessLight" as const] : [])];
}
export function runTrainerCycle(state: TrainerState, level: Irrigation, adjustment: Adjustment = "none"):
  { state: TrainerState; result: CycleResult | null; error: "ended" | "water" | "adjustment" | null } {
  if (state.ended) return { state, result: null, error: "ended" };
  const usedMl = IRRIGATION_ML[level];
  if (usedMl > state.waterMl) return { state, result: null, error: "water" };
  if (!adjustmentsFor(state).includes(adjustment)) return { state, result: null, error: "adjustment" };
  // Explicit training transition: each irrigation moves soil one band, or maintains the ideal band.
  const bands: Band[] = ["low", "medium", "high"];
  let index = level === "high" ? 2 : level === "medium" ? 1 : Math.max(0, bands.indexOf(state.soil) - 1);
  index += adjustment === "moreWater" ? 1 : adjustment === "lessWater" ? -1 : 0;
  const soil = bands[Math.max(0, Math.min(2, index))] ?? "medium";
  const light = adjustment === "moreLight" || adjustment === "lessLight" ? "medium" : state.light;
  const problems = Number(soil !== "medium") + Number(light !== "medium");
  const problemStreak = problems ? state.problemStreak + 1 : 0;
  const health: Health = problems === 2 || problemStreak >= 3 ? "risk" : problems === 1 ? "stressed" : "healthy";
  const crop = focusedCrop(state.cycle), waterMl = state.waterMl - usedMl;
  return { state: { ...state, waterMl, cycle: state.cycle + 1, soil, light: lightForCycle(state.cycle + 1),
    crops: { ...state.crops, [crop]: health }, problemStreak, ended: waterMl === 0 },
    result: { crop, health, soil, light, usedMl }, error: null };
}
/** Session-only state; never writes campaign allocations, task results, narrator facts or rewards. */
export const useGreenhouseTrainer = create<{ open: boolean; state: TrainerState; result: CycleResult | null;
  setOpen: (open: boolean) => void; decide: (level: Irrigation, adjustment: Adjustment) => boolean; reset: () => void; finish: () => void }>((set, get) => ({
  open: false, state: initialTrainer(), result: null,
  setOpen: open => set({ open }),
  decide: (level, adjustment) => { const next = runTrainerCycle(get().state, level, adjustment); if (next.error) return false; set({ state: next.state, result: next.result }); return true; },
  reset: () => set({ state: initialTrainer(), result: null }),
  finish: () => set(s => ({ state: { ...s.state, ended: true } })),
}));