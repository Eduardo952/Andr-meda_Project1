import { MODULES, PATHS } from "@/config/rooms";
import { distToSegment } from "@/game/sim/world";

/** Visual relief only. All walkable areas retain the existing flat collision plane. */
export const CRATERS = [{ x: -55, z: -30, radius: 5 }, { x: 48, z: -24, radius: 4 }, { x: 64, z: 36, radius: 6 }, { x: -48, z: 57, radius: 4.5 }];
export function terrainClearance(x: number, z: number) {
  let clearance = Infinity;
  for (const m of MODULES) {
    const dx = Math.abs(x - m.x), dz = Math.abs(z - m.z);
    const d = m.shape === "hub" || m.shape === "dome"
      ? Math.hypot(dx, dz) - Math.max(m.width, m.depth) / 2
      : Math.max(dx - m.width / 2, dz - m.depth / 2);
    clearance = Math.min(clearance, d);
  }
  for (const p of PATHS) clearance = Math.min(clearance, distToSegment(x, z, p.a.x, p.a.z, p.b.x, p.b.z) - p.width / 2);
  return clearance;
}
export function terrainHeight(x: number, z: number) {
  const t = Math.max(0, Math.min(1, (terrainClearance(x, z) - 2) / 9));
  const mask = t * t * (3 - 2 * t);
  let height = .25 + .28 * Math.sin(x * .09 + Math.sin(z * .035)) + .18 * Math.cos(z * .12 + x * .035);
  for (const c of CRATERS) {
    const d = Math.hypot(x - c.x, z - c.z) / c.radius;
    height += .65 * Math.exp(-(((d - 1) / .22) ** 2)) - .6 * Math.exp(-((d / .75) ** 4));
  }
  return -.04 + mask * height;
}