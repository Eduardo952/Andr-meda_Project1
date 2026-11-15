import { useMemo, useRef } from "react";
import { useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { runtime } from "@/game/runtime";
import { seeded } from "./materials";
import { useGame } from "@/game/store";

const vert = `varying vec3 vDir; void main(){ vDir = normalize(position); gl_Position = projectionMatrix * modelViewMatrix * vec4(position,1.0); }`;
const frag = `
uniform vec3 sunDir; uniform float day; varying vec3 vDir;
void main(){
  vec3 d = normalize(vDir); float h = clamp(d.y, 0.0, 1.0);
  vec3 zen = mix(vec3(0.03,0.03,0.10), vec3(0.74,0.58,0.42), day);
  vec3 hor = mix(vec3(0.12,0.08,0.18), vec3(0.93,0.76,0.56), day);
  vec3 col = mix(hor, zen, pow(h, 0.55));
  vec3 sd = normalize(sunDir);
  float s = max(dot(d, sd), 0.0);
  float low = 1.0 - smoothstep(0.0, 0.35, abs(sd.y));
  vec3 glow = mix(vec3(1.0,0.92,0.7), vec3(0.45,0.68,1.0), low);
  float vis = smoothstep(-0.2, 0.05, sd.y);
  col += glow * pow(s, 6.0) * (0.25 + 0.75 * low) * vis;
  col += vec3(1.0,0.98,0.9) * smoothstep(0.9988, 0.9996, s) * vis;
  gl_FragColor = vec4(col, 1.0);
}`;

function StarLayer({ count, radius, size, seed, speed }: { count: number; radius: number; size: number; seed: number; speed: number }) {
  const ref = useRef<THREE.Points>(null);
  const geo = useMemo(() => {
    const r = seeded(seed);
    const arr = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      const th = r() * Math.PI * 2, ph = Math.acos(r() * 0.95);
      arr[i * 3] = Math.sin(ph) * Math.cos(th) * radius;
      arr[i * 3 + 1] = Math.cos(ph) * radius;
      arr[i * 3 + 2] = Math.sin(ph) * Math.sin(th) * radius;
    }
    const g = new THREE.BufferGeometry();
    g.setAttribute("position", new THREE.BufferAttribute(arr, 3));
    return g;
  }, [count, radius, seed]);
  const reduced = useGame((s) => s.settings.reducedMotion);
  useFrame(({ camera }, dt) => {
    const p = ref.current; if (!p) return;
    p.position.set(camera.position.x * speed, 0, camera.position.z * speed); // parallax
    if (!reduced) p.rotation.y += dt * 0.004 * (1 + speed * 4);
    (p.material as THREE.PointsMaterial).opacity = 1 - runtime.day * 0.95;
  });
  return (
    <points ref={ref} geometry={geo}>
      <pointsMaterial size={size} sizeAttenuation={false} color="#fff6e0" transparent depthWrite={false} fog={false} />
    </points>
  );
}

function Orbiter({ radius, speed, size, color, emissive, tilt, phase }: { radius: number; speed: number; size: number; color: string; emissive: number; tilt: number; phase: number }) {
  const ref = useRef<THREE.Mesh>(null);
  useFrame(({ camera }) => {
    const a = runtime.solTime * Math.PI * 2 * speed + phase;
    ref.current?.position.set(camera.position.x + Math.cos(a) * radius, 40 + Math.sin(a) * radius * tilt + radius * 0.25, camera.position.z + Math.sin(a) * radius);
  });
  return (
    <mesh ref={ref}>
      <icosahedronGeometry args={[size, 1]} />
      <meshBasicMaterial color={color} fog={false} transparent opacity={0.6 + emissive * 0.4} />
    </mesh>
  );
}

export function Sky() {
  const mat = useRef<THREE.ShaderMaterial>(null);
  const uniforms = useMemo(() => ({ sunDir: { value: new THREE.Vector3(0, 1, 0) }, day: { value: 1 } }), []);
  const dome = useRef<THREE.Mesh>(null);
  useFrame(({ camera }) => {
    uniforms.sunDir.value.set(...runtime.sunDir);
    uniforms.day.value = runtime.day;
    dome.current?.position.copy(camera.position);
  });
  return (
    <>
      <mesh ref={dome} renderOrder={-10}>
        <sphereGeometry args={[220, 32, 16]} />
        <shaderMaterial ref={mat} side={THREE.BackSide} depthWrite={false} uniforms={uniforms} vertexShader={vert} fragmentShader={frag} fog={false} />
      </mesh>
      <StarLayer count={500} radius={200} size={1.6} seed={11} speed={0.9} />
      <StarLayer count={260} radius={190} size={2.4} seed={23} speed={0.6} />
      {/* Phobos (faster) and Deimos (slower), tiny */}
      <Orbiter radius={120} speed={3.2} size={2.2} color="#b9a48c" emissive={0.3} tilt={0.5} phase={0} />
      <Orbiter radius={150} speed={0.8} size={1.4} color="#d8c7b0" emissive={0.3} tilt={0.3} phase={2} />
      {/* Earth: small blue dot */}
      <Orbiter radius={170} speed={0.05} size={1.1} color="#5aa8ff" emissive={1} tilt={0.6} phase={4} />
      {/* Decorative ringed planet */}
      <DecorPlanet />
    </>
  );
}

function DecorPlanet() {
  const ref = useRef<THREE.Group>(null);
  useFrame(({ camera }) => {
    ref.current?.position.set(camera.position.x - 150, 70, camera.position.z - 120);
    const m = ref.current?.children as THREE.Mesh[] | undefined;
    m?.forEach((c) => ((c.material as THREE.MeshBasicMaterial).opacity = 0.25 + (1 - runtime.day) * 0.6));
  });
  return (
    <group ref={ref} rotation={[0.4, 0, 0.3]}>
      <mesh><sphereGeometry args={[9, 20, 12]} /><meshBasicMaterial color="#c79bff" transparent fog={false} /></mesh>
      <mesh rotation={[Math.PI / 2.3, 0, 0]}><torusGeometry args={[15, 1.2, 4, 40]} /><meshBasicMaterial color="#ffd98a" transparent fog={false} /></mesh>
    </group>
  );
}
