import { Button } from "@/components/ui/button";
import { useState } from "react";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { thermalDemand } from "@/engine/demand";
import type { NormalizedSol } from "@/engine/normalize";
import { TaskShell, type TaskProps } from "./TaskShell";

export function T07({ sol, onDone }: TaskProps & { sol: NormalizedSol }) {
  const { t, n } = useT();
  const at = sol.airTemp.value!.av;
  const need = thermalDemand(at)!;
  const T = BALANCE.thermal;
  const [v, setV] = useState(0);
  const [result, setResult] = useState<null | boolean>(null);
  return (
    <TaskShell id="T07" onDone={onDone} result={result} sample={sol.sample}>
      <p className="sr-only">{t("tasks.T07.goal", { t: n(at, { maximumFractionDigits: 1 }), tin: T.internalC })}</p>
      <div className="mb-2 flex items-center justify-between font-display text-xl">
        <span className="rounded-2xl bg-bar-oxygen px-3 py-1">{n(at, { maximumFractionDigits: 1 })} °C</span>
        <span>→</span>
        <span className="rounded-2xl bg-coral px-3 py-1 text-milk">{T.internalC} °C</span>
      </div>
      <input type="range" min={0} max={50} step={5} value={v} disabled={result !== null} onChange={(e) => setV(Number(e.target.value))} className="h-12 w-full accent-[var(--coral)]" aria-label={t("tasks.T07.heater", { n: v })} />
      <p className="font-display text-2xl">{t("tasks.T07.heater", { n: v })}</p>
      <p className="mt-2 rounded-xl bg-sun/40 p-2 text-sm"><b>{t("tasks.badgeRule")}:</b> {t("tasks.T07.rule", { base: T.base, k: n(T.k), tin: T.internalC })}</p>
      {result === null && <Button className="btn-game alt mt-3" onClick={() => setResult(v === need)}>{t("tasks.check")}</Button>}
      {result !== null && <p className="mt-2 font-bold">= {need} kWh</p>}
    </TaskShell>
  );
}
