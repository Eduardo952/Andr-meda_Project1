import { DERIVED, SOLS } from "@/data/campaign";
import { useCampaign } from "@/game/campaign";
import { useGame } from "@/game/store";
import { buildRequest } from "./facts";
import { narrate } from "./NarratorService";
import type { NarratorPhase, QuestionId } from "./types";

/** Ask the narrator for the current campaign state and show it in Bubble's balloon. */
export async function speak(phase: NarratorPhase, questionId: QuestionId | null = null) {
  const c = useCampaign.getState();
  const g = useGame.getState();
  const sol = SOLS[c.solIdx], derived = DERIVED[c.solIdx]; if (!sol || !derived) return;
  const taskResults = (c.results[c.solIdx] ?? []).filter(r => r.success && r.seconds !== undefined && r.mistakes !== undefined && r.stars !== undefined).map(r => ({ id: r.id, seconds: r.seconds ?? 0, mistakes: r.mistakes ?? 0, stars: r.stars ?? 0 }));
  const req = buildRequest({ lang: g.lang, sol, derived, alloc: c.alloc, outcome: c.outcome, phase, questionId, taskResults,
    game: { dose: c.dose, panelDust: c.panelDust, cleaned: c.cleanedThisSol, shelterMissed: c.shelterMissed, consequences: c.consequences } });
  const line = await narrate(req);
  useGame.getState().sayText(line.text, line.source, line.mood);
}
