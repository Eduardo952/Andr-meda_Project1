import * as THREE from "three";
import { THEME } from "@/config/theme";

let gradient: THREE.DataTexture | null = null;
/** 3-tone ramp shared by every toon material. */
export function toonGradient(): THREE.DataTexture {
  if (!gradient) {
    gradient = new THREE.DataTexture(new Uint8Array(THEME.materials.toonTones), 3, 1, THREE.RedFormat);
    gradient.minFilter = THREE.NearestFilter;
    gradient.magFilter = THREE.NearestFilter;
    gradient.needsUpdate = true;
  }
  return gradient;
}

export function Toon({ color, emissive, emissiveIntensity = 0 }: { color: string; emissive?: string; emissiveIntensity?: number }) {
  return <meshToonMaterial color={color} gradientMap={toonGradient()} emissive={emissive ?? THEME.ink} emissiveIntensity={emissiveIntensity} />;
}

/** Deterministic PRNG so scenery is identical on every load. */
export function seeded(seed: number) {
  let a = seed;
  return () => {
    a |= 0; a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
