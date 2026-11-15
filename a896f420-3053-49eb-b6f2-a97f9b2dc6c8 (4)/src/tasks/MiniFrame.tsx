import { useEffect, useRef, useState, type ReactNode } from "react";
import * as Dialog from "@radix-ui/react-dialog";
import { Star, RotateCcw, Check, Lightbulb, AlertCircle, Target, Activity, Timer, Fish } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { useCampaign } from "@/game/campaign";
import type { NormalizedSol } from "@/engine/normalize";
import { numeric, scoreTask, type AttemptResult, type Origin } from "./rules";
import { sfx } from "@/audio/sfx";
import { rewardPreview } from "./context";
import { sectorForTask, type MiniTaskId } from "./assignments";
import { ChallengePanel } from "./ChallengePanel";
import { ChallengeReadings } from "./ChallengeReadings";
import { useGame } from "@/game/store";

export interface MiniProps { sol: NormalizedSol; onDone: (result: AttemptResult) => void }
export interface PlayState { mistake: () => void; win: () => void; disabled: boolean }
export function NumberValue({ value, origin, suffix = "" }: { value: number; origin: Origin; suffix?: string }) {
  const { n } = useT(); const registered = numeric(value, origin);
  return <span data-origin={registered.origin} data-value={registered.value}>{n(value, { maximumFractionDigits: 1 })}{suffix}</span>;
}
export function MiniFrame({ id, sol, onDone, source, practice, ready = true, compact = false, learned, children }: MiniProps & {
  id: MiniTaskId; source?: string | undefined; practice?: boolean; ready?: boolean; compact?: boolean; learned: string; children: (state: PlayState, attempt: number) => ReactNode;
}) {
  const { t } = useT();
  const [begun, setBegun] = useState(false), [feedback, setFeedback] = useState(false), [hint, setHint] = useState(false);
  const sector = sectorForTask(id);
  const integrity = useCampaign(s=>s.integrity), oxygen = useCampaign(s=>s.oxygen);
  const preview = rewardPreview(integrity, oxygen);
  const [attempt, setAttempt] = useState(0), [seconds, setSeconds] = useState(0), [mistakes, setMistakes] = useState(0);
  const [result, setResult] = useState<AttemptResult | null>(null), [expired, setExpired] = useState(false);
  const started = useRef(performance.now()); const finished = useRef(false); const mistakesRef = useRef(0);
  useEffect(() => {
    if (!begun || !ready) return;
    started.current = performance.now(); finished.current = false; mistakesRef.current = 0;
    setSeconds(0); setMistakes(0); setResult(null); setExpired(false); setFeedback(false);
    const timer = setInterval(() => {
      if (finished.current) return;
      const elapsed = (performance.now() - started.current) / 1000;
      setSeconds(Math.min(BALANCE.taskSeconds, elapsed));
      if (elapsed >= BALANCE.taskSeconds) { finished.current = true; setExpired(true); }
    }, 100);
    return () => clearInterval(timer);
  }, [attempt, begun, ready]);
  const close = () => useCampaign.getState().setOpenTask(null);
  const state: PlayState = {
    disabled: !begun || !ready || !!result || expired,
    mistake: () => { if (!finished.current) { mistakesRef.current += 1; setMistakes(mistakesRef.current); setFeedback(true); sfx.blub(); } },
    win: () => {
      if (finished.current) return;
      const elapsed = (performance.now() - started.current) / 1000;
      if (elapsed > BALANCE.taskSeconds) { finished.current = true; setExpired(true); return; }
      finished.current = true;
      setFeedback(false); sfx.use();
      setResult({ seconds: Math.round(elapsed * 10) / 10, mistakes: mistakesRef.current, stars: scoreTask(elapsed, mistakesRef.current) });
    },
  };
  return <ChallengePanel title={t(`mini.${id}.name`)} sector={sector} source={source} sample={sol.sample} onClose={close} descriptionId="mini-goal" details={<>
          {sol.sample && <p>{t("tasks.sampleNote")}</p>}{practice && <p>{t("mini.practice")}</p>}
          <p>{t("mini.rulesNote")}</p><p>{t("info.snapshot")}</p>
          <small>{source ?? t("tasks.badgeRule")}</small>
          <ul>{["timer", "mistakes", "stars"].map(key => <li key={key}><span data-origin={key}>{t(`mini.${key}`)}</span>: {t("tasks.badgeRule")}</li>)}</ul>
        </>}>
      {!result && !expired && <div className="mini-context">{(!compact || !begun) && <div className="mini-context-item"><Activity aria-hidden="true"/><div><h3>{t("challenge.happening")}</h3><p>{t(`challenge.briefings.${id}.happening`)}</p></div></div>}<div className="mini-context-item"><Target aria-hidden="true"/><div><h3>{t("challenge.action")}</h3><Dialog.Description id="mini-goal">{t(`mini.${id}.goal`)}</Dialog.Description></div></div></div>}
      {practice && <p className="mini-practice">{t("report.noReading")} · {t("mini.practice")}</p>}
      {!result && !expired && <ChallengeReadings id={id} sol={sol}/>}
      {!begun ? <section className="mini-briefing"><p><strong>{t("challenge.importance")}</strong>{t(`challenge.briefings.${id}.importance`)}</p></section> : !result && !expired && <main className="mini-play" key={attempt} inert={state.disabled ? true : undefined}>{children(state, attempt)}</main>}
      {feedback && !result && !expired && <div className="mini-feedback" role="alert"><AlertCircle/><p>{t(`challenge.briefings.${id}.error`)}</p><Button onClick={()=>{setAttempt(v=>v+1);setFeedback(false);}}>{t("mini.retry")}</Button></div>}
      {hint && !result && !expired && <div className="mini-hint" role="status"><Fish aria-hidden="true"/><div><strong>{t("bubble.name")}</strong><p>{t(`challenge.briefings.${id}.hint`)}</p></div></div>}

      {(result || expired) && <section className={`mini-result ${result ? "mini-result-success" : "mini-result-retry"}`} role="status" aria-describedby="mini-goal"><p id="mini-goal" className="sr-only">{t(`mini.${id}.goal`)}</p><div className="mini-result-icon">{result ? <Check /> : <RotateCcw />}</div><h2>{t(result ? "tasks.success" : "mini.retryTitle")}</h2>
        {result ? <><div className="mini-stars">{Array.from({ length: result.stars }, (_, i) => <Star key={i} />)}<NumberValue value={result.stars} origin="stars" /></div><p><strong>{t("mini.learned")}</strong> {learned}</p><div className="mini-consequence">{(["integrity","oxygen"] as const).map(k=><div key={k}><strong>{t(`hud.${k}`)}</strong><progress aria-label={t(`hud.${k}`)} value={preview[k].after} max={100}/><p><NumberValue value={preview[k].before} origin="status"/> → <NumberValue value={preview[k].after} origin="status"/></p><small>{t(preview[k].after===preview[k].before?"challenge.maintained":"challenge.preview")}</small></div>)}</div><small>{t("challenge.rewardNote")}</small><p>{t("mini.timer")} <NumberValue value={result.seconds} origin="timer" /> · {t("mini.mistakes")} <NumberValue value={result.mistakes} origin="mistakes" /></p><Button onClick={() => onDone(result)}>{t("tasks.continue")}</Button></>
          : <><p>{t("mini.retryBody")}</p><Button onClick={() => setAttempt(v => v + 1)}>{t("mini.retry")}</Button></>}
      </section>}
      {!result && !expired && <footer className="mini-footer"><div className="mini-status"><span><Timer aria-hidden="true"/>{t("mini.timer")} <NumberValue value={Math.max(0, Math.ceil(BALANCE.taskSeconds - seconds))} origin="timer" /></span><span>{t("mini.mistakes")} <NumberValue value={mistakes} origin="mistakes" /></span></div><div className="mini-footer-actions"><Button variant="ghost" aria-expanded={hint} onClick={()=>{setHint(v=>!v);if(!hint){useGame.getState().say(`challenge.briefings.${id}.hint`);sfx.blub();}}}><Lightbulb/>{t("challenge.hint")}</Button>{!begun ? <Button className="mini-action" disabled={!ready} onClick={()=>{setBegun(true);sfx.click();}}>{t(ready ? "challenge.begin" : "observatory.loading")}</Button> : <Button className="mini-action" onClick={()=>{setAttempt(v=>v+1);setFeedback(false);}}><RotateCcw/>{t("mini.retry")}</Button>}</div></footer>}
  </ChallengePanel>;
}