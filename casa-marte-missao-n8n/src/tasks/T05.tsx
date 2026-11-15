import { ShieldAlert } from "lucide-react";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { useCampaign } from "@/game/campaign";

/**
 * T05 "Go to the Shelter" is played in the 3D world: the alarm starts it and entering the
 * Radiation Shelter completes it (detected by the Stepper). This is the on-screen objective + timer.
 */
export function T05Objective() {
  const { t } = useT();
  const alarm = useCampaign((s) => s.alarm);
  const elapsed = useCampaign(s => s.alarmElapsed);
  const left = Math.max(0, Math.ceil(BALANCE.shelterSeconds - elapsed));
  if (!alarm) return null;
  return (
    <div className="panel animate-pop flex items-center gap-3 border-destructive px-4 py-2" role="alert">
      <ShieldAlert className="h-8 w-8 text-destructive" />
      <div>
        <div className="font-display text-lg text-destructive">{t("alarm.title")}</div>
        <div className="font-bold">{t("alarm.goShelter")} <span className="tabular-nums">{t("tasks.time", { s: left })}</span></div>
        <div className="text-sm">{t("alarm.adjust")}</div>
      </div>
    </div>
  );
}
