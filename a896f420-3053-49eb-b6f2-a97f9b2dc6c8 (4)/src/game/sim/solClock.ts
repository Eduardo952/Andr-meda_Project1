/** Real length of one Martian Sol: 24 h 39 min 35.244 s (✅ doc §7 / I-15). */
export const SOL_SECONDS = 88775.244;

export function solIndex(solTime: number, campaignSols: number): number {
  return (Math.floor(solTime) % campaignSols) + 1;
}

export function solFraction(solTime: number): number {
  return ((solTime % 1) + 1) % 1;
}

/** Mars local clock (hours 0-24, minutes 0-59) for a fraction of the Sol. */
export function marsClock(fraction: number): { h: number; m: number } {
  const s = fraction * SOL_SECONDS;
  return { h: Math.floor(s / 3600), m: Math.floor((s % 3600) / 60) };
}

/** Unit vector toward the Sun. Sunrise at 0.25, noon at 0.5, sunset at 0.75. */
export function sunDirection(fraction: number): [number, number, number] {
  const a = Math.PI * 2 * (fraction - 0.25);
  const x = -Math.cos(a);
  const y = Math.sin(a);
  const z = 0.35;
  const len = Math.hypot(x, y, z);
  return [x / len, y / len, z / len];
}

export function dayFactor(sunY: number): number {
  const t = Math.min(1, Math.max(0, (sunY + 0.12) / 0.4));
  return t * t * (3 - 2 * t);
}
