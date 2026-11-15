import type { RoomId } from "@/config/rooms";
export const SECTOR_TASKS = {
  observatory: ["SortFlares", "PhotoDetective"],
  shield: ["AlignCoils", "WireShield", "CmeCountdown"],
  lifeSupport: ["PipeFlow", "ThermostatDial"],
  greenhouse: ["SmartFarmCare"],
  solar: ["CleanPanels", "TiltSun"],
} as const;
export type MiniTaskId = typeof SECTOR_TASKS[keyof typeof SECTOR_TASKS][number];
export type Sector = keyof typeof SECTOR_TASKS;
export interface Assignment { id: MiniTaskId; room: Sector }
/** Fixed Sol seed; data availability never changes the assignment. */
export function assignTasks(sol: number): Assignment[] {
  return (Object.keys(SECTOR_TASKS) as Sector[]).map((room, index) => {
    const list = SECTOR_TASKS[room];
    const offset = [0, 1, 0, 0, 1][index] ?? 0;
    const id = list[((Math.max(1, Math.trunc(sol)) - 1) + offset) % list.length];
    if (!id) throw new Error("Empty sector task list");
    return { id, room };
  });
}
export function isMiniTask(id: string): id is MiniTaskId { return Object.values(SECTOR_TASKS).some(list => (list as readonly string[]).includes(id)); }
export function sectorForTask(id: MiniTaskId): RoomId { return assignableTasks().find(t => t.id === id)?.room ?? "control"; }
export function assignableTasks(): Assignment[] { return (Object.keys(SECTOR_TASKS) as Sector[]).flatMap(room => SECTOR_TASKS[room].map(id => ({ id, room }))); }