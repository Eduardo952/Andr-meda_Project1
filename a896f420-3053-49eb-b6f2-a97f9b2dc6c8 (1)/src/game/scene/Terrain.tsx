import { useEffect, useMemo } from "react";
import * as THREE from "three";
import { seeded, toonGradient } from "./materials";
import { PALETTE as P } from "@/config/palette";
import { useGame } from "@/game/store";
import { MODULES } from "@/config/rooms";
import { isWalkable } from "@/game/sim/world";
import { Construction } from "./Construction";

function groundTexture() {
  const canvas = document.createElement("canvas"); canvas.width = canvas.height = 128;
  const g = canvas.getContext("2d"); if (!g) return new THREE.Texture();
  g.fillStyle = P.coral; g.fillRect(0, 0, 128, 128);
  const random = seeded(5);
  for (let i = 0; i < 150; i++) { g.fillStyle = random() > 0.5 ? P.rust : P.sand; g.globalAlpha = 0.15; g.beginPath(); g.arc(random() * 128, random() * 128, 1 + random() * 8, 0, Math.PI * 2); g.fill(); }
  const texture = new THREE.CanvasTexture(canvas); texture.wrapS = texture.wrapT = THREE.RepeatWrapping; texture.repeat.set(35, 35); texture.colorSpace = THREE.SRGBColorSpace; return texture;
}
export function Terrain() {
  const high = useGame((s) => s.settings.quality === "high");
  const texture = useMemo(groundTexture, []);
  const model = useMemo(() => {
    const b = new Construction(high), random = seeded(9);
    for (let i = 0; i < (high ? 180 : 70); i++) {
      const x = (random() - 0.5) * 180, z = (random() - 0.4) * 180;
      if (isWalkable(x, z) || MODULES.some((m) => Math.abs(x - m.x) < m.width / 2 + 5 && Math.abs(z - m.z) < m.depth / 2 + 5)) continue;
      const s = 0.25 + random(); b.instance("rock", [x, s * 0.15, z], [s, s * 0.6, s], P.rust, [random(), random() * 6, 0]);
    }
    for (let i = 0; i < 16; i++) {
      const a = i * Math.PI / 8, d = 110 + random() * 25;
      b.sphere([Math.cos(a) * d, -3, Math.sin(a) * d], [18 + random() * 18, 10 + random() * 15, 20], P.sand);
    }
    for (const [x, z] of [[-55, -30], [48, -24], [55, 36], [-48, 57]] as const) {
      b.torus([x, 0.05, z], 3, 0.35, P.rust);
      b.sphere([x, -0.55, z], [2.8, 0.65, 2.8], P.rust);
      b.sphere([x + 7, -0.1, z + 5], [7, 1.3, 4], P.sand);
    }
    b.finish(); return b;
  }, [high]);
  useEffect(() => () => model.dispose(), [model]);
  useEffect(() => () => texture.dispose(), [texture]);
  return <group>
    <mesh rotation={[-Math.PI / 2, 0, 0]} receiveShadow position={[0, -0.04, 0]}><circleGeometry args={[200, 48]} /><meshToonMaterial map={texture} gradientMap={toonGradient()} /></mesh>
    <primitive object={model.group} dispose={null} />
  </group>;
}
