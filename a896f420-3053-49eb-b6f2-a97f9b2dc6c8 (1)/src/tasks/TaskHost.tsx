import { lazy, Suspense, type ComponentType, type LazyExoticComponent } from "react";
import { useCampaign } from "@/game/campaign";
import { SOLS } from "@/data/campaign";
import { useT } from "@/i18n";
import { isMiniTask, type MiniTaskId } from "./assignments";
import type { MiniProps } from "./MiniFrame";
const components: Record<MiniTaskId, LazyExoticComponent<ComponentType<MiniProps>>> = {
  SortFlares: lazy(() => import("./SortFlares")), PhotoDetective: lazy(() => import("./PhotoDetective")),
  AlignCoils: lazy(() => import("./AlignCoils")), WireShield: lazy(() => import("./WireShield")), CmeCountdown: lazy(() => import("./CmeCountdown")),
  PipeFlow: lazy(() => import("./PipeFlow")), ThermostatDial: lazy(() => import("./ThermostatDial")), SmartFarmCare: lazy(() => import("./SmartFarmCare")),
  CleanPanels: lazy(() => import("./CleanPanels")), TiltSun: lazy(() => import("./TiltSun")),
};
const SealSolReport = lazy(() => import("./SealSolReport"));
const EnergyPanel = lazy(() => import("./EnergyPanel"));
export function TaskHost() {
  const { t } = useT(); const id = useCampaign(s => s.openTask), solIdx = useCampaign(s => s.solIdx);
  const energyOpen = useCampaign(s => s.energyOpen), sealOpen = useCampaign(s => s.sealOpen);
  const sol = SOLS[solIdx]; if (!sol) return null;
  const Task = id && isMiniTask(id) ? components[id] : null;
  return <Suspense fallback={<div className="mini-overlay" role="status">{t("mini.loading")}</div>}>
    {Task && id && <Task key={`${solIdx}-${id}`} sol={sol} onDone={result => useCampaign.getState().finishMiniTask(id, result)} />}
    {energyOpen && <EnergyPanel />}{sealOpen && <SealSolReport />}
  </Suspense>;
}
