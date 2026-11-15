// Offline, reproducible geometry/data extraction. Never executes the reference's server code.
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { stripTypeScriptTypes } from 'node:module';
import * as THREE from 'three';
import ts from 'typescript';
import { createCanvas } from '@napi-rs/canvas';
import { createHash } from 'node:crypto';
import { exportConsequenceFixtures } from './consequence-fixtures.mjs';
const root=path.resolve(import.meta.dirname,'../..');
const reference='a896f420-3053-49eb-b6f2-a97f9b2dc6c8 (4)';
const source=path.join(root,reference,'src');
// Only the local procedural texture functions use this Canvas adapter.
globalThis.document={createElement(tag){if(tag!=='canvas')throw Error(`Unsupported element: ${tag}`);return createCanvas(1,1);}};
const cache=path.join(import.meta.dirname,'generated'); fs.mkdirSync(cache,{recursive:true});
const output=path.join(root,'Assets/ARES/Import'); fs.mkdirSync(output,{recursive:true});
const files=['config/theme','config/palette','config/rooms','config/telescope','config/balance','engine/rounding','engine/flare','engine/demand','engine/outcome','engine/normalize','engine/consequences','tasks/assignments','game/sim/world','game/terrain','game/scene/surfaceTextures','game/scene/Construction','game/scene/AresStructure','game/scene/AresInteriors'];
function emit(id,code){
 code=stripTypeScriptTypes(code,{mode:'strip'}).replace(/from\s+["'](@\/[^"']+|\.\.?\/[^"']+)["']/g,(_,p)=>{
 const resolved=p.startsWith('@/')?p.slice(2):path.posix.normalize(path.posix.join(path.posix.dirname(id),p));
 return `from './${resolved.replaceAll('/','_')}.mjs'`;
 });
 fs.writeFileSync(path.join(cache,id.replaceAll('/','_')+'.mjs'),code);
}
for(const id of files)emit(id,fs.readFileSync(path.join(source,id+'.ts'),'utf8'));
let materials=fs.readFileSync(path.join(source,'game/scene/materials.tsx'),'utf8');
emit('game/scene/materials',materials.slice(0,materials.indexOf('export function Toon'))+materials.slice(materials.indexOf('export function seeded')));
let greenhouse=fs.readFileSync(path.join(source,'game/scene/Greenhouse.tsx'),'utf8');
greenhouse=greenhouse.slice(0,greenhouse.indexOf('export function Greenhouse(')).replace(/^import .*?(?:from ["'][^"']+["']);?\r?\n/gm,line=>/Construction|three["']|materials|config\/theme|config\/rooms/.test(line)?line:'');
const apples=fs.readFileSync(path.join(source,'game/greenhouse.ts'),'utf8');
greenhouse=greenhouse.replace('const G =',apples.slice(apples.indexOf('export const APPLE_COUNT'),apples.indexOf('/** Presentation'))+'\nconst G =').replace('function makeGarden','export function makeGarden');
emit('game/scene/Greenhouse',greenhouse);
const get=id=>import(pathToFileURL(path.join(cache,id.replaceAll('/','_')+'.mjs')));
const {MODULES,CORRIDORS,PATHS}=await get('config/rooms');
const {buildStructure,buildTunnels}=await get('game/scene/AresStructure');
const {buildAresInterior}=await get('game/scene/AresInteriors');
const {buildGreenhouseShell,makeGarden}=await get('game/scene/Greenhouse');
const groups=[]; const meshes=[];
const textureFiles=new Map();
function textureFile(texture){
 if(!texture?.image?.toBuffer)return '';
 if(!textureFiles.has(texture.image)){
  const bytes=texture.image.toBuffer('image/png');
  const name='texture-'+createHash('sha256').update(bytes).digest('hex').slice(0,16)+'.png';
  fs.writeFileSync(path.join(output,name),bytes);textureFiles.set(texture.image,name);
 }
 return textureFiles.get(texture.image);
}
function add(name,group,room='',layer='floor'){
 group.updateMatrixWorld(true); const first=meshes.length;
 group.traverse(obj=>{
 if(!obj.isMesh)return;
 for(let ancestor=obj;ancestor;ancestor=ancestor.parent)if(!ancestor.visible)return;
 const count=obj.isInstancedMesh?obj.count:1;
 for(let i=0;i<count;i++){
 const matrix=obj.matrixWorld.clone(); if(obj.isInstancedMesh){const instance=new THREE.Matrix4();obj.getMatrixAt(i,instance);matrix.multiply(instance);}
 const geo=obj.geometry.clone().applyMatrix4(matrix); const pos=geo.getAttribute('position'),norm=geo.getAttribute('normal');
 const mat=obj.material; if(Array.isArray(mat))throw Error('Material groups need explicit handling');
 meshes.push({name:name+'_'+meshes.length,room,layer,vertices:Array.from(pos.array),normals:norm?Array.from(norm.array):[],uv:geo.getAttribute('uv')?Array.from(geo.getAttribute('uv').array):[],colors:geo.getAttribute('color')?Array.from(geo.getAttribute('color').array):[],indices:geo.index?Array.from(geo.index.array):Array.from({length:pos.count},(_,j)=>j),color:'#'+mat.color.getHexString(),emissive:mat.emissive?'#'+mat.emissive.getHexString():'#000000',glow:mat.emissiveIntensity??0,opacity:mat.opacity,transparent:mat.transparent,unlit:!!mat.isMeshBasicMaterial,metalness:mat.metalness??0,roughness:mat.roughness??.8,depthWrite:mat.depthWrite,side:mat.side,map:textureFile(mat.map),bumpMap:textureFile(mat.bumpMap),bumpScale:mat.bumpScale??0,repeat:mat.map?.repeat.toArray()??[1,1]}); geo.dispose();
 }
 });groups.push({name,first,count:meshes.length-first});
}
for(const m of MODULES.filter(m=>m.shape!=='park')){
 const built=m.id==='greenhouse'?buildGreenhouseShell(true):buildStructure(m,true);
 for(const layer of ['floor','walls','roof'])add(m.id+'_'+layer,built[layer].group,m.id,layer);
 add(m.id+'_interior',m.id==='greenhouse'?makeGarden(true).b.group:buildAresInterior(m,true).group,m.id,'interior');
}
const tunnels=buildTunnels(true);add('corridor_floor',tunnels.floor.group);add('corridor_shell',tunnels.shell.group,'','roof');
// JSX model extraction executes only visual components, with a tiny React element adapter.
const {PALETTE}=await get('config/palette'); const {THEME}=await get('config/theme');
const frameCallbacks=[];
function element(type,props,...children){
 props??={}; children=children.flat(Infinity).filter(Boolean);
 if(typeof type==='function')return type({...props,children});
 if(type===Symbol.for('fragment')){const g=new THREE.Group();g.add(...children);return g;}
 if(type==='primitive')return props.object;
 const ctor=THREE[type[0].toUpperCase()+type.slice(1)];
 let object;
 if(type.endsWith('Geometry'))return new ctor(...(props.args??[]));
 if(type.endsWith('Material')){const {ref,children,...settings}=props;object=new ctor(settings);}
 else {
 object=type==='group'?new THREE.Group():type==='instancedMesh'?new THREE.InstancedMesh(undefined,undefined,props.args[2]):new THREE.Mesh();
 for(const child of children){if(child.isBufferGeometry)object.geometry=child;else if(child.isMaterial)object.material=child;else if(child.isObject3D)object.add(child);}
 for(const field of ['position','rotation','scale'])if(props[field]!=null){if(typeof props[field]==='number')object[field].setScalar(props[field]);else object[field].set(...props[field]);}
 for(const axis of ['x','y','z'])if(props['rotation-'+axis]!=null)object.rotation[axis]=props['rotation-'+axis];
  if(props.geometry)object.geometry=props.geometry;
  if(props.visible!=null)object.visible=props.visible;
 }
 if(props.ref){props.ref.current=object;if(props.ref.binding && object.isObject3D)object.userData.binding=props.ref.binding;}return object;
}
const React={createElement:element,Fragment:Symbol.for('fragment')};
let refNames=[], capturedRefs={};
const hooks={useRef:value=>{const binding=refNames.shift();const ref={current:value,binding};if(binding)capturedRefs[binding]=ref;return ref;},useMemo:f=>f(),useEffect:()=>{},useFrame:f=>frameCallbacks.push(f)};
const runtime={ambienceTime:0,mood:'happy',sunDir:[0,1,0],x:0,z:4,heading:Math.PI,speed:0,walkPhase:0,jump:0,observe:0,observing:false,interiorId:'control'};
const {BALANCE}=await get('config/balance');
const {TELESCOPE}=await get('config/telescope');
const {Construction}=await get('game/scene/Construction');
const {regolithMaps,solarCellMap}=await get('game/scene/surfaceTextures');
const {terrainClearance,terrainHeight}=await get('game/terrain');
const consequences=await get('engine/consequences');
const {seeded}=await get('game/scene/materials');
const inspectionCampaign={solIdx:0,phase:'report',panelDust:[0,0,0,0],cleanedThisSol:false};
const useCampaign={getState:()=>inspectionCampaign};
Object.assign(runtime,{cleaning:0,cleaningSol:-1,cleaningFrom:[0,0,0,0],observeArrived:false});
function model(file,exportName,cut,props={},reducedMotion=false){
 let code=fs.readFileSync(path.join(source,file),'utf8'); if(cut)code=code.slice(0,code.indexOf(cut));
 code=code.replace(/^import[^;]+;\r?\n/gm,'').replaceAll('export function','function');
 const js=ts.transpileModule(code,{compilerOptions:{target:ts.ScriptTarget.ES2022,jsx:ts.JsxEmit.React}}).outputText;
 const fn=new Function('React','THREE','PALETTE','P','THEME','Toon','Outlines','useRef','useMemo','useEffect','useFrame','runtime','Construction','useGame','BALANCE','TELESCOPE','sfx','props','solarCellMap','useCampaign','panelDustAfterStorm','stormForSol','SOLS','T','regolithMaps','terrainClearance','terrainHeight','seeded',js+`;return ${exportName}(props);`);
 return fn(React,THREE,PALETTE,PALETTE,THEME,({children,...p})=>new THREE.MeshToonMaterial(p),()=>null,hooks.useRef,hooks.useMemo,hooks.useEffect,hooks.useFrame,runtime,Construction,fn=>fn({settings:{quality:'high',reducedMotion}}),BALANCE,TELESCOPE,{step(){},use(){},clean(){}},props,solarCellMap,useCampaign,consequences.panelDustAfterStorm,consequences.stormForSol,[],THEME,regolithMaps,terrainClearance,terrainHeight,seeded);
}
const bindings=Object.fromEntries(['bodyRef','lArm','rArm','lLeg','rLeg'].map(binding=>[binding,{current:null,binding}]));
const astronaut=model('game/characters/Astronaut.tsx','AstronautModel','const DUST',bindings); add('astronaut',astronaut,'astronaut','character');
frameCallbacks.length=0; refNames=['fish','tail','mouth','fin','bubbles','eyes'];
const bubble=model('game/characters/Bubble.tsx','BubbleModel','export function Bubble3D'); for(const callback of frameCallbacks)callback({clock:{elapsedTime:0}},0); add('bubble',bubble,'bubble','character');
// Keep local geometry and every parent/pivot for animated models. Never infer limbs from vertex positions.
function character(id,root){
 const nodes=[], localMeshes=[];
 function visit(object,parent){
  const index=nodes.length, mesh=object.isMesh?localMeshes.length:-1;
  nodes.push({name:object.userData.binding||`${id}_${index}`,binding:object.userData.binding||'',parent,mesh,position:object.position.toArray(),rotation:object.quaternion.toArray(),scale:object.scale.toArray()});
  if(object.isMesh){
   const geo=object.geometry, mat=object.material, pos=geo.getAttribute('position'),norm=geo.getAttribute('normal');
   localMeshes.push({name:`${id}_rig_${mesh}`,vertices:Array.from(pos.array),normals:norm?Array.from(norm.array):[],indices:geo.index?Array.from(geo.index.array):Array.from({length:pos.count},(_,i)=>i),color:'#'+mat.color.getHexString(),emissive:mat.emissive?'#'+mat.emissive.getHexString():'#000000',glow:mat.emissiveIntensity??0,opacity:mat.opacity,transparent:mat.transparent,unlit:!!mat.isMeshBasicMaterial,metalness:mat.metalness??0,roughness:mat.roughness??.8,depthWrite:mat.depthWrite});
  }
  for(const child of object.children)visit(child,index);
 }
 visit(root,-1);return {id,nodes,meshes:localMeshes};
}
fs.writeFileSync(path.join(output,'characters.json'),JSON.stringify({characters:[character('astronaut',astronaut),character('bubble',bubble)]}));
const bindGeometry=meshes.filter(m=>m.layer==='character').map(m=>({id:m.room,vertices:m.vertices}));
frameCallbacks.length=0; const decor=model('game/scene/Decor.tsx','Decor');for(const callback of frameCallbacks)callback({clock:{elapsedTime:0}},0);add('decor',decor);
// Execute the updated terrain component itself: relief, vertex colors, geology and textures.
const {isWalkable}=await get('game/sim/world');
frameCallbacks.length=0;add('landscape',model('game/scene/Terrain.tsx','Terrain'));
const worldMeshes=meshes.filter(m=>m.layer!=='character');
fs.writeFileSync(path.join(output,'world.json'),JSON.stringify({meshes:worldMeshes}));
const {normalizeSnapshot,deriveSol}=await get('engine/normalize');
const {assignTasks}=await get('tasks/assignments');
const sols=Array.from({length:5},(_,i)=>{const raw=JSON.parse(fs.readFileSync(path.join(source,`data/snapshots/sol${i+1}.json`),'utf8')); const n=normalizeSnapshot(raw);return {sol:n.sol,date:n.earthDate,sample:n.sample,capturedAt:raw.captured_at,...deriveSol(n),facts:{flareClass:n.flare.value?.classType??'',hasCmeSpeed:n.cme.value!==null,cmeSpeed:n.cme.value?.speed??0,hasAirTempMean:n.airTemp.value!==null,airTempMean:n.airTemp.value?.av??0,hasWindSpeed:n.windSpeed.value!==null,windSpeed:n.windSpeed.value?.av??0},tasks:assignTasks(n.sol),raw:JSON.stringify(raw)};});
fs.writeFileSync(path.join(output,'campaign.json'),JSON.stringify({sols}));
const {INTERIOR_PATHS}=await get('config/rooms');
fs.writeFileSync(path.join(output,'layout.json'),JSON.stringify({rooms:MODULES.map(m=>({...m,ports:m.ports.map(([x,z])=>({x,z})),stationX:m.entrance[0],stationZ:m.entrance[1]})),paths:[...PATHS,...INTERIOR_PATHS],corridors:CORRIDORS}));
fs.writeFileSync(path.join(output,'balance.json'),JSON.stringify(BALANCE));
fs.writeFileSync(path.join(output,'telescope.json'),JSON.stringify(TELESCOPE));
for(const locale of ['pt-BR','en'])fs.copyFileSync(path.join(source,`locales/${locale}.json`),path.join(output,locale+'.json'));
let firstWorldMesh=0;
const worldGroups=groups.filter(g=>g.name!=='astronaut'&&g.name!=='bubble').map(g=>{const group={...g,first:firstWorldMesh};firstWorldMesh+=g.count;return group;});
const sourceHashes=fs.readdirSync(source,{recursive:true}).filter(f=>/\.(ts|tsx|json|css)$/.test(f)).sort().map(file=>({file:file.replaceAll('\\','/'),sha256:createHash('sha256').update(fs.readFileSync(path.join(source,file))).digest('hex')}));
const manifest={reference,generator:'Tools/AresImport/export.mjs',three:THREE.REVISION,sourceHashes,groups:worldGroups,meshCount:meshes.length,worldMeshCount:worldMeshes.length,characterMeshCount:meshes.length-worldMeshes.length};
const {shieldDemand,thermalDemand}=await get('engine/demand');
emit('game/sim/camera',fs.readFileSync(path.join(source,'game/sim/camera.ts'),'utf8'));
const cameraModule=await get('game/sim/camera');
const camera=[];const cameraRig=cameraModule.createCameraRig();
for(let i=0;i<120;i++){
 const dx=Math.sin(i*.7)*15,dy=Math.cos(i*.3)*8,factor=i%15===0?1.2:1,dt=.016,isometric=i>=80;
 cameraModule.orbitCamera(cameraRig,dx,dy,1,false,isometric);cameraModule.zoomCamera(cameraRig,factor);cameraModule.stepCameraRig(cameraRig,dt,isometric);
 const position=cameraModule.cameraPosition(cameraRig,0,4,true);
 camera.push({dx,dy,factor,dt,isometric,yaw:cameraRig.yaw,pitch:cameraRig.pitch,distance:cameraRig.distance,x:position.x,y:position.y,z:position.z});
}
const navigation=[];for(let x=-63;x<79;x+=1.37)for(let z=-59;z<101;z+=1.73){const px=Math.fround(x),pz=Math.fround(z);navigation.push({x:px,z:pz,walkable:isWalkable(px,pz)});}
const rules=[];for(const flare of ['', 'A','B','C','M','X'])for(const magnitude of [1,2,5,9])for(const speed of [-1,400,900,1800])for(const temperature of [-100,-60,0,20])rules.push({flare,magnitude,speed,temperature,shield:shieldDemand(flare?{letter:flare,magnitude}:null,speed<0?null:speed),thermal:thermalDemand(temperature)});
fs.writeFileSync(path.join(output,'parity.json'),JSON.stringify({navigation,rules,camera}));
// Evaluate the actual reference callbacks as the oracle, rather than duplicating formulas in JS tests.
const astronautFrames=[],bubbleFrames=[];
const capture=(binding,object)=>({binding,position:object.position.toArray(),rotation:object.quaternion.toArray(),scale:object.scale.toArray()});
for(const reduced of [false,true]){
 frameCallbacks.length=0;capturedRefs={};refNames=['root','bodyRef','lArm','rArm','lLeg','rLeg','pack','dust','next','lastStep'];
 runtime.observe=0;
 model('game/characters/Astronaut.tsx','Astronaut',undefined,{},reduced);
 const callback=frameCallbacks[0];
 for(let i=0;i<100;i++){
  const elapsed=i*.071,dt=i%3===0?.1:.016,phase=i*.39,speed=i<15?0:i<65?5.2:1.7;
  Object.assign(runtime,{x:i*.01,z:4,heading:Math.PI-i*.02,speed,walkPhase:phase,ambienceTime:reduced?0:elapsed,jump:i>=50&&i<65?1.2-(i-50)*.08:0,observing:i>=70&&i<85,observeArrived:i>=75});
  callback({clock:{elapsedTime:elapsed}},dt);
  astronautFrames.push({reduced,elapsed,dt,phase,speed,x:runtime.x,z:runtime.z,heading:runtime.heading,ambience:runtime.ambienceTime,jump:runtime.jump,observing:runtime.observing,observeArrived:runtime.observeArrived,observe:runtime.observe,poses:['root','bodyRef','lArm','rArm','lLeg','rLeg'].map(k=>capture(k,capturedRefs[k].current))});
 }
}
for(const mood of ['happy','worried','excited','proud'])for(const talking of [false,true])for(const wave of [false,true]){
 frameCallbacks.length=0;capturedRefs={};refNames=['fish','tail','mouth','fin','bubbles','eyes'];
 model('game/characters/Bubble.tsx','BubbleModel','export function Bubble3D',{talking,wave});
 for(const time of [0,.2,1.1,2.9,4.13,4.29,4.31,9.7]){
  Object.assign(runtime,{ambienceTime:time,mood});frameCallbacks[0]({clock:{elapsedTime:time}},.016);
  const poses=['fish','tail','mouth','fin','bubbles','eyes'].map(k=>capture(k,capturedRefs[k].current));
  capturedRefs.bubbles.current.children.forEach((b,i)=>poses.push(capture('air'+i,b)));
  bubbleFrames.push({time,mood,talking,wave,poses});
 }
}
fs.writeFileSync(path.join(output,'character-parity.json'),JSON.stringify({bindGeometry,astronaut:astronautFrames,bubble:bubbleFrames}));
await exportConsequenceFixtures({source,output,get,emit,BALANCE,sols});
// Publish the manifest last. A failed extraction cannot silently mix generations in Unity.
manifest.outputHashes=fs.readdirSync(output).filter(f=>/\.(json|png)$/.test(f)&&f!=='manifest.json').sort().map(file=>({file,sha256:createHash('sha256').update(fs.readFileSync(path.join(output,file))).digest('hex')}));
fs.writeFileSync(path.join(output,'manifest.json'),JSON.stringify(manifest,null,2));
console.log(`Exported ${meshes.length} meshes, ${MODULES.length} modules, ${sols.length} Sols.`);




