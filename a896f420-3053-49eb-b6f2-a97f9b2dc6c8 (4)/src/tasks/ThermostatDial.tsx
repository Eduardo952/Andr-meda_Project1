import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Thermometer } from "lucide-react";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { MiniFrame, NumberValue, type MiniProps, type PlayState } from "./MiniFrame";
import { taskData } from "./rules";
function Thermostat({ state, target, temperature }: { state: PlayState; target: number; temperature: number | null }) {
  const { t } = useT(); const [value, setValue] = useState(0);
  return <><Thermometer className="mini-thermometer" /><p>{t("mini.outside")} {temperature !== null && <NumberValue value={temperature} origin="temperature" />}</p><p>{t("mini.heating")} <NumberValue value={target} origin="heating" /></p><input aria-label={t("mini.heating")} type="range" min={0} max={BALANCE.energyPool} step={BALANCE.sliderStep} value={value} onChange={e => setValue(Number(e.target.value))} /><p><NumberValue value={value} origin="input" /></p><Button onClick={() => value === target ? state.win() : state.mistake()}>{t("tasks.check")}</Button></>;
}
export default function ThermostatDial(props: MiniProps) { const { t, n } = useT(); const data = taskData(props.sol); return <MiniFrame {...props} id="ThermostatDial" source="InSight AT.av" practice={data.tempPractice} learned={t(data.tempPractice ? "mini.practiceLearned" : "mini.ThermostatDial.learned", { value: n(data.temperature?.value ?? 0) })}>{state => <Thermostat state={state} target={data.heating.value} temperature={data.temperature?.value ?? null} />}</MiniFrame>; }