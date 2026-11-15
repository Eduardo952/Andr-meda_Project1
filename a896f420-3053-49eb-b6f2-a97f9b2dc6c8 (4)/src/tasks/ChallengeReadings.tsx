import { Compass, Droplets, Gauge, HeartPulse, Shield, Sprout, Sun, Thermometer, Zap } from "lucide-react";
import type { ReactNode } from "react";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { useCampaign } from "@/game/campaign";
import { deriveSol, type NormalizedSol } from "@/engine/normalize";
import { SYSTEMS } from "@/engine/demand";
import { ChallengeDataCard } from "./ChallengePanel";
import { IRRIGATION } from "./context";
import { taskData, numeric, type Origin } from "./rules";
import type { MiniTaskId } from "./assignments";

/** Read-only presentation of the same snapshot and training inputs used by each puzzle. */
export function ChallengeReadings({ id, sol }: { id: MiniTaskId; sol: NormalizedSol }) {
  const { t, n } = useT();
  const oxygen = useCampaign(state => state.oxygen);
  const data = taskData(sol), demand = deriveSol(sol).demand;
  const value = (amount: number, origin: Origin) => {
    const registered = numeric(amount, origin);
    return <span data-origin={registered.origin} data-value={registered.value}>{n(amount, { maximumFractionDigits: 1 })}</span>;
  };
  const cards: { label: string; icon: ReactNode; content: ReactNode; note: string }[] = [];
  const add = (label: string, icon: ReactNode, content: ReactNode, note = t("mini.rulesNote")) => cards.push({ label, icon, content, note });
  if (id === "CmeCountdown") add(t("mini.speed"), <Zap />, value(data.speed.value, data.speed.origin), t("mini.constantSpeed"));
  if (id === "AlignCoils") add(t("challenge.marks"), <Shield />, value(data.marks.value, data.marks.origin));
  if (id === "WireShield") add(t("challenge.connections"), <Zap />, "◆ ● ▲ ✚", t("challenge.briefings.WireShield.hint"));
  if (id === "ThermostatDial") {
    add(t("mini.outside"), <Thermometer />, data.temperature ? value(data.temperature.value, "temperature") : t("report.noReading"), t("challenge.dataNotes.temperature"));
    add(t("mini.heating"), <Zap />, value(data.heating.value, "heating"));
  }
  if (id === "ResourcePriority") {
    add(t("challenge.oxygen"), <HeartPulse />, value(oxygen, "status"));
    add(t("sys.life"), <Thermometer />, value(demand.life, "heating"));
    add(t("sys.greenhouse"), <Sprout />, value(demand.greenhouse, "energy"));
  }
  if (id === "IrrigationPlan") {
    add(t("challenge.waterBudget"), <Droplets />, value(IRRIGATION.water, "count"));
    add(t("challenge.moistureTarget"), <Sprout />, value(IRRIGATION.target, "count"));
  }
  if (id === "SmartFarmCare") add(t("mini.lightTarget"), <Sun />, value(data.light.value, "light"));
  if (id === "CleanPanels") add(t("challenge.dust"), <Sun />, value(data.dust.value, data.dust.origin), t(BALANCE.marsWeatherModifier ? "mini.weatherInspired" : "mini.rulesNote"));
  if (id === "TiltSun") add(t("mini.targetAngle"), <Sun />, value(data.tilt.value, "angle"));
  if (id === "PowerBalance") {
    add(t("challenge.available"), <Zap />, value(BALANCE.energyPool, "energy"));
    add(t("challenge.demand"), <Gauge />, value(SYSTEMS.reduce((total, key) => total + demand[key], 0), "energy"));
  }
  if (id === "WindReading") add(t("challenge.wind"), <Compass />, sol.windDir.value ? t(`compass.${sol.windDir.value.mostCommon}`) : t("report.noReading"), t("challenge.dataNotes.wind"));
  if (!cards.length) return null;
  return <div className="mini-data-grid">{cards.map(card => <ChallengeDataCard key={card.label} icon={card.icon} label={card.label} explanation={card.note}>{card.content}</ChallengeDataCard>)}</div>;
}