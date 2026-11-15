import { createServerFn } from "@tanstack/react-start";
import { z } from "zod";
import { MISSION_ID_RE, n8nSolCache, resolveN8n, type N8nFacts, type N8nSol } from "./n8n";

/**
 * Fatos reais do Sol (DONKI + InSight) vindos do webhook GET /nasa-sol do n8n.
 * Retorna { ok: false } se o n8n não estiver configurado ou falhar: nesse caso o jogo
 * continua usando os snapshots locais (src/data/snapshots), que seguem sendo a base das regras.
 * Ponto único para ligar fatos ao vivo ao motor quando isso for decidido (ver docs/n8n/workflow.md).
 */
export const getSolFacts = createServerFn({ method: "GET" })
  .inputValidator((input: unknown) => z.object({ sol: z.number().int().min(1).max(5), mission: z.string().regex(MISSION_ID_RE).optional() }).parse(input))
  .handler(async ({ data }): Promise<
    { ok: true; source: "n8n"; sol: number; earthDate: string; insightSol: string | null; facts: N8nFacts; effects: N8nSol["effects"]; window: N8nSol["mission_window"]; dataDates: N8nSol["data_dates"] } | { ok: false }
  > => {
    const live = await n8nSolCache.get(resolveN8n(process.env), data.sol, { mission: data.mission });
    if (!live) return { ok: false };
    return { ok: true, source: "n8n", sol: live.sol, earthDate: live.earth_date, insightSol: live.insight_sol, facts: live.facts, effects: live.effects, window: live.mission_window, dataDates: live.data_dates };
  });
