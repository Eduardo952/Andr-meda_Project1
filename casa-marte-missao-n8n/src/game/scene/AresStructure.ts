import * as THREE from "three";
import { Construction } from "./Construction";
import { PALETTE as P } from "@/config/palette";
import { CORRIDORS, type ModuleDef } from "@/config/rooms";

export function buildStructure(m: ModuleDef, high: boolean) {
  const floor = new Construction(high), walls = new Construction(high), roof = new Construction(high);
  const { x, z, width: w, depth: d, height: h, color: c } = m;
  if(m.shape==="hub"||m.shape==="dome"){floor.cylinder([x,-.25,z],w/2,.5,P.seam);floor.cylinder([x,.015,z],w/2-.2,.03,P.sand);}else{floor.box([x,-.25,z],[w,.5,d],P.seam);floor.box([x,.015,z],[w-.4,.03,d-.4],P.sand);}
  const circle=m.shape==="hub"||m.shape==="dome";
  for(let xx=-w/2+2;xx<w/2;xx+=2){
    const span=circle?2*Math.sqrt(Math.max(0,(w/2-.5)**2-xx**2)):d-.7;
    floor.box([x+xx,.04,z],[.025,.015,span],P.seam);
  }
  for(let zz=-d/2+2;zz<d/2;zz+=2){
    const span=circle?2*Math.sqrt(Math.max(0,(d/2-.5)**2-zz**2)):w-.7;
    floor.box([x,.04,z+zz],[span,.015,.025],P.seam);
  }
  const circular=m.shape==="hub"||m.shape==="dome";
  const n=circular?32:4;
  for(let i=0;i<n;i++){
    const angle=circular?i*Math.PI*2/n:i*Math.PI/2;
    const px=x+Math.sin(angle)*w/2,pz=z+Math.cos(angle)*d/2;
    const length=circular?Math.PI*w/n:(i%2?d:w);
    const port=m.ports.some(([a,b])=>Math.hypot(a-px,b-pz)<(circular?3:1));
    if(port&&!circular){
      const side=(length-5)/2;
      for(const sign of [-1,1]) walls.box([px+Math.cos(angle)*sign*(2.5+side/2),2,pz-Math.sin(angle)*sign*(2.5+side/2)],[side,4,.35],P.milk,[0,angle,0],false,true);
      walls.box([px,4.2,pz],[5,.65,.5],c,[0,angle,0]);
    }else if(!port){
      floor.box([px,.32,pz],[length,.55,.42],c,[0,angle,0]);
      walls.box([px,2.2,pz],[length,3.4,.28],P.milk,[0,angle,0],false,true);
      walls.box([px,3.8,pz],[length,.4,.42],c,[0,angle,0]);
    }
  }
  if(m.id==="greenhouse"){
    const geo=new THREE.IcosahedronGeometry(w/2,high?2:1);
    const pos=geo.getAttribute("position");
    for(let i=0;i<pos.count;i++)pos.setY(i,Math.abs(pos.getY(i))*.64);
    pos.needsUpdate=true;geo.computeVertexNormals();
    const mesh=new THREE.Mesh(geo,roof.material(P.glass));mesh.position.set(x,.4,z);roof.group.add(mesh);
    const lines=new THREE.LineSegments(new THREE.EdgesGeometry(geo),new THREE.LineBasicMaterial({color:P.armor,transparent:true,opacity:.65}));lines.position.copy(mesh.position);roof.group.add(lines);
  }else if(circular){
    roof.add(new THREE.SphereGeometry(1,high?32:20,8,0,Math.PI*2,0,Math.PI/2),P.milk,[x,4.3,z],[0,0,0],[w/2,2,d/2]);
    roof.torus([x,5,z],w*.34,.3,c);roof.torus([x,4.3,z],w/2,.22,P.armor);
  }else{
    roof.add(new THREE.CylinderGeometry(1,1,1,high?24:12,1,true,0,Math.PI),P.milk,[x,4,z],[0,0,Math.PI/2],[2,w,d/2]);
    for(let i=0;i<6;i++)roof.add(new THREE.TorusGeometry(1,.025,4,high?20:12,Math.PI),P.armor,[x-w/2+i*w/5,4,z],[0,-Math.PI/2,0],[d/2,2,1]);
  }
  if(circular&&m.shape!=="hub"){for(let i=0;i<12;i++){const a=i*Math.PI/6+Math.PI/12,bx=x+Math.sin(a)*(w/2+.9),bz=z+Math.cos(a)*(d/2+.9);if(!m.ports.some(([px,pz])=>Math.hypot(px-bx,pz-bz)<4))floor.instance("berm",[bx,.2,bz],[1.5,.4,.9],P.sand,[0,a,0]);floor.instance("bolt",[x+Math.sin(a)*w/2,.65,z+Math.cos(a)*d/2],[.12,.12,.12],P.seam,[Math.PI/2,0,0]);}}
  else for(const side of [-1,1])for(let i=0;i<5;i++){
    const xx=x-w*.4+i*w*.2;
    floor.instance("leg",[xx,-.6,z+side*d*.42],[.65,1.2,.65],P.seam);
    floor.instance("bolt",[xx,.65,z+side*d/2],[.12,.12,.12],P.seam,[Math.PI/2,0,0]);
    if(!m.ports.some(([px,pz])=>Math.hypot(px-xx,pz-(z+side*d/2))<3))floor.instance("berm",[xx,.22,z+side*(d/2+1)],[1.7,.4,1],P.sand);
  }
  if(m.id==="observatory"){roof.sphere([x+7,6,z-2],[3.1,2.2,3.1],P.glass);}
  if(!circular)for(const sign of [-1,1])for(let i=0;i<6;i++)floor.instance("panel-seam",[x-w*.4+i*w*.16,1,z+sign*d/2],[.14,1.4,.5],P.seam);
  floor.finish();walls.finish();roof.finish();
  for(const mat of walls.materials.values()){mat.transparent=true;mat.depthWrite=false;}
  return {floor,walls,roof,dispose:()=>{floor.dispose();walls.dispose();roof.dispose();}};
}

export function buildTunnels(high:boolean){
  const floor=new Construction(high),shell=new Construction(high);
  for(const p of CORRIDORS){
    const length=Math.hypot(p.b.x-p.a.x,p.b.z-p.a.z),yaw=Math.atan2(p.b.x-p.a.x,p.b.z-p.a.z),x=(p.a.x+p.b.x)/2,z=(p.a.z+p.b.z)/2;
    floor.box([x,-.15,z],[5,.3,length],P.sand,[0,yaw,0]);
    for(const sign of [-1,1]){
      floor.box([x+Math.cos(yaw)*sign*2.5,.3,z-Math.sin(yaw)*sign*2.5],[.22,.6,length],P.milk,[0,yaw,0]);
      shell.box([x+Math.cos(yaw)*sign*2.5,2,z-Math.sin(yaw)*sign*2.5],[.18,3.4,length],P.glass,[0,yaw,0]);
    }
    shell.box([x,4,z],[5,.15,length],P.glass,[0,yaw,0]);
    for(let t=1;t<length;t+=3){const xx=p.a.x+Math.sin(yaw)*t,zz=p.a.z+Math.cos(yaw)*t;for(const sign of [-1,1])floor.instance("strut",[xx+Math.cos(yaw)*sign*2.4,2,zz-Math.sin(yaw)*sign*2.4],[.16,4,.16],P.seam);floor.instance("light",[xx,.1,zz],[.6,.04,.3],P.seam,[0,yaw,0],false);}
  }
  floor.finish();shell.finish();return{floor,shell,dispose:()=>{floor.dispose();shell.dispose();}};
}