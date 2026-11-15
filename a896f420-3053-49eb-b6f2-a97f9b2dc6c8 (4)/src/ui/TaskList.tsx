import { CheckCircle2, Circle } from "lucide-react";
import { useT } from "@/i18n";
import { useCampaign } from "@/game/campaign";
import { assignTasks } from "@/tasks/assignments";
import { NumberValue } from "@/tasks/MiniFrame";
export function TaskList() {
  const { t } = useT(); const solIdx = useCampaign(s => s.solIdx), results = useCampaign(s => s.results);
  const list = assignTasks(solIdx + 1), mine = results[solIdx] ?? [];
  const count = list.filter(task => mine.some(r => r.id === task.id && r.success)).length;
  return <details className="mini-checklist" open><summary>{t("tasks.title", { n: solIdx + 1 })} <span><NumberValue value={count} origin="progress" />/<NumberValue value={list.length} origin="count" /></span></summary>
    <ul>{list.map(task => { const done = mine.some(r => r.id === task.id && r.success); return <li key={task.id} title={t(`mini.${task.id}.name`)}>{done ? <CheckCircle2 className="text-bar-integrity" /> : <Circle />}<span>{t(`rooms.${task.room}`)}</span></li>; })}</ul>
    <progress aria-label={t("tasks.progress")} value={count} max={list.length} />{count === list.length && <p>{t("mini.returnControl")}</p>}
  </details>;
}
