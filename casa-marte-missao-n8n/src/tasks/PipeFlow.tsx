import { useState } from "react";
import { Check, Droplets, HeartPulse, Play, RotateCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { MiniFrame, type MiniProps, type PlayState } from "./MiniFrame";
import { aligned, rotatePipe } from "./rules";

const TARGETS = [...BALANCE.miniGames.dialTargets, ...BALANCE.miniGames.dialTargets] as const;

function PipeBoard({ state }: { state: PlayState }) {
  const { t } = useT();
  const [values, setValues] = useState(() => TARGETS.map(() => 0));
  const connected = values.map((value, index) => value === TARGETS[index]);
  const allConnected = aligned(values, TARGETS);

  const rotate = (index: number) => {
    setValues(current => current.map((value, item) => item === index ? rotatePipe(value) : value));
  };

  return <>
    <p className="mini-task-instruction">{t("mini.PipeFlow.instruction")}</p>
    <div className="mini-circuits">
      {([0, 1] as const).map(row => {
        const Icon = row === 0 ? HeartPulse : Droplets;
        const tank = t(row === 0 ? "mini.oxygenTank" : "mini.waterTank");
        return <section className={`mini-circuit mini-circuit-${row === 0 ? "oxygen" : "water"}`} key={tank}>
          <header className="mini-circuit-heading"><Icon aria-hidden="true" /><strong>{tank}</strong></header>
          <div className="mini-pipe-row">
            {TARGETS.slice(row * 3, row * 3 + 3).map((target, column) => {
              const index = row * 3 + column;
              const isConnected = connected[index] ?? false;
              return <Button key={index} className={`mini-pipe-piece ${isConnected ? "is-connected" : ""}`} aria-label={t(isConnected ? "mini.PipeFlow.connectedPipe" : "mini.PipeFlow.rotate", { n: index + 1 })} onClick={() => rotate(index)}>
                <svg className="mini-pipe-art" viewBox="0 0 100 100" aria-hidden="true">
                  <path d="M50 8 V50 H92" className="mini-pipe-target" transform={`rotate(${target * 30} 50 50)`} />
                  <path d="M50 8 V50 H92" className="mini-pipe-current" transform={`rotate(${(values[index] ?? 0) * 30} 50 50)`} />
                  <circle cx="50" cy="50" r="7" className="mini-pipe-joint" />
                </svg>
                <span>{isConnected ? <Check aria-hidden="true" /> : <RotateCw aria-hidden="true" />}{t(isConnected ? "mini.PipeFlow.connected" : "mini.PipeFlow.turn")}</span>
              </Button>;
            })}
          </div>
        </section>;
      })}
    </div>
    <p className={`mini-flow-status ${allConnected ? "is-ready" : ""}`} aria-live="polite">
      {allConnected ? t("mini.PipeFlow.ready") : t("mini.PipeFlow.keepGoing")}
    </p>
    <Button className="mini-confirm" onClick={() => allConnected ? state.win() : state.mistake()}><Play aria-hidden="true" />{t("mini.flow")}</Button>
  </>;
}

export default function PipeFlow(props: MiniProps) {
  const { t } = useT();
  return <MiniFrame {...props} id="PipeFlow" compact learned={t("mini.PipeFlow.learned")}>
    {state => <PipeBoard state={state} />}
  </MiniFrame>;
}
