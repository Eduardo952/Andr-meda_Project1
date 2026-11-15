import { BALANCE } from "@/config/balance";
import { SOLS } from "@/data/campaign";
import { FLARE_LETTERS, type FlareLetter } from "@/engine/flare";
import { cmeTravelHours, SUN_MARS_KM } from "@/engine/cme";
import { thermalDemand } from "@/engine/demand";
import type { NormalizedSol } from "@/engine/normalize";
import { severityOf } from "@/services/narrator/facts";
import { deriveSol } from "@/engine/normalize";
import { sunDirection } from "@/game/sim/solClock";

export interface AttemptResult { seconds: number; mistakes: number; stars: number }
export function scoreTask(seconds: number, mistakes: number, success = true): number {
  if (!success || !Number.isFinite(seconds) || !Number.isInteger(mistakes) || seconds < 0 || mistakes < 0 || seconds > BALANCE.taskSeconds) return 0;
  const b = BALANCE.miniGames;
  return seconds <= b.threeStarSeconds && mistakes === 0 ? 3 : seconds <= b.twoStarSeconds && mistakes <= b.twoStarMistakes ? 2 : 1;
}
export const ORIGINS = {
  status: "campaign integrity/oxygen (GAME RULE)",
  timer: "balance.taskSeconds / monotonic attempt clock", mistakes: "player attempt errors", stars: "balance.miniGames scoring (PROVISIONAL)",
  index: "GAME RULE puzzle slot", angle: "GAME RULE dial/panel orientation", count: "balance.miniGames puzzle count (PROVISIONAL)",
  speed: "DONKI/CMEAnalysis.speed snapshot", hours: "engine/cme: SUN_MARS_KM / speed / secondsPerHour; constant speed", distance: "engine/cme.SUN_MARS_KM reference",
  temperature: "InSight AT.av snapshot", heating: "engine/demand.thermalDemand / balance.thermal (PROVISIONAL)",
  energy: "campaign allocation / balance.energyPool / sliderStep (GAME RULE)", light: "balance.miniGames.lightTarget (GAME RULE)",
  flare: "DONKI/FLR.classType snapshot", weather: "InSight HWS.av snapshot, GAME RULE inspired by Mars weather",
  progress: "successful assigned tasks / sector count", sol: "snapshot.sol", input: "player input (GAME RULE)",
} as const;
export type Origin = keyof typeof ORIGINS;
export interface RegisteredNumber { value: number; origin: Origin }
export function numeric(value: number, origin: Origin): RegisteredNumber {
  if (!Number.isFinite(value) || !ORIGINS[origin]) throw new Error("Unregistered numeric value");
  return { value, origin };
}
export function taskData(sol: NormalizedSol) {
  const cmeSol = sol.cme.value ? sol : SOLS.find(s => s.cme.value) ?? sol;
  const tempSol = sol.airTemp.value ? sol : SOLS.find(s => s.airTemp.value) ?? sol;
  const flareSol = sol.flare.value ? sol : SOLS.find(s => s.flare.value) ?? sol;
  const speed = cmeSol.cme.value?.speed ?? BALANCE.shield.cmeReferenceSpeed;
  const temperature = tempSol.airTemp.value?.av ?? null;
  const severity = severityOf(sol, deriveSol(sol));
  const level = severity === "alert" ? 2 : severity === "attention" ? 1 : 0;
  const sun = sunDirection(BALANCE.startSolFraction);
  const tilt = Math.round(Math.atan2(Math.abs(sun[0]), Math.max(.01, sun[1])) * 180 / Math.PI / BALANCE.miniGames.tiltStepDegrees) * BALANCE.miniGames.tiltStepDegrees;
  return {
    speed: numeric(speed, cmeSol.cme.value ? "speed" : "input"), hours: numeric(cmeTravelHours(speed) ?? 0, "hours"), distance: numeric(SUN_MARS_KM, "distance"),
    temperature: temperature === null ? null : numeric(temperature, "temperature"), heating: numeric(thermalDemand(temperature) ?? BALANCE.thermal.missingFallback, "heating"),
    flare: flareSol.flare.value, flareMagnitude: numeric(flareSol.flare.value?.magnitude ?? 0, "flare"),
    marks: numeric(BALANCE.miniGames.coilMarks[level] ?? 4, "count"), motion: BALANCE.miniGames.coilMotionSeconds[level] ?? 8,
    tilt: numeric(tilt, "angle"), light: numeric(BALANCE.miniGames.lightTarget, "light"),
    dust: numeric(BALANCE.marsWeatherModifier && sol.windSpeed.value ? Math.min(BALANCE.miniGames.weatherDustCap, BALANCE.miniGames.dustPatches + Math.round(sol.windSpeed.value.av)) : BALANCE.miniGames.dustPatches, BALANCE.marsWeatherModifier && sol.windSpeed.value ? "weather" : "count"),
    cmePractice: !sol.cme.value, tempPractice: !sol.airTemp.value, flarePractice: !sol.flare.value,
    cmeSample: cmeSol.sample, tempSample: tempSol.sample,
  };
}
export function advanceTaskClock(time: number, solIdx: number, seconds: number, paused: boolean) {
  return paused ? time : Math.min(solIdx + .999, time + Math.max(0, seconds) / BALANCE.realSecondsPerSol);
}
export function aligned(values: number[], targets: readonly number[]) { return values.length === targets.length && values.every((v, i) => v === targets[i]); }

/** One tap places the next correct flare class; an incorrect tap changes nothing. */
export function placeFlareCard(placed: readonly FlareLetter[], value: FlareLetter): FlareLetter[] | null {
  return value === FLARE_LETTERS[placed.length] ? [...placed, value] : null;
}

/** Pipe pieces turn one quarter-turn per tap, retaining the existing target angles. */
export function rotatePipe(position: number): number {
  const steps = BALANCE.miniGames.dialSteps;
  return (position + steps / 4) % steps;
}
