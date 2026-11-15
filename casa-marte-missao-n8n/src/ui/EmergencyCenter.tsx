import { useEffect, useMemo } from "react";
import { AlertTriangle, CheckCircle2, ShieldAlert, Wind } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useCampaign } from "@/game/campaign";
import { alertStage, useEmergencies, type AlertItem, type AlertKind } from "@/game/presentation";
import { stormForSol } from "@/engine/consequences";
import { SOLS } from "@/data/campaign";
import { BALANCE } from "@/config/balance";
import { sfx } from "@/audio/sfx";
import { useT } from "@/i18n";
import { ConsequenceList, consequenceKind } from "./ConsequenceList";

const IDS: Record<AlertKind, string> = { solar: "solar-alarm", lifeSupport: "life-support", dust: "dust-storm", greenhouse: "greenhouse", info: "information", perfect: "perfect" };

export function EmergencyCenter() {
  const { t, n } = useT();
  const alarm = useCampaign(s => s.alarm), phase = useCampaign(s => s.phase), solIdx = useCampaign(s => s.solIdx);
  const consequences = useCampaign(s => s.consequences), cleaned = useCampaign(s => s.cleanedThisSol);
  const active = useEmergencies(s => s.active), stage = active ? alertStage(active) : null;
  const storm = useMemo(() => stormForSol(solIdx, SOLS[solIdx]?.windSpeed.value?.av ?? null), [solIdx]);

  useEffect(() => {
    const emergencies = useEmergencies.getState();
    if (alarm) emergencies.offer({ id: IDS.solar, kind: "solar", hold: true });
    else emergencies.release(IDS.solar);
  }, [alarm]);
  useEffect(() => {
    if (phase === "report" || phase === "gameover" || phase === "victory") { useEmergencies.getState().reset(); sfx.stopDanger(); return; }
    if (phase === "tasks" && storm.active && !cleaned) useEmergencies.getState().offer({ id: `${IDS.dust}-${solIdx}`, kind: "dust" });
  }, [phase, solIdx, storm.active, cleaned]);
  useEffect(() => {
    for (const event of consequences) {
      if (event.id === "perfect-sol" || event.id === "energy-surplus") continue;
      const item: AlertItem = { id: `sol-${solIdx}-${event.id}`, kind: consequenceKind(event) };
      useEmergencies.getState().offer(item);
    }
  }, [consequences, solIdx]);
  useEffect(() => {
    if (stage === "emergency" && active?.kind !== "perfect" && active?.kind !== "info") sfx.danger(active?.kind === "dust" ? "storm" : "solar");
    if (stage === "recovery") sfx.allClear();
    return () => { if (stage === "emergency") sfx.stopDanger(); };
  }, [stage, active?.id, active?.kind]);
  useEffect(() => {
    const key = (event: KeyboardEvent) => { if (event.key === "Escape" && useEmergencies.getState().active) useEmergencies.getState().dismiss(); };
    window.addEventListener("keydown", key); return () => window.removeEventListener("keydown", key);
  }, []);

  if (!active || !stage) return null;
  const event = consequences.find(item => active.id.endsWith(item.id));
  const dustAlert = active.kind === "dust" && !event;
  const solarAlarm = active.id === IDS.solar && !!alarm;
  const title = solarAlarm ? t("alarm.title") : dustAlert ? t("consequences.storm.title") : event ? t(`consequences.events.${event.id}.title`) : t(`consequences.kinds.${active.kind}`);
  const Icon = stage === "recovery" ? CheckCircle2 : active.kind === "dust" ? Wind : active.kind === "solar" ? ShieldAlert : AlertTriangle;
  return <div data-alert-id={active.id} className={`emergency-backdrop stage-${stage} kind-${active.kind}`}>
    <section className="emergency-card" role="alertdialog" aria-modal="true" aria-labelledby="emergency-title" aria-describedby="emergency-body">
      <div className="emergency-stage"><Icon aria-hidden="true" /><span>{t(`consequences.stages.${stage}`)}</span>{(dustAlert || event?.simulated) && <strong>{t("consequences.simulated")}</strong>}</div>
      <h2 id="emergency-title">{title}</h2>
      <div id="emergency-body">
        {solarAlarm && <><p>{t("alarm.goShelter")}</p><p>{t("consequences.shelterTimer", { seconds: n(Math.max(0, BALANCE.shelterSeconds - useCampaign.getState().alarmElapsed), { maximumFractionDigits: 0 }) })}</p></>}
        {dustAlert && <><p>{t("consequences.storm.cause", { wind: active.id === "forced-dust" || storm.windSpeed === null ? t("consequences.trainingWind") : t("report.windValue", { v: n(storm.windSpeed) }) })}</p><p>{t("consequences.storm.effect")}</p></>}
        {event && <ConsequenceList events={[event]} />}
        {!solarAlarm && !dustAlert && !event && <p>{t(`consequences.kinds.${active.kind}Body`)}</p>}
        {stage === "recovery" && <p className="emergency-all-clear">{t("consequences.allClear")}</p>}
      </div>
      {stage !== "recovery" && <Button className="btn-game" autoFocus onClick={() => useEmergencies.getState().dismiss()}>{t("consequences.acknowledge")}</Button>}
    </section>
  </div>;
}
