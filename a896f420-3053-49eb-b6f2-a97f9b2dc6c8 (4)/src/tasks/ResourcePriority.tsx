import { Button } from "@/components/ui/button";
import { useCampaign } from "@/game/campaign";
import { deriveSol } from "@/engine/normalize";
import { useT } from "@/i18n";
import { priorityFor } from "./context";
import { MiniFrame, NumberValue, type MiniProps } from "./MiniFrame";
export default function ResourcePriority(props:MiniProps){const {t}=useT();const oxygen=useCampaign(s=>s.oxygen);const d=deriveSol(props.sol);return <MiniFrame {...props} id="ResourcePriority" learned={t("mini.ResourcePriority.learned")}>{state=><><p>{t("challenge.oxygen")} <NumberValue value={oxygen} origin="status"/></p><progress aria-label={t("challenge.oxygen")} value={oxygen} max={100}/><p>{t("sys.life")} <NumberValue value={d.demand.life} origin="heating"/> · {t("sys.greenhouse")} <NumberValue value={d.demand.greenhouse} origin="energy"/></p><div className="mini-cards">{(["air","water","heat"] as const).map(k=><Button key={k} onClick={()=>k===priorityFor(oxygen,props.sol)?state.win():state.mistake()}>{t(`challenge.${k}`)}</Button>)}</div></>}</MiniFrame>;}
