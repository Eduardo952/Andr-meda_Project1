import { Button } from "@/components/ui/button";
import { useEffect, useState, type ReactNode } from "react";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { TASK_BY_ID, type TaskId } from "./registry";
import { sfx } from "@/audio/sfx";

export interface TaskProps { onDone: (success: boolean) => void }

/** Shared frame: title, data badge, timer, and result screen. */
export function TaskShell({ id, onDone, children, result, sample }: {
  id: TaskId; onDone: (success: boolean) => void; children: ReactNode; result: null | boolean; sample?: boolean;
}) {
  const { t } = useT();
  const def = TASK_BY_ID[id];
  const [left, setLeft] = useState<number>(BALANCE.taskSeconds);
  useEffect(() => {
    if (result !== null) return;
    if (left <= 0) { onDone(false); return; }
    const i = setTimeout(() => setLeft((x) => x - 1), 1000);
    return () => clearTimeout(i);
  }, [left, result, onDone]);
  useEffect(() => { if (result !== null) (result ? sfx.use : sfx.click)(); }, [result]);
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-ink/55 p-3" role="dialog" aria-modal="true">
      <div className="panel animate-pop max-h-[94dvh] w-full max-w-lg overflow-y-auto p-5">
        <div className="mb-2 flex flex-wrap items-center gap-2">
          <span className={`rounded-full px-3 py-1 text-xs font-extrabold ${def.source ? "bg-turquoise" : "bg-sun"}`}>
            {def.source ? t("tasks.badgeReal", { src: def.source }) : t("tasks.badgeRule")}
          </span>
          {sample && <span className="rounded-full bg-coral px-3 py-1 text-xs font-extrabold text-milk">{t("report.sample")}</span>}
          <span className="ml-auto font-display text-lg tabular-nums">{t("tasks.time", { s: left })}</span>
        </div>
        <h2 className="text-2xl font-bold">{t(`tasks.${id}.name`)}</h2>
        <p className="mb-4 text-lg font-bold">{t(`tasks.${id}.goal`)}</p>
        {children}
        {result !== null && (
          <div className="mt-4 flex items-center justify-between gap-3 rounded-2xl bg-muted p-3">
            <span className="font-display text-2xl">{result ? t("tasks.success") : t("tasks.fail")}</span>
            <Button className="btn-game" onClick={() => onDone(result)}>{t("tasks.continue")}</Button>
          </div>
        )}
      </div>
    </div>
  );
}

export function Choice({ children, onClick, state }: { children: ReactNode; onClick: () => void; state?: "right" | "wrong" | "picked" | undefined }) {
  const cls = state === "right" ? "bg-bar-integrity" : state === "wrong" ? "bg-coral text-milk" : state === "picked" ? "bg-turquoise" : "bg-milk";
  return <Button className={`min-h-12 rounded-2xl border-2 border-ink/20 px-4 py-2 text-left text-base font-bold ${cls}`} onClick={onClick}>{children}</Button>;
}
