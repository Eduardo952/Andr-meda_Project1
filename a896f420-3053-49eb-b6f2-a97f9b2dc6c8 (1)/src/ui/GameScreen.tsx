import { useState,useEffect,useRef } from "react";
import { useServerFn } from "@tanstack/react-start";
import { useQuery } from "@tanstack/react-query";
import { getObservatory } from "@/lib/observatory.functions";
import { ObservatoryDialog } from "./ObservatoryDialog";
import { runtime } from "@/game/runtime";
import { Button } from "@/components/ui/button";
import { GameCanvas } from "@/game/scene/GameCanvas";
import { useGame } from "@/game/store";
import { useCampaign } from "@/game/campaign";
import { useT } from "@/i18n";
import { useStation } from "@/game/actions";
import { TaskHost } from "@/tasks/TaskHost";
import { T05Objective } from "@/tasks/T05";
import { Hud } from "./Hud";
import { Minimap } from "./Minimap";
import { BubblePanel } from "./BubblePanel";
import { FirstWalk } from "./FirstWalk";
import { GreenhousePanel } from "./GreenhousePanel";
import { Joystick } from "./Joystick";
import { SettingsDialog } from "./SettingsDialog";
import { DataDialog } from "./DataDialog";
import { SolReport } from "./SolReport";
import { TaskList } from "./TaskList";
import { SolSummary } from "./SolSummary";
import { Certificate, GameOver } from "./EndScreens";

export function GameScreen() {
  const { t } = useT();
  const sol = useCampaign(s=>s.solIdx+1);
  const loadObservatory=useServerFn(getObservatory);
  useQuery({queryKey:["observatory",sol],queryFn:()=>loadObservatory({data:{sol}}),staleTime:86400000});
  const [observatoryOpen,setObservatoryOpen]=useState(false);
  const nearScope = useGame((s) => s.nearTelescope);
  const scopeTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  // Short approach animation (character turns and leans in), then the lens opens.
  const startObserving = () => { if (runtime.observing || !useGame.getState().nearTelescope) return; runtime.observing = true; scopeTimer.current = setTimeout(() => setObservatoryOpen(true), 800); };
  const stopObserving = () => { if (scopeTimer.current) clearTimeout(scopeTimer.current); runtime.observing = false; setObservatoryOpen(false); };
  useEffect(() => () => { if (scopeTimer.current) clearTimeout(scopeTimer.current); runtime.observing = false; }, []);
  useEffect(() => {
    const enter = (event: KeyboardEvent) => {
      if (event.key.toLowerCase() !== "v" || event.repeat) return;
      const target = event.target;
      if (target instanceof HTMLElement && target.closest("input, textarea, select, [role=dialog]")) return;
      const campaign = useCampaign.getState();
      if (campaign.reportOpen || campaign.openTask || campaign.energyOpen || campaign.sealOpen) return;
      event.preventDefault(); startObserving();
    };
    window.addEventListener("keydown", enter);
    return () => window.removeEventListener("keydown", enter);
  }, []);
  const near = useGame((s) => s.nearStation);
  const isTouch = useGame((s) => s.isTouch);
  const phase = useCampaign((s) => s.phase);
  const reportOpen = useCampaign((s) => s.reportOpen);
  const energyOpen = useCampaign((s) => s.energyOpen);
  const openTask = useCampaign(s => s.openTask), sealOpen = useCampaign(s => s.sealOpen);
  const alarm = useCampaign((s) => s.alarm);
  const [dialog, setDialog] = useState<null | "settings" | "data">(null);
  return (
    <div data-game-root className="relative h-dvh w-full overflow-hidden bg-ink">
      <div className="absolute inset-0"><GameCanvas /></div>
      {alarm && <div className="alarm-vignette pointer-events-none absolute inset-0" />}
      <div hidden={!!(openTask || energyOpen || sealOpen)}>
      <Hud onSettings={() => setDialog("settings")} onData={() => setDialog("data")} />

      <div className="absolute left-1/2 top-60 -translate-x-1/2 sm:top-52"><T05Objective /></div>
      <BubblePanel />{!nearScope&&<FirstWalk />}<GreenhousePanel />
      {phase === "tasks" && <div className={`task-list-position absolute ${isTouch ? "left-3 top-28 scale-90 origin-top-left" : "left-3 top-28"}`}><TaskList /></div>}
      <div className={`map-position absolute ${isTouch ? "right-3 top-28" : "bottom-3 left-3"}`}><Minimap /></div>
      {isTouch && <div className="absolute bottom-6 left-6"><Joystick /></div>}
      {(near || isTouch) && (
        <Button data-station-use disabled={!near} onClick={useStation}
          className={`btn-game absolute ${isTouch ? "bottom-8 right-24 h-16 w-20 text-xl" : "bottom-8 left-1/2 -translate-x-1/2 px-10 text-2xl"} ${near ? "animate-pop" : "opacity-40"}`}>
          {t("hud.use")}{!isTouch && <span className="ml-2 rounded-md bg-ink/15 px-2 text-base">E</span>}
        </Button>
      )}
      {nearScope&&!observatoryOpen&&!reportOpen&&<Button title={t("observatory.viewImage")} className="btn-game absolute bottom-24 left-1/2 -translate-x-1/2 animate-pop" onClick={startObserving}>{t("observatory.viewImage")}</Button>}
      {observatoryOpen&&<ObservatoryDialog onClose={stopObserving}/>}
      </div>
      {reportOpen && <SolReport />}
      <TaskHost />
      {phase === "summary" && <SolSummary />}
      {phase === "gameover" && <GameOver />}
      {phase === "victory" && <Certificate />}
      {dialog === "settings" && <SettingsDialog onClose={() => setDialog(null)} showHome />}
      {dialog === "data" && <DataDialog onClose={() => setDialog(null)} />}
    </div>
  );
}
