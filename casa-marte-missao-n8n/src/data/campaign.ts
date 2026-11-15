import { RAW_SNAPSHOTS } from "./snapshots";
import { deriveSol, normalizeSnapshot } from "@/engine/normalize";

export const SOLS = RAW_SNAPSHOTS.map(normalizeSnapshot);
export const DERIVED = SOLS.map(deriveSol);
export const ANY_SAMPLE = SOLS.some((s) => s.sample);
