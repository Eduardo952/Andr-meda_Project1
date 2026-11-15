import { useState } from "react";
import { Apple, ShoppingBasket, Info, X, Sprout } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useGame } from "@/game/store";
import { useCampaign } from "@/game/campaign";
import { APPLES, HARVEST_RADIUS, useGarden, gardenVisuals } from "@/game/greenhouse";
import { runtime } from "@/game/runtime";
import { DERIVED } from "@/data/campaign";
import { useT } from "@/i18n";
import { useGreenhouseTrainer } from "@/game/greenhouseTrainer";

export function GreenhouseLearning() {
  const { t } = useT();
  return <div className="garden-learning"><p>{t("garden.learning")}</p><small>{t("garden.sourcePending")}</small></div>;
}
export function GreenhousePanel() {
  const [info, setInfo] = useState(false), { t } = useT();
  const room = useGame(s => s.currentRoom), collected = useGarden(s => s.collected);
  const blocked = useCampaign(s => !!s.openTask || s.energyOpen || s.reportOpen || s.phase === "summary");
  if (room !== "greenhouse" || blocked) return null;
  const harvest = () => {
    const c = useCampaign.getState(), v = gardenVisuals(c.alloc.greenhouse, DERIVED[c.solIdx]?.demand.greenhouse ?? 20);
    const id = APPLES.findIndex((p, i) => i < v.ripe && !collected.includes(i) && Math.hypot(runtime.x - p[0], runtime.z - p[2]) <= HARVEST_RADIUS);
    const p = APPLES[id]; if (p) useGarden.getState().pick(id, v.ripe, Math.hypot(runtime.x - p[0], runtime.z - p[2]));
  };
  return <aside className="garden-hud" aria-label={t("rooms.greenhouse")}>
    {info && <div className="garden-info"><header><strong>{t("rooms.greenhouse")}</strong><Button variant="ghost" size="icon" aria-label={t("settings.close")} onClick={() => setInfo(false)}><X size={18}/></Button></header><GreenhouseLearning/><small>{t("garden.cosmetic")}</small></div>}
    <div className="garden-actions"><span role="status" aria-live="polite"><ShoppingBasket size={20}/>{t("garden.basket", { n: collected.length })}</span><Button size="icon" variant="secondary" aria-label={t("garden.harvest")} title={t("garden.harvest")} onClick={harvest}><Apple size={22}/></Button><Button size="icon" variant="ghost" aria-label={t("garden.learn")} title={t("garden.learn")} onClick={() => setInfo(!info)}><Info size={20}/></Button></div>
    <Button className="btn-game garden-trainer-launch" onClick={() => useGreenhouseTrainer.getState().setOpen(true)}><Sprout/>{t("trainer.title")}</Button>
  </aside>;
}