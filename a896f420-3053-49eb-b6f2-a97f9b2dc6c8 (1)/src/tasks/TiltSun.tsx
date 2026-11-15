import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Sun } from "lucide-react";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { MiniFrame, NumberValue, type MiniProps, type PlayState } from "./MiniFrame";
import { taskData } from "./rules";
function Tilt({ state, target }: { state: PlayState; target: number }) {
  const { t } = useT(); const [angle, setAngle] = useState(0);
  return <><Sun className="mini-sun" /><svg viewBox="0 0 240 180" className="mini-tilt-art" aria-hidden="true"><path d="M25 150 H215 M120 150 V75" className="mini-needle" /><g transform={`rotate(${-target} 120 90)`}><path d="M55 90 H185" className="mini-tilt-target" /></g><g transform={`rotate(${-angle} 120 90)`}><rect x="55" y="75" width="130" height="30" rx="4" className="mini-dial-face" /><path d="M75 75 V105 M100 75 V105 M125 75 V105 M150 75 V105 M175 75 V105" className="mini-tick" /></g></svg><p>{t("mini.targetAngle")} <NumberValue value={target} origin="angle" /></p><input type="range" min={0} max={90} step={BALANCE.miniGames.tiltStepDegrees} value={angle} aria-label={t("mini.angle")} onChange={e => setAngle(Number(e.target.value))} /><p><NumberValue value={angle} origin="input" /></p><Button onClick={() => angle === target ? state.win() : state.mistake()}>{t("tasks.check")}</Button></>;
}
export default function TiltSun(props: MiniProps) { const { t } = useT(); const data = taskData(props.sol); return <MiniFrame {...props} id="TiltSun" learned={t("mini.TiltSun.learned")}>{state => <Tilt state={state} target={data.tilt.value} />}</MiniFrame>; }