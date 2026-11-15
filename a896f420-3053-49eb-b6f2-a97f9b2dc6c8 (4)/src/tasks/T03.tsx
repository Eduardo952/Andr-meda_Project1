import { Button } from "@/components/ui/button";
import { useState } from "react";
import { useT } from "@/i18n";
import { cmeTravelHours } from "@/engine/cme";
import type { NormalizedSol } from "@/engine/normalize";
import { TaskShell, type TaskProps } from "./TaskShell";

export const T03_TOLERANCE = 50; // km/s, GAME RULE

export function T03({ sol, onDone }: TaskProps & { sol: NormalizedSol }) {
  const { t, n } = useT();
  const target = sol.cme.value!.speed;
  const [v, setV] = useState(300);
  const [result, setResult] = useState<null | boolean>(null);
  const h = cmeTravelHours(v)!;
  return (
    <TaskShell id="T03" onDone={onDone} result={result} sample={sol.sample}>
      <p className="font-bold">{t("tasks.T03.target", { v: n(target) })}</p>
      <input type="range" min={200} max={3000} step={10} value={v} disabled={result !== null} onChange={(e) => setV(Number(e.target.value))} className="my-3 h-12 w-full accent-[var(--tech)]" aria-label={t("tasks.T03.speed", { v })} />
      <p className="font-display text-xl">{t("tasks.T03.speed", { v: n(v) })}</p>
      <p className="font-display text-xl text-tech">{t("tasks.T03.time", { h: n(Math.round(h)), d: n(h / 24, { maximumFractionDigits: 1 }) })}</p>
      <p className="mt-2 text-sm">{t("tasks.T03.dist")}</p>
      <p className="text-sm text-muted-foreground">{t("tasks.T03.note")}</p>
      {result === null && <Button className="btn-game alt mt-3" onClick={() => setResult(Math.abs(v - target) <= T03_TOLERANCE)}>{t("tasks.check")}</Button>}
    </TaskShell>
  );
}
