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
import { THEME } from '@/config/theme';
import { stormForSol } from '@/engine/consequences';

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
 const dust=useRef<THREE.InstancedMesh>(null),steam=useRef<THREE.InstancedMesh>(null),spark=useRef<THREE.InstancedMesh>(null),radar=useRef<THREE.Group>(null),beacons=useRef<THREE.InstancedMesh>(null),heat=useRef<THREE.Mesh>(null),solar=useRef<THREE.InstancedMesh>(null),flare=useRef<THREE.Mesh>(null);
 const object=useMemo(()=>new THREE.Object3D(),[]);
 const count=high?64:24;
 const traces=useRef<THREE.InstancedMesh>(null),lastStep=useRef({x:0,z:0,index:0});
 const footprints=useMemo(()=>Array.from({length:20},()=>({x:0,z:0,heading:0,born:-100})),[]);
 useFrame(()=>{
  const t=runtime.ambienceTime,reduced=useGame.getState().settings.reducedMotion,c=useCampaign.getState();
  const wind=windVector(SOLS[c.solIdx]?.windDir.value?.mostCommon),storm=stormForSol(c.solIdx,SOLS[c.solIdx]?.windSpeed.value?.av??null),storming=storm.active&&c.phase==='tasks',solarActive=!!c.alarm||runtime.emergencyKind==='solar';
  if(dust.current)(dust.current.material as THREE.MeshBasicMaterial).opacity=storming?.42:.18;
  if(flare.current){flare.current.visible=solarActive;flare.current.position.set(runtime.x-45,38,runtime.z-65);flare.current.scale.setScalar(3+(reduced?0:Math.sin(t*2)*.5));}
  for(let i=0;i<count;i++){
   const f=(t*.1+i*.618)%1;
   object.position.set(runtime.x+((i*7.1)%32-16)+wind.x*f*12,.15+(i%7)*.35,runtime.z+((i*5.3)%32-16)+wind.z*f*12);
   const size=storming?2.4:1;object.scale.set((.035+.14*Math.sin(f*Math.PI))*size,.025*size,(.08+.2*Math.sin(f*Math.PI))*size);object.updateMatrix();dust.current?.setMatrixAt(i,object.matrix);
   if(i<6){const drift=(t*.8+i/6)%1;object.position.set(runtime.x-runtime.vx*.12+wind.x*drift*.8+(i%2-.5)*.35,.08+drift*.2,runtime.z-runtime.vz*.12+wind.z*drift*.8);object.scale.setScalar(Math.min(1,runtime.speed/2)*Math.sin(drift*Math.PI)*.18);object.updateMatrix();dust.current?.setMatrixAt(i,object.matrix);}
   object.position.set(10+(i%3-1)*.18,5+f*3,42+Math.sin(i)*.15);object.scale.setScalar(.1+f*.5);object.updateMatrix();steam.current?.setMatrixAt(i,object.matrix);
   const a=i*Math.PI*2/count;
   object.position.set(runtime.x+Math.sin(a)*(1.1+(1-runtime.interaction)*1.5),.5+(i%4)*.5,runtime.z+Math.cos(a)*(1.1+(1-runtime.interaction)*1.5));object.scale.setScalar(runtime.interaction*.09);object.updateMatrix();spark.current?.setMatrixAt(i,object.matrix);
  }
  for(const mesh of [dust.current,steam.current,spark.current])if(mesh){mesh.instanceMatrix.needsUpdate=true;mesh.visible=!reduced;}
  if(dust.current)dust.current.visible=!reduced&&!runtime.interiorId;
  if(!runtime.interiorId&&runtime.speed>.5&&Math.hypot(runtime.x-lastStep.current.x,runtime.z-lastStep.current.z)>.7){const i=lastStep.current.index++%footprints.length,p=footprints[i];if(p){const side=i%2?1:-1;p.x=runtime.x+Math.cos(runtime.heading)*side*.18;p.z=runtime.z-Math.sin(runtime.heading)*side*.18;p.heading=runtime.heading;p.born=t;}lastStep.current.x=runtime.x;lastStep.current.z=runtime.z;}
  footprints.forEach((p,i)=>{const fade=Math.max(0,1-(t-p.born)/12);object.position.set(p.x,.012,p.z);object.rotation.set(0,p.heading,0);object.scale.set(.16*fade,.008,.3*fade);object.updateMatrix();traces.current?.setMatrixAt(i,object.matrix);});object.rotation.set(0,0,0);
  if(traces.current){traces.current.instanceMatrix.needsUpdate=true;traces.current.visible=!reduced;}
  if(radar.current)radar.current.rotation.y=reduced?0:t*.65;
  for(let i=0;i<3;i++){object.position.set(i===0?-44:i===1?0:0,i===0?15:i===1?9:9,i===0?0:i===1?42:-42);object.scale.setScalar(.16+Math.max(0,Math.sin(t*3+i))*.07);object.updateMatrix();beacons.current?.setMatrixAt(i,object.matrix);}
  if(beacons.current)beacons.current.instanceMatrix.needsUpdate=true;
  for(let i=0;i<count;i++){const f=(t*.7+i*.37)%1,a=i*2.4;object.position.set(runtime.x+Math.cos(a)*(8+f*25),4+f*25,runtime.z+Math.sin(a)*(8+f*25));object.rotation.set(.7,0,a);object.scale.set(solarActive?.05:0,solarActive?1.8:0,solarActive?.05:0);object.updateMatrix();solar.current?.setMatrixAt(i,object.matrix);}object.rotation.set(0,0,0);
  if(solar.current){solar.current.instanceMatrix.needsUpdate=true;solar.current.visible=solarActive&&!reduced;}
  if(heat.current){heat.current.visible=high&&!reduced;heat.current.scale.x=1+Math.sin(t*2)*.04;}
 });
 return <>
  {CREW.map((p,i)=>!p.player&&<Crewmate key={p.id} index={i}/>)}
  <instancedMesh ref={dust} args={[undefined,undefined,count]} frustumCulled={false}><sphereGeometry args={[1,6,4]}/><meshBasicMaterial color={THEME.geology.dust} transparent opacity={.18} depthWrite={false}/></instancedMesh>
  <instancedMesh ref={traces} args={[undefined,undefined,20]} frustumCulled={false}><boxGeometry/><meshBasicMaterial color={THEME.geology.basalt} transparent opacity={.22} depthWrite={false}/></instancedMesh>
  <instancedMesh ref={steam} args={[undefined,undefined,count]}><sphereGeometry args={[1,6,4]}/><meshBasicMaterial color={P.milk} transparent opacity={.13} depthWrite={false}/></instancedMesh>
  <instancedMesh ref={spark} args={[undefined,undefined,count]} frustumCulled={false}><octahedronGeometry args={[1,0]}/><meshBasicMaterial color={P.sunYellow}/></instancedMesh>
  <instancedMesh ref={beacons} args={[undefined,undefined,3]}><sphereGeometry args={[1,6,4]}/><meshBasicMaterial color={P.sunYellow} /></instancedMesh>
  <group ref={radar} position={[8,9.3,-42]}><mesh rotation={[.5,0,0]}><boxGeometry args={[2,.15,.6]}/><meshToonMaterial color={P.aqua}/></mesh></group>
  {high&&<mesh ref={heat} position={[10,5.5,42]}><planeGeometry args={[3,1]}/><meshBasicMaterial color={P.lamp} transparent opacity={.025} depthWrite={false} side={THREE.DoubleSide}/></mesh>}
  <instancedMesh ref={solar} args={[undefined,undefined,count]} frustumCulled={false}><cylinderGeometry args={[1,1,1,5]}/><meshBasicMaterial color={THEME.emergency.critical} transparent opacity={.55} depthWrite={false}/></instancedMesh>
  <mesh ref={flare} visible={false}><icosahedronGeometry args={[1,1]}/><meshBasicMaterial color={THEME.emergency.advisory} fog={false}/></mesh>
 </>;
}
