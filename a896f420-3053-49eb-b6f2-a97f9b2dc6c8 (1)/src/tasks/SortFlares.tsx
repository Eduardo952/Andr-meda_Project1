import { useState } from "react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { FLARE_LETTERS } from "@/engine/flare";
import { MiniFrame, NumberValue, type MiniProps, type PlayState } from "./MiniFrame";
import { taskData } from "./rules";

function SortBoard({ state, letter, classType }: { state: PlayState; letter: string; classType: string }) {
  const { t } = useT(); const [selected, setSelected] = useState<string | null>(null), [placed, setPlaced] = useState<string[]>([]);
  const [ordered, setOrdered] = useState(false);
  const place = (value: string | null) => {
    if (!value) return;
    if (value !== FLARE_LETTERS[placed.length]) { state.mistake(); return; }
    const next = [...placed, value]; setPlaced(next); setSelected(null); if (next.length === FLARE_LETTERS.length) setOrdered(true);
  };
  return <><div className="mini-flare-order">{FLARE_LETTERS.map((_, i) => <div key={i} className="mini-flare-slot"><NumberValue value={i + 1} origin="index" />{placed[i] ?? "·"}</div>)}</div>
    <div className="mini-cards">{["M", "A", "X", "B", "C"].map(value => <Button key={value} draggable disabled={!ordered && placed.includes(value)} aria-pressed={selected === value} onDragStart={e => { e.dataTransfer.setData("text/plain", value); setSelected(value); }} onClick={() => ordered ? value === letter ? state.win() : state.mistake() : setSelected(value)}>{value}</Button>)}</div>
    {ordered ? <p>{t("mini.SortFlares.reading", { value: classType })}</p> : <Button className="mini-dropzone" onDragOver={e => e.preventDefault()} onDrop={e => { e.preventDefault(); place(e.dataTransfer.getData("text/plain")); }} onClick={() => place(selected)}>{t("mini.place")}</Button>}
  </>;
}
export default function SortFlares(props: MiniProps) {
  const { t } = useT(); const data = taskData(props.sol);
  return <MiniFrame {...props} id="SortFlares" source="DONKI FLR.classType" practice={data.flarePractice} learned={t(data.flarePractice ? "mini.practiceLearned" : "mini.SortFlares.learned", { value: data.flare?.classType ?? "" })}>
    {state => <SortBoard state={state} letter={data.flare?.letter ?? "C"} classType={data.flare?.classType ?? ""} />}
  </MiniFrame>;
}