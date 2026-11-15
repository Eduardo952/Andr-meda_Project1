import { useState } from "react";
import { RotateCcw, RotateCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { NumberValue, type PlayState } from "./MiniFrame";
import { aligned } from "./rules";
import { BALANCE } from "@/config/balance";

export function DialPuzzle({ targets, state, pipes = false, marks = BALANCE.miniGames.dialSteps, motion = 0 }: { targets: readonly number[]; state: PlayState; pipes?: boolean; marks?: number; motion?: number }) {
  const { t } = useT(); const steps = BALANCE.miniGames.dialSteps;
  const [values, setValues] = useState(() => targets.map(() => 0));
  const rotate = (index: number, direction: number) => setValues(old => old.map((v, i) => i === index ? (v + direction + steps) % steps : v));
  return <><div className={`mini-dials ${pipes ? "mini-pipes" : ""}`}>{targets.map((target, index) => <section key={index}>
    <h3>{t(pipes ? "mini.pipe" : "mini.dial")} <NumberValue value={index + 1} origin="index" /></h3>
    <svg viewBox="0 0 120 120" className="mini-dial-art" aria-hidden="true"><circle cx="60" cy="60" r="48" className="mini-dial-face" />
      {Array.from({ length: marks }, (_, i) => <line key={i} x1="60" y1="13" x2="60" y2="21" transform={`rotate(${i * 360 / marks} 60 60)`} className="mini-tick" />)}
      <g transform={`rotate(${target * 360 / steps} 60 60)`}><path d="M54 10 L66 10 L60 25 Z" className="mini-target" /></g>
      <g transform={`rotate(${(values[index] ?? 0) * 360 / steps} 60 60)`}>{pipes ? <path d="M60 16 V60 H104" className="mini-pipe-path" /> : <path d="M60 65 V27 M50 65 H70" className="mini-needle" />}</g>
      <circle cx="60" cy="60" r="8" className="mini-target" />
      {motion > 0 && <circle r="3" className="mini-target"><animateMotion dur={`${motion}s`} repeatCount="indefinite" path="M60 12 A48 48 0 1 1 59.9 12" /></circle>}
    </svg>
    <div className="mini-dial-buttons"><Button aria-label={t("mini.rotateLeft", { n: index + 1 })} onClick={() => rotate(index, -1)}><RotateCcw /></Button><Button aria-label={t("mini.rotateRight", { n: index + 1 })} onClick={() => rotate(index, 1)}><RotateCw /></Button></div>
    <label>{t("mini.position")} <NumberValue value={values[index] ?? 0} origin="angle" /><input aria-label={t("mini.dialValue", { n: index + 1 })} type="range" min={0} max={steps - 1} step={1} value={values[index] ?? 0} onChange={e => setValues(old => old.map((v, i) => i === index ? Number(e.target.value) : v))} /></label>
  </section>)}</div><Button className="mini-confirm" onClick={() => aligned(values, targets) ? state.win() : state.mistake()}>{t(pipes ? "mini.flow" : "tasks.check")}</Button></>;
}