/** Mean Sun-Mars distance: 1.524 AU ≈ 2.28 × 10^8 km. */
export const SUN_MARS_KM = 2.28e8;

/** t = distance / speed. Simplification: constant speed. Returns hours, or null without a speed. */
export function cmeTravelHours(speedKmS: number | null): number | null {
  if (!speedKmS || speedKmS <= 0) return null;
  return SUN_MARS_KM / speedKmS / 3600;
}
