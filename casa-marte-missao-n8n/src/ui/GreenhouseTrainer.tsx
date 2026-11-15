import { useState } from "react";
import { Droplets, Sprout, Sun, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { ChallengePanel } from "@/tasks/ChallengePanel";
import { translate, formatNumber } from "@/i18n";
import { IRRIGATION_ML, CROPS, focusedCrop, adjustmentsFor, hasLowWater, useGreenhouseTrainer, type Adjustment, type Irrigation } from "@/game/greenhouseTrainer";

const t = (key: string, vars?: Record<string, string | number>) => translate("pt-BR", `trainer.${key}`, vars);
const liters = (ml: number) => formatNumber("pt-BR", ml / 1000, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const cropIcons = { fruit: "🍓", vegetable: "🥕", greens: "🥬" };

export default function GreenhouseTrainer() {
  const { state, result, decide, setOpen, reset, finish } = useGreenhouseTrainer();
  const [level, setLevel] = useState<Irrigation | null>(null), [adjustment, setAdjustment] = useState<Adjustment>("none"), [error, setError] = useState(false);
  const crop = focusedCrop(state.cycle), options = adjustmentsFor(state);
  const affordable = state.waterMl >= IRRIGATION_ML.low;
  const submit = () => { if (!level) return; if (!decide(level, adjustment)) { setError(true); return; } setLevel(null); setAdjustment("none"); setError(false); };
  return <ChallengePanel language="pt-BR" title={t("title")} sector="greenhouse" descriptionId="trainer-intro" onClose={() => setOpen(false)} details={<p>{t("source")}</p>}>
    <div className="trainer-body" lang="pt-BR">
      <p id="trainer-intro">{t("welcome")} {t("mission")}</p>
      <small className="trainer-source">{t("source")}</small>
      <div className="trainer-crops" aria-label={t("scoreboard")}>{CROPS.map(id => <div key={id} className="trainer-crop" data-health={state.crops[id]}>
        <span className="trainer-plant" aria-hidden="true">{cropIcons[id]}</span><strong>{t(`crops.${id}`)}</strong><span>{t(`health.${state.crops[id]}`)}</span>
      </div>)}</div>
      {result && <section className="trainer-result" role="status" aria-live="polite">
        <strong>{t("result", { crop: t(`crops.${result.crop}`), health: t(`health.${result.health}`) })}</strong>
        <p>{t(result.soil !== "medium" && result.light !== "medium" ? "explainBoth" : result.soil !== "medium" ? "explainSoil" : result.light !== "medium" ? "explainLight" : "explainHealthy")}</p>
        <p>{t("updatedWater", { water: liters(state.waterMl) })}</p>
      </section>}
      {state.ended ? <section className="trainer-ending"><h3>{t("finished")}</h3><p>{t(state.waterMl === 0 ? "empty" : "saved")}</p><p>{t("reflection")}</p><Button onClick={() => { reset(); setLevel(null); setAdjustment("none"); setError(false); }}><RotateCcw/>{t("restart")}</Button></section> : <>
        <section className="trainer-cycle" aria-label={t("cycle", { n: state.cycle })}>
          <h3>🌍 {t("cycle", { n: state.cycle })}</h3>
          <p>💧 {t("reservoir", { water: liters(state.waterMl) })}</p>
          <meter className="trainer-reservoir" min={0} max={11000} value={state.waterMl} aria-label={t("reservoir", { water: liters(state.waterMl) })}/>
          <p>🪴 {t("focus", { crop: t(`crops.${crop}`) })}</p>
          <p>🌱 {t("soil", { state: t(`soilBands.${state.soil}`) })} | ☀️ {t("light", { state: t(`lightBands.${state.light}`) })}</p>
          {state.cycle > 1 && <small>{t(state.light === "low" ? "dustEvent" : state.light === "high" ? "sunEvent" : "calmEvent")}</small>}
          <p>💬 {t("hint", { hint: t(state.soil === "low" ? "hintDry" : state.soil === "high" ? "hintWet" : state.light !== "medium" ? "hintLight" : "hintBalanced") })}</p>
          <strong>{t("question")}</strong>
        </section>
        {hasLowWater(state.waterMl) && <p className="trainer-warning" role="status">{t("lowWater")}</p>}
        {!affordable ? <><p role="alert">{t("noLevel")}</p><Button className="btn-game" onClick={finish}>{t("finish")}</Button></> : <>
          <div className="trainer-levels" role="group" aria-label={t("question")}>{(Object.keys(IRRIGATION_ML) as Irrigation[]).map(id => <Button key={id} variant="outline" aria-pressed={level === id} onClick={() => { setLevel(id); setError(false); }}><Droplets/><strong>{t(`levels.${id}`)}</strong><span>{liters(IRRIGATION_ML[id])} L</span></Button>)}</div>
          {options.length > 1 && <fieldset className="trainer-adjustments"><legend>{t("optional")}</legend><small>{t("adjustmentRule")}</small><div>{options.map(id => <Button key={id} variant="outline" aria-pressed={adjustment === id} onClick={() => setAdjustment(id)}>{id.includes("Light") ? <Sun/> : <Sprout/>}{t(`adjustments.${id}`)}</Button>)}</div></fieldset>}
          {error && <p role="alert">{t("insufficient")}</p>}
          <Button className="btn-game trainer-submit" disabled={!level} onClick={submit}>{t("confirm")}</Button>
        </>}
      </>}
    </div>
  </ChallengePanel>;
}