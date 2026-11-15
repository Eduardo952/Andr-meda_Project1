/** Round to the nearest multiple of 5, halves go up (fixes I-09: no banker's rounding). */
export function roundTo5(x: number): number {
  return Math.floor(x / 5 + 0.5) * 5;
}
export function clamp(x: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, x));
}
