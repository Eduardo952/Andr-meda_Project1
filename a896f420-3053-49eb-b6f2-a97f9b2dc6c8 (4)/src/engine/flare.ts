export type FlareLetter = "A" | "B" | "C" | "M" | "X";
export const FLARE_LETTERS: FlareLetter[] = ["A", "B", "C", "M", "X"];
/** Peak X-ray flux floor per class in W/m² (each letter is 10x the previous). */
const FLUX: Record<FlareLetter, number> = { A: 1e-8, B: 1e-7, C: 1e-6, M: 1e-5, X: 1e-4 };

export interface FlareClass { letter: FlareLetter; magnitude: number; classType: string }

export function parseFlareClass(classType: string | null | undefined): FlareClass | null {
  if (!classType) return null;
  const m = /^([ABCMX])(\d+(?:\.\d+)?)$/.exec(classType.trim().toUpperCase());
  if (!m) return null;
  return { letter: m[1] as FlareLetter, magnitude: Number(m[2]), classType: classType.trim().toUpperCase() };
}

export function flareFlux(f: FlareClass): number {
  return FLUX[f.letter] * f.magnitude;
}
