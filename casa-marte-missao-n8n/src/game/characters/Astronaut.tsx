import { TELESCOPE } from "@/config/telescope";
import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import { Outlines } from "@react-three/drei";
import * as THREE from "three";
import { PALETTE } from "@/config/palette";
import { Toon } from "@/game/scene/materials";
import { runtime } from "@/game/runtime";
import { useGame } from "@/game/store";
import { sfx } from "@/audio/sfx";
import { THEME } from "@/config/theme";
import { BALANCE } from "@/config/balance";

/** Visual model only; positioned by the parent. `walk` is 0..1 speed factor, `phase` drives limbs. */
export function AstronautModel({ bodyRef, lArm, rArm, lLeg, rLeg, packLight }: {
  bodyRef?: React.Ref<THREE.Group>; lArm?: React.Ref<THREE.Group>; rArm?: React.Ref<THREE.Group>;
  lLeg?: React.Ref<THREE.Group>; rLeg?: React.Ref<THREE.Group>; packLight?: React.Ref<THREE.MeshToonMaterial>;
}) {
  return (
    <group ref={bodyRef ?? null}>
      <mesh position={[0, 0.95, 0]} castShadow><capsuleGeometry args={[0.39, 0.52, 6, 12]} /><Toon color={PALETTE.milk} /><Outlines thickness={0.025} color={PALETTE.dark} /></mesh>
      <mesh position={[0, 1.05, 0.38]}><boxGeometry args={[0.36, 0.22, 0.08]} /><meshToonMaterial color={PALETTE.coral} /></mesh>
      <mesh position={[0, 1.85, 0]} castShadow><sphereGeometry args={[0.57, 16, 12]} /><Toon color={PALETTE.milk} /><Outlines thickness={0.025} color={PALETTE.dark} /></mesh>
      <mesh position={[0, 1.88, 0.43]} scale={[1, 0.78, 0.48]}><sphereGeometry args={[0.47, 20, 14]} /><meshStandardMaterial color={THEME.visor} metalness={THEME.materials.visorMetalness} roughness={THEME.materials.visorRoughness} envMapIntensity={1.5} /></mesh>
      <mesh position={[0.17, 2.02, 0.63]}><sphereGeometry args={[0.08, 8, 6]} /><meshBasicMaterial color={THEME.hull} transparent opacity={0.8} /></mesh>
      <mesh position={[0, 1.88, .65]} scale={[1,.5,.25]}><sphereGeometry args={[.18,12,8]}/><meshBasicMaterial color={THEME.face} transparent opacity={.28}/></mesh>
      <group position={[0, 1.1, -0.45]}>
        <mesh castShadow><boxGeometry args={[0.6, 0.65, 0.3]} /><Toon color={THEME.hull} /></mesh>
        <mesh position={[0, 0.25, -0.17]}><sphereGeometry args={[0.08, 8, 6]} /><meshToonMaterial ref={packLight ?? null} color={PALETTE.sunYellow} emissive={PALETTE.sunYellow} emissiveIntensity={0} /></mesh>
      </group>
      {([[-0.5, lArm], [0.5, rArm]] as const).map(([x, ref], i) => (
        <group key={i} ref={ref ?? null} position={[x, 1.2, 0]}>
          <mesh position={[0, -0.28, 0]}><capsuleGeometry args={[0.18, 0.28, 4, 8]} /><Toon color={PALETTE.milk} /></mesh>
          <mesh position={[x < 0 ? -.18 : .18, -.15, .05]} rotation-z={Math.PI/2}><cylinderGeometry args={[.1,.1,.025,8]}/><Toon color={PALETTE.coral}/></mesh>
          <mesh position={[0, -0.55, 0]}><sphereGeometry args={[0.15, 8, 6]} /><Toon color={PALETTE.coral} /></mesh>
        </group>
      ))}
      {([[-0.2, lLeg], [0.2, rLeg]] as const).map(([x, ref], i) => (
        <group key={i} ref={ref ?? null} position={[x, 0.55, 0]}>
          <mesh position={[0, -0.25, 0]}><capsuleGeometry args={[0.2, 0.22, 4, 8]} /><Toon color={PALETTE.milk} /></mesh>
          <mesh position={[0, -0.47, 0.06]}><boxGeometry args={[0.35, 0.2, 0.46]} /><Toon color={PALETTE.coral} /></mesh>
        </group>
      ))}
    </group>
  );
}

