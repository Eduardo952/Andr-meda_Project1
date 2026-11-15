import { useState } from "react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { cmeTravelHours } from "@/engine/cme";
import { BALANCE } from "@/config/balance";
import { MiniFrame, NumberValue, type MiniProps, type PlayState } from "./MiniFrame";
import { taskData } from "./rules";
function Countdown({ state, speed }: { state: PlayState; speed: number }) {
  const { t } = useT(); const [value, setValue] = useState<number>(BALANCE.shield.cmeReferenceSpeed), [matched, setMatched] = useState(false);
  const hours = Math.round(cmeTravelHours(speed) ?? 0);
  return <><div className="mini-orbit" aria-hidden="true"><span>☀</span><div /><span>◉</span></div><p>{t("mini.CmeCountdown.reading")} <NumberValue value={speed} origin="speed" /></p>
    <input aria-label={t("mini.speed")} type="range" min={0} max={Math.max(3000, Math.ceil(speed / 100) * 100)} step={BALANCE.miniGames.speedStep} value={value} onChange={e => setValue(Number(e.target.value))} />
    <p>{t("mini.speed")} <NumberValue value={value} origin="input" /> · {t("mini.hours")} <NumberValue value={cmeTravelHours(value) ?? 0} origin="hours" /></p><p>{t("mini.constantSpeed")}</p>
    {!matched ? <Button onClick={() => Math.abs(value - speed) <= BALANCE.miniGames.speedTolerance ? setMatched(true) : state.mistake()}>{t("tasks.check")}</Button> : <><p>{t("mini.CmeCountdown.arrival")}</p><div className="mini-cards">{[hours + 12, hours, hours + 24].map(h => <Button key={h} onClick={() => h === hours ? state.win() : state.mistake()}><NumberValue value={h} origin={h === hours ? "hours" : "input"} /></Button>)}</div></>}
  </>;
}
export default function CmeCountdown(props: MiniProps) { const { t, n } = useT(); const data = taskData(props.sol); return <MiniFrame {...props} id="CmeCountdown" source="DONKI CMEAnalysis.speed" practice={data.cmePractice} learned={t(data.cmePractice ? "mini.practiceLearned" : "mini.CmeCountdown.learned", { value: n(data.speed.value) })}>{state => <Countdown state={state} speed={data.speed.value} />}</MiniFrame>; }