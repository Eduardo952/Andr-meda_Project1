import { create } from "zustand";
import type { RoomId } from "@/config/rooms";
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