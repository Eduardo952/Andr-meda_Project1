import { useState } from "react";
import { Droplets, Sprout, Minus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { IRRIGATION, irrigationCorrect } from "./context";
import { MiniFrame, NumberValue, type MiniProps, type PlayState } from "./MiniFrame";
function Irrigate({ state }: { state: PlayState }) {
 const { t } = useT(); const [water, setWater] = useState([0, 0, 0]); const free = IRRIGATION.water - water.reduce((a,b)=>a+b,0);
 return <><p>{t("challenge.waterBudget")} <NumberValue value={free} origin="count" /></p><p>{t("challenge.moistureTarget")} <NumberValue value={IRRIGATION.target} origin="count" /></p><div className="mini-dials">{IRRIGATION.moisture.map((m,i)=><section key={i}><Sprout className="mini-thermometer"/><p>{t("challenge.moisture")} <NumberValue value={m+(water[i]??0)} origin="count"/></p><div className="mini-dial-buttons"><Button aria-label={t("challenge.resetWater",{n:i+1})} disabled={!water[i]} onClick={()=>setWater(v=>v.map((x,j)=>j===i?x-1:x))}><Minus/></Button><Button aria-label={t("challenge.addWater",{n:i+1})} disabled={free<=0} onClick={()=>setWater(v=>v.map((x,j)=>j===i?x+1:x))}><Droplets/></Button></div></section>)}</div><Button onClick={()=>irrigationCorrect(water)?state.win():state.mistake()}>{t("tasks.check")}</Button></>;
}
export default function IrrigationPlan(props:MiniProps){const {t}=useT();return <MiniFrame {...props} id="IrrigationPlan" learned={t("mini.IrrigationPlan.learned")}>{state=><Irrigate state={state}/>}</MiniFrame>;}
