import { useEffect, useMemo } from "react";
import { useFrame, type ThreeEvent } from "@react-three/fiber";
import * as THREE from "three";
import { Construction, type Position } from "./Construction";
import { toonGradient } from "./materials";
import { THEME as T } from "@/config/theme";
import { ROOM_BY_ID } from "@/config/rooms";
import { useCampaign } from "@/game/campaign";
import { useGame } from "@/game/store";
import { runtime } from "@/game/runtime";
import { DERIVED } from "@/data/campaign";
import { APPLES, gardenVisuals, useGarden } from "@/game/greenhouse";

const G = T.garden;

export function buildGreenhouseShell(high: boolean) {
  const floor = new Construction(high), walls = new Construction(high), roof = new Construction(high);
  const m = ROOM_BY_ID.greenhouse;
  floor.box([52, -.2, 0], [40, .4, 32], T.trim);
  floor.box([52, .02, 0], [39.6, .04, 31.6], T.floor);
  for (let x = 34; x < 72; x += 2) floor.box([x, .05, 0], [.025, .015, 31.5], T.floorSeam);
  for (let z = -14; z < 16; z += 2) floor.box([52, .05, z], [39.5, .015, .025], T.floorSeam);
  floor.torus([52, .08, 0], 4.8, .09, T.regions.greenhouse);
  // Glass vault runs east-west. All overhead structure belongs to the roof cutaway group.
  roof.add(new THREE.CylinderGeometry(1, 1, 1, high ? 32 : 20, 1, true, 0, Math.PI), T.glass, [52, 3, 0], [0, 0, Math.PI / 2], [8.7, 40, 16]);
  for (let x = 32; x <= 72; x += 4) {
    roof.add(new THREE.TorusGeometry(1, .012, 4, high ? 32 : 20, Math.PI), T.hull, [x, 3, 0], [0, -Math.PI / 2, 0], [16, 8.7, 16]);
    for (const sign of [-1, 1]) walls.box([x, 1.55, sign * 16], [.2, 3.1, .2], T.hull);
  }
  for (const sign of [-1, 1]) {
    walls.box([52, 1.6, sign * 16], [40, 3.2, .12], T.glass);
    floor.box([52, .35, sign * 16], [40, .7, .4], T.hull);
    walls.box([52, 2.9, sign * 16], [40, .16, .3], T.regions.greenhouse);
    for (let z = -16; z <= 16; z += 4) {
      const y = 3 + Math.sqrt(Math.max(0, 1 - (z / 16) ** 2)) * 8.7;
      roof.box([52, y, z], [40, .12, .12], T.hull);
    }
    const x = sign < 0 ? 32 : 72;
    for (const half of [-1, 1]) {
      walls.box([x, 1.55, half * 9.25], [.12, 3.1, 13.5], T.glass);
      floor.box([x, .35, half * 9.25], [.4, .7, 13.5], T.hull);
    }
    roof.add(new THREE.CircleGeometry(1, high ? 32 : 20, 0, Math.PI), T.glass, [x, 3, 0], [0, -Math.PI / 2, 0], [16, 8.7, 1]);
  }
  for (const z of [-2.5, 2.5]) walls.box([32, 2, z], [.6, 4, .45], m.color);
  walls.box([32, 4, 0], [.6, .4, 5.5], m.color);
  floor.finish(); walls.finish(); roof.finish();
  for (const mat of walls.materials.values()) { mat.transparent = true; mat.depthWrite = false; }
  return { floor, walls, roof, dispose: () => { floor.dispose(); walls.dispose(); roof.dispose(); } };
}

function cropBatch(points: Position[], color: string, scale: Position, high: boolean) {
  const geometry=high ? new THREE.IcosahedronGeometry(1, 1) : new THREE.DodecahedronGeometry(1, 0);
  const leafy=[G.leaf,G.deepLeaf,G.tenderLeaf].some(value=>value===color);
  if(leafy){const positions=geometry.getAttribute('position');for(let i=0;i<positions.count;i++){const x=positions.getX(i),y=positions.getY(i),z=positions.getZ(i);positions.setXYZ(i,x*(1-.35*Math.abs(z)),y*.45+.15*(1-z*z),z*1.3);}geometry.computeVertexNormals();}
  const mesh = new THREE.InstancedMesh(geometry, new THREE.MeshToonMaterial({ color, gradientMap: toonGradient() }), points.length);
  const object = new THREE.Object3D();
  points.forEach((p, i) => { object.position.set(...p); const size=.88+(i%7)*.04;object.scale.set(scale[0]*size,scale[1]*size,scale[2]*size); object.rotation.set(i * .1, i * 1.7, 0); object.updateMatrix(); mesh.setMatrixAt(i, object.matrix); });
  mesh.computeBoundingSphere();
  return mesh;
}

