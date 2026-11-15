import sol1 from "./sol1.json";
import sol2 from "./sol2.json";
import sol3 from "./sol3.json";
import sol4 from "./sol4.json";
import sol5 from "./sol5.json";
import type { RawSnapshot } from "@/engine/normalize";

/** Raw layer: frozen local snapshots. No live API calls, no API keys. */
export const RAW_SNAPSHOTS = [sol1, sol2, sol3, sol4, sol5] as unknown as RawSnapshot[];
