import * as Dialog from "@radix-ui/react-dialog";
import { NumberValue } from "@/tasks/MiniFrame";
import { Button } from "@/components/ui/button";
import { Shield, HeartPulse, Sprout } from "lucide-react";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { DERIVED } from "@/data/campaign";
import { useCampaign } from "@/game/campaign";
import { SYSTEMS, freeEnergy, type SystemId } from "@/engine/demand";
import { sfx } from "@/audio/sfx";

const ICON: Record<SystemId, React.ReactNode> = { shield: <Shield className="h-6 w-6" />, life: <HeartPulse className="h-6 w-6" />, greenhouse: <Sprout className="h-6 w-6" /> };
const TONE: Record<SystemId, string> = { shield: "bg-bar-oxygen", life: "bg-coral", greenhouse: "bg-bar-integrity" };

export function EnergyPanel() {
  const { t } = useT();
  const alloc = useCampaign((s) => s.alloc);
  const solIdx = useCampaign((s) => s.solIdx);
  const phase = useCampaign((s) => s.phase);
  const setAlloc = useCampaign((s) => s.setAlloc);
  const startSol = useCampaign((s) => s.startSol);
  const setOpen = useCampaign((s) => s.setEnergyOpen);
  const free = freeEnergy(alloc);
  const demand = DERIVED[solIdx]?.demand; if (!demand) return null;
  return (
    <Dialog.Root open onOpenChange={value => { if (!value) setOpen(false); }}><Dialog.Portal><Dialog.Content className="mini-overlay region-control" onCloseAutoFocus={e => { e.preventDefault(); document.querySelector<HTMLElement>("[data-station-use]")?.focus(); }}>
      <div className="mini-play">
        <span className="rule-badge">{t("report.gameRule")}</span>
        <Dialog.Title className="text-3xl font-bold">{t("energy.title")}</Dialog.Title><Dialog.Description>{t("energy.hint")}</Dialog.Description>
        <p className="font-bold">{t("energy.pool", { n: BALANCE.energyPool })}</p>
        <div className="my-4 space-y-4">
          {SYSTEMS.map((s) => (
            <div key={s}>
              <div className="mb-1 flex items-center gap-2">
                <span className={`grid h-10 w-10 place-items-center rounded-full ${TONE[s]}`}>{ICON[s]}</span>
                <span className="flex-1 font-extrabold">{t(`sys.${s}`)}</span>
                <span className="text-sm text-muted-foreground">{t("energy.need", { n: demand[s] })}</span>
                <span className="w-16 text-right font-display text-2xl tabular-nums"><NumberValue value={alloc[s]} origin="energy" /></span>
              </div>
              <input type="range" min={0} max={BALANCE.energyPool} step={BALANCE.sliderStep} value={alloc[s]} onChange={(e) => { setAlloc(s, Number(e.target.value)); sfx.click(); }}
                className="energy-lever h-12 w-full accent-tech" aria-label={t(`sys.${s}`)} />
            </div>
          ))}
        </div>
        <div className={`flex items-center justify-between rounded-2xl p-3 ${free === 0 ? "bg-bar-integrity/50" : "bg-sun/50"}`}>
          <span className="font-extrabold">{t("energy.free")}</span>
          <span className="font-display text-3xl tabular-nums"><NumberValue value={free} origin="energy" /></span>
        </div>
        <p className="mt-2 text-sm font-bold">{t("energy.hint")}</p>
        <p className="text-xs text-muted-foreground">{t("energy.budgetNote")}</p>
        <div className="mt-4 flex justify-between gap-2">
          <Button className="btn-game ghost" onClick={() => setOpen(false)}>{t("energy.close")}</Button>
          <Button className="btn-game alt text-xl disabled:opacity-40" disabled={free !== 0} onClick={() => { sfx.use(); startSol(); }}>{phase === "tasks" ? t("energy.save") : t("energy.start")}</Button>
        </div>
      </div>
    </Dialog.Content></Dialog.Portal></Dialog.Root>
  );
}
