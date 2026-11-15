import { TELESCOPE } from "@/config/telescope";
import * as THREE from "three";
import { Construction } from "./Construction";
import { PALETTE as P } from "@/config/palette";
import { COMMAND_CONSOLE as K, consolePoint } from "@/config/rooms";
import type { ModuleDef } from "@/config/rooms";
export function buildAresInterior(m: ModuleDef, high: boolean) {
 const b=new Construction(high),{x,z,color:c}=m;
 const screen=(px:number,pz:number)=>{b.box([px,1.3,pz],[2,1.2,.8],P.milk,[0,0,0],false,true);b.box([px,2,pz+.45],[1.8,1,.1],c);};
 if(m.id==="control"){
  // Command console occupies the north-east wall segment, leaving every port clear.
  for(let i=-2;i<=2;i++){const a=K.angle+i*.11;b.box([Math.sin(a)*12.1,1.15,Math.cos(a)*12.1],[1.8,1.7,1.2],P.milk,[0,a,0],false,true);b.box([Math.sin(a)*K.deskRadius,1.9,Math.cos(a)*K.deskRadius],[1.6,.15,1.2],P.coral,[0,a,0]);}
  const [wx,wz]=consolePoint(K.screenRadius+.2);b.box([wx,K.screenY,wz],[5.4,2.6,.3],P.visor,[0,K.angle,0],false,true);
  // Paired operator seats face the console, with a clear central approach to USE.
  for(const side of [-1,1]){
   const tangent=side*K.seatGap,[sx,sz]=consolePoint(K.seatRadius,tangent),[bx,bz]=consolePoint(K.seatRadius-K.seatBackOffset,tangent);
   b.box([sx,.9,sz],[1.25,.35,1.25],P.coral,[0,K.angle,0],false,true);
   b.box([bx,1.45,bz],[1.25,.9,.25],P.milk,[0,K.angle,0],false,true);
   b.cylinder([sx,.4,sz],.25,.8,P.seam);
  }
  b.torus([0,.07,0],3.4,.035,P.coral);
 }else{
  const [sx,sz]=m.entrance;screen(sx+1.7,sz);
  if(m.id==="lifeSupport"){
   for(const sign of [-1,1])for(let i=0;i<3;i++){const xx=x+sign*10,zz=z-5+i*4;b.cylinder([xx,2,zz],1.1,3.7,c);b.torus([xx,2,zz],1.15,.15,P.milk);b.cylinder([xx-sign*1.4,1,zz],.12,3,P.milk,[0,0,Math.PI/2]);}
  }else if(m.id==="shield"){
   b.cylinder([x-7,4,z-3],1,8,P.armor);for(let i=0;i<7;i++)b.torus([x-7,1+i,z-3],2,.2,c);
   for(let i=0;i<3;i++)b.box([x+6,1,z-5+i*3],[3,1,1.5],P.milk,[0,0,0],false,true);
  }else if(m.id==="observatory"){
   // Pier-mounted telescope: base, column, mount head, tilted tube pointing to the sky, lens ring, eyepiece and finder scope.
   const tx=TELESCOPE.x,tz=TELESCOPE.z,tilt=-.95,dy=Math.cos(tilt),dz=Math.sin(tilt); // tube axis = (0,.54,-.84): up and toward the dome window
   const at=(d:number,r=0):[number,number,number]=>[tx,3.2+dy*d+r,tz+dz*d];
   b.cylinder([tx,.25,tz],1.1,.5,P.seam);b.cylinder([tx,1.5,tz],.3,2.1,P.milk);
   b.box([tx,2.7,tz],[1,.6,.8],c,[0,0,0],false,true);
   for (const side of [-1,1]) { b.box([tx+side*.8,3.3,tz],[.2,1.5,.55],P.milk,[0,0,0],false,true); b.cylinder([tx+side*.7,3.5,tz],.25,.3,c,[0,0,Math.PI/2]); }
   b.box([tx,2.6,tz],[1.8,.25,.6],P.seam,[0,0,0],false,true);
   b.cylinder(at(1.2),.55,3.6,P.milk,[tilt,0,0]);
   b.torus(at(3.0),.58,.1,c,[tilt+Math.PI/2,0,0]);b.cylinder(at(3.05),.5,.06,P.visor,[tilt,0,0]);
   b.cylinder(at(-.8),.16,.7,P.seam,[tilt,0,0]);b.torus(at(-1.15),.2,.05,c,[tilt+Math.PI/2,0,0]);
   b.cylinder(at(1.6,.75),.14,1.6,P.seam,[tilt,0,0]);
   b.torus([tx,.07,tz+2.2],1.05,.05,c,[Math.PI/2,0,0],Math.PI*2,true); // glowing interaction area on the floor
  }else if(m.id==="airlock"){
   for(const side of [-1,1])b.box([x+side*2,1.5,z],[.6,3,6],P.milk,[0,0,0],false,true);
  }
 }
 b.finish();return b;
}
