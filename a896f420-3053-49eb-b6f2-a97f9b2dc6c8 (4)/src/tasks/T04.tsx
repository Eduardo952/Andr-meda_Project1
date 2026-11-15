import { useState } from "react";
import { useT } from "@/i18n";
import { cmeTravelHours } from "@/engine/cme";
import type { NormalizedSol } from "@/engine/normalize";
import { TaskShell, Choice, type TaskProps } from "./TaskShell";

export function T04({ sol, onDone }: TaskProps & { sol: NormalizedSol }) {
  const { t, n } = useT();
  const h = cmeTravelHours(sol.cme.value!.speed)!;
  const impact = sol.marsImpact.value;
  const [pick, setPick] = useState<null | boolean>(null);
  // Without a forecast any honest answer is accepted: the lesson is that there is no forecast.
  const result = pick === null ? null : impact ? pick === impact.impact : true;
  return (
    <TaskShell id="T04" onDone={onDone} result={result} sample={sol.sample}>
      <p className="mb-3 font-bold">{t("tasks.T04.yours", { d: n(h / 24, { maximumFractionDigits: 1 }) })}</p>
      <div className="grid grid-cols-2 gap-2">
        {[true, false].map((v) => (
          <Choice key={String(v)} onClick={() => pick === null && setPick(v)} state={pick === v ? (result ? "right" : "wrong") : undefined}>{v ? t("tasks.T04.yes") : t("tasks.T04.no")}</Choice>
        ))}
      </div>
      {pick !== null && (
        <p className="mt-3 font-bold">
          {!impact ? t("tasks.T04.noSim") : impact.impact ? `${t("tasks.T04.enlilYes", { t: impact.arrival ?? "?" })} ${impact.glancing ? t("tasks.T04.enlilGlancing") : ""}` : t("tasks.T04.enlilNo")}
        </p>
      )}
    </TaskShell>
  );
}
