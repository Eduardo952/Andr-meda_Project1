import { useState } from "react";
import { Droplets, Sprout, Apple, Sun } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { MiniFrame, NumberValue, type MiniProps, type PlayState } from "./MiniFrame";
function Farm({ state }: { state: PlayState }) {
  const { t } = useT(); const b = BALANCE.miniGames;
  const [water, setWater] = useState<number[]>([]), [apples, setApples] = useState<number[]>([]), [light, setLight] = useState(0), [drop, setDrop] = useState(false);
  const watered = water.length === b.beds;
  const pour = (index: number) => { if (!drop || water.includes(index)) return; setWater(old => [...old, index]); setDrop(false); };
  const harvest = (index: number) => { if (apples.includes(index)) return; const next = [...apples, index]; setApples(next); if (next.length === b.apples) state.win(); };
  return <>{!watered ? <><Button draggable aria-pressed={drop} aria-label={t("mini.drop")} onPointerDown={() => setDrop(true)} onDragStart={e => { e.dataTransfer.setData("text/plain", "water"); setDrop(true); }} onClick={() => setDrop(true)}><Droplets />{t("mini.drop")}</Button><div className="mini-beds">{Array.from({ length: b.beds }, (_, i) => <Button key={i} className={water.includes(i) ? "mini-watered" : ""} disabled={water.includes(i)} aria-label={t("mini.bed", { n: i + 1 })} onDragOver={e => e.preventDefault()} onDrop={e => { e.preventDefault(); pour(i); }} onPointerUp={() => pour(i)} onClick={() => pour(i)}><Sprout /><NumberValue value={i + 1} origin="index" />{water.includes(i) ? "✓" : <Droplets />}</Button>)}</div></>
    : light !== b.lightTarget ? <><Sun className="mini-thermometer" /><p>{t("mini.lightTarget")} <NumberValue value={b.lightTarget} origin="light" /></p><input type="range" min={0} max={5} step={1} value={light} aria-label={t("mini.light")} onChange={e => setLight(Number(e.target.value))} /><NumberValue value={light} origin="input" /></>
    : <><p>{t("mini.harvest")}</p><div className="mini-apples">{Array.from({ length: b.apples }, (_, i) => <Button key={i} disabled={apples.includes(i)} aria-label={t("mini.apple", { n: i + 1 })} onClick={() => harvest(i)}><Apple /><NumberValue value={i + 1} origin="index" />{apples.includes(i) && "✓"}</Button>)}</div></>}
  </>;
}
export default function SmartFarmCare(props: MiniProps) { const { t } = useT(); return <MiniFrame {...props} id="SmartFarmCare" learned={t("mini.SmartFarmCare.learned")}>{state => <Farm state={state} />}</MiniFrame>; }