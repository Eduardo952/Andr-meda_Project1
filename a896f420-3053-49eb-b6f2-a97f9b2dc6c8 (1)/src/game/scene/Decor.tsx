import { useMemo,useEffect,useRef } from "react";
import { useFrame } from "@react-three/fiber";

import * as THREE from "three";
import { Construction } from "./Construction";
import { PALETTE as P } from "@/config/palette";
import { useGame } from "@/game/store";
import { runtime } from "@/game/runtime";


export function Decor(){
 const high=useGame(s=>s.settings.quality==="high"),panels=useRef<THREE.InstancedMesh>(null),object=useMemo(()=>new THREE.Object3D(),[]);
 const model=useMemo(()=>{const b=new Construction(high);
  for(let row=0;row<4;row++)for(let col=0;col<8;col++){const x=-21+col*6+(col<4?-1:1),z=72+row*6;b.instance("strut",[x,.8,z],[.3,1.6,.3],P.seam);b.box([x,.05,z],[.09,.08,6],P.dark);}
  for(let i=0;i<20;i++)b.instance("track",[4,.025,63+i*1.2],[.7,.03,.18],P.rust);
  b.box([0,1.2,68],[3,2.4,2],P.armor);b.box([0,2,66.9],[2.5,1,.1],P.sunYellow,[0,0,0],false);b.box([0,.03,65],[.1,.06,9],P.dark);
  b.add(new THREE.CylinderGeometry(10,10,.2,6),P.seam,[43,.1,76]);b.torus([43,.23,76],8,.18,P.sunYellow);
  for(let i=0;i<6;i++){const a=i*Math.PI/3;b.instance("lamp",[43+Math.sin(a)*9,.4,76+Math.cos(a)*9],[.3,.8,.3],P.lamp,[0,0,0],false);}
  b.cylinder([9,8,-42],.22,16,P.seam);b.sphere([9,15,-42],[1.5,.5,1.5],P.milk);
  for(let z=62;z<83;z+=4)for(const side of [-1,1])b.instance("light",[side*3,.3,z],[.25,.6,.25],P.lamp,[0,0,0],false);
  b.finish();return b;
 },[high]);useEffect(()=>()=>model.dispose(),[model]);
 useFrame(()=>{const mesh=panels.current;if(!mesh)return;const pitch=Math.max(-.5,Math.min(.5,-runtime.sunDir[2]*.5));
  for(let i=0;i<32;i++){object.position.set(-21+(i%8)*6+(i%8<4?-1:1),1.5,72+Math.floor(i/8)*6);object.rotation.set(pitch,0,0);object.scale.set(4.8,.15,3.8);object.updateMatrix();mesh.setMatrixAt(i,object.matrix);}mesh.instanceMatrix.needsUpdate=true;
 });
 return <><primitive object={model.group} dispose={null}/><instancedMesh ref={panels} args={[undefined,undefined,32]}><boxGeometry/><meshToonMaterial color={P.solar} /></instancedMesh></>;
}
