import { useEffect } from "react";
import { createFileRoute } from "@tanstack/react-router";
import { useGame } from "@/game/store";
import { TitleScreen } from "@/ui/TitleScreen";
import { GameScreen } from "@/ui/GameScreen";

export const Route = createFileRoute("/")({
  ssr: false,
  head: () => ({
    meta: [
      { title: "Mars Mission Trainer — Base ARES" },
      { name: "description", content: "Explore Base ARES on Mars as a junior astronaut with Bubble, your fish friend." },
      { property: "og:title", content: "Mars Mission Trainer — Base ARES" },
      { property: "og:description", content: "A playful 3D Mars base adventure for kids aged 7 to 12." },
      { property: "og:type", content: "website" },
      { name: "twitter:card", content: "summary_large_image" },
    ],
  }),
  component: Index,
});

function Index() {
  const screen = useGame((s) => s.screen);
  const lang = useGame((s) => s.lang);
  const largeText = useGame((s) => s.settings.largeText);
  const reduced = useGame((s) => s.settings.reducedMotion);
  useEffect(() => {
    void useGame.persist.rehydrate();
    useGame.getState().setTouch(window.matchMedia("(pointer: coarse)").matches);
  }, []);
  useEffect(() => {
    document.documentElement.lang = lang;
    document.documentElement.classList.toggle("large-text", largeText);
    document.documentElement.classList.toggle("reduce-motion", reduced);
  }, [lang, largeText, reduced]);
  return screen === "title" ? <TitleScreen /> : <GameScreen />;
}