const DUST = 16;

export function Astronaut() {
  const root = useRef<THREE.Group>(null);
  const body = useRef<THREE.Group>(null);
  const lArm = useRef<THREE.Group>(null), rArm = useRef<THREE.Group>(null);
  const lLeg = useRef<THREE.Group>(null), rLeg = useRef<THREE.Group>(null);
  const pack = useRef<THREE.MeshToonMaterial>(null);
  const dust = useRef<THREE.InstancedMesh>(null);
  const puffs = useMemo(() => Array.from({ length: DUST }, () => ({ x: 0, z: 0, life: 0 })), []);
  const next = useRef(0);
  const lastStep = useRef(0);
  const reduced = useGame((s) => s.settings.reducedMotion);
  const tmp = useMemo(() => new THREE.Object3D(), []);

  useFrame(({ clock }, delta) => {
    const dt = Math.min(delta, 0.05);
    const w = Math.min(1, runtime.speed / 4);
    const ph = runtime.walkPhase;
    root.current?.position.set(runtime.x, 0, runtime.z);
    runtime.observe += ((runtime.observing && runtime.observeArrived ? 1 : 0) - runtime.observe) * Math.min(1, dt * 4);
    const ob = reduced ? (runtime.observing && runtime.observeArrived ? 1 : 0) : runtime.observe;
    if (root.current) {
      const toScope = Math.atan2(TELESCOPE.x - runtime.x, TELESCOPE.z - runtime.z);
      const diff = ((toScope - runtime.heading + Math.PI * 3) % (Math.PI * 2)) - Math.PI;
      root.current.rotation.y = runtime.heading + diff * ob;
    }
    const idle = Math.sin(clock.elapsedTime * 2) * 0.03;
    const jump = reduced ? 0 : Math.sin(Math.PI * Math.max(0, 1 - runtime.jump / BALANCE.ambience.jumpSeconds)) * (runtime.jump > 0 ? 0.9 : 0);
    if (body.current) body.current.position.y = (reduced ? 0.4 : 1) * Math.abs(Math.sin(ph)) * 0.18 * w + idle * (1 - w) + jump;
    const swing = Math.sin(ph) * 0.7 * w;
    if (body.current) body.current.rotation.x = 0.28 * ob;
    if (lArm.current) lArm.current.rotation.x = swing;
    if (rArm.current) { rArm.current.rotation.x = -swing; rArm.current.rotation.z = !reduced && runtime.ambienceTime < 3 ? -1.9 + Math.sin(runtime.ambienceTime*8)*.2 : 0; }
    if (rArm.current && ob > 0.01) rArm.current.rotation.x = -1.3 * ob + (-swing) * (1 - ob);
    if (lLeg.current) lLeg.current.rotation.x = -swing;
    if (rLeg.current) rLeg.current.rotation.x = swing;
    if (pack.current) pack.current.emissiveIntensity = 0;
    // Footstep dust
    const step = Math.floor(ph / Math.PI);
    if (step !== lastStep.current && w > 0.3) {
      lastStep.current = step;
      const p = puffs[next.current]; next.current = (next.current + 1) % DUST;
      if (p && !runtime.interiorId && !reduced) { p.x = runtime.x - Math.sin(runtime.heading) * 0.3; p.z = runtime.z - Math.cos(runtime.heading) * 0.3; p.life = 1; }
      if (step % 2 === 0) sfx.step();
    }
    puffs.forEach((p, i) => {
      p.life = Math.max(0, p.life - dt * 1.6);
      tmp.position.set(p.x, 0.15 + (1 - p.life) * 0.4, p.z);
      tmp.scale.setScalar(p.life > 0 ? 0.15 + (1 - p.life) * 0.35 : 0.0001);
      tmp.updateMatrix(); dust.current?.setMatrixAt(i, tmp.matrix);
    });
    if (dust.current) dust.current.instanceMatrix.needsUpdate = true;
  });

  return (
    <>
      <group ref={root}>
        <AstronautModel bodyRef={body} lArm={lArm} rArm={rArm} lLeg={lLeg} rLeg={rLeg} packLight={pack} />
      </group>
      <instancedMesh ref={dust} args={[undefined, undefined, DUST]}>
        <sphereGeometry args={[1, 8, 6]} />
        <meshBasicMaterial color={PALETTE.sand} transparent opacity={0.55} depthWrite={false} />
      </instancedMesh>
    </>
  );
}
