import { create } from "zustand";
import { classify } from "@/engine/outcome";

export const APPLE_COUNT = 24;
export const HARVEST_RADIUS = 7;
export const APPLES: [number, number, number][] = Array.from({ length: APPLE_COUNT }, (_, i) => {
  const angle = i * 2.39996, ring = i < 12 ? 2.1 : 2.65;
  return [52 + Math.cos(angle) * ring, 3.5 + (i % 3) * .65, Math.sin(angle) * ring];
});
/** Presentation bands only: existing engine classification still decides mission outcomes. */
export function gardenVisuals(allocated: number, demand: number) {
  const result = classify(allocated, demand);
  const coverage = Math.max(0, Math.min(1, allocated / Math.max(1, demand)));
  const state = result.status !== "deficit" ? "lush" : coverage >= .5 ? "moderate" : "deficit";
  return { state, fullness: .65 + coverage * .35, warmth: coverage, mist: state === "lush", droneSpeed: state === "lush" ? .22 : state === "moderate" ? .12 : .055, ripe: state === "lush" ? APPLE_COUNT : state === "moderate" ? 12 : 4 };
}
export function collectApple(collected: readonly number[], id: number, ripe: number, distance: number) {
  if (!Number.isInteger(id) || id < 0 || id >= ripe || distance > HARVEST_RADIUS || collected.includes(id)) return collected;
  return [...collected, id];
}
/** Session-only basket. Never persisted or connected to energy, rewards or task results. */
export const useGarden = create<{ collected: readonly number[]; pick: (id: number, ripe: number, distance: number) => void; reset: () => void }>((set) => ({
  collected: [],
  pick: (id, ripe, distance) => set(s => ({ collected: collectApple(s.collected, id, ripe, distance) })),
  reset: () => set({ collected: [] }),
}));