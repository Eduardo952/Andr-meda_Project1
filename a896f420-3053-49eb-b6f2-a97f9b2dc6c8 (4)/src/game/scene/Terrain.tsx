import { useEffect, useMemo } from "react";
import * as THREE from "three";
import { seeded } from "./materials";
import { THEME as T } from "@/config/theme";
import { useGame } from "@/game/store";
import { terrainClearance, terrainHeight } from "@/game/terrain";
import { regolithMaps } from "./surfaceTextures";

function geologicalMesh(high: boolean) {
  const group = new THREE.Group(), random = seeded(9), scratch = new THREE.Object3D();
  const geometry = new THREE.IcosahedronGeometry(1, high ? 1 : 0);
  const positions = geometry.getAttribute("position");
  for (let i = 0; i < positions.count; i++) {
    const x = positions.getX(i), y = positions.getY(i), z = positions.getZ(i);
    const r = 1 + .16 * Math.sin(x * 11 + z * 7) + .12 * Math.cos(y * 9 - z * 4);
    positions.setXYZ(i, x * r, y * r, z * r);
  }
  geometry.computeVertexNormals();
  const add = (count: number, color: string, fragments: boolean) => {
    const mesh = new THREE.InstancedMesh(geometry, new THREE.MeshStandardMaterial({ color, roughness: .97, flatShading: true }), count);
    let used = 0;
    for (let attempt = 0; attempt < count * 8 && used < count; attempt++) {
      const x = (random() - .5) * 210, z = (random() - .3) * 220;
      if (terrainClearance(x, z) < (fragments ? 1.2 : 4)) continue;
      const s = fragments ? .07 + random() * .17 : .3 + random() ** 3 * 2.8;
      scratch.position.set(x, terrainHeight(x, z) + s * .2, z); scratch.rotation.set(random() * .5, random() * Math.PI * 2, random() * .25);
      scratch.scale.set(s * (1 + random()), s * (.4 + random() * .4), s); scratch.updateMatrix(); mesh.setMatrixAt(used++, scratch.matrix);
    }
    mesh.count = used; mesh.castShadow = !fragments; mesh.receiveShadow = true; mesh.computeBoundingSphere(); group.add(mesh);
  };
  add(high ? 145 : 70, T.geology.basalt, false); add(high ? 280 : 110, T.geology.mineral, true);
  const mountains = new THREE.InstancedMesh(geometry, new THREE.MeshStandardMaterial({ color: T.geology.clay, roughness: 1, flatShading: true }), 18);
  for (let i = 0; i < 18; i++) { const a = i * Math.PI / 9, d = 145 + random() * 15; scratch.position.set(Math.cos(a) * d, 1, Math.sin(a) * d); scratch.rotation.set(.1, a, .15); scratch.scale.set(12 + random() * 16, 6 + random() * 12, 10 + random() * 12); scratch.updateMatrix(); mountains.setMatrixAt(i, scratch.matrix); }
  mountains.computeBoundingSphere(); group.add(mountains);
  return { group, dispose: () => { geometry.dispose(); group.children.forEach(child => { if (child instanceof THREE.Mesh && !Array.isArray(child.material)) child.material.dispose(); }); } };
}

export function Terrain() {
  const high = useGame(s => s.settings.quality === "high");
  const maps = useMemo(regolithMaps, []), model = useMemo(() => geologicalMesh(high), [high]);
  const geometry = useMemo(() => {
    const g = new THREE.PlaneGeometry(400, 400, high ? 128 : 80, high ? 128 : 80); g.rotateX(-Math.PI / 2);
    const position = g.getAttribute("position"), colors = new Float32Array(position.count * 3);
    const clay = new THREE.Color(T.hull), sand = new THREE.Color(T.geology.sand), tint = new THREE.Color();
    for (let i = 0; i < position.count; i++) {
      const x = position.getX(i), z = position.getZ(i); position.setY(i, terrainHeight(x, z));
      const blend = .32 + .35 * Math.sin(x * .034 + Math.cos(z * .04)) + .2 * Math.cos(z * .047 - x * .017);
      tint.copy(clay).lerp(sand, Math.max(0, blend)); colors.set([tint.r, tint.g, tint.b], i * 3);
    }
    g.setAttribute("color", new THREE.BufferAttribute(colors, 3)); g.computeVertexNormals(); return g;
  }, [high]);
  useEffect(() => () => model.dispose(), [model]); useEffect(() => () => geometry.dispose(), [geometry]);
  useEffect(() => () => { maps.map.dispose(); maps.bumpMap.dispose(); }, [maps]);
  return <group><mesh geometry={geometry} receiveShadow><meshStandardMaterial map={maps.map} bumpMap={maps.bumpMap} bumpScale={high ? .12 : .07} roughness={.98} vertexColors /></mesh><primitive object={model.group} dispose={null}/></group>;
}