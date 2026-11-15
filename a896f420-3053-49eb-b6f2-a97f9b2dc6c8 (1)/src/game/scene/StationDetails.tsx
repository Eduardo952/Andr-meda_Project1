import { useEffect, useMemo, useRef, useState } from "react";
import { Html, Billboard } from "@react-three/drei";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { MODULES, ROOM_BY_ID, type RoomId } from "@/config/rooms";
import { PALETTE as P } from "@/config/palette";
import { useGame } from "@/game/store";
import { currentTarget, pendingTasks, useCampaign } from "@/game/campaign";
import { runtime } from "@/game/runtime";
import { labelCandidate } from "@/game/presentation";
import { useT } from "@/i18n";
import { RegionIcon } from "@/ui/RegionIcon";
import { Construction } from "./Construction";
import { marsClock, solFraction } from "@/game/sim/solClock";

function Station({ room }: { room: RoomId }) {
 const root=useRef<THREE.Group>(null),symbol=useRef<HTMLSpanElement>(null);const ring=useRef<THREE.Mesh>(null);const m=ROOM_BY_ID[room];
 useFrame(({clock})=>{const near=Math.hypot(runtime.x-m.entrance[0],runtime.z-m.entrance[1])<22;if(root.current)root.current.visible=near;if(symbol.current)symbol.current.style.visibility=near?"visible":"hidden";if(ring.current){const c=useCampaign.getState();const pending=room==="control"?c.phase==="plan"||c.phase==="tasks"&&!pendingTasks(c.solIdx,c.results).length:pendingTasks(c.solIdx,c.results).some(t=>t.room===room);ring.current.visible=pending;const s=useGame.getState().settings;ring.current.scale.setScalar(s.reducedMotion?1:1+Math.sin(clock.elapsedTime*2)*.06);const mat=ring.current.material as THREE.MeshToonMaterial;mat.emissiveIntensity=s.reducedMotion?.2:.2+Math.sin(clock.elapsedTime*2)*.12;}});
 return <group ref={root} position={[...([m.entrance[0],0,m.entrance[1]] as const)]}>
  <mesh ref={ring} rotation-x={Math.PI/2} position-y={.1}><torusGeometry args={[1.25,.055,4,20]}/><meshToonMaterial color={m.color} emissive={m.color} emissiveIntensity={.2}/></mesh>
  <Billboard position={[0,2.7,0]}><Html center zIndexRange={[2,0]}><span ref={symbol} className={`station-symbol region-${room}`}><RegionIcon room={room}/></span></Html></Billboard>
 </group>;
}
export function StationDetails(){
 const high=useGame(s=>s.settings.quality==="high");
 const arches=useMemo(()=>{const b=new Construction(high);const hub=ROOM_BY_ID.control;
  for(const [px,pz] of hub.ports){const room:RoomId=pz<0?"observatory":px<0?"shield":px>0?"greenhouse":"lifeSupport";const c=ROOM_BY_ID[room].color;const yaw=px?Math.PI/2:0;
   for(const sign of [-1,1])b.box([px+Math.cos(yaw)*sign*2.4,2,pz-Math.sin(yaw)*sign*2.4],[.4,4,.5],c,[0,yaw,0],false,true);
   b.box([px,4,pz],[5.2,.45,.5],c,[0,yaw,0],false,true);
   const angle=Math.atan2(px,pz);b.add(new THREE.ConeGeometry(.65,1.5,3),c,[px*.65,.08,pz*.65],[Math.PI/2,0,-angle],[1,1,.08]);
  }b.finish();return b;
 },[high]);useEffect(()=>()=>arches.dispose(),[arches]);
 return <><primitive object={arches.group} dispose={null}/>{MODULES.filter(m=>m.room&&m.id!=="airlock").map(m=><Station key={m.id} room={m.room as RoomId}/>)}
 {ROOM_BY_ID.control.ports.map(([x,z])=>{const r:RoomId=z<0?"observatory":x<0?"shield":x>0?"greenhouse":"lifeSupport";return <Html key={r} position={[x,4.2,z]} center zIndexRange={[1,0]}><span className={`door-symbol region-${r}`}><RegionIcon room={r}/></span></Html>;})}
 <EnergyKnobs/><CommandDisplay/>
 </>;
}
function EnergyKnobs(){
 const refs=useRef<(THREE.Mesh|null)[]>([]);const colors=[P.skyBlue,P.aqua,P.leaf];
 useFrame(()=>{const alloc=useCampaign.getState().alloc;[alloc.shield,alloc.life,alloc.greenhouse].forEach((value,i)=>{const knob=refs.current[i];if(knob)knob.rotation.x=-.3-value/100*.7;});});
 return <group position={[8.15,1.9,-8.15]} rotation-y={-Math.PI/4}>{colors.map((c,i)=><group key={c} position={[(i-1)*1.25,0,0]}><mesh rotation-x={Math.PI/2}><cylinderGeometry args={[.35,.35,.12,12]}/><meshToonMaterial color={c}/></mesh><mesh ref={r=>{refs.current[i]=r;}} position={[0,.1,.15]}><capsuleGeometry args={[.1,.45,4,8]}/><meshToonMaterial color={P.visor}/></mesh></group>)}</group>;
}
function CommandDisplay(){
 const {t}=useT();const sol=useCampaign(s=>s.solIdx);const target=useCampaign(()=>currentTarget());const [,tick]=useState(0);
 useEffect(()=>{const timer=setInterval(()=>tick(v=>v+1),1000);return()=>clearInterval(timer);},[]);
 const c=marsClock(solFraction(runtime.solTime));const campaign=useCampaign.getState();const task=pendingTasks(sol,campaign.results).find(q=>!q.inWorld);
 return <Html position={[8.1,3.6,-8.1]} rotation={[0,-Math.PI/4,0]} transform distanceFactor={7} zIndexRange={[1,0]}><div className="command-display"><strong>{t("hud.sol",{n:sol+1,total:5})} · {String(c.h).padStart(2,"0")}:{String(c.m).padStart(2,"0")}</strong><span>{task?t(`mini.${task.id}.name`):target?t("hud.next",{room:t(`rooms.${target}`)}):t("tasks.none")}</span><div className="command-knobs">{(["shield","life","greenhouse"] as const).map(k=><span key={k}>{t(`sys.${k}`)}<b>{campaign.alloc[k]}</b></span>)}</div></div></Html>;
}
export function WorldLabel(){
 const near=useGame(s=>s.nearStation),area=useGame(s=>s.currentRoom);const [entry,setEntry]=useState<RoomId|null>(area);const ref=useRef<HTMLDivElement>(null);const {t}=useT();
 const blocked=useCampaign(s=>s.energyOpen||s.sealOpen||s.reportOpen||!!s.openTask||s.phase==="summary");
 useEffect(()=>{setEntry(area);const timer=setTimeout(()=>setEntry(null),3500);return()=>clearTimeout(timer);},[area]);
 const id=labelCandidate(near,entry);const m=id?ROOM_BY_ID[id]:null;
 useFrame(({camera,size})=>{if(!ref.current||!m)return;const p=new THREE.Vector3(near?m.entrance[0]:m.x,6.2,near?m.entrance[1]:m.z).project(camera);const x=(p.x+1)*size.width/2,y=(1-p.y)*size.height/2;ref.current.style.visibility=blocked||p.z>1||x<120||x>size.width-190||y<210||y>size.height-220||Math.abs(x-size.width/2)<85&&Math.abs(y-size.height/2)<65?"hidden":"visible";});
 if(!m)return null;
 return <Html position={[near?m.entrance[0]:m.x,6.2,near?m.entrance[1]:m.z]} center zIndexRange={[3,0]}><div ref={ref} className="world-label" key={id}>{near&&id==="control"?t("energy.title"):t(`rooms.${id}`)}</div></Html>;
}