import { MODULES, ROOMS, ROOM_BY_ID, PATHS, INTERIOR_PATHS, doorway, type RoomId, type ModuleDef, type PathPoint } from "@/config/rooms";

export function distToSegment(px: number, pz: number, ax: number, az: number, bx: number, bz: number) {
  const dx = bx - ax, dz = bz - az;
  const length = dx * dx + dz * dz;
  const t = length ? Math.max(0, Math.min(1, ((px - ax) * dx + (pz - az) * dz) / length)) : 0;
  return Math.hypot(px - (ax + t * dx), pz - (az + t * dz));
}
export function insideHull(x: number, z: number, m: ModuleDef, padding = 0.35) {
  const dx = x - m.x, dz = z - m.z;
  if (m.shape === "hub" || m.shape === "dome") return (dx / (m.width / 2 + padding)) ** 2 + (dz / (m.depth / 2 + padding)) ** 2 < 1;
  return Math.abs(dx) < m.width / 2 + padding && Math.abs(dz) < m.depth / 2 + padding;
}
export function areaAt(x: number, z: number): ModuleDef | undefined {
  return interiorAt(x, z) ?? MODULES.find((m) => Math.hypot(x - m.entrance[0], z - m.entrance[1]) < 3);
}
export function insideInterior(x: number, z: number, m: ModuleDef) { return insideHull(x, z, m, -0.6); }
export function interiorAt(x: number, z: number) { return MODULES.find((m) => insideInterior(x, z, m)); }
function inDoorway(x:number,z:number,m:ModuleDef){return m.ports.some(([px,pz])=>{const axisX=Math.abs(px-m.x)>Math.abs(pz-m.z);return Math.abs(axisX?z-pz:x-px)<2.25&&Math.abs(axisX?x-px:z-pz)<1.5;});}
export function roomAt(x:number,z:number):RoomId|null{return areaAt(x,z)?.room??null;}
export function isWalkable(x:number,z:number):boolean{
 if(MODULES.some(m=>m.shape!=="park"&&insideHull(x,z,m)&&!insideInterior(x,z,m)&&!inDoorway(x,z,m)))return false;
 if(interiorAt(x,z)||MODULES.some(m=>inDoorway(x,z,m)))return true;
 return [...PATHS,...INTERIOR_PATHS].some(({a,b,width})=>distToSegment(x,z,a.x,a.z,b.x,b.z)<=width/2);
}
export function stationPosition(id: RoomId): [number, number] { return ROOM_BY_ID[id].entrance; }
export function nearestStation(x: number, z: number, radius: number): RoomId | null {
  let best: RoomId | null = null, bestD = radius;
  for (const r of ROOMS) {
    const d = Math.hypot(x - r.entrance[0], z - r.entrance[1]);
    if (d < bestD) { bestD = d; best = r.id; }
  }
  return best;
}
// A small sampled path graph also handles T-junctions in the exterior loop.
const key = (p: PathPoint) => `${p.x.toFixed(2)},${p.z.toFixed(2)}`;
const nodes: PathPoint[] = [];
const adjacency: Map<number, { index: number; distance: number }[]> = new Map();
const indices = new Map<string, number>();
function addNode(p: PathPoint) {
  const existing = indices.get(key(p)); if (existing !== undefined) return existing;
  const index = nodes.length; nodes.push(p); indices.set(key(p), index); adjacency.set(index, []); return index;
}
for (const path of [...PATHS, ...INTERIOR_PATHS]) {
  const length = Math.hypot(path.b.x - path.a.x, path.b.z - path.a.z);
  const count = Math.max(1, Math.ceil(length));
  for (let i = 0; i <= count; i++) addNode({ x: path.a.x + (path.b.x - path.a.x) * i / count, z: path.a.z + (path.b.z - path.a.z) * i / count });
}
for (let i = 0; i < nodes.length; i++) for (let j = i + 1; j < nodes.length; j++) {
  const a = nodes[i], b = nodes[j]; if (!a || !b) continue;
  const distance = Math.hypot(a.x - b.x, a.z - b.z);
  if (distance > 1.6) continue;
  if (![0, 0.25, 0.5, 0.75, 1].every((t) => isWalkable(a.x + (b.x - a.x) * t, a.z + (b.z - a.z) * t))) continue;
  adjacency.get(i)?.push({ index: j, distance }); adjacency.get(j)?.push({ index: i, distance });
}
function closest(x: number, z: number) {
  let best = 0, distance = Infinity;
  nodes.forEach((p, i) => { const d = Math.hypot(p.x - x, p.z - z); if (d < distance && isWalkable(p.x, p.z)) { best = i; distance = d; } });
  return best;
}
export function routeBetween(x: number, z: number, tx: number, tz: number): PathPoint[] {
  const start = closest(x, z), end = closest(tx, tz);
  const distance = new Map<number, number>([[start, 0]]), previous = new Map<number, number>();
  const open = new Set([start]);
  while (open.size) {
    let current = start, best = Infinity;
    for (const index of open) { const d = distance.get(index) ?? Infinity; if (d < best) { current = index; best = d; } }
    open.delete(current); if (current === end) break;
    for (const next of adjacency.get(current) ?? []) {
      const d = best + next.distance;
      if (d < (distance.get(next.index) ?? Infinity)) { distance.set(next.index, d); previous.set(next.index, current); open.add(next.index); }
    }
  }
  if (!distance.has(end)) return [];
  const result: PathPoint[] = [{ x: tx, z: tz }]; let current = end;
  for (let limit = 0; limit < nodes.length; limit++) {
    const p = nodes[current]; if (p) result.unshift(p);
    if (current === start) break;
    const next = previous.get(current); if (next === undefined) return []; current = next;
  }
  return result;
}
let cachedTarget = "", cachedOrigin = "", cachedRoute: PathPoint[] = [];
export function guideWaypoint(x: number, z: number, id: RoomId): PathPoint {
  const [tx, tz] = stationPosition(id);
  const origin = `${Math.round(x)},${Math.round(z)}`;
  if (cachedTarget !== id || cachedOrigin !== origin) {
    cachedTarget = id; cachedOrigin = origin; cachedRoute = routeBetween(x, z, tx, tz);
  }
  return cachedRoute.find((p) => Math.hypot(p.x - x, p.z - z) > 2) ?? { x: tx, z: tz };
}
export interface Mover { x: number; z: number; vx: number; vz: number; heading: number; speed: number }
export function stepMover(m: Mover, ix: number, iz: number, dt: number, maxSpeed: number, accel: number) {
  const len = Math.hypot(ix, iz); if (len > 1) { ix /= len; iz /= len; }
  const k = 1 - Math.exp(-accel * dt);
  m.vx += (ix * maxSpeed - m.vx) * k; m.vz += (iz * maxSpeed - m.vz) * k;
  const nx = m.x + m.vx * dt; if (isWalkable(nx, m.z)) m.x = nx; else m.vx = 0;
  const nz = m.z + m.vz * dt; if (isWalkable(m.x, nz)) m.z = nz; else m.vz = 0;
  m.speed = Math.hypot(m.vx, m.vz);
  if (len > 0.1) { const target = Math.atan2(ix, iz); const d = Math.atan2(Math.sin(target - m.heading), Math.cos(target - m.heading)); m.heading += d * Math.min(1, dt * 12); }
}
