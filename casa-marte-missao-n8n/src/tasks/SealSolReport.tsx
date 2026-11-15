import { useState } from "react";
import * as Dialog from "@radix-ui/react-dialog";
import { Stamp } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { SYSTEMS, type Allocation } from "@/engine/demand";
import { useCampaign } from "@/game/campaign";
import { NumberValue } from "./MiniFrame";
import { ChallengePanel } from "./ChallengePanel";
export default function SealSolReport() {
  const { t } = useT(); const confirmed = useCampaign(s => s.confirmedAlloc); const seal = useCampaign(s => s.sealSol);
  const [values, setValues] = useState<Allocation>({ shield: 0, life: 0, greenhouse: 0 });
  const ready = confirmed && SYSTEMS.every(k => values[k] === confirmed[k]);
  return <ChallengePanel title={t("mini.sealTitle")} sector="control" descriptionId="seal-goal" onClose={()=>useCampaign.getState().setSealOpen(false)}>
    <Dialog.Description id="seal-goal" className="mini-goal">{t("mini.sealGoal")}</Dialog.Description><main className="mini-play"><Stamp className="mini-thermometer" />
      {SYSTEMS.map(k => <label key={k} className="mini-energy-row"><strong>{t(`sys.${k}`)}</strong><span>{t("mini.confirmed")} <NumberValue value={confirmed?.[k] ?? 0} origin="energy" /></span><input type="range" aria-label={t(`sys.${k}`)} min={0} max={BALANCE.energyPool} step={BALANCE.sliderStep} value={values[k]} onChange={e => setValues(old => ({ ...old, [k]: Number(e.target.value) }))} /><NumberValue value={values[k]} origin="energy" /></label>)}
      <Button className="mini-seal-button" disabled={!ready} onClick={() => seal(values)}><Stamp />{t("mini.seal")}</Button>
    </main></ChallengePanel>;
}