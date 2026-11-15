import { Button } from "@/components/ui/button";
import type { ReactNode } from "react";
import { Sun, Wind, Thermometer, Zap, Atom, Globe2, Gauge } from "lucide-react";
import { useT } from "@/i18n";
import { DERIVED, SOLS } from "@/data/campaign";
import { useCampaign } from "@/game/campaign";
import { SYSTEMS } from "@/engine/demand";
import type { Provenance } from "@/engine/normalize";
import { InfoTip } from "./InfoTip";

function Card({ icon, title, children, prov, tone }: { icon: ReactNode; title: string; children: ReactNode; prov?: Provenance; tone: string }) {
  return (
    <div className="flex gap-3 rounded-2xl border-2 border-ink/10 bg-milk p-3">
      <span className={`grid h-12 w-12 shrink-0 place-items-center rounded-2xl ${tone}`}>{icon}</span>
      <div className="min-w-0 flex-1"><div className="text-sm font-extrabold text-muted-foreground">{title}</div><div className="font-display text-xl">{children}</div></div>
      {prov && <InfoTip prov={prov} />}
    </div>
  );
}

export function SolReport() {
  const { t, n } = useT();
  const solIdx = useCampaign((s) => s.solIdx);
  const phase = useCampaign((s) => s.phase);
  const close = useCampaign((s) => s.closeReport);
  const sol = SOLS[solIdx], d = DERIVED[solIdx]; if (!sol || !d) return null;
  const none = <span className="text-muted-foreground">{t("report.noReading")}</span>;
  const impact = sol.marsImpact.value;
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-ink/55 p-3" role="dialog" aria-modal="true" aria-label={t("report.title", { n: sol.sol })}>
      <div className="panel animate-pop max-h-[94dvh] w-full max-w-2xl overflow-y-auto p-5">
        <h2 className="text-3xl font-bold">{t("report.title", { n: sol.sol })}</h2>
        {sol.sample && <p className="my-2 rounded-xl bg-coral px-3 py-1 text-center font-extrabold text-milk">{t("report.sample")}</p>}
        <h3 className="mt-3 mb-2 inline-block rounded-full bg-turquoise px-3 py-1 text-sm font-extrabold">{t("report.realData")}</h3>
        <div className="grid gap-2 sm:grid-cols-2">
          <Card icon={<Sun className="h-7 w-7" />} tone="bg-sun" title={t("report.flare")} prov={sol.flare.prov}>
            {sol.flare.value ? t("report.flareClass", { c: sol.flare.value.classType }) : t("report.noFlare")}
          </Card>
          <Card icon={<Zap className="h-7 w-7" />} tone="bg-coral" title={t("report.cme")} prov={sol.cme.prov}>
            {sol.cme.value ? t("report.cmeSpeed", { v: n(sol.cme.value.speed) }) : t("report.noCme")}
          </Card>
          <Card icon={<Atom className="h-7 w-7" />} tone="bg-tech text-milk" title={t("report.sep")} prov={sol.sep.prov}>
            {sol.sep.value ? t("report.sepYes") : t("report.noSep")}
          </Card>
          <Card icon={<Globe2 className="h-7 w-7" />} tone="bg-bar-oxygen" title={t("report.marsImpact")} prov={sol.marsImpact.prov}>
            {!impact ? t("report.noSim") : impact.impact ? (impact.glancing ? t("report.impactGlancing") : t("report.impactYes")) : t("report.impactNo")}
          </Card>
          <Card icon={<Thermometer className="h-7 w-7" />} tone="bg-bar-oxygen" title={t("report.temp")} prov={sol.airTemp.prov}>
            {sol.airTemp.value ? t("report.tempValue", { v: n(sol.airTemp.value.av, { maximumFractionDigits: 1 }) }) : none}
          </Card>
          <Card icon={<Wind className="h-7 w-7" />} tone="bg-turquoise" title={t("report.wind")} prov={sol.windSpeed.prov}>
            {sol.windSpeed.value ? t("report.windValue", { v: n(sol.windSpeed.value.av, { maximumFractionDigits: 1 }) }) : none}
            {sol.windDir.value && <span className="ml-2 text-base">· {t(`compass.${sol.windDir.value.mostCommon}`)}</span>}
          </Card>
          <Card icon={<Gauge className="h-7 w-7" />} tone="bg-muted" title={t("report.pressure")} prov={sol.pressure.prov}>
            {sol.pressure.value ? t("report.pressureValue", { v: n(sol.pressure.value.av, { maximumFractionDigits: 0 }) }) : none}
          </Card>
        </div>
        <p className="mt-2 text-sm font-bold text-muted-foreground">{t("report.weatherWhere")}</p>
        {sol.cme.value && impact && !impact.impact && <p className="mt-2 font-bold">{t("report.sunActive")}</p>}

        <h3 className="mt-4 mb-2 inline-block rounded-full bg-sun px-3 py-1 text-sm font-extrabold">{t("report.gameRule")}</h3>
        <div className="rounded-2xl border-2 border-ink/10 bg-milk p-3">
          <div className="mb-1 font-extrabold">{t("report.energyNeeded")}</div>
          <div className="grid grid-cols-3 gap-2 text-center">
            {SYSTEMS.map((s) => (
              <div key={s} className="rounded-xl bg-muted p-2"><div className="text-xs font-extrabold">{t(`sys.${s}`)}</div><div className="font-display text-2xl">{t("report.kwh", { n: d.demand[s] })}</div></div>
            ))}
          </div>
          {d.thermalMissing && <p className="mt-2 text-sm font-bold text-rust">{t("report.fallback")}</p>}
        </div>
        <p className="mt-3 text-xs text-muted-foreground">{t("report.dateNote", { n: sol.sol, date: sol.earthDate })}</p>
        <div className="mt-4 flex justify-end">
          <Button className="btn-game text-xl" onClick={close}>{phase === "report" ? t("report.continue") : t("report.close")}</Button>
        </div>
      </div>
    </div>
  );
}
