import { useEffect,useMemo,useRef } from "react";
import { useFrame } from "@react-three/fiber";

import * as THREE from "three";
import { MODULES } from "@/config/rooms";
import { useGame } from "@/game/store";
import { runtime } from "@/game/runtime";

import { roofOpacity } from "@/game/sim/visibility";
import { buildStructure,buildTunnels } from "./AresStructure";
import { Greenhouse, buildGreenhouseShell } from "./Greenhouse";
import { buildAresInterior } from "./AresInteriors";
import { useNearbyInteriors } from "./InteriorRooms";
import type { ModuleDef } from "@/config/rooms";
function Module({m,high,near}:{m:ModuleDef;high:boolean;near:boolean}){
 const model=useMemo(()=>m.id==="greenhouse"?buildGreenhouseShell(high):buildStructure(m,high),[m,high]);
 const interior=useMemo(()=>near&&m.id!=="greenhouse"?buildAresInterior(m,high):null,[m,high,near]);

 useEffect(()=>()=>model.dispose(),[model]);useEffect(()=>()=>interior?.dispose(),[interior]);
 useFrame(({camera},delta)=>{
  const settings=useGame.getState().settings,occupied=runtime.interiorId===m.id;
  const opacity=m.id==="greenhouse"?(settings.roofs==="off"||settings.isometric||runtime.camera.pitch>Math.PI/3?0:occupied?.025:.22):roofOpacity(settings.roofs,occupied,settings.isometric,runtime.camera.pitch);
  model.roof.group.visible=opacity>0;
  model.roof.group.userData["cutawayOpacity"]=opacity;
  model.roof.group.traverse(o=>{if(o instanceof THREE.Mesh){const mats=Array.isArray(o.material)?o.material:[o.material];for(const mat of mats){mat.transparent=true;mat.opacity=opacity;mat.depthWrite=false;}}});
  const cameraDistance=Math.hypot(camera.position.x-m.x,camera.position.z-m.z);
  const playerDistance=Math.hypot(runtime.x-m.x,runtime.z-m.z);
  const obstruction=occupied||cameraDistance<Math.max(m.width,m.depth)/2+3&&playerDistance<30||settings.isometric;
  for(const mat of model.walls.materials.values())mat.opacity+=( (obstruction?.09:1)-mat.opacity)*(1-Math.exp(-7*Math.min(delta,.05)));

 });
 return <>{m.id==="greenhouse"&&near&&<Greenhouse high={high}/>}<primitive object={model.floor.group} dispose={null}/><primitive object={model.walls.group} dispose={null}/><primitive object={model.roof.group} dispose={null}/>{interior&&<primitive object={interior.group} dispose={null}/>}</>;
}
export function Base(){
 const high=useGame(s=>s.settings.quality==="high"),nearby=useNearbyInteriors();
 const tunnels=useMemo(()=>buildTunnels(high),[high]);
 useEffect(()=>()=>tunnels.dispose(),[tunnels]);
 useFrame(()=>{const st=useGame.getState().settings;
  tunnels.shell.group.visible=st.roofs!=="off";
  for(const mat of tunnels.shell.materials.values())mat.opacity=st.isometric?.025:.1;

 });
 return <><primitive object={tunnels.floor.group} dispose={null}/><primitive object={tunnels.shell.group} dispose={null}/>{MODULES.filter(m=>m.shape!=="park").map(m=><Module key={m.id} m={m} high={high} near={nearby.includes(m.id)}/>)}</>;
}
