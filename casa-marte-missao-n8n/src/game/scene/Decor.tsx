import { useMemo, useEffect, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { Construction } from "./Construction";
import { PALETTE as P } from "@/config/palette";
import { useGame } from "@/game/store";
import { useCampaign } from "@/game/campaign";
import { runtime } from "@/game/runtime";
import { solarCellMap } from "./surfaceTextures";
import { THEME } from "@/config/theme";
import { panelDustAfterStorm, stormForSol } from "@/engine/consequences";
import { SOLS } from "@/data/campaign";
import { BALANCE } from "@/config/balance";
import { sfx } from "@/audio/sfx";
import { ROOM_BY_ID, SOLAR_YAW } from "@/config/rooms";

const ARRAY_CENTERS = [[-13, 75], [13, 75], [-13, 87], [13, 87]] as const;

export function Decor() {
  const high = useGame(s => s.settings.quality === "high");
  const panels = useRef<THREE.InstancedMesh>(null), dust = useRef<THREE.InstancedMesh>(null), puffs = useRef<THREE.InstancedMesh>(null);
  const panelMaterial = useRef<THREE.MeshStandardMaterial>(null), robot = useRef<THREE.Group>(null), brush = useRef<THREE.Group>(null), sparkle = useRef<THREE.Mesh>(null);
  const object = useMemo(() => new THREE.Object3D(), []), celebrated = useRef(false);
  const cellMap = useMemo(solarCellMap, []); useEffect(() => () => cellMap.dispose(), [cellMap]);
  const model = useMemo(() => {
    const b = new Construction(high);
    for (let row = 0; row < 4; row++) for (let col = 0; col < 8; col++) { const x = -21 + col * 6 + (col < 4 ? -1 : 1), z = 72 + row * 6; b.instance("strut", [x, .8, z], [.3, 1.6, .3], P.seam); b.box([x, .05, z], [.09, .08, 6], P.dark); }
    for (let i = 0; i < 20; i++) b.instance("track", [4, .025, 63 + i * 1.2], [.7, .03, .18], P.rust);
    b.box([0, 1.2, 68], [3, 2.4, 2], P.armor); b.box([0, 2, 66.9], [2.5, 1, .1], P.sunYellow, [0, 0, 0], false); b.box([0, .03, 65], [.1, .06, 9], P.dark);
    for (let z = 62; z < 83; z += 4) for (const side of [-1, 1]) b.instance("light", [side * 3, .3, z], [.25, .6, .25], P.lamp, [0, 0, 0], false);
    b.finish(); return b;
  }, [high]);
  useEffect(() => () => model.dispose(), [model]);

  useFrame((_, delta) => {
    const c = useCampaign.getState(), storm = stormForSol(c.solIdx, SOLS[c.solIdx]?.windSpeed.value?.av ?? null);
    const predicted = c.phase === "summary" ? c.panelDust : panelDustAfterStorm(c.panelDust, storm);
    const animating = runtime.cleaning > 0 && runtime.cleaning < 1 && runtime.cleaningSol === c.solIdx;
    if (animating) runtime.cleaning = Math.min(1, runtime.cleaning + Math.min(delta, 1) / BALANCE.cleaning.animationSeconds);
    if (runtime.cleaning >= 1 && !celebrated.current) { celebrated.current = true; sfx.clean(); }
    if (runtime.cleaning <= 0) celebrated.current = false;
    const pitch = Math.max(-.5, Math.min(.5, -runtime.sunDir[2] * .5));
    for (let i = 0; i < 32; i++) {
      const x = -21 + (i % 8) * 6 + (i % 8 < 4 ? -1 : 1), z = 72 + Math.floor(i / 8) * 6;
      object.position.set(x, 1.5, z); object.rotation.set(pitch, 0, 0); object.scale.set(4.8, .15, 3.8); object.updateMatrix(); panels.current?.setMatrixAt(i, object.matrix);
      const array = (Math.floor(i / 8) >= 2 ? 2 : 0) + (i % 8 < 4 ? 0 : 1);
      const from = runtime.cleaningSol === c.solIdx && runtime.cleaning > 0 ? runtime.cleaningFrom[array] ?? 0 : predicted[array] ?? 0;
      const local = runtime.cleaningSol === c.solIdx ? Math.max(0, Math.min(1, runtime.cleaning * 4 - array)) : 0;
      const level = Math.max(0, from / 100 * (1 - local));
      object.position.set(x, 1.61, z); object.scale.set(4.65 * Math.sqrt(level), .025, 3.65 * Math.sqrt(level)); object.updateMatrix(); dust.current?.setMatrixAt(i, object.matrix);
    }
    if (panels.current) panels.current.instanceMatrix.needsUpdate = true;
    if (dust.current) dust.current.instanceMatrix.needsUpdate = true;
    if (panelMaterial.current) { panelMaterial.current.emissive.set(THEME.regions.solar); panelMaterial.current.emissiveIntensity = c.cleanedThisSol && runtime.cleaning >= 1 ? .22 : 0; }
    const progress = runtime.cleaningSol === c.solIdx ? runtime.cleaning : 0;
    const step = Math.min(3, Math.floor(progress * 4)), local = Math.max(0, Math.min(1, progress * 4 - step));
    const target = ARRAY_CENTERS[step] ?? ARRAY_CENTERS[0], previous = step === 0 ? [0, 65] as const : ARRAY_CENTERS[step - 1] ?? target;
    if (robot.current) { robot.current.visible = progress > 0; robot.current.position.set(previous[0] + (target[0] - previous[0]) * local, .2, previous[1] + (target[1] - previous[1]) * local); robot.current.rotation.y = Math.atan2(target[0] - previous[0], target[1] - previous[1]); }
    if (brush.current) brush.current.position.x = Math.sin(local * Math.PI * 6) * 1.3;
    if (sparkle.current) { sparkle.current.visible = runtime.cleaning >= 1 && c.cleanedThisSol; sparkle.current.rotation.y += .02; }
    for (let i = 0; i < 12; i++) { const phase = (local + i / 12) % 1; object.position.set((robot.current?.position.x ?? 0) + (i % 4 - 1.5) * .5, .4 + phase * 1.4, (robot.current?.position.z ?? 0) + (i % 3 - 1) * .6); object.rotation.set(0, 0, 0); object.scale.setScalar(animating ? Math.sin(phase * Math.PI) * .18 : 0); object.updateMatrix(); puffs.current?.setMatrixAt(i, object.matrix); }
    if (puffs.current) puffs.current.instanceMatrix.needsUpdate = true;
  });

  // Solar field geometry keeps its original local frame; the group places it on the radial solar spoke.
  const solar = ROOM_BY_ID.solar;
  return <group position={[solar.x, 0, solar.z]} rotation-y={SOLAR_YAW}><group position={[0, 0, -82]}>
    <primitive object={model.group} dispose={null} />
    <instancedMesh ref={panels} args={[undefined, undefined, 32]} receiveShadow><boxGeometry /><meshStandardMaterial ref={panelMaterial} map={cellMap} color={THEME.hull} roughness={.3} metalness={.4} /></instancedMesh>
    <instancedMesh ref={dust} args={[undefined, undefined, 32]}><boxGeometry /><meshStandardMaterial color={THEME.geology.dust} transparent opacity={.78} depthWrite={false} roughness={1} /></instancedMesh>
    <group ref={robot} visible={false}>
      <mesh position={[0, .55, 0]}><boxGeometry args={[1.4, .65, 1]} /><meshToonMaterial color={THEME.hull} /></mesh>
      <mesh position={[0, .65, .52]}><boxGeometry args={[.7, .2, .08]} /><meshBasicMaterial color={THEME.visor} /></mesh>
      {[-.55, .55].map(x => <mesh key={x} position={[x, .2, 0]} rotation={[Math.PI / 2, 0, 0]}><cylinderGeometry args={[.24, .24, .18, 10]} /><meshToonMaterial color={THEME.ink} /></mesh>)}
      <group ref={brush} position={[0, .18, 1]}><mesh><boxGeometry args={[2.2, .18, .35]} /><meshToonMaterial color={THEME.regions.solar} /></mesh></group>
    </group>
    <instancedMesh ref={puffs} args={[undefined, undefined, 12]}><sphereGeometry args={[1, 6, 4]} /><meshBasicMaterial color={THEME.geology.dust} transparent opacity={.35} depthWrite={false} /></instancedMesh>
    <mesh ref={sparkle} visible={false} position={[0, 4, 80]}><octahedronGeometry args={[.8, 0]} /><meshBasicMaterial color={THEME.regions.solar} /></mesh>
  </group></group>;
}
