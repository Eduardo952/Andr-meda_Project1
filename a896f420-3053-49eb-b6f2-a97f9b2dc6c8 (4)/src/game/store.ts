import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import type { RoomId } from "@/config/rooms";
import type { Lang, Vars } from "@/i18n";
import { BALANCE } from "@/config/balance";

export interface Settings { sound: boolean; bubbleToasts: boolean; largeText: boolean; reducedMotion: boolean; isometric: boolean; invertCamera: boolean; cameraSensitivity: number; quality: "low" | "high"; roofs: "auto" | "off" }

interface GameState {
  screen: "title" | "play";
  lang: Lang;
  settings: Settings;
  visited: RoomId[];
  nearStation: RoomId | null;
  nearTelescope: boolean;
  currentRoom: RoomId | null;
  currentArea: string | null;
  setArea: (id: string | null) => void;
  bubbleLine: { key: string; vars?: Vars | undefined; id: number; text?: string | undefined; source?: "n8n" | "template" | undefined; mood?: string | undefined };
  sayText: (text: string, source: "n8n" | "template", mood: string) => void;
  isTouch: boolean;
  nickname: string;
  setNickname: (n: string) => void;
  setScreen: (s: GameState["screen"]) => void;
  setLang: (l: Lang) => void;
  setSetting: <K extends keyof Settings>(k: K, v: Settings[K]) => void;
  setNear: (r: RoomId | null) => void;
  setRoom: (r: RoomId | null) => void;
  visit: (r: RoomId) => void;
  say: (key: string, vars?: Vars) => void;
  setTouch: (v: boolean) => void;
}

export const useGame = create<GameState>()(
  persist(
    (set) => ({
      screen: "title",
      lang: "en",
      settings: { sound: true, bubbleToasts: true, largeText: false, reducedMotion: false, isometric: false, invertCamera: false, cameraSensitivity: BALANCE.camera.defaultSensitivity, quality: "high", roofs: "auto" },
      visited: [],
      nearStation: null,
      nearTelescope: false,
      currentRoom: null,
      currentArea: null,
      setArea: (currentArea) => set({ currentArea }),
      bubbleLine: { key: "bubble.welcome", id: 0 },
      isTouch: false,
      nickname: "",
      setNickname: (nickname) => set({ nickname: nickname.slice(0, 20) }),
      setScreen: (screen) => set({ screen }),
      setLang: (lang) => set({ lang }),
      setSetting: (k, v) => { if (k === "quality") localStorage.setItem("mmt-quality-chosen", "1"); set((s) => ({ settings: { ...s.settings, [k]: v } })); },
      setNear: (nearStation) => set({ nearStation }),
      setRoom: (currentRoom) => set({ currentRoom }),
      visit: (r) => set((s) => (s.visited.includes(r) ? s : { visited: [...s.visited, r] })),
      say: (key, vars) => set((s) => ({ bubbleLine: { key, vars, id: s.bubbleLine.id + 1 } })),
      sayText: (text, source, mood) => set((s) => ({ bubbleLine: { key: "", text, source, mood, id: s.bubbleLine.id + 1 } })),
      setTouch: (isTouch) => set((s) => ({ isTouch, settings: { ...s.settings, quality: localStorage.getItem("mmt-quality-chosen") ? s.settings.quality : isTouch ? "low" : s.settings.quality } })),
    }),
    {
      name: "mmt-prefs",
      storage: createJSONStorage(() => localStorage),
      partialize: (s) => ({ lang: s.lang, settings: s.settings, nickname: s.nickname }),
      merge: (persisted, current) => {
        const saved = persisted as Partial<GameState> | undefined;
        const sensitivity = saved?.settings?.cameraSensitivity;
        return { ...current, ...saved, settings: { ...current.settings, ...saved?.settings,
          cameraSensitivity: typeof sensitivity === "number" && Number.isFinite(sensitivity)
            ? Math.max(BALANCE.camera.minSensitivity, Math.min(BALANCE.camera.maxSensitivity, sensitivity))
            : BALANCE.camera.defaultSensitivity } };
      },
      skipHydration: true,
    },
  ),
);
