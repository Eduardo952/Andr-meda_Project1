import * as THREE from "three";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";
import { RoundedBoxGeometry } from "three/addons/geometries/RoundedBoxGeometry.js";
import { PALETTE } from "@/config/palette";
import { toonGradient } from "./materials";

export type Position = [number, number, number];
interface Instance { position: Position; scale: Position; rotation: Position }
/** Static geometry is merged by material; repeated hardware uses one instanced draw per kind. */
export class Construction {
  readonly group = new THREE.Group();
  readonly materials = new Map<string, THREE.MeshToonMaterial>();
  private parts = new Map<string, THREE.BufferGeometry[]>();
  private batches = new Map<string, { geometry: THREE.BufferGeometry; color: string; instances: Instance[]; glow: boolean }>();
  readonly segments: number;
  constructor(high: boolean) { this.segments = high ? 20 : 12; }
  material(color: string, glow = false) {
    const key = `${color}:${glow}`;
    let material = this.materials.get(key);
    if (!material) {
      material = new THREE.MeshToonMaterial({ color, gradientMap: toonGradient(), emissive: glow ? color : PALETTE.dark, emissiveIntensity: glow ? 0.65 : 0 });
      if (color === PALETTE.glass) { material.transparent = true; material.opacity = 0.38; material.depthWrite = false; material.side = THREE.DoubleSide; }
      this.materials.set(key, material);
    }
    return material;
  }
  add(geometry: THREE.BufferGeometry, color: string, position: Position, rotation: Position = [0, 0, 0], scale: Position = [1, 1, 1], glow = false) {
    if (geometry.index) { const original = geometry; geometry = original.toNonIndexed(); original.dispose(); }
    geometry.deleteAttribute("uv");
    geometry.applyMatrix4(new THREE.Matrix4().compose(new THREE.Vector3(...position), new THREE.Quaternion().setFromEuler(new THREE.Euler(...rotation)), new THREE.Vector3(...scale)));
    const key = `${color}:${glow}`; this.material(color, glow);
    const parts = this.parts.get(key) ?? []; parts.push(geometry); this.parts.set(key, parts);
  }
  box(position: Position, scale: Position, color: string, rotation: Position = [0, 0, 0], glow = false, rounded = false) {
    this.add(rounded ? new RoundedBoxGeometry(1, 1, 1, 1, 0.12) : new THREE.BoxGeometry(1, 1, 1), color, position, rotation, scale, glow);
  }
  cylinder(position: Position, radius: number, height: number, color: string, rotation: Position = [0, 0, 0]) {
    this.add(new THREE.CylinderGeometry(radius, radius, height, this.segments), color, position, rotation);
  }
  sphere(position: Position, scale: Position, color: string) {
    this.add(new THREE.SphereGeometry(1, this.segments, 8), color, position, [0, 0, 0], scale);
  }
  torus(position: Position, radius: number, tube: number, color: string, rotation: Position = [Math.PI / 2, 0, 0], arc = Math.PI * 2, glow = false) {
    this.add(new THREE.TorusGeometry(radius, tube, 4, this.segments, arc), color, position, rotation, [1, 1, 1], glow);
  }
  instance(kind: string, position: Position, scale: Position, color: string, rotation: Position = [0, 0, 0], glow = false) {
    const key = `${kind}:${color}:${glow}`;
    let batch = this.batches.get(key);
    if (!batch) {
      const geometry = kind === "bolt" ? new THREE.CylinderGeometry(1, 1, 1, 6)
        : kind === "rock" || kind === "berm" ? new THREE.DodecahedronGeometry(1, 0)
        : kind === "crate" ? new RoundedBoxGeometry(1, 1, 1, 1, 0.1) : new THREE.BoxGeometry(1, 1, 1);
      batch = { geometry, color, instances: [], glow }; this.batches.set(key, batch);
    }
    batch.instances.push({ position, scale, rotation });
  }
  finish() {
    for (const [key, geometries] of this.parts) {
      const geometry = mergeGeometries(geometries, false);
      geometries.forEach((g) => g.dispose());
      const material = this.materials.get(key); if (!geometry || !material) continue;
      const mesh = new THREE.Mesh(geometry, material); mesh.castShadow = true; mesh.receiveShadow = true;
      this.group.add(mesh);
    }
    for (const batch of this.batches.values()) {
      const mesh = new THREE.InstancedMesh(batch.geometry, this.material(batch.color, batch.glow), batch.instances.length);
      const object = new THREE.Object3D();
      batch.instances.forEach((p, i) => { object.position.set(...p.position); object.scale.set(...p.scale); object.rotation.set(...p.rotation); object.updateMatrix(); mesh.setMatrixAt(i, object.matrix); });
      mesh.instanceMatrix.needsUpdate = true; mesh.castShadow = true; mesh.receiveShadow = true; mesh.computeBoundingSphere(); this.group.add(mesh);
    }
    return this.group;
  }
  dispose() {
    this.group.traverse((o) => { if (o instanceof THREE.Mesh) o.geometry.dispose(); });
    this.materials.forEach((m) => m.dispose());
  }
}
