import { Compass } from "lucide-react";
import { Button } from "@/components/ui/button";
import { SOLS } from "@/data/campaign";
import { useT } from "@/i18n";
import { MiniFrame, type MiniProps } from "./MiniFrame";
export default function WindReading(props:MiniProps){const {t}=useT();const reading=props.sol.windDir.value??SOLS.find(s=>s.windDir.value)?.windDir.value;const dir=reading?.mostCommon??"N";const choices=Array.from(new Set(["N","E","S","W",dir]));return <MiniFrame {...props} id="WindReading" source={reading?"InSight WD":undefined} practice={!props.sol.windDir.value} learned={t("mini.WindReading.learned")}>{state=><><Compass className="mini-thermometer"/><p>{t("challenge.wind")} <strong>{t(`compass.${dir}`)}</strong></p><div className="mini-cards">{choices.map(d=><Button key={d} onClick={()=>d===dir?state.win():state.mistake()}>{t(`compass.${d}`)}</Button>)}</div></>}</MiniFrame>;}
