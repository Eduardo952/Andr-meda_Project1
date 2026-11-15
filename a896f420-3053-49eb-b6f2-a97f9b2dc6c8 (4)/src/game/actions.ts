import { useGame } from "./store";
import { useCampaign, pendingTasks } from "./campaign";
import { sfx } from "@/audio/sfx";

/** USE pressed near a station. */
export function useStation() {
  const g = useGame.getState();
  const c = useCampaign.getState();
  const id = g.nearStation;
  if (!id) return;
  if (c.openTask || c.energyOpen || c.reportOpen || c.sealOpen) return;
  sfx.use();
  g.visit(id);
  if (id === "control" && c.phase === "plan") { c.setEnergyOpen(true); return; }
  if (c.phase === "tasks") {
    const task = pendingTasks(c.solIdx, c.results).find((t) => t.room === id && !t.inWorld);
    if (task) { c.setOpenTask(task.id); return; }
    if (id === "control") { if (!pendingTasks(c.solIdx, c.results).length) c.setSealOpen(true); else c.setEnergyOpen(true); return; }
  }
  g.say(`bubble.room.${id}`);
}
