import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import { Outlines } from "@react-three/drei";
import * as THREE from "three";
import { Toon } from "@/game/scene/materials";
import { runtime } from "@/game/runtime";

/** Bubble: an original little fish living in a water-filled glass helmet. */
export function BubbleModel({ talking = false, wave = false }: { talking?: boolean; wave?: boolean }) {
  const fish = useRef<THREE.Group>(null);
  const tail = useRef<THREE.Mesh>(null);
  const mouth = useRef<THREE.Mesh>(null);
  const fin = useRef<THREE.Mesh>(null);
  const bubbles = useRef<THREE.Group>(null);
  const eyes = useRef<THREE.Group>(null);
  const seeds = useMemo(() => [0, 0.33, 0.66], []);
  useFrame(({ clock }) => {
    const t = runtime.ambienceTime;
    if (fish.current) { fish.current.rotation.y = Math.sin(t * 0.8) * 0.4; fish.current.position.y = Math.sin(t * 2) * 0.04; }
    if (tail.current) tail.current.rotation.y = Math.sin(t * 9) * 0.5;
    if (mouth.current) { mouth.current.scale.y = talking ? 0.4 + Math.abs(Math.sin(t * 14)) * 1.2 : runtime.mood === "worried" ? 1 : runtime.mood === "excited" ? 1.5 : 0.4; mouth.current.scale.x = runtime.mood === "proud" ? 1.5 : 1; }
    if (eyes.current) eyes.current.scale.y = t % 4.3 > 4.12 ? 0.12 : 1;
    if (fin.current) fin.current.rotation.z = wave ? 0.6 + Math.sin(t * 8) * 0.6 : Math.sin(t * 4) * 0.2;
    bubbles.current?.children.forEach((b, i) => {
      const k = (t * 0.4 + (seeds[i] ?? 0)) % 1;
      b.position.set(Math.sin(t + i) * 0.12, -0.2 + k * 0.6, 0.15);
      b.scale.setScalar(0.03 + k * 0.03);
    });
  });
  return (
    <group>
      {/* Glass bowl helmet */}
      <mesh><sphereGeometry args={[0.55, 24, 18]} /><meshStandardMaterial color="#d9fbff" transparent opacity={0.22} roughness={0.05} metalness={0.1} depthWrite={false} /></mesh>
      <mesh position={[0, -0.12, 0]}><sphereGeometry args={[0.5, 20, 14, 0, Math.PI * 2, Math.PI * 0.35, Math.PI * 0.65]} /><meshToonMaterial color="#5fd6ff" transparent opacity={0.45} depthWrite={false} /></mesh>
      <mesh position={[0, -0.52, 0]}><torusGeometry args={[0.28, 0.07, 8, 20]} /><Toon color="#ffc93c" /></mesh>
      <group ref={fish}>
        <mesh scale={[1, 0.85, 0.8]}><sphereGeometry args={[0.24, 16, 12]} /><Toon color="#ff7b54" /><Outlines thickness={0.012} color="#2b2340" /></mesh>
        <mesh position={[0, -0.06, 0.02]} scale={[0.8, 0.5, 0.7]}><sphereGeometry args={[0.22, 12, 8]} /><Toon color="#ffe3a3" /></mesh>
        <mesh ref={tail} position={[0, 0, -0.26]} rotation={[Math.PI / 2, 0, 0]}><coneGeometry args={[0.14, 0.2, 4]} /><Toon color="#8b6cf0" /></mesh>
        <mesh ref={fin} position={[0.2, -0.02, 0]} scale={[0.5, 0.25, 0.6]}><sphereGeometry args={[0.2, 8, 6]} /><Toon color="#8b6cf0" /></mesh>
        <mesh position={[0, 0.18, -0.02]} scale={[0.15, 0.6, 0.5]}><sphereGeometry args={[0.2, 8, 6]} /><Toon color="#2fd3c4" /></mesh>
        <group ref={eyes}>{[-0.09, 0.09].map((x) => (
          <group key={x} position={[x, 0.06, 0.18]}>
            <mesh><sphereGeometry args={[0.075, 12, 10]} /><meshBasicMaterial color="#ffffff" /></mesh>
            <mesh position={[0, 0, 0.05]}><sphereGeometry args={[0.04, 8, 6]} /><meshBasicMaterial color="#1b1530" /></mesh>
            <mesh position={[0.015, 0.02, 0.08]}><sphereGeometry args={[0.012, 6, 4]} /><meshBasicMaterial color="#ffffff" /></mesh>
          </group>
        ))}</group>
        <mesh ref={mouth} position={[0, -0.06, 0.2]}><sphereGeometry args={[0.035, 8, 6]} /><meshBasicMaterial color="#7a1f3d" /></mesh>
      </group>
      <group ref={bubbles}>
        {seeds.map((s) => <mesh key={s}><sphereGeometry args={[1, 8, 6]} /><meshBasicMaterial color="#ffffff" transparent opacity={0.6} /></mesh>)}
      </group>
    </group>
  );
}

export function Bubble3D({ talking }: { talking: boolean }) {
  const ref = useRef<THREE.Group>(null);
  useFrame(({ clock }, delta) => {
    const g = ref.current; if (!g) return;
    const side = runtime.heading + Math.PI / 2;
    const tx = runtime.x + Math.sin(side) * 1.1 - Math.sin(runtime.heading) * 0.4;
    const tz = runtime.z + Math.cos(side) * 1.1 - Math.cos(runtime.heading) * 0.4;
    const k = 1 - Math.exp(-3 * Math.min(delta, 0.05));
    g.position.x += (tx - g.position.x) * k;
    g.position.z += (tz - g.position.z) * k;
    g.position.y = 2.3 + Math.sin(clock.elapsedTime * 1.6) * 0.15;
    g.rotation.y += (runtime.heading - g.rotation.y) * k;
  });
  return <group ref={ref} scale={0.75}><BubbleModel talking={talking} /></group>;
}
