import { useT } from "@/i18n";
import { MiniFrame, type MiniProps } from "./MiniFrame";
import { DialPuzzle } from "./TaskControls";
import { BALANCE } from "@/config/balance";
export default function PipeFlow(props: MiniProps) { const { t } = useT(); return <MiniFrame {...props} id="PipeFlow" learned={t("mini.PipeFlow.learned")}>{state => <><div className="mini-tanks"><span>◇ {t("mini.oxygenTank")}</span><span>≈ {t("mini.waterTank")}</span></div><DialPuzzle state={state} pipes targets={[...BALANCE.miniGames.dialTargets, ...BALANCE.miniGames.dialTargets]} /></>}</MiniFrame>; }