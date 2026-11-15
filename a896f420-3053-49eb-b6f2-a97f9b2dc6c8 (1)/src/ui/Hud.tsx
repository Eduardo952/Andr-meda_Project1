import { useEffect, useState } from "react";
import { Heart, Wind, Zap, Star, Settings as Gear, Info, Camera, Sun, Moon, ClipboardList, Focus, PanelsTopLeft } from "lucide-react";
import { useT } from "@/i18n";
import { useGame } from "@/game/store";
import { BALANCE } from "@/config/balance";
import { DERIVED } from "@/data/campaign";
import { SYSTEMS } from "@/engine/demand";
import { runtime } from "@/game/runtime";
import { currentTarget, useCampaign } from "@/game/campaign";
import { marsClock, solFraction } from "@/game/sim/solClock";
import { sfx } from "@/audio/sfx";
import { recenterCamera } from "@/game/sim/camera";
import { Button } from "@/components/ui/button";

function Bar({ icon, label, value, fill }: { icon: React.ReactNode; label: string; value: number; fill: string }) {
  return (
    <div className="flex items-center gap-2" title={label}>
      <span className={`bar-icon grid h-9 w-9 place-items-center rounded-full ${fill} text-ink`}>{icon}</span>
      <div className="bar-body w-20 sm:w-28">
        <div className="bar-title text-xs font-extrabold uppercase tracking-wide">{label}</div>
        <div className="bar-track"><div className={`bar-fill ${fill}`} style={{ width: `${value}%` }} /></div>
      </div>
    </div>
  );
}

function SolClock() {
  const { t } = useT();
  const solIdx = useCampaign((s) => s.solIdx);
  const [, tick] = useState(0);
  useEffect(() => { const i = setInterval(() => tick((x) => x + 1), 250); return () => clearInterval(i); }, []);
  const c = marsClock(solFraction(runtime.solTime));
  return (
    <div className="panel flex items-center gap-2 px-4 py-2">
      {runtime.day > 0.5 ? <Sun className="h-6 w-6 text-rust" /> : <Moon className="h-6 w-6 text-tech" />}
      <div className="leading-tight">
        <div className="font-display text-sm">{t("hud.sol", { n: solIdx + 1, total: BALANCE.campaignSols })}</div>
        <div className="font-display text-xl tabular-nums">{String(c.h).padStart(2, "0")}:{String(c.m).padStart(2, "0")}</div>
      </div>
    </div>
  );
}

function PerformanceCounter() {
  const { t, lang } = useT();
  const [metrics, setMetrics] = useState(() => ({ ...runtime.metrics }));
  useEffect(() => {
    const timer = setInterval(() => setMetrics({ ...runtime.metrics }), 500);
    return () => clearInterval(timer);
  }, []);
  return (
    <div className="panel flex items-center gap-3 px-3 py-1 text-xs tabular-nums" role="group" aria-label={t("hud.performance")}>
      <span className="whitespace-nowrap"><strong>{metrics.fps || "—"}</strong> {t("hud.fps")}</span>
      <span className="whitespace-nowrap" title={t("hud.polygonsDetail")}><strong>{metrics.triangles.toLocaleString(lang)}</strong> {t("hud.polygons")}</span>
    </div>
  );
}

export function Hud({ onSettings, onData }: { onSettings: () => void; onData: () => void }) {
  const { t } = useT();
  const integrity = useCampaign((s) => s.integrity);
  const oxygen = useCampaign((s) => s.oxygen);
  const alloc = useCampaign((s) => s.alloc);
  const solIdx = useCampaign((s) => s.solIdx);
  const results = useCampaign((s) => s.results);
  const openReport = useCampaign((s) => s.openReport);
  const target = useCampaign(() => currentTarget());
  const iso = useGame((s) => s.settings.isometric);
  const roofs = useGame(s=>s.settings.roofs);
  const isTouch = useGame((s) => s.isTouch);
  const setSetting = useGame((s) => s.setSetting);
   const demand = DERIVED[solIdx]?.demand ?? DERIVED[0]?.demand;
   if (!demand) return null;
  // Energy bar: how much of today's need is covered by the plan.
  const need = SYSTEMS.reduce((n, s) => n + demand[s], 0);
  const covered = SYSTEMS.reduce((n, s) => n + Math.min(alloc[s], demand[s]), 0);
  const energy = Math.round((covered / need) * 100);
  const stars = Object.values(results).reduce((n, r) => n + r.reduce((sum, x) => sum + (x.stars ?? 0), 0), 0);
  return (
    <div className="pointer-events-none absolute inset-x-0 top-0 z-20 flex flex-wrap items-start justify-between gap-2 p-3">
      <div className="hud-bars pointer-events-auto panel flex flex-wrap items-center gap-3 px-3 py-2">
        <Bar icon={<Heart className="h-5 w-5" />} label={t("hud.integrity")} value={integrity} fill="bg-bar-integrity" />
        <Bar icon={<Wind className="h-5 w-5" />} label={t("hud.oxygen")} value={oxygen} fill="bg-bar-oxygen" />
        <Bar icon={<Zap className="h-5 w-5" />} label={t("hud.energy")} value={energy} fill="bg-bar-energy" />
        <Button className="btn-game alt btn-round" aria-label={t("hud.viewData")} title={t("hud.viewData")} onClick={() => { sfx.click(); onData(); }}><Info className="h-6 w-6" /></Button>
      </div>
      <div className="hud-clock flex flex-col items-center gap-2">
        <SolClock />
        {target && <div className="panel px-3 py-1 font-display text-sm">{t("hud.next", { room: t(`rooms.${target}`) })}</div>}
      </div>
      <div className="hud-stats pointer-events-auto fixed bottom-24 right-3 z-20 flex flex-col items-end gap-2">
        <div className="panel flex items-center gap-1 px-3 py-1" aria-label={t("hud.stars")}>
          <Star className="h-5 w-5 fill-sun text-rust" /><span className="font-display text-lg">{stars}</span>
        </div>
        <PerformanceCounter />
      </div>
      <div className={`hud-tools pointer-events-auto flex gap-2 ${isTouch ? "fixed bottom-44 left-3 z-20" : ""}`}>
        <Button variant="ghost" className="btn-game ghost btn-round" aria-label={t("settings.roofs")} title={t("hud.roofs",{mode:t(`settings.roof${roofs}`)})} onClick={()=>setSetting("roofs",roofs==="auto"?"off":"auto")}><PanelsTopLeft /></Button>
        <Button variant="ghost" className="btn-game ghost btn-round" aria-label={t("hud.recenterCamera")} title={t("hud.recenterCamera")} onClick={() => { sfx.click(); recenterCamera(runtime.camera, iso); }}><Focus /></Button>
        <Button className="btn-game btn-round" aria-label={t("hud.report")} title={t("hud.report")} onClick={() => { sfx.click(); openReport(); }}><ClipboardList className="h-6 w-6" /></Button>
        <Button className="btn-game ghost btn-round" aria-label={t("hud.camera")} title={t("hud.camera")} aria-pressed={iso} onClick={() => { sfx.click(); setSetting("isometric", !iso); }}><Camera className="h-6 w-6" /></Button>
        <Button className="btn-game ghost btn-round" aria-label={t("hud.settings")} title={t("hud.settings")} onClick={() => { sfx.click(); onSettings(); }}><Gear className="h-6 w-6" /></Button>
      </div>
      
    </div>
  );
}
