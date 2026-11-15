import { useState } from "react";
import { ArrowRight, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { FLARE_LETTERS, type FlareLetter } from "@/engine/flare";
import { MiniFrame, NumberValue, type MiniProps, type PlayState } from "./MiniFrame";
import { placeFlareCard, taskData } from "./rules";

const SHUFFLED_CARDS: readonly FlareLetter[] = ["M", "A", "X", "B", "C"];

function SortBoard({ state, letter, classType }: { state: PlayState; letter: FlareLetter; classType: string }) {
  const { t } = useT();
  const [placed, setPlaced] = useState<readonly FlareLetter[]>([]);

  const choose = (value: FlareLetter) => {
    const next = placeFlareCard(placed, value);
    if (!next) {
      state.mistake();
      return;
    }
    setPlaced(next);
    if (next.length === FLARE_LETTERS.length) state.win();
  };

  return <>
    <div className="mini-flare-reading" data-origin="flare">
      <span>{t("mini.SortFlares.reading", { value: classType })}</span>
      <small>{t("mini.SortFlares.readingHint")}</small>
    </div>
    <p className="mini-task-instruction">{t("mini.SortFlares.instruction")}</p>
    <div className="mini-flare-scale" aria-hidden="true">
      <span>{t("mini.SortFlares.weakest")}</span><ArrowRight /><span>{t("mini.SortFlares.strongest")}</span>
    </div>
    <ol className="mini-flare-order" aria-label={t("mini.SortFlares.orderLabel")}>
      {FLARE_LETTERS.map((_, index) => {
        const value = placed[index];
        return <li key={index} className={`mini-flare-slot ${value ? "is-filled" : ""}`}>
          <NumberValue value={index + 1} origin="index" />
          <strong>{value ?? "·"}</strong>
          {value && <Check aria-hidden="true" />}
        </li>;
      })}
    </ol>
    <div className="mini-flare-cards">
      {SHUFFLED_CARDS.map(value => {
        const isPlaced = placed.includes(value);
        return <Button key={value} className="mini-flare-card" disabled={isPlaced} aria-label={t("mini.SortFlares.choose", { value })} onClick={() => choose(value)}>
          <strong>{value}</strong>
          {value === letter && <small>{classType}</small>}
        </Button>;
      })}
    </div>
    <p className="mini-next-card" aria-live="polite">
      {placed.length < FLARE_LETTERS.length
        ? t("mini.SortFlares.next", { value: FLARE_LETTERS[placed.length] ?? "" })
        : t("challenge.correct")}
    </p>
  </>;
}

export default function SortFlares(props: MiniProps) {
  const { t } = useT();
  const data = taskData(props.sol);
  return <MiniFrame {...props} id="SortFlares" compact source="DONKI FLR.classType" practice={data.flarePractice} learned={t(data.flarePractice ? "mini.practiceLearned" : "mini.SortFlares.learned", { value: data.flare?.classType ?? "" })}>
    {state => <SortBoard state={state} letter={data.flare?.letter ?? "C"} classType={data.flare?.classType ?? ""} />}
  </MiniFrame>;
}
