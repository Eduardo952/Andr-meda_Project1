import { Button } from "@/components/ui/button";
import { useRef, useState } from "react";
import { Canvas, useFrame } from "@react-three/fiber";
import * as THREE from "three";
import { Globe, HelpCircle, Play } from "lucide-react";
import { useT } from "@/i18n";
import { useGame } from "@/game/store";
import { useCampaign } from "@/game/campaign";
import { sfx } from "@/audio/sfx";
import { BubbleModel } from "@/game/characters/Bubble";
import { Toon } from "@/game/scene/materials";
import { PALETTE } from "@/config/palette";
import { HowToPlay } from "./HowToPlay";
import { Modal } from "./Modal";
import { LanguageToggle } from "./LanguageToggle";

function Mars() {
  const ref = useRef<THREE.Group>(null);
  useFrame((_, dt) => { if (ref.current) ref.current.rotation.y += dt * 0.08; });
  return (
    <group ref={ref} position={[0, -5.2, -2]}>
      <mesh><sphereGeometry args={[5, 48, 32]} /><Toon color={PALETTE.coral} /></mesh>
      {([[1, 4, 2.6], [-2.5, 3.6, 2.2], [3, 2, 3.4], [-1, 2.5, 4.1]] as const).map(([x, y, z], i) => (
        <mesh key={i} position={[x, y, z]} scale={[1, 0.3, 1]}>
          <sphereGeometry args={[0.6 + i * 0.1, 12, 8]} /><Toon color={PALETTE.rust} />
        </mesh>
      ))}
    </group>
  );
}

function WavingBubble() {
  const ref = useRef<THREE.Group>(null);
  useFrame(({ clock }) => { if (ref.current) { ref.current.position.y = 0.4 + Math.sin(clock.elapsedTime * 1.5) * 0.12; ref.current.rotation.z = Math.sin(clock.elapsedTime * 2) * 0.08; } });
  return <group ref={ref} position={[1.9, 0.4, 0]} scale={1.1}><BubbleModel wave talking /></group>;
}

function TitleStars() {
  const geo = useRef<THREE.BufferGeometry>(null);
  const positions = useRef<Float32Array | null>(null);
  if (!positions.current) {
    const arr = new Float32Array(600 * 3);
    let a = 7;
    const r = () => { a = (a * 16807) % 2147483647; return a / 2147483647; };
    for (let i = 0; i < 600; i++) { arr[i * 3] = (r() - 0.5) * 60; arr[i * 3 + 1] = (r() - 0.3) * 30; arr[i * 3 + 2] = -15 - r() * 10; }
    positions.current = arr;
  }
  return (
    <points>
      <bufferGeometry ref={geo}><bufferAttribute attach="attributes-position" args={[positions.current, 3]} /></bufferGeometry>
      <pointsMaterial size={2} sizeAttenuation={false} color="#fff6e0" />
    </points>
  );
}

export function TitleScreen() {
  const { t } = useT();
  const setScreen = useGame((s) => s.setScreen);
  const [dialog, setDialog] = useState<null | "how" | "lang">(null);
  return (
    <div className="relative h-dvh w-full overflow-hidden bg-ink">
      <div className="absolute inset-0">
        <Canvas dpr={[1, 1.5]} camera={{ position: [0, 0.5, 6], fov: 50 }}>
          <color attach="background" args={["#241a3d"]} />
          <ambientLight intensity={0.6} color="#9fb4ff" />
          <directionalLight position={[-4, 5, 4]} intensity={2.2} color="#ffe2b8" />
          <TitleStars />
          <Mars />
          <WavingBubble />
        </Canvas>
      </div>
      <div className="relative z-10 flex h-full flex-col items-center justify-start gap-6 px-4 pt-[10vh] text-center">
        <h1 className="title-3d text-5xl sm:text-7xl">{t("title.name")}</h1>
        <p className="panel px-5 py-2 text-lg font-extrabold">{t("title.tagline")}</p>
        <div className="mt-4 flex flex-col gap-4 sm:flex-row">
          <Button className="btn-game px-10 text-2xl" onClick={() => { sfx.use(); setScreen("play"); useCampaign.getState().start(); }}><Play className="mr-2 inline h-6 w-6" />{t("title.play")}</Button>
          <Button className="btn-game alt text-xl" onClick={() => { sfx.click(); setDialog("lang"); }}><Globe className="mr-2 inline h-5 w-5" />{t("title.language")}</Button>
          <Button className="btn-game ghost text-xl" onClick={() => { sfx.click(); setDialog("how"); }}><HelpCircle className="mr-2 inline h-5 w-5" />{t("title.howTo")}</Button>
        </div>
      </div>
      {dialog === "how" && <HowToPlay onClose={() => setDialog(null)} />}
      {dialog === "lang" && <Modal title={t("title.language")} onClose={() => setDialog(null)} closeLabel={t("settings.close")}><LanguageToggle /></Modal>}
    </div>
  );
}
