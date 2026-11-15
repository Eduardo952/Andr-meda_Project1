import { useEffect, useRef, useState, type ReactNode } from "react";
import * as Dialog from "@radix-ui/react-dialog";
import { X, Info, Star, RotateCcw, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { useCampaign } from "@/game/campaign";
import type { NormalizedSol } from "@/engine/normalize";
import { numeric, scoreTask, type AttemptResult, type Origin } from "./rules";
import { sectorForTask, type MiniTaskId } from "./assignments";

export interface MiniProps { sol: NormalizedSol; onDone: (result: AttemptResult) => void }
export interface PlayState { mistake: () => void; win: () => void; disabled: boolean }
export function NumberValue({ value, origin, suffix = "" }: { value: number; origin: Origin; suffix?: string }) {
  const { n } = useT(); const registered = numeric(value, origin);
  return <span data-origin={registered.origin} data-value={registered.value}>{n(value, { maximumFractionDigits: 1 })}{suffix}</span>;
}
export function MiniFrame({ id, sol, onDone, source, practice, learned, children }: MiniProps & {
  id: MiniTaskId; source?: string | undefined; practice?: boolean; learned: string; children: (state: PlayState, attempt: number) => ReactNode;
}) {
  const { t } = useT();
  const [attempt, setAttempt] = useState(0), [seconds, setSeconds] = useState(0), [mistakes, setMistakes] = useState(0);
  const [result, setResult] = useState<AttemptResult | null>(null), [expired, setExpired] = useState(false);
  const started = useRef(performance.now()); const finished = useRef(false); const mistakesRef = useRef(0);
  useEffect(() => {
    started.current = performance.now(); finished.current = false; mistakesRef.current = 0;
    setSeconds(0); setMistakes(0); setResult(null); setExpired(false);
    const timer = setInterval(() => {
      if (finished.current) return;
      const elapsed = (performance.now() - started.current) / 1000;
      setSeconds(Math.min(BALANCE.taskSeconds, elapsed));
      if (elapsed >= BALANCE.taskSeconds) { finished.current = true; setExpired(true); }
    }, 100);
    return () => clearInterval(timer);
  }, [attempt]);
  const close = () => useCampaign.getState().setOpenTask(null);
  const state: PlayState = {
    disabled: !!result || expired,
    mistake: () => { if (!finished.current) { mistakesRef.current += 1; setMistakes(mistakesRef.current); } },
    win: () => {
      if (finished.current) return;
      const elapsed = (performance.now() - started.current) / 1000;
      if (elapsed > BALANCE.taskSeconds) { finished.current = true; setExpired(true); return; }
      finished.current = true;
      setResult({ seconds: Math.round(elapsed * 10) / 10, mistakes: mistakesRef.current, stars: scoreTask(elapsed, mistakesRef.current) });
    },
  };
  return <Dialog.Root open onOpenChange={open => { if (!open) close(); }}><Dialog.Portal>
    <Dialog.Overlay className="mini-backdrop" />
    <Dialog.Content className={`mini-overlay region-${sectorForTask(id)}`} aria-describedby="mini-goal" onCloseAutoFocus={e => { e.preventDefault(); document.querySelector<HTMLElement>("[data-station-use]")?.focus(); }}>
      <header className="mini-header"><div><span className="mini-sector">{t(`rooms.${sectorForTask(id)}`)}</span><Dialog.Title>{t(`mini.${id}.name`)}</Dialog.Title></div>
        <div className="mini-header-actions"><details className="mini-details"><summary aria-label={t("info.show")}><Info /></summary><div>
          <strong>{source ? t("tasks.badgeReal", { src: source }) : t("tasks.badgeRule")}</strong>
          {sol.sample && <p>{t("tasks.sampleNote")}</p>}{practice && <p>{t("mini.practice")}</p>}
          <p>{t("mini.rulesNote")}</p><p>{t("info.snapshot")}</p>
          <small>{source ?? t("tasks.badgeRule")}</small>
          <ul>{["timer", "mistakes", "stars"].map(key => <li key={key}><span data-origin={key}>{t(`mini.${key}`)}</span>: {t("tasks.badgeRule")}</li>)}</ul>
        </div></details><Dialog.Close asChild><Button variant="ghost" size="icon" title={t("settings.close")} aria-label={t("settings.close")}><X /></Button></Dialog.Close></div>
      </header>
      <div className="mini-status"><span>{t("mini.timer")} <NumberValue value={Math.max(0, Math.ceil(BALANCE.taskSeconds - seconds))} origin="timer" /></span><span>{t("mini.mistakes")} <NumberValue value={mistakes} origin="mistakes" /></span></div>
      <Dialog.Description id="mini-goal" className="mini-goal">{t(`mini.${id}.goal`)}</Dialog.Description>
      {practice && <p className="mini-practice">{t("report.noReading")} · {t("mini.practice")}</p>}
      <main className="mini-play" key={attempt} inert={state.disabled ? true : undefined}>{children(state, attempt)}</main>
      {(result || expired) && <section className="mini-result" role="status"><div className="mini-result-icon">{result ? <Check /> : <RotateCcw />}</div><h2>{t(result ? "tasks.success" : "mini.retryTitle")}</h2>
        {result ? <><div className="mini-stars">{Array.from({ length: result.stars }, (_, i) => <Star key={i} />)}<NumberValue value={result.stars} origin="stars" /></div><p><strong>{t("mini.learned")}</strong> {learned}</p><p>{t("mini.timer")} <NumberValue value={result.seconds} origin="timer" /> · {t("mini.mistakes")} <NumberValue value={result.mistakes} origin="mistakes" /></p><Button onClick={() => onDone(result)}>{t("tasks.continue")}</Button></>
          : <><p>{t("mini.retryBody")}</p><Button onClick={() => setAttempt(v => v + 1)}>{t("mini.retry")}</Button></>}
      </section>}
    </Dialog.Content>
  </Dialog.Portal></Dialog.Root>;
}