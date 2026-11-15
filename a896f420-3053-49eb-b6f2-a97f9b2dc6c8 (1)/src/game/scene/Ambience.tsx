import { useEffect, useMemo, useRef } from 'react';
import { useFrame } from '@react-three/fiber';
import * as THREE from 'three';
import { CREW } from '@/config/crew';
import { MODULES } from '@/config/rooms';
import { PALETTE as P } from '@/config/palette';
import { runtime } from '@/game/runtime';
import { useGame } from '@/game/store';
import { useCampaign } from '@/game/campaign';
import { SOLS } from '@/data/campaign';
import { crewRoutine, windVector } from '@/game/sim/ambience';
import { Construction } from './Construction';

function Crewmate({ index }: { index: number }) {
 const root=useRef<THREE.Group>(null);
 const model=useMemo(()=>{
  const b=new Construction(false),person=CREW[index];
  b.sphere([0,1,0],[.35,.55,.3],P.milk);b.sphere([0,1.8,0],[.5,.5,.5],P.milk);b.sphere([0,1.8,.3],[.35,.3,.2],P.visor);
  b.box([0,1,.32],[.3,.25,.06],person?.color??P.turquoise);
  for(const x of [-.45,.45])b.cylinder([x,.9,0],.12,.65,person?.color??P.turquoise);
  for(const x of [-.18,.18])b.cylinder([x,.35,0],.14,.65,P.milk);
  b.finish();return b;
 },[index]);
 useEffect(()=>()=>model.dispose(),[model]);
 useFrame(()=>{
  const g=root.current;if(!g)return;
  const routine=crewRoutine(index,runtime.solTime),m=MODULES.find(m=>m.id===routine.area);
  if(!m){g.visible=false;return;}
  g.visible=Math.hypot(runtime.x-m.x,runtime.z-m.z)<16;
  const t=runtime.ambienceTime,indexOffset=index-2;
  g.rotation.set(0,Math.PI,0);g.position.set(m.x+indexOffset*1.4,.12+Math.sin(t*2+index)*.025,m.z-6);g.scale.setScalar(1);
 });
 return <group ref={root}><primitive object={model.group} dispose={null}/></group>;
}
export function Ambience() {
 const high=useGame(s=>s.settings.quality==='high');
 const dust=useRef<THREE.InstancedMesh>(null),steam=useRef<THREE.InstancedMesh>(null),spark=useRef<THREE.InstancedMesh>(null),radar=useRef<THREE.Group>(null),beacons=useRef<THREE.InstancedMesh>(null),heat=useRef<THREE.Mesh>(null);
 const object=useMemo(()=>new THREE.Object3D(),[]);
 const count=high?32:12;
 useFrame(()=>{
  const t=runtime.ambienceTime,reduced=useGame.getState().settings.reducedMotion,c=useCampaign.getState();
  const wind=windVector(SOLS[c.solIdx]?.windDir.value?.mostCommon);
  for(let i=0;i<count;i++){
   const f=(t*.1+i*.618)%1;
   object.position.set(runtime.x+((i*7.1)%24-12)+wind.x*f*12,.3+(i%4)*.35,runtime.z+((i*5.3)%24-12)+wind.z*f*12);
   object.scale.set(.12+.3*f,.06,.12+.3*f);object.updateMatrix();dust.current?.setMatrixAt(i,object.matrix);
   object.position.set(10+(i%3-1)*.18,5+f*3,42+Math.sin(i)*.15);object.scale.setScalar(.1+f*.5);object.updateMatrix();steam.current?.setMatrixAt(i,object.matrix);
   const a=i*Math.PI*2/count;
   object.position.set(runtime.x+Math.sin(a)*(1.1+(1-runtime.interaction)*1.5),.5+(i%4)*.5,runtime.z+Math.cos(a)*(1.1+(1-runtime.interaction)*1.5));object.scale.setScalar(runtime.interaction*.09);object.updateMatrix();spark.current?.setMatrixAt(i,object.matrix);
  }
  for(const mesh of [dust.current,steam.current,spark.current])if(mesh){mesh.instanceMatrix.needsUpdate=true;mesh.visible=!reduced;}
  if(dust.current)dust.current.visible=!reduced&&!runtime.interiorId;
  if(radar.current)radar.current.rotation.y=reduced?0:t*.65;
  for(let i=0;i<3;i++){object.position.set(i===0?-44:i===1?0:0,i===0?15:i===1?9:9,i===0?0:i===1?42:-42);object.scale.setScalar(.16+Math.max(0,Math.sin(t*3+i))*.07);object.updateMatrix();beacons.current?.setMatrixAt(i,object.matrix);}
  if(beacons.current)beacons.current.instanceMatrix.needsUpdate=true;
  if(heat.current){heat.current.visible=high&&!reduced;heat.current.scale.x=1+Math.sin(t*2)*.04;}
 });
 return <>
  {CREW.map((p,i)=>!p.player&&<Crewmate key={p.id} index={i}/>)}
  <instancedMesh ref={dust} args={[undefined,undefined,count]} frustumCulled={false}><sphereGeometry args={[1,6,4]}/><meshBasicMaterial color={P.sand} transparent opacity={.14} depthWrite={false}/></instancedMesh>
  <instancedMesh ref={steam} args={[undefined,undefined,count]}><sphereGeometry args={[1,6,4]}/><meshBasicMaterial color={P.milk} transparent opacity={.13} depthWrite={false}/></instancedMesh>
  <instancedMesh ref={spark} args={[undefined,undefined,count]} frustumCulled={false}><octahedronGeometry args={[1,0]}/><meshBasicMaterial color={P.sunYellow}/></instancedMesh>
  <instancedMesh ref={beacons} args={[undefined,undefined,3]}><sphereGeometry args={[1,6,4]}/><meshBasicMaterial color={P.sunYellow} /></instancedMesh>
  <group ref={radar} position={[8,9.3,-42]}><mesh rotation={[.5,0,0]}><boxGeometry args={[2,.15,.6]}/><meshToonMaterial color={P.aqua}/></mesh></group>
  {high&&<mesh ref={heat} position={[10,5.5,42]}><planeGeometry args={[3,1]}/><meshBasicMaterial color={P.lamp} transparent opacity={.025} depthWrite={false} side={THREE.DoubleSide}/></mesh>}
 </>;
}
