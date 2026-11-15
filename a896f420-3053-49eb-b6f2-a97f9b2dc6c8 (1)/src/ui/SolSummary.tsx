import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { useCampaign } from "@/game/campaign";
import { SYSTEMS } from "@/engine/demand";

export function SolSummary() {
  const { t } = useT();
  const s = useCampaign();
  const o = s.outcome;
  if (!o) return null;
  const last = s.solIdx + 1 >= BALANCE.campaignSols;
  const failed = s.integrity <= 0 || s.oxygen <= 0;
  const tone = { ideal: "bg-bar-integrity", deficit: "bg-coral text-milk", surplus: "bg-sun" } as const;
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-ink/40 p-3" role="dialog" aria-modal="true">
      <div className="panel animate-pop w-full max-w-lg p-5">
        <h2 className="text-3xl font-bold">{t("summary.title", { n: s.solIdx + 1 })}</h2>
        <table className="my-3 w-full text-left">
          <thead><tr className="text-sm"><th /><th>{t("summary.given")}</th><th>{t("summary.needed")}</th><th /></tr></thead>
          <tbody>
            {SYSTEMS.map((k) => {
              const r = o.systems[k];
              return (
                <tr key={k} className="font-bold">
                  <td className="py-1">{t(`sys.${k}`)}</td><td className="font-display text-xl">{r.allocated}</td><td className="font-display text-xl">{r.demand}</td>
                  <td><span className={`rounded-full px-2 py-1 text-xs font-extrabold ${tone[r.status]}`}>{t(`summary.${r.status}`)}</span></td>
                </tr>
              );
            })}
          </tbody>
        </table>
        {o.systems.shield.status === "deficit" && <p className="mb-2 rounded-xl bg-coral/30 p-2 font-bold">{t("summary.sparks")}</p>}
        <div className="grid grid-cols-2 gap-2 text-center">
          <div className="rounded-xl bg-muted p-2"><div className="text-sm font-extrabold">{t("summary.integrity")}</div><div className="font-display text-2xl">{s.integrity}% <span className="text-base">({o.integrityDelta})</span></div></div>
          <div className="rounded-xl bg-muted p-2"><div className="text-sm font-extrabold">{t("summary.oxygen")}</div><div className="font-display text-2xl">{s.oxygen}% <span className="text-base">({Math.round(o.oxygenDelta)})</span></div></div>
        </div>
        <p className="mt-2 text-sm font-bold">{t("summary.tasks", { n: (s.results[s.solIdx] ?? []).filter((r) => r.success).length })}</p>
        <div className="mt-4 flex justify-end"><Button className="btn-game text-xl" onClick={s.nextSol}>{failed ? t("tasks.continue") : last ? t("summary.finish") : t("summary.next")}</Button></div>
      </div>
    </div>
  );
}
