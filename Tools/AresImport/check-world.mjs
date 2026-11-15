import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../..');
const read=f=>JSON.parse(fs.readFileSync(path.join(root,'Assets/ARES/Import',f),'utf8'));
const {meshes}=read('world.json'), layout=read('layout.json');
const {isWalkable,insideInterior}=await import('./generated/game_sim_world.mjs');
// Rasterize upward floor triangles into a spatial index; query actual triangle coverage, not bounds.
const cells=new Map(); let triangles=0, vertices=0, zeroNormals=0;
const key=(x,z)=>`${Math.floor(x)},${Math.floor(z)}`;
for(const m of meshes){
  vertices+=m.vertices.length/3; triangles+=m.indices.length/3;
  for(let n=0;n<m.normals.length;n+=3)if(Math.hypot(...m.normals.slice(n,n+3))<.5)zeroNormals++;
  if(m.layer!=='floor' || (!m.room && !m.name.startsWith('corridor_floor')))continue;
  for(let j=0;j<m.indices.length;j+=3){
    const p=m.indices.slice(j,j+3).map(i=>m.vertices.slice(i*3,i*3+3));
    if(p.some(v=>v[1]<-.001||v[1]>.09))continue;
    const [a,b,c]=p,area=(b[0]-a[0])*(c[2]-a[2])-(b[2]-a[2])*(c[0]-a[0]);
    if(Math.abs(area)<1e-10)continue;
    for(let x=Math.floor(Math.min(...p.map(v=>v[0])));x<=Math.floor(Math.max(...p.map(v=>v[0])));x++)
      for(let z=Math.floor(Math.min(...p.map(v=>v[2])));z<=Math.floor(Math.max(...p.map(v=>v[2])));z++){
        const k=key(x,z);if(!cells.has(k))cells.set(k,[]);cells.get(k).push(p);
      }
  }
}
function covered(x,z){return (cells.get(key(x,z))??[]).some(([a,b,c])=>{
  const e=(u,v)=>(v[0]-u[0])*(z-u[2])-(v[2]-u[2])*(x-u[0]);
  const es=[e(a,b),e(b,c),e(c,a)];return es.every(n=>n>=-1e-6)||es.every(n=>n<=1e-6);
});}
const holes=[], seen=new Set();let samples=0;
function sample(x,z){const k=`${x.toFixed(4)},${z.toFixed(4)}`;if(seen.has(k)||!isWalkable(x,z))return;seen.add(k);samples++;if(!covered(x,z))holes.push([x,z]);}
for(const room of layout.rooms.filter(r=>r.shape!=='park')){
  for(let x=room.x-room.width/2+.6;x<=room.x+room.width/2-.6;x+=.25)
    for(let z=room.z-room.depth/2+.6;z<=room.z+room.depth/2-.6;z+=.25)if(insideInterior(x,z,room))sample(x,z);
  for(const p of room.ports){
    // Airlock south opens deliberately onto terrain; all other thresholds join floor slabs.
    if(room.id==='airlock'&&Math.hypot(p.x,p.z)>Math.hypot(room.x,room.z))continue;
    const length=Math.hypot(p.x-room.x,p.z-room.z),ux=(p.x-room.x)/length,uz=(p.z-room.z)/length;
    for(let across=-2.1;across<=2.1;across+=.1)for(let along=-1;along<=1;along+=.1)
      sample(p.x+ux*along+uz*across,p.z+uz*along-ux*across);
  }
}
for(const p of layout.corridors){const dx=p.b.x-p.a.x,dz=p.b.z-p.a.z,l=Math.hypot(dx,dz);
  for(let s=0;s<=l;s+=.1)for(let w=-2.1;w<=2.1;w+=.3)sample(p.a.x+dx*s/l+dz*w/l,p.a.z+dz*s/l-dx*w/l);
}
const quality=read('mesh-quality.json').meshes.reduce((s,m)=>{for(const k of ['sourceVertices','vertices','sourceTriangles','triangles','degenerate','duplicate'])s[k]=(s[k]??0)+m[k];return s;},{});
const result={pass:holes.length===0&&zeroNormals===0,meshes:meshes.length,vertices,triangles,zeroNormals,floorSamples:samples,holes:holes.length,firstHoles:holes.slice(0,30),quality};
fs.mkdirSync(path.join(root,'Logs'),{recursive:true});fs.writeFileSync(path.join(root,'Logs/ares-world-geometry.json'),JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));if(!result.pass)process.exitCode=1;
