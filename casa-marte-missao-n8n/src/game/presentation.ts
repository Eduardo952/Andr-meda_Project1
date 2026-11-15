import { create } from "zustand";
import type { RoomId } from "@/config/rooms";
import { BALANCE } from "@/config/balance";
export const FIRST_WALK: RoomId[] = ["control", "observatory", "shield", "lifeSupport", "greenhouse", "solar"];
/** Navigation is presentation only: it never writes campaign state. */
export function guidedTarget(mission: RoomId | null, manual: RoomId | null, tutorial: RoomId | null, urgent: boolean) {
  return urgent ? mission : manual ?? mission ?? tutorial;
}
export function labelCandidate(near: RoomId | null, entering: RoomId | null) { return near ?? entering; }
export function advanceWalk(step: number, reached: RoomId | null) { return FIRST_WALK[step] === reached ? Math.min(FIRST_WALK.length, step + 1) : step; }
export function enqueueLine<T extends { id: number }>(queue: T[], line: T): T[] { return queue.some(q => q.id === line.id) ? queue : [...queue, line]; }
interface Presentation {
  manual: RoomId | null; cameraUp: boolean; tourStep: number; tourDismissed: boolean;
  setManual: (room: RoomId | null) => void; setCameraUp: (value: boolean) => void;
  reach: (room: RoomId | null) => void; dismissTour: () => void;
}
export const usePresentation = create<Presentation>((set) => ({
  manual: null, cameraUp: false, tourStep: 0, tourDismissed: false,
  setManual: manual => set({ manual }), setCameraUp: cameraUp => set({ cameraUp }),
  reach: room => set(s => { const tourStep=advanceWalk(s.tourStep,room),manual=s.manual===room?null:s.manual; return tourStep===s.tourStep&&manual===s.manual?s:{tourStep,manual}; }),
  dismissTour: () => set({ tourDismissed: true }),
}));

export type AlertKind = "solar" | "lifeSupport" | "dust" | "greenhouse" | "info" | "perfect";
export type AlertStage = "advisory" | "warning" | "emergency" | "recovery";
export interface AlertItem { id: string; kind: AlertKind; hold?: boolean }
export interface ActiveAlert extends AlertItem { elapsed: number; released: boolean }
const ALERT_PRIORITY: Record<AlertKind, number> = { solar: 1, lifeSupport: 2, dust: 3, greenhouse: 4, info: 5, perfect: 5 };
export const ALERT_TIMING = BALANCE.alerts;
export function alertStage(item: ActiveAlert): AlertStage | null {
  if (item.elapsed < ALERT_TIMING.advisory) return "advisory";
  if (item.elapsed < ALERT_TIMING.warning) return "warning";
  if (item.hold && !item.released) return "emergency";
  if (item.elapsed < ALERT_TIMING.emergency) return "emergency";
  if (item.elapsed < ALERT_TIMING.recovery) return "recovery";
  return null;
}
export function orderAlerts<T extends AlertItem>(items: readonly T[]): T[] {
  return [...items].sort((a, b) => ALERT_PRIORITY[a.kind] - ALERT_PRIORITY[b.kind]);
}
interface Emergencies {
  active: ActiveAlert | null; queue: AlertItem[];
  offer: (item: AlertItem) => void; release: (id: string) => void;
  dismiss: () => void; tick: (seconds: number) => void; reset: () => void;
}
/** Ordered presentation only. Alert timers never write mission resources. */
export const useEmergencies = create<Emergencies>((set) => ({
  active: null, queue: [],
  offer: item => set(s => {
    if (s.active?.id === item.id || s.queue.some(q => q.id === item.id)) return s;
    if (!s.active) return { active: { ...item, elapsed: 0, released: false }, queue: s.queue };
    if (ALERT_PRIORITY[item.kind] < ALERT_PRIORITY[s.active.kind]) {
      const { elapsed: _elapsed, released: _released, ...current } = s.active;
      return { active: { ...item, elapsed: 0, released: false }, queue: orderAlerts([current, ...s.queue]) };
    }
    return { active: s.active, queue: orderAlerts([...s.queue, item]) };
  }),
  release: id => set(s => s.active?.id === id ? { active: { ...s.active, hold: false, released: true, elapsed: Math.max(s.active.elapsed, ALERT_TIMING.emergency) } } : s),
  dismiss: () => set(s => s.active ? { active: { ...s.active, hold: false, released: true, elapsed: ALERT_TIMING.emergency } } : s),
  tick: seconds => set(s => {
    if (!s.active) return s;
    const active = { ...s.active, elapsed: s.active.elapsed + Math.max(0, seconds) };
    if (alertStage(active)) return { active };
    const [next, ...queue] = s.queue;
    return { active: next ? { ...next, elapsed: 0, released: false } : null, queue };
  }),
  reset: () => set({ active: null, queue: [] }),
}));