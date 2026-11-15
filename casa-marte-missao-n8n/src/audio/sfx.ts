import { useGame } from "@/game/store";

let ctx: AudioContext | null = null;
function audio(): AudioContext | null {
  if (typeof window === "undefined" || !useGame.getState().settings.sound) return null;
  if (!ctx) ctx = new AudioContext();
  if (ctx.state === "suspended") void ctx.resume();
  return ctx;
}

function tone(freq: number, dur: number, type: OscillatorType, vol: number, delay = 0, slideTo?: number) {
  const a = audio();
  if (!a) return;
  const t = a.currentTime + delay;
  const o = a.createOscillator();
  const g = a.createGain();
  o.type = type;
  o.frequency.setValueAtTime(freq, t);
  if (slideTo) o.frequency.exponentialRampToValueAtTime(slideTo, t + dur);
  g.gain.setValueAtTime(0.0001, t);
  g.gain.exponentialRampToValueAtTime(vol, t + 0.015);
  g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
  o.connect(g).connect(a.destination);
  o.start(t);
  o.stop(t + dur + 0.05);
}

let dangerTimer: ReturnType<typeof setInterval> | null = null;
let dangerStep = 0;
let ducked = false;

function stopDanger() {
  if (dangerTimer) clearInterval(dangerTimer);
  dangerTimer = null;
  ducked = false;
}

/** Synthesized, optional emergency audio. No audio files or network requests. */
function startDanger(kind: "solar" | "storm") {
  if (dangerTimer || !audio()) return;
  ducked = true;
  const pulse = () => {
    if (kind === "solar") {
      const high = dangerStep++ % 2 === 0;
      tone(high ? 620 : 440, 0.42, "sine", 0.075);
      tone(high ? 440 : 620, 0.42, "sine", 0.045, 0.44);
    } else tone(72, 0.85, "sawtooth", 0.025, 0, 48);
  };
  pulse();
  dangerTimer = setInterval(pulse, kind === "solar" ? 880 : 1000);
}

export const sfx = {
  click: () => tone(660, 0.08, "triangle", ducked ? 0.05 : 0.12),
  use: () => { tone(523, 0.12, "sine", 0.15); tone(784, 0.18, "sine", 0.15, 0.09); tone(1046, 0.22, "sine", 0.12, 0.18); },
  step: () => tone(120, 0.06, "sine", 0.05, 0, 70),
  near: () => tone(880, 0.1, "sine", 0.06),
  door: () => tone(300, 0.25, "triangle", 0.05, 0, 520),
  blub: () => tone(400, 0.12, "sine", 0.08, 0, 900),
  danger: startDanger,
  allClear: () => { stopDanger(); tone(523, 0.18, "sine", 0.09); tone(659, 0.2, "sine", 0.09, 0.16); tone(784, 0.34, "sine", 0.1, 0.32); },
  clean: () => { tone(740, 0.1, "sine", 0.06); tone(988, 0.24, "sine", 0.07, 0.1); },
  stopDanger,
};

useGame.subscribe((state, previous) => {
  if (previous.settings.sound && !state.settings.sound) stopDanger();
});
