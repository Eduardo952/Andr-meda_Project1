import { BALANCE } from "@/config/balance";
import { useT } from "@/i18n";
import { useGame } from "@/game/store";
import { MiniFrame, type MiniProps } from "./MiniFrame";
import { taskData } from "./rules";
import { DialPuzzle } from "./TaskControls";
export default function AlignCoils(props: MiniProps) {
  const { t } = useT(); const data = taskData(props.sol); const reduced = useGame(s => s.settings.reducedMotion);
  return <MiniFrame {...props} id="AlignCoils" learned={t("mini.AlignCoils.learned")}>
    {state => <DialPuzzle targets={BALANCE.miniGames.dialTargets} state={state} marks={data.marks.value} motion={reduced ? 0 : data.motion} />}
  </MiniFrame>;
}