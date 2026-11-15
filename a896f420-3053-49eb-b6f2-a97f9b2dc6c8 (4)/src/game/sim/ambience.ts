import { BALANCE } from "@/config/balance";
import { CREW } from "@/config/crew";

export function crewRoutine(index: number, solTime: number) {
  const areas=["control","observatory","lifeSupport","greenhouse"];
  return {area:areas[index]??"control",activity:"working" as const};
}

/** InSight compass directions are FROM; drifting dust travels in the opposite direction. */
export function windVector(direction: string | null | undefined) {
  const compass = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
  const index = compass.indexOf(direction ?? "");
  const yaw = index >= 0 ? index * Math.PI / 8 : BALANCE.ambience.fallbackWindYaw;
  return { x: -Math.sin(yaw), z: Math.cos(yaw), snapshotDirection: index >= 0 };
}