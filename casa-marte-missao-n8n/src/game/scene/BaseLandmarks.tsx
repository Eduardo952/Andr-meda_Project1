import { useEffect, useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { Construction } from "./Construction";
import { seeded, toonGradient } from "./materials";
import { THEME as T } from "@/config/theme";
import { LANDMARKS, MODULES, SATELLITE_ANGLES, polar } from "@/config/rooms";
import { terrainClearance, terrainHeight } from "@/game/terrain";
import { pendingTasks, useCampaign } from "@/game/campaign";
import { useGame } from "@/game/store";
import { runtime } from "@/game/runtime";

/** Decoration only: reads campaign progress, never writes game state. */
const G = T.garden;
const FIREFLIES = 18;

function buildStatic(high: boolean) {
  const b = new Construction(high);
  // Hub plaza: round planter, bench ring, ground lights, plant beds between the five doors.
  b.cylinder([0, .35, 0], 2.1, .7, T.hull); b.torus([0, .72, 0], 2.1, .12, T.regions.greenhouse);
  b.cylinder([0, .72, 0], 1.9, .06, G.trunk);
  for (let i = 0; i < 10; i++) { const a = i * Math.PI / 5 + Math.PI / 10; b.box([Math.sin(a) * 3.3, .45, Math.cos(a) * 3.3], [1.6, .16, .7], G.trunk, [0, a, 0]); b.instance("bench-leg", [Math.sin(a) * 3.3, .2, Math.cos(a) * 3.3], [.9, .4, .5], T.trim, [0, a, 0]); }
  for (let i = 0; i < 12; i++) { const a = i * Math.PI / 6; b.instance("ground-light", [Math.sin(a) * 2.45, .05, Math.cos(a) * 2.45], [.22, .06, .22], T.face, [0, 0, 0], true); }
  // Plaza paths from the tree to every door, edged with soft amber/cyan lights.
  for (const deg of Object.values(SATELLITE_ANGLES)) {
    const a = deg * Math.PI / 180, [x, z] = polar(deg, 8.6);
    b.box([x, .035, z], [2.4, .02, 9.2], T.floorSeam, [0, a, 0]);
    for (let r = 5; r < 13; r += 2.5) for (const side of [-1, 1]) { const px = Math.sin(a) * r + Math.cos(a) * side * 1.5, pz = Math.cos(a) * r - Math.sin(a) * side * 1.5; b.instance("path-light", [px, .06, pz], [.14, .08, .14], side < 0 ? T.face : G.nutrient, [0, 0, 0], true); }
  }
  for (const deg of [54, 198, 270, 342]) { const [x, z] = polar(deg, 8.5); const a = deg * Math.PI / 180; b.box([x, .3, z], [2.6, .6, 1.2], T.hull, [0, a + Math.PI / 2, 0], false, true); b.box([x, .62, z], [2.4, .06, 1], G.trunk, [0, a + Math.PI / 2, 0]); for (const t of [-.7, 0, .7]) b.sphere([x + Math.cos(a) * t, .85, z - Math.sin(a) * t], [.38, .3, .38], t ? G.leaf : G.tenderLeaf); }
  // Monitors near the hub wall (the command console keeps its own wall segment).
  for (const deg of [306 + 36, 18 + 36]) { const [x, z] = polar(deg, 12); const a = deg * Math.PI / 180 + Math.PI; b.box([x, .8, z], [1.4, 1.6, .6], T.hull, [0, a, 0], false, true); b.box([x, 1.7, z], [1.3, .8, .08], T.regions.shield, [0, a, 0], true); }

  // Magnetic shield generator on the perimeter.
  const [gx, gz] = LANDMARKS.generator.p;
  b.cylinder([gx, .4, gz], 2.4, .8, T.trim); b.cylinder([gx, 3, gz], .9, 5, T.hull);
  for (let i = 0; i < 4; i++) b.torus([gx, 1.8 + i * 1.1, gz], 1.15, .14, T.regions.shield, [Math.PI / 2, 0, 0], Math.PI * 2, true);
  b.sphere([gx, 6, gz], [1, 1, 1], T.regions.shield);
  // Communication mast.
  const [mx, mz] = LANDMARKS.mast.p; b.cylinder([mx, 6, mz], .22, 12, T.trim); b.sphere([mx, 12, mz], [1.5, .5, 1.5], T.hull);
  // Landing pad and rocket outside the ring.
  const [px, pz] = LANDMARKS.landingPad.p;
  b.add(new THREE.CylinderGeometry(10, 10, .25, 8), T.trim, [px, .12, pz]); b.torus([px, .27, pz], 7.5, .2, T.regions.solar);
  for (let i = 0; i < 8; i++) { const a = i * Math.PI / 4; b.instance("pad-lamp", [px + Math.sin(a) * 9.2, .4, pz + Math.cos(a) * 9.2], [.3, .5, .3], T.face, [0, 0, 0], true); }
  b.cylinder([px, 5, pz], 1.5, 8, T.hull); b.add(new THREE.ConeGeometry(1.5, 3, high ? 24 : 12), T.regions.control, [px, 10.5, pz]);
  b.cylinder([px, 6.5, pz], 1.53, .4, T.regions.shield); b.sphere([px, 7.6, pz + 1.35], [.45, .45, .2], T.visor);
  for (let i = 0; i < 3; i++) { const a = i * Math.PI * 2 / 3; b.box([px + Math.sin(a) * 1.7, 1.6, pz + Math.cos(a) * 1.7], [.3, 3, 1.4], T.regions.control, [0, a, 0]); }
  // Rover parked near the edge.
  const [rx, rz] = LANDMARKS.rover.p, ry = Math.atan2(-rx, -rz) + .6;
  const rp = (lx: number, ly: number, lz: number): [number, number, number] => [rx + lx * Math.cos(ry) + lz * Math.sin(ry), ly, rz - lx * Math.sin(ry) + lz * Math.cos(ry)];
  b.box(rp(0, 1.1, 0), [2.6, .9, 4], T.hull, [0, ry, 0], false, true); b.box(rp(0, 1.85, -.4), [2.2, .7, 1.8], T.visor, [0, ry, 0], false, true);
  for (const sx of [-1.45, 1.45]) for (const sz of [-1.4, 0, 1.4]) b.cylinder(rp(sx, .5, sz), .5, .4, T.ink, [0, ry, Math.PI / 2]);
  b.cylinder(rp(.8, 2.6, 1.3), .05, 1.4, T.trim); b.sphere(rp(.8, 3.3, 1.3), [.15, .15, .15], T.regions.solar);
  b.finish(); return b;
}

/** Seeded scatter of shrubs and supply crates, kept clear of modules, paths and landmarks. */
function buildScatter(high: boolean) {
  const b = new Construction(high), random = seeded(31);
  let shrubs = 0, crates = 0;
  for (let attempt = 0; attempt < 600 && (shrubs < (high ? 46 : 24) || crates < 14); attempt++) {
    const a = random() * Math.PI * 2, r = 20 + random() * 70, x = Math.sin(a) * r, z = Math.cos(a) * r;
    if (terrainClearance(x, z) < 3 || MODULES.some(m => Math.hypot(x - m.x, z - m.z) < Math.max(m.width, m.depth) / 2 + 3)) continue;
    const y = terrainHeight(x, z);
    if (crates < 14 && random() < .3) {
      const s = .7 + random() * .4, color = crates % 3 === 0 ? T.regions.solar : crates % 3 === 1 ? T.hull : T.trim;
      b.instance("crate", [x, y + s / 2, z], [s, s, s], color, [0, random() * Math.PI, 0]);
      if (random() < .5) b.instance("crate", [x + s * .9, y + s * .4, z + .2], [s * .8, s * .8, s * .8], T.hull, [0, random(), 0]);
      crates++;
    } else if (shrubs < (high ? 46 : 24)) {
      const s = .35 + random() * .4;
      for (let k = 0; k < 3; k++) b.sphere([x + (k - 1) * s * .7, y + s * .45, z + (k % 2) * s * .5], [s, s * .7, s], k === 1 ? "#8a9f7a" : "#6f8a6a");
      shrubs++;
    }
  }
  b.finish(); return b;
}

function fresnelShield() {
  return new THREE.ShaderMaterial({
    transparent: true, depthWrite: false, side: THREE.FrontSide,
    uniforms: { uColor: { value: new THREE.Color(T.regions.shield) }, uTime: { value: 0 } },
    vertexShader: "varying vec3 vN; varying vec3 vV; varying float vY; void main(){ vec4 w=modelMatrix*vec4(position,1.); vN=normalize(mat3(modelMatrix)*normal); vV=normalize(cameraPosition-w.xyz); vY=position.y; gl_Position=projectionMatrix*viewMatrix*w; }",
    fragmentShader: "uniform vec3 uColor; uniform float uTime; varying vec3 vN; varying vec3 vV; varying float vY; void main(){ float f=pow(1.-abs(dot(vN,vV)),2.6); float band=.5+.5*sin(vY*.35-uTime*.8); gl_FragColor=vec4(uColor, clamp(f*f*.5+band*f*.06,0.,.4)); }",
  });
}

/** Holographic pictograms (water, sun, energy, shield) built from flat shapes. */
function hologramShapes() {
  const bolt = new THREE.Shape([[.1, .5], [-.25, -.05], [0, -.05], [-.1, -.5], [.25, .08], [0, .08]].map(([x, y]) => new THREE.Vector2(x, y)));
  const shield = new THREE.Shape(); shield.moveTo(0, .45); shield.quadraticCurveTo(.3, .4, .38, .32); shield.quadraticCurveTo(.38, -.2, 0, -.45); shield.quadraticCurveTo(-.38, -.2, -.38, .32); shield.quadraticCurveTo(-.3, .4, 0, .45);
  const drop = new THREE.Shape(); drop.moveTo(0, .5); drop.quadraticCurveTo(.35, 0, .3, -.15); drop.absarc(0, -.15, .3, 0, Math.PI, true); drop.quadraticCurveTo(-.35, 0, 0, .5);
  const sun = new THREE.Shape(); for (let i = 0; i <= 16; i++) { const a = i * Math.PI / 8, r = i % 2 ? .3 : .48; const fn = i ? "lineTo" : "moveTo"; sun[fn](Math.cos(a) * r, Math.sin(a) * r); }
  return [[drop, G.nutrient], [sun, T.regions.solar], [bolt, T.face], [shield, T.regions.shield]] as const;
}

export function BaseLandmarks() {
  const high = useGame(s => s.settings.quality === "high");
  const statics = useMemo(() => buildStatic(high), [high]);
  const scatter = useMemo(() => buildScatter(high), [high]);
  useEffect(() => () => { statics.dispose(); scatter.dispose(); }, [statics, scatter]);
  const shieldMat = useMemo(fresnelShield, []);
  const holos = useMemo(() => hologramShapes().map(([shape, color]) => ({ geometry: new THREE.ShapeGeometry(shape), material: new THREE.MeshBasicMaterial({ color, transparent: true, opacity: .75, side: THREE.DoubleSide, depthWrite: false }) })), []);
  const foliageMats = useMemo(() => [G.leaf, G.deepLeaf, G.tenderLeaf].map(c => new THREE.MeshToonMaterial({ color: c, gradientMap: toonGradient(), emissive: c, emissiveIntensity: .1 })), []);
  const leafGeo = useMemo(() => new THREE.IcosahedronGeometry(1, high ? 1 : 0), [high]);
  const fireflyGeo = useMemo(() => new THREE.SphereGeometry(.07, 6, 4), []);
  const fireflyMat = useMemo(() => new THREE.MeshBasicMaterial({ color: T.face, transparent: true, opacity: .9, depthWrite: false }), []);
  useEffect(() => () => { shieldMat.dispose(); holos.forEach(h => { h.geometry.dispose(); h.material.dispose(); }); foliageMats.forEach(m => m.dispose()); leafGeo.dispose(); fireflyGeo.dispose(); fireflyMat.dispose(); }, [shieldMat, holos, foliageMats, leafGeo, fireflyGeo, fireflyMat]);
  const holoRefs = useRef<(THREE.Mesh | null)[]>([]), flies = useRef<THREE.InstancedMesh>(null), canopy = useRef<THREE.Group>(null);
  const scratch = useMemo(() => new THREE.Object3D(), []), tired = useMemo(() => new THREE.Color(G.tiredLeaf), []), tmp = useMemo(() => new THREE.Color(), []);
  const health = useRef(1);
  const clusters = useMemo(() => Array.from({ length: 9 }, (_, i) => { const a = i * 2.4, r = i ? 1 + (i % 3) * .3 : 0; return { p: [Math.cos(a) * r, 4.3 + (i % 3) * .45 + (i ? 0 : .7), Math.sin(a) * r] as [number, number, number], s: i ? 1.05 + (i % 2) * .2 : 1.4, m: i % 3 }; }), []);

  useFrame((_, delta) => {
    const t = runtime.ambienceTime, reduced = useGame.getState().settings.reducedMotion;
    shieldMat.uniforms['uTime']!.value = t;
    // Tree mood: greener and brighter once today's tasks are done, softer while tasks are pending.
    const c = useCampaign.getState(), target = pendingTasks(c.solIdx, c.results).length === 0 ? 1 : .55;
    health.current += (target - health.current) * (1 - Math.exp(-2 * Math.min(delta, .05)));
    const pulse = reduced ? 0 : Math.sin(t * 1.6) * .5 + .5;
    foliageMats.forEach((m, i) => { tmp.set([G.leaf, G.deepLeaf, G.tenderLeaf][i]!); m.color.copy(tmp).lerp(tired, (1 - health.current) * .7); m.emissive.copy(m.color); m.emissiveIntensity = .06 + health.current * (.1 + pulse * .12); });
    if (canopy.current) canopy.current.scale.setScalar(.9 + health.current * .1);
    holoRefs.current.forEach((h, i) => { if (!h) return; const a = i * Math.PI / 2 + Math.PI / 4 + (reduced ? 0 : t * .15); h.position.set(Math.sin(a) * 3.1, 3 + (reduced ? 0 : Math.sin(t * 1.2 + i) * .18), Math.cos(a) * 3.1); h.rotation.y = a; });
    if (flies.current) {
      for (let i = 0; i < FIREFLIES; i++) { const a = i * 2.39 + (reduced ? 0 : t * (.18 + (i % 4) * .05)), r = 1.4 + (i % 5) * .45; scratch.position.set(Math.sin(a) * r, 1.6 + (i % 6) * .55 + (reduced ? 0 : Math.sin(t * 1.3 + i) * .3), Math.cos(a) * r); scratch.scale.setScalar(.6 + (reduced ? .4 : (Math.sin(t * 3 + i * 1.7) * .5 + .5)) * health.current); scratch.updateMatrix(); flies.current.setMatrixAt(i, scratch.matrix); }
      flies.current.instanceMatrix.needsUpdate = true;
    }
  });

  return <>
    <primitive object={statics.group} dispose={null} />
    <primitive object={scatter.group} dispose={null} />
    {/* Tree of life at the heart of the hub */}
    <mesh position={[0, 2.1, 0]} castShadow><cylinderGeometry args={[.22, .38, 3.2, 10]} /><meshToonMaterial color={G.trunk} gradientMap={toonGradient()} /></mesh>
    {[-1, 1].map(s => <mesh key={s} position={[s * .5, 3.4, s * .15]} rotation={[0, 0, -s * .6]}><cylinderGeometry args={[.09, .14, 1.4, 8]} /><meshToonMaterial color={G.trunk} gradientMap={toonGradient()} /></mesh>)}
    <group ref={canopy}>{clusters.map((k, i) => <mesh key={i} geometry={leafGeo} material={foliageMats[k.m]!} position={k.p} scale={[k.s, k.s * .8, k.s]} castShadow />)}</group>
    <pointLight position={[0, 1.2, 0]} color={T.face} intensity={6} distance={7} decay={2} />
    {holos.map((h, i) => <mesh key={i} ref={r => { holoRefs.current[i] = r; }} geometry={h.geometry} material={h.material} scale={.75} />)}
    <instancedMesh ref={flies} args={[fireflyGeo, fireflyMat, FIREFLIES]} frustumCulled={false} />
    {/* Faint magnetic shield bubble around the whole base; invisible from inside (front faces only). */}
    <mesh material={shieldMat} position={[0, -4, 0]} renderOrder={5}><sphereGeometry args={[LANDMARKS.shieldBubble, high ? 48 : 28, high ? 24 : 14, 0, Math.PI * 2, 0, Math.PI / 2]} /></mesh>
  </>;
}
