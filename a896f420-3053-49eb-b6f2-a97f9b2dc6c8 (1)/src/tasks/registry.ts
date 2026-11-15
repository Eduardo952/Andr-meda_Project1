import { BALANCE } from "@/config/balance";
import type { RoomId } from "@/config/rooms";
import type { NormalizedSol } from "@/engine/normalize";
import { assignTasks, assignableTasks, type MiniTaskId } from "./assignments";

export type TaskId = "T02" | "T03" | "T04" | "T05" | "T06" | "T07" | "T08" | "T10" | "T11" | "T14" | "T15" | MiniTaskId;

export interface TaskDef {
  id: TaskId;
  room: RoomId;
  /** Badge: real data source, or null for GAME RULE. */
  source: string | null;
  /** Whether the snapshot has what this task needs. Missing data never blocks the game. */
  available: (s: NormalizedSol) => boolean;
  /** T05 is completed in the world (reach the shelter), not in a modal. */
  inWorld?: boolean;
}

export const TASKS: TaskDef[] = [
  { id: "T06", room: "shield", source: "DONKI notifications", available: (s) => !!s.notifications.value?.length },
  { id: "T02", room: "observatory", source: "DONKI FLR", available: (s) => !!s.flare.value },
  { id: "T03", room: "shield", source: "DONKI CMEAnalysis", available: (s) => !!s.cme.value },
  { id: "T04", room: "shield", source: "DONKI WSA+Enlil", available: (s) => !!s.cme.value },
  { id: "T05", room: "shield", source: "DONKI SEP", available: (s) => !!s.sep.value, inWorld: true },
  { id: "T07", room: "lifeSupport", source: "InSight AT", available: (s) => !!s.airTemp.value },
  { id: "T08", room: "solar", source: "InSight WD", available: (s) => !!s.windDir.value },
  { id: "T10", room: "observatory", source: "NASA Image and Video Library", available: (s) => !!s.photo.value },
  { id: "T11", room: "observatory", source: "APOD", available: (s) => BALANCE.optionalTasksEnabled && !!s.apod?.value },
  { id: "T14", room: "greenhouse", source: null, available: () => true },
  { id: "T15", room: "lifeSupport", source: null, available: () => true },
];

const MINI_SOURCES: Partial<Record<MiniTaskId, string>> = { SortFlares: "DONKI FLR", PhotoDetective: "NASA Image Library", CmeCountdown: "DONKI CMEAnalysis", ThermostatDial: "InSight AT" };
const MINI_TASKS: TaskDef[] = assignableTasks().map(t => ({ ...t, source: MINI_SOURCES[t.id] ?? null, available: () => true }));
export const TASK_BY_ID = Object.fromEntries([...TASKS, ...MINI_TASKS].map((t) => [t.id, t])) as Record<TaskId, TaskDef>;

export function tasksForSol(s: NormalizedSol): TaskDef[] {
  return assignTasks(s.sol).map(t => TASK_BY_ID[t.id]);
}
