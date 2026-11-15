import { useEffect, useState } from "react";
import { X, Minus, Plus, Eye, Check } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { getObservatory } from "@/lib/observatory.functions";
import { useCampaign } from "@/game/campaign";
import { TELESCOPE } from "@/config/telescope";
import { SOLS } from "@/data/campaign";
import { observationImpact } from "@/tasks/context";
import { useGame } from "@/game/store";
import { useT } from "@/i18n";
import { useWebhookMedia } from "@/services/narrator/media";

/** Telescope eyepiece view: only the picture, inside a circular lens with a dark mask (no frames, no side panels). */
export function ObservatoryDialog({ onClose }: { onClose: () => void }) {
  const { t } = useT();
  const sol = useCampaign(s => s.solIdx + 1), missionId = useCampaign(s => s.missionId);
  const webhook = useWebhookMedia(s => s.images[sol]);
  const { data, isPending } = useQuery({ queryKey: ["observatory", missionId, sol], queryFn: () => getObservatory({ data: { sol, mission: missionId || undefined } }), staleTime: 86400000 });
  const options: { key: string; label: string; src: string; title?: string | undefined; credit?: string | undefined }[] = [];
  if (webhook) options.push({ key: `webhook-${webhook.url}`, label: t("observatory.webhook"), src: webhook.url, title: webhook.title ?? t("observatory.webhook"), credit: webhook.credit });
  const apodSrc = data?.apod?.media_type === "image" ? data.apod.url : data?.apod?.thumbnail_url;
  if (apodSrc && data?.apod) options.push({ key: "apod", label: t("observatory.apod"), src: apodSrc, title: data.apod.title, credit: data.apod.copyright });
  if (data?.photo?.url) options.push({ key: "library", label: t("observatory.library"), src: data.photo.url, title: data.photo.title, credit: data.photo.credit });
  const [zoom, setZoom] = useState<number>(TELESCOPE.zoomMin), [question, setQuestion] = useState(false), [captionMatched, setCaptionMatched] = useState(false), [answer, setAnswer] = useState(false), [wrong, setWrong] = useState(false);
  const reduced = useGame(s=>s.settings.reducedMotion);
  const [pick, setPick] = useState(0), [failed, setFailed] = useState<string[]>([]);
  const current = options[Math.min(pick, Math.max(0, options.length - 1))];
  useEffect(() => { const esc = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); }; window.addEventListener("keydown", esc); return () => window.removeEventListener("keydown", esc); }, [onClose]);
  const snapshot = SOLS[sol-1];
  const forecast = snapshot ? observationImpact(snapshot) : "unknown";
  const broken = !!current && failed.includes(current.key);
  return <div className="lens-root" role="dialog" aria-modal="true" aria-label={t("observatory.title")}>
    <Button variant="ghost" size="icon" className="lens-close" aria-label={t("observatory.lensClose")} title={t("observatory.lensClose")} onClick={onClose}><X /></Button>
    <div className={`lens ${reduced ? "" : "lens-moving"}`} data-state={isPending ? "loading" : current && !broken ? "ready" : "empty"}>
      {current && !broken && <div className="lens-zoom" style={{ transform: `scale(${zoom})` }}><img key={current.key} className="lens-image" src={current.src} alt={current.title ?? current.label} onError={() => setFailed(f => [...f, current.key])} /></div>}
      <div className="lens-vignette" aria-hidden="true" /><div className="lens-reticle" aria-hidden="true" />
      {isPending && <p className="lens-note" role="status">{t("observatory.loading")}</p>}
      {!isPending && (!current || broken) && <p className="lens-note" role="status">{t("observatory.unavailable")}</p>}
    </div>
    <footer className="lens-caption">
      {current && !broken && <><strong>{current.title}</strong>{current.credit && <small>{t("observatory.credit", { credit: current.credit })}</small>}</>}
      {current && !broken && <div className="lens-controls"><Button aria-label={t("challenge.zoom")} disabled={zoom<=TELESCOPE.zoomMin} onClick={()=>setZoom(z=>Math.max(TELESCOPE.zoomMin,z-TELESCOPE.zoomStep))}><Minus/></Button><input aria-label={t("challenge.zoom")} type="range" min={TELESCOPE.zoomMin} max={TELESCOPE.zoomMax} step={TELESCOPE.zoomStep} value={zoom} onChange={e=>setZoom(Number(e.target.value))}/><Button aria-label={t("challenge.zoom")} disabled={zoom>=TELESCOPE.zoomMax} onClick={()=>setZoom(z=>Math.min(TELESCOPE.zoomMax,z+TELESCOPE.zoomStep))}><Plus/></Button><Button onClick={()=>setQuestion(true)}><Eye/>{t("challenge.observe")}</Button></div>}
      {question && current && <section className="lens-question" role="region" aria-label={t("challenge.observe")}><p>{t(captionMatched ? "challenge.impactQuestion" : "challenge.observeQuestion")}</p>{!captionMatched ? <><Button onClick={()=>{setCaptionMatched(true);setWrong(false);}}>{current.title}</Button><Button onClick={()=>setWrong(true)}>{t("mini.PhotoDetective.decoyLab")}</Button></> : <><Button onClick={()=>setWrong(true)}>{t("challenge.yes")}</Button><Button onClick={()=>{setAnswer(true);setWrong(false);}}>{t("challenge.no")}</Button></>}{wrong&&<p role="alert">{t("challenge.briefings.PhotoDetective.error")}</p>}{answer&&<p role="status"><Check/>{t("challenge.impactAnswer")} {t(`challenge.${forecast === "yes" ? "forecastYes" : forecast === "no" ? "forecastNo" : "unknown"}`)}</p>}<Button onClick={()=>setQuestion(false)}>{t("settings.close")}</Button></section>}
      {options.length > 1 && <div className="lens-switch">{options.map((o, i) => <Button key={o.key} size="sm" variant={i === pick ? "secondary" : "ghost"} onClick={() => {setPick(i);setQuestion(false);setZoom(1);setCaptionMatched(false);setAnswer(false);setWrong(false);}}>{o.label}</Button>)}</div>}
    </footer>
  </div>;
}