function makeGarden(high: boolean) {
  const b = new Construction(high), leaves: Position[] = [], fruit: Position[] = [], flowers: Position[] = [];
  const tree = (x: number, z: number, size: number) => {
    b.cylinder([x, size * 1.8, z], .28 * size, size * 3.3, G.trunk);
    for (const side of [-1, 1]) b.cylinder([x + side * size * .4, size * 2.7, z], .12 * size, 1.4 * size, G.trunk, [0, 0, side * -.65]);
    b.cylinder([x, .45, z], size * 1.35, .8, T.hull);
    b.cylinder([x, .86, z], size * 1.18, .05, G.nutrient);
    for (let i = 0; i < 17; i++) { const a = i * 2.4, r = Math.sqrt(i / 17) * size * 2; leaves.push([x + Math.cos(a) * r, size * (3.5 + (i % 3) * .35), z + Math.sin(a) * r]); }
  };
  tree(52, 0, 1.1);
  for (let x = 40; x <= 68; x += 7) tree(x, 13, .55);
  // Four long shelves of ten growing containers (40 total) flank a central walkway along the vault.
  // Veggie-style pillows: white trays, a nutrient wick strip and pink-white grow lights overhead.
  for (const z of [-9.5, -5, 5, 9.5]) {
    for (const x of [36.5, 67.5]) for (const dz of [-.55, .55]) b.instance("rack", [x, 1.8, z + dz], [.12, 3.6, .12], T.trim);
    b.box([52, .9, z], [31.4, .16, 1.5], T.trim);
    b.box([52, 3.55, z], [31.4, .1, .3], T.hull);
    for (let i = 0; i < 10; i++) {
      const x = 38 + i * 3.2;
      b.instance("container", [x, 1.22, z], [2.4, .5, 1.15], T.hull);
      b.instance("wick", [x, 1.49, z], [2.2, .05, .95], G.nutrient);
      b.instance("led", [x, 3.45, z], [2.6, .06, .2], G.ledWarm, [0, 0, 0], true);
      leaves.push([x - .55, 1.85, z], [x + .55, 1.85, z]);
      if (i % 3 === 1) fruit.push([x, 1.95, z + (z < 0 ? .45 : -.45)]);
    }
    b.cylinder([52, .45, z + (z < 0 ? -.85 : .85)], .07, 31, G.nutrient, [0, 0, Math.PI / 2]);
  }
  // Water reservoir at the far end of the walkway, piped into the shelf lines.
  b.cylinder([69.2, 1.5, 0], 1.3, 3, T.glass); b.cylinder([69.2, 1.1, 0], 1.2, 2.1, G.nutrient);
  b.cylinder([69.2, 3.05, 0], 1.38, .14, T.hull); b.cylinder([69.2, .08, 0], 1.45, .16, T.trim);
  b.cylinder([68.4, .5, -2.6], .1, 4.4, G.nutrient, [Math.PI / 2, 0, 0]); b.cylinder([68.4, .5, 2.6], .1, 4.4, G.nutrient, [Math.PI / 2, 0, 0]);
  // Manual watering can on a small stand beside the entrance.
  b.cylinder([35.2, .55, 2.8], .55, .1, T.trim); b.cylinder([35.2, .3, 2.8], .08, .5, T.trim);
  b.cylinder([35.2, .9, 2.8], .32, .6, T.regions.shield);
  b.cylinder([35.65, 1.05, 2.8], .05, .7, T.regions.shield, [0, 0, -.9]);
  b.torus([35, 1.25, 2.8], .2, .04, T.regions.shield, [0, 0, 0], Math.PI);
  // Rear tomato/bean trellises and vertical towers.
  for (let x = 38; x < 70; x += 6) {
    b.cylinder([x, 2, -13], .65, 3.7, T.hull);
    for (let i = 0; i < 5; i++) { const a = i * 2.4; leaves.push([x + Math.cos(a) * .75, .9 + i * .55, -13 + Math.sin(a) * .75]); }
    b.box([x, 2.2, -14.7], [4.3, .12, .12], G.trunk);
    for (let j = 0; j < 4; j++) {
      const px = x - 1.5 + j;
      b.instance("trellis", [px, 2, -14.7], [.07, 3.8, .07], T.trim);
      for (let i = 0; i < 4; i++) { leaves.push([px + Math.sin(i) * .25, .8 + i * .8, -14.7]); if (j % 2 === 0) fruit.push([px + .2, 1.1 + i * .65, -14.4]); }
      flowers.push([px, 3.8, -14.7]);
    }
  }
  // Small care console beside (not on) the central walkway.
  b.box([48, 1, 1.4], [1.8, 1.7, .9], T.hull, [0, 0, 0], false, true);
  b.box([48, 1.9, 1.4], [1.5, .15, .8], T.regions.greenhouse);
  b.finish();
  const foliage = [G.leaf, G.deepLeaf, G.tenderLeaf].map((c, i) => cropBatch(leaves.filter((_, j) => j % 3 === i), c, [.6, .4, .6], high));
  // Give orchard canopy clusters more volume than tray crops.
  const temp = new THREE.Object3D();
  foliage.forEach((mesh, batch) => leaves.filter((_, j) => j % 3 === batch).forEach((p, i) => { if (Math.hypot(p[0] - 52, p[2]) < 3.5 && p[1] > 3) { temp.position.set(...p); temp.scale.set(1.55, 1.1, 1.45); temp.updateMatrix(); mesh.setMatrixAt(i, temp.matrix); } }));
  const tomatoes = cropBatch(fruit, G.tomato, [.18, .2, .18], high), petals = cropBatch(flowers, G.flower, [.12, .12, .12], false);
  const apples = cropBatch(APPLES, G.apple, [.25, .28, .25], true);
  apples.userData["cameraInteractive"] = true;
  const drone = new Construction(high);
  drone.sphere([0, 0, 0], [.7, .35, .55], T.hull); drone.box([0, .02, .45], [.55, .16, .08], T.visor);
  for (const side of [-1, 1]) { drone.torus([side * .9, .12, 0], .4, .05, G.nutrient); drone.box([side * .9, .12, 0], [.65, .06, .12], T.trim); }
  drone.cylinder([0, -.4, 0], .12, .5, G.nutrient); drone.finish();
  b.group.add(...foliage, tomatoes, petals, apples, drone.group);
  const particles = new THREE.InstancedMesh(new THREE.IcosahedronGeometry(.1, 0), new THREE.MeshBasicMaterial({ color: G.mist, transparent: true, opacity: .22, depthWrite: false }), high ? 28 : 12);
  particles.frustumCulled = false; b.group.add(particles);
  return { b, foliage, tomatoes, petals, apples, drone, particles, dispose: () => { b.dispose(); drone.materials.forEach(m => m.dispose()); [particles, ...foliage, tomatoes, petals, apples].forEach(mesh => { const mat = mesh.material; if (!Array.isArray(mat)) mat.dispose(); }); } };
}

