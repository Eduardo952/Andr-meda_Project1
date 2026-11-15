import { useEffect, useState } from "react";
import { X } from "lucide-react";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { getObservatory } from "@/lib/observatory.functions";
import { useCampaign } from "@/game/campaign";
import { useT } from "@/i18n";

/** Telescope eyepiece view: only the picture, inside a circular lens with a dark mask (no frames, no side panels). */
export function ObservatoryDialog({ onClose }: { onClose: () => void }) {
  const { t } = useT();
  const sol = useCampaign(s => s.solIdx + 1);
  const { data, isPending } = useQuery({ queryKey: ["observatory", sol], queryFn: () => getObservatory({ data: { sol } }), staleTime: 86400000 });
  const options: { key: string; label: string; src: string; title?: string | undefined; credit?: string | undefined }[] = [];
  const apodSrc = data?.apod?.media_type === "image" ? data.apod.url : data?.apod?.thumbnail_url;
  if (apodSrc && data?.apod) options.push({ key: "apod", label: t("observatory.apod"), src: apodSrc, title: data.apod.title, credit: data.apod.copyright });
  if (data?.photo?.url) options.push({ key: "library", label: t("observatory.library"), src: data.photo.url, title: data.photo.title, credit: data.photo.credit });
  const [pick, setPick] = useState(0), [failed, setFailed] = useState<string[]>([]);
  const current = options[Math.min(pick, Math.max(0, options.length - 1))];
  useEffect(() => { const esc = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); }; window.addEventListener("keydown", esc); return () => window.removeEventListener("keydown", esc); }, [onClose]);
  const broken = !!current && failed.includes(current.key);
  return <div className="lens-root" role="dialog" aria-modal="true" aria-label={t("observatory.title")}>
    <Button variant="ghost" size="icon" className="lens-close" aria-label={t("observatory.lensClose")} title={t("observatory.lensClose")} onClick={onClose}><X /></Button>
    <div className="lens" data-state={isPending ? "loading" : current && !broken ? "ready" : "empty"}>
      {current && !broken && <img key={current.key} className="lens-image" src={current.src} alt={current.title ?? current.label} onError={() => setFailed(f => [...f, current.key])} />}
      <div className="lens-vignette" aria-hidden="true" /><div className="lens-reticle" aria-hidden="true" />
      {isPending && <p className="lens-note" role="status">{t("observatory.loading")}</p>}
      {!isPending && (!current || broken) && <p className="lens-note" role="status">{t("observatory.unavailable")}</p>}
    </div>
    <footer className="lens-caption">
      {current && !broken && <><strong>{current.title}</strong>{current.credit && <small>{t("observatory.credit", { credit: current.credit })}</small>}</>}
      {options.length > 1 && <div className="lens-switch">{options.map((o, i) => <Button key={o.key} size="sm" variant={i === pick ? "secondary" : "ghost"} onClick={() => setPick(i)}>{o.label}</Button>)}</div>}
    </footer>
  </div>;
}
