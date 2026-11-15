import { useEffect,useRef,useState } from "react";
import { Maximize2, X, Compass, Navigation, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { MODULES, PATHS, MAP_BOUNDS, ROOM_BY_ID, type RoomId } from "@/config/rooms";
import { runtime } from "@/game/runtime";
import { currentTarget,useCampaign } from "@/game/campaign";
import { usePresentation } from "@/game/presentation";
import { useGame } from "@/game/store";
import { useT } from "@/i18n";
import { RegionIcon } from "./RegionIcon";
export function Minimap(){
 const [expanded,setExpanded]=useState(false);const dot=useRef<SVGGElement>(null),floor=useRef<SVGGElement>(null);
 const mission=useCampaign(()=>currentTarget()),manual=usePresentation(s=>s.manual),cameraUp=usePresentation(s=>s.cameraUp);const setManual=usePresentation(s=>s.setManual),setCameraUp=usePresentation(s=>s.setCameraUp);const current=useGame(s=>s.currentArea);const {t}=useT();
 useEffect(()=>{let raf=0;const loop=()=>{dot.current?.setAttribute("transform",`translate(${runtime.x} ${runtime.z}) rotate(${runtime.heading*180/Math.PI+180})`);floor.current?.setAttribute("transform",cameraUp?`rotate(${runtime.camera.yaw*180/Math.PI} 0 20)`:"");raf=requestAnimationFrame(loop);};raf=requestAnimationFrame(loop);return()=>cancelAnimationFrame(raf);},[cameraUp,expanded]);
 const {minX,minZ,maxX,maxZ}=MAP_BOUNDS;const target=mission?ROOM_BY_ID[mission].entrance:null;
 const plan=<svg viewBox={cameraUp?"-110 -90 220 220":`${minX-8} ${minZ-8} ${maxX-minX+16} ${maxZ-minZ+16}`} className="floorplan" aria-label={t("hud.minimap")}><g ref={floor}>
 {PATHS.map((p,i)=><line key={i} x1={p.a.x} y1={p.a.z} x2={p.b.x} y2={p.b.z} className="map-corridor" strokeWidth={p.width}/>)}
 {MODULES.map(m=><g key={m.id} role={expanded?"button":undefined} tabIndex={expanded?0:undefined} aria-label={expanded?t(`rooms.${m.id}`):undefined} onClick={()=>{if(expanded){setManual(m.id as RoomId);setExpanded(false);}}} onKeyDown={e=>{if(expanded&&(e.key==="Enter"||e.key===" ")){setManual(m.id as RoomId);setExpanded(false);}}} className={`region-${m.id} map-module`}>
 {m.shape==="hub"||m.shape==="dome"?<ellipse cx={m.x} cy={m.z} rx={m.width/2} ry={m.depth/2}/>:<rect x={m.x-m.width/2} y={m.z-m.depth/2} width={m.width} height={m.depth} rx={m.shape==="barrel"?5:2}/>}
 <foreignObject x={m.x-5} y={m.z-5} width={10} height={10}><div className="map-icon"><RegionIcon room={m.id as RoomId} size={9}/></div></foreignObject>
 </g>)}
 {target&&<circle cx={target[0]} cy={target[1]} r={4.5} className="map-task"/>}
 {manual&&<circle cx={ROOM_BY_ID[manual].x} cy={ROOM_BY_ID[manual].z} r={6} className="map-guided"/>}
 <g ref={dot}><path d="M0 -4.5 L3.5 3 L0 1.8 L-3.5 3 Z" className="map-player"/></g>
 </g></svg>;
 const controls=<div className="map-controls"><Button variant="ghost" size="icon" title={t(cameraUp?"refine.cameraUp":"refine.northUp")} aria-label={t("refine.rotateMap")} aria-pressed={cameraUp} onClick={()=>setCameraUp(!cameraUp)}>{cameraUp?<Navigation size={17}/>:<Compass size={17}/>}</Button>{manual&&<Button variant="ghost" size="icon" title={t("refine.clearGuide")} aria-label={t("refine.clearGuide")} onClick={()=>setManual(null)}><RotateCcw size={17}/></Button>}<Button variant="ghost" size="icon" title={t(expanded?"settings.close":"refine.expandMap")} aria-label={t(expanded?"settings.close":"refine.expandMap")} onClick={()=>setExpanded(!expanded)}>{expanded?<X size={17}/>:<Maximize2 size={17}/>}</Button></div>;
 return <>{!expanded&&<div className="map-card">{controls}{plan}<div className="map-location">{current?t(`areas.${current}`):t("refine.corridor")}</div></div>}{expanded&&<div className="safe-overlay" role="dialog" aria-modal="true" aria-label={t("hud.minimap")}><section className="map-expanded">{controls}<h2>{t("hud.minimap")}</h2>{plan}<div className="map-destinations">{MODULES.filter(m=>m.id!=="airlock").map(m=><Button key={m.id} variant="ghost" className={`region-${m.id}`} onClick={()=>{setManual(m.id as RoomId);setExpanded(false);}}><RegionIcon room={m.id as RoomId}/>{t(`rooms.${m.id}`)}</Button>)}</div></section></div>}</>;
}
