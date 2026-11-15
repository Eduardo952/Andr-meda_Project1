import { TELESCOPE } from "@/config/telescope";
import { BALANCE } from "@/config/balance";
import { distToSegment, stepMover, type Mover } from "./world";
import type { PathPoint } from "@/config/rooms";

/** Keep the walking approach outside the pier; render pose never teleports the collider. */
export function telescopeRoute(x: number, z: number): PathPoint[] {
  const t = TELESCOPE;
  const end = { x: t.standX, z: t.standZ };
  if (distToSegment(t.x, t.z, x, z, end.x, end.z) > t.clearance) return [end];
  const side = x < t.x ? -1 : 1;
  return [{ x: t.x + side * (t.clearance + 1), z }, { x: t.x + side * (t.clearance + 1), z: t.standZ }, end];
}
export function stepTelescope(mover: Mover, path: PathPoint[], dt: number) {
  const next = path[0];
  if (!next) return true;
  const dx = next.x - mover.x, dz = next.z - mover.z, distance = Math.hypot(dx, dz);
  if (distance <= TELESCOPE.arrivalRadius) {
    path.shift(); mover.vx = 0; mover.vz = 0; mover.speed = 0;
    return path.length === 0;
  }
  const speed = Math.min(1, distance / .65);
  stepMover(mover, dx / distance * speed, dz / distance * speed, dt, BALANCE.walkSpeed * .6, BALANCE.walkAccel);
  return false;
}