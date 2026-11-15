import { useState } from "react";
import { ImageOff, Film } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useQuery } from "@tanstack/react-query";
import { getObservatory } from "@/lib/observatory.functions";
import { useCampaign } from "@/game/campaign";
import { useT } from "@/i18n";
export function MediaCard({src,title,credit,video,source,snapshot,pending,wall}:{src?:string|undefined;title?:string|undefined;credit?:string|undefined;video?:string|undefined;source:string;snapshot?:boolean|undefined;pending?:boolean|undefined;wall?:boolean|undefined}){
 const {t}=useT();const [failed,setFailed]=useState(false),[loaded,setLoaded]=useState(false),[expanded,setExpanded]=useState(false);
 const media=<div className="nasa-media">{(pending||src&&!loaded&&!failed)&&<div className="nasa-skeleton" aria-label={t("observatory.loading")}/>} {src&&!failed?<img src={src} alt={title??source} onLoad={()=>setLoaded(true)} onError={()=>setFailed(true)}/>:!pending&&<div className="nasa-fallback"><ImageOff size={32}/><p>{t(failed?"refine.imageFailed":"observatory.unavailable")}</p></div>}</div>;
 // Wall variant: image only, so the big screen stays clean; details live in the dialog.
 if(wall)return <article className="nasa-card">{media}</article>;
 return <article className="nasa-card"><div className="nasa-source"><span>{source}</span>{snapshot&&<span>{t("observatory.snapshot")}</span>}</div>{media}{title&&<h3>{title}</h3>}<p className={expanded?"":"caption-clamp"}>{t("observatory.explanation")}{expanded&&<> {t("refine.nasaMore")}</>}</p><Button variant="ghost" size="sm" onClick={()=>setExpanded(!expanded)}>{t(expanded?"refine.readLess":"refine.readMore")}</Button>{credit&&<small>{t("observatory.credit",{credit})}</small>}{video&&<a className="nasa-video" href={video} target="_blank" rel="noopener noreferrer"><Film size={18}/>{t("observatory.video")}</a>}</article>;
}
export function NasaCards({variant="dialog"}:{variant?:"dialog"|"wall"}){
 // This read-only query also renders in Drei Html's separate React root.
 // Call the generated RPC directly: useServerFn requires router context there.
 const wall=variant==="wall";
 const sol=useCampaign(s=>s.solIdx+1),{t}=useT();const {data,isPending}=useQuery({queryKey:["observatory",sol],queryFn:()=>getObservatory({data:{sol}}),staleTime:86400000});
 return <>{!wall&&data?.sample&&data.photo&&<p>{t("observatory.archiveNote")}</p>}<div className="nasa-grid">{(isPending||data?.apod)&&<MediaCard key={`apod-${sol}-${data?.apod?.url}`} src={data?.apod?.media_type==="image"?data.apod.url:data?.apod?.thumbnail_url} title={data?.apod?.title} credit={data?.apod?.copyright??(data?.apod?"NASA":undefined)} video={data?.apod?.media_type==="video"?data.apod.url:undefined} source={t("observatory.apod")} snapshot={data?.source==="snapshot"} pending={isPending} wall={wall}/>}<MediaCard key={`library-${sol}-${data?.photo?.url}`} src={data?.photo?.url} title={data?.photo?.title} credit={data?.photo?.credit} source={t("refine.librarySource")} snapshot={data?.photoSource==="snapshot"} pending={isPending} wall={wall}/></div></>;
}
