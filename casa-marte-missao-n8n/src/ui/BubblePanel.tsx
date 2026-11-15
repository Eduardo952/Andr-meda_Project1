import { useEffect, useState } from "react";
import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useGame } from "@/game/store";
import { useCampaign } from "@/game/campaign";
import { enqueueLine } from "@/game/presentation";
import { useT } from "@/i18n";
import { QUESTION_IDS } from "@/services/narrator/types";
import { speak } from "@/services/narrator/speak";
import { sfx } from "@/audio/sfx";
export function BubblePanel(){
 const line=useGame(s=>s.bubbleLine),enabled=useGame(s=>s.settings.bubbleToasts);const phase=useCampaign(s=>s.phase);const blocked=useCampaign(s=>s.reportOpen||s.energyOpen||!!s.openTask||s.phase==="summary");const {t}=useT();
 const [debug,setDebug]=useState(false);useEffect(()=>{try{setDebug(new URLSearchParams(window.location.search).get("debug")==="n8n");}catch{/* no window */}},[]);
 const tag=(l:typeof line)=>debug&&l.source?<small style={{display:"block",opacity:.7,fontSize:"0.75em"}} data-testid="bubble-source">via {l.source}</small>:null;
 const [open,setOpen]=useState(false),[queue,setQueue]=useState<typeof line[]>([]);
 useEffect(()=>{if(enabled)setQueue(q=>enqueueLine(q,line));else setQueue([]);},[line,enabled]);
 useEffect(()=>{if(blocked){setOpen(false);setQueue([]);}},[blocked]);
 const toast=queue[0];useEffect(()=>{if(!toast||blocked||open)return;const timer=setTimeout(()=>setQueue(q=>q.slice(1)),4500);return()=>clearTimeout(timer);},[toast,blocked,open]);
 return <div className="bubble-hud">
 {!open&&!blocked&&toast&&enabled&&<div className="bubble-toast" role="status">{toast.text??t(toast.key,toast.vars)}{tag(toast)}</div>}
 {open&&!blocked&&<section className="bubble-sheet" role="dialog" aria-label={t("ask.title")}><header><h2>{t("bubble.name")}</h2><Button variant="ghost" size="icon" aria-label={t("settings.close")} onClick={()=>{setQueue([]);setOpen(false);}}><X/></Button></header><p>{line.text??t(line.key,line.vars)}{tag(line)}</p><h3>{t("ask.title")}</h3>{QUESTION_IDS.map(q=><Button variant="ghost" key={q} onClick={()=>{sfx.blub();void speak("answer",q);}}>{t(`ask.${q}`)}</Button>)}{(phase==="plan"||phase==="tasks")&&<Button variant="secondary" onClick={()=>void speak("hint")}>{t("ask.hint")}</Button>}</section>}
 <Button className="bubble-face-button" aria-label={t("ask.title")} title={t("ask.title")} aria-expanded={open} onClick={()=>{setQueue([]);setOpen(v=>!v);sfx.click();}}><svg viewBox="0 0 48 48" aria-hidden="true"><path className="fish-fin" d="M10 24 3 15v18z"/><ellipse className="fish-body" cx="26" cy="25" rx="17" ry="14"/><path className="fish-fin" d="M20 13 23 5 32 13"/><circle className="fish-eye" cx="25" cy="21" r="5"/><circle className="fish-eye" cx="36" cy="21" r="5"/><circle className="fish-pupil" cx="26" cy="22" r="2.5"/><circle className="fish-pupil" cx="37" cy="22" r="2.5"/><path className="fish-smile" d="M25 30q5 5 10 0"/></svg></Button>
 </div>;
}
