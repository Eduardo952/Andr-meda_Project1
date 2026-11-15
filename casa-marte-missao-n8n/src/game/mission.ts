/**
 * Mission id: one random id per new game, kept for all 5 Sols. The n8n workflow derives every real date from it
 * (seeded), so the game never chooses or sends dates. Same id = same mission; a new game = a new mission.
 * Format must match MISSION_ID_RE in lib/n8n.ts (8-64 chars of A-Z a-z 0-9 _ -).
 */
export function newMissionId(): string {
  try {
    if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") return crypto.randomUUID();
  } catch { /* fall through */ }
  return Array.from({ length: 4 }, () => Math.random().toString(36).slice(2, 10).padEnd(8, "0")).join("-");
}
