import { useState } from "react";
import { BrushCleaning, Check, Gauge, Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { useCampaign } from "@/game/campaign";
import { SOLS } from "@/data/campaign";
import { cleaningPasses, panelDustAfterStorm, stormForSol } from "@/engine/consequences";
import { MiniFrame, NumberValue, type MiniProps, type PlayState } from "./MiniFrame";

function ArrayCleaning({ state }: { state: PlayState }) {
  const { t, n } = useT();
  const solIdx = useCampaign(s => s.solIdx), current = useCampaign(s => s.panelDust);
  const storm = stormForSol(solIdx, SOLS[solIdx]?.windSpeed.value?.av ?? null);
  const dusty = panelDustAfterStorm(current, storm);
  const required = dusty.map(cleaningPasses);
  const [passes, setPasses] = useState([0, 0, 0, 0]);
  const order = [0, 1, 2, 3].sort((a, b) => (dusty[b] ?? 0) - (dusty[a] ?? 0));
  const active = order.find(i => passes[i]! < required[i]!);
  const clean = (index: number) => {
    if (index !== active || state.disabled) return;
    const next = passes.map((value, i) => i === index ? value + 1 : value);
    setPasses(next);
    if (next.every((value, i) => value >= (required[i] ?? 0))) state.win();
  };
  if (active === undefined) return <div className="solar-verify"><Sparkles aria-hidden="true" /><p>{t("mini.CleanPanels.alreadyClean")}</p><Button className="mini-confirm" onClick={() => state.win()}><Check />{t("mini.CleanPanels.verify")}</Button></div>;
  return <>
    <p className="mini-task-instruction">{t("mini.CleanPanels.instruction")}</p>
    <div className="solar-array-list">
      {order.map(index => {
        const dust = dusty[index] ?? 0, need = required[index] ?? 0, done = passes[index] ?? 0;
        const restored = Math.round(100 - dust * 0.2 * (1 - done / Math.max(1, need)));
        return <section key={index} className={`solar-array-card ${index === active ? "active" : ""}`} data-array={index}>
          <header><div><span>{t("mini.CleanPanels.array", { n: index + 1 })}</span><strong>{t(`mini.CleanPanels.level.${dust <= 10 ? "clear" : dust <= 35 ? "light" : dust <= 70 ? "heavy" : "buried"}`)}</strong></div><Gauge aria-hidden="true" /></header>
          <div className="solar-array-metrics"><span>{t("mini.CleanPanels.dust", { n: n(dust) })}</span><span>{t("mini.CleanPanels.output", { n: restored })}</span></div>
          <progress aria-label={t("mini.CleanPanels.efficiency", { n: index + 1 })} value={restored} max={100} />
          <Button data-clean-array={index} disabled={index !== active} onClick={() => clean(index)} aria-label={t("mini.CleanPanels.wipe", { n: index + 1, pass: done + 1, total: need })}>
            <BrushCleaning aria-hidden="true" /><span>{t("mini.CleanPanels.pass", { pass: Math.min(done + 1, need), total: need })}</span>
          </Button>
        </section>;
      })}
    </div>
    <p className="solar-restore-note"><NumberValue value={100} origin="progress" suffix="%" /> · {t("mini.CleanPanels.restore")}</p>
  </>;
}

export default function CleanPanels(props: MiniProps) {
  const { t } = useT();
  return <MiniFrame {...props} id="CleanPanels" compact source={t("consequences.simulated")} learned={t("mini.CleanPanels.learned")}>{state => <ArrayCleaning state={state} />}</MiniFrame>;
}