export function Greenhouse({ high }: { high: boolean }) {
  const model = useMemo(() => makeGarden(high), [high]);
  useEffect(() => () => model.dispose(), [model]);
  const scratch = useMemo(() => new THREE.Object3D(), []);
  useFrame(() => {
    const c = useCampaign.getState(), base = gardenVisuals(c.alloc.greenhouse, DERIVED[c.solIdx]?.demand.greenhouse ?? 20), failing = c.phase === "summary" && (c.outcome?.systems.greenhouse.missing ?? 0) > 0, perfect = c.consequences.some(e => e.id === "perfect-sol");
    const v = perfect ? { ...base, fullness: 1, warmth: 1, mist: true, ripe: APPLES.length } : failing ? { ...base, fullness: .58, warmth: .12, mist: false, ripe: 2, droneSpeed: .04 } : base;
    const reduced = useGame.getState().settings.reducedMotion, time = runtime.ambienceTime;
    model.foliage.forEach((mesh, i) => { const mat = mesh.material as THREE.MeshToonMaterial; mat.color.set([G.leaf, G.deepLeaf, G.tenderLeaf][i] ?? G.leaf).lerp(new THREE.Color(G.tiredLeaf), 1 - v.warmth); mesh.scale.y = v.fullness; mesh.rotation.z = v.state === "deficit" ? .015 : 0; });
    for (const mat of model.b.materials.values()) if (mat.emissiveIntensity > 0 || mat.emissive.getHexString() === new THREE.Color(G.ledWarm).getHexString()) { mat.color.set(G.ledCold).lerp(new THREE.Color(G.ledWarm), v.warmth); mat.emissive.copy(mat.color); mat.emissiveIntensity = .04 + v.warmth * .5; }
    const collected = useGarden.getState().collected;
    APPLES.forEach((p, i) => { scratch.position.set(...p); scratch.scale.setScalar(i < v.ripe && !collected.includes(i) ? .27 : 0); scratch.updateMatrix(); model.apples.setMatrixAt(i, scratch.matrix); });
    model.apples.instanceMatrix.needsUpdate = true;
    const a = reduced ? .4 : time * v.droneSpeed;
    model.drone.group.position.set(52 + Math.cos(a) * 12, 4.8 + (reduced ? 0 : Math.sin(time * .8) * .12), Math.sin(a) * 10);
    model.drone.group.rotation.y = -a;
    model.particles.visible = v.mist && !reduced;
    for (let i = 0; i < model.particles.count; i++) { const phase = (time * .12 + i * .37) % 1; scratch.position.set(38 + (i * 7.3) % 29, i % 3 === 0 ? 3 + phase * 2 : 7 + phase * 2, Math.sin(i * 3.7) * 10); scratch.scale.setScalar(i % 3 === 0 ? .55 : 2 + phase * 2); scratch.updateMatrix(); model.particles.setMatrixAt(i, scratch.matrix); }
    model.particles.instanceMatrix.needsUpdate = true;
  });
  const pick = (event: ThreeEvent<MouseEvent>) => {
    if (event.object !== model.apples || event.instanceId === undefined) return;
    event.stopPropagation(); const p = APPLES[event.instanceId]; if (!p) return;
    const c = useCampaign.getState(); if (c.openTask || c.energyOpen || c.reportOpen) return;
    const v = gardenVisuals(c.alloc.greenhouse, DERIVED[c.solIdx]?.demand.greenhouse ?? 20);
    useGarden.getState().pick(event.instanceId, v.ripe, Math.hypot(runtime.x - p[0], runtime.z - p[2]));
  };
  return <primitive object={model.b.group} dispose={null} onClick={pick} />;
}