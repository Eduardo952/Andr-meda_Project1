import { useState } from "react";
import * as Dialog from "@radix-ui/react-dialog";
import { X, Stamp } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { SYSTEMS, type Allocation } from "@/engine/demand";
import { useCampaign } from "@/game/campaign";
import { NumberValue } from "./MiniFrame";
export default function SealSolReport() {
  const { t } = useT(); const confirmed = useCampaign(s => s.confirmedAlloc); const seal = useCampaign(s => s.sealSol);
  const [values, setValues] = useState<Allocation>({ shield: 0, life: 0, greenhouse: 0 });
  const ready = confirmed && SYSTEMS.every(k => values[k] === confirmed[k]);
  return <Dialog.Root open onOpenChange={open => { if (!open) useCampaign.getState().setSealOpen(false); }}><Dialog.Portal><Dialog.Content className="mini-overlay region-control" onCloseAutoFocus={e => { e.preventDefault(); document.querySelector<HTMLElement>("[data-station-use]")?.focus(); }}>
    <header className="mini-header"><Dialog.Title>{t("mini.sealTitle")}</Dialog.Title><Dialog.Close asChild><Button variant="ghost" size="icon" aria-label={t("settings.close")}><X /></Button></Dialog.Close></header>
    <Dialog.Description className="mini-goal">{t("mini.sealGoal")}</Dialog.Description><main className="mini-play"><Stamp className="mini-thermometer" /><details><summary>{t("info.show")}</summary><p>{t("tasks.badgeRule")}</p></details>
      {SYSTEMS.map(k => <label key={k} className="mini-energy-row"><strong>{t(`sys.${k}`)}</strong><span>{t("mini.confirmed")} <NumberValue value={confirmed?.[k] ?? 0} origin="energy" /></span><input type="range" aria-label={t(`sys.${k}`)} min={0} max={BALANCE.energyPool} step={BALANCE.sliderStep} value={values[k]} onChange={e => setValues(old => ({ ...old, [k]: Number(e.target.value) }))} /><NumberValue value={values[k]} origin="energy" /></label>)}
      <Button className="mini-seal-button" disabled={!ready} onClick={() => seal(values)}><Stamp />{t("mini.seal")}</Button>
    </main></Dialog.Content></Dialog.Portal></Dialog.Root>;
}