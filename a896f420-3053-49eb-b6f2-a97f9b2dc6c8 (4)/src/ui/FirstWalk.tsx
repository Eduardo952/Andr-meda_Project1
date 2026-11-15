import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { FIRST_WALK, usePresentation } from "@/game/presentation";
import { currentTarget, useCampaign } from "@/game/campaign";
import { useT } from "@/i18n";
export function FirstWalk(){
 const {t}=useT();const step=usePresentation(s=>s.tourStep),dismissed=usePresentation(s=>s.tourDismissed),dismiss=usePresentation(s=>s.dismissTour);const blocked=useCampaign(s=>s.reportOpen||s.energyOpen||!!s.openTask||!!s.alarm);
 const target=useCampaign(()=>currentTarget());
 const room=FIRST_WALK[step];if(!room||dismissed||blocked||target&&target!==room)return null;
 return <div className="first-walk"><span>{t(`refine.walk.${room}`)}</span><Button variant="ghost" size="icon" aria-label={t("refine.dismissWalk")} title={t("refine.dismissWalk")} onClick={dismiss}><X size={18}/></Button></div>;
}