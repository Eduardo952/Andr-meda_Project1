import { useState } from "react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { MiniFrame, type MiniProps, type PlayState } from "./MiniFrame";
const SYMBOLS = ["◆", "●", "▲", "✚"];
function Wires({ state }: { state: PlayState }) {
  const { t } = useT(); const [picked, setPicked] = useState<number | null>(null), [done, setDone] = useState<number[]>([]);
  const connect = (index: number, from = picked) => { if (from === null) return; if (index !== from) { state.mistake(); return; } if (done.includes(index)) return; const next = [...done, index]; setDone(next); setPicked(null); if (next.length === SYMBOLS.length) state.win(); };
  return <div className="mini-wires">{[0, 1].map(side => <div key={side}>{(side ? [2, 0, 3, 1] : [0, 1, 2, 3]).map(i => <Button key={i} className={`wire-tone-${i} ${done.includes(i) ? "mini-connected" : ""}`} disabled={done.includes(i)} aria-label={t(side ? "mini.wireEnd" : "mini.wireStart", { symbol: SYMBOLS[i] ?? "" })} aria-pressed={side === 0 && picked === i} draggable={side === 0} onDragStart={e => { e.dataTransfer.setData("text/plain", String(i)); setPicked(i); }} onDragOver={e => e.preventDefault()} onDrop={e => { e.preventDefault(); if (side) connect(i, Number(e.dataTransfer.getData("text/plain"))); }} onPointerDown={() => { if (!side) setPicked(i); }} onPointerUp={() => { if (side) connect(i); }} onClick={() => side ? connect(i) : setPicked(i)}>{SYMBOLS[i]}{done.includes(i) && " ✓"}</Button>)}</div>)}<svg viewBox="0 0 100 160" aria-hidden="true">{done.map(i => <path key={i} d={`M0 ${20 + i * 40} C55 ${20 + i * 40} 45 ${20 + [2, 0, 3, 1].indexOf(i) * 40} 100 ${20 + [2, 0, 3, 1].indexOf(i) * 40}`} className={`mini-wire-line wire-tone-${i}`} />)}</svg></div>;
}
export default function WireShield(props: MiniProps) { const { t } = useT(); return <MiniFrame {...props} id="WireShield" learned={t("mini.WireShield.learned")}>{state => <Wires state={state} />}</MiniFrame>; }