<!-- LOVABLE:BEGIN -->
> [!IMPORTANT]
> This project is connected to [Lovable](https://lovable.dev). Avoid rewriting
> published git history — force pushing, or rebasing/amending/squashing commits
> that are already pushed — as it rewrites history on Lovable's side and the
> user will likely lose their project history.
>
> Commits you push to the connected branch sync back to Lovable and show up in
> the editor, so keep the branch in a working state.

## What Lovable manages

Rules outside this block are the project's own. Change the pieces below in
Lovable, not by hand. With the [Lovable MCP](https://mcp.lovable.dev)
connected, `send_message` asks Lovable to do it; otherwise ask the user to
use the editor.

- **Database.** Do not add or edit `supabase/migrations/` or `drizzle/`.
  Ask Lovable to change the schema.
- **Generated files.** Do not edit a file whose header says it is automatically
  generated.
- **AI gateway.** Keep model calls on Lovable's gateway, and keep
  `LOVABLE_API_KEY` out of browser code.
- **Connectors.** Lovable provides native connectors. `list_connectors` shows
  which are enabled; `add_connector` only returns the dashboard link. When a
  connector is enabled, calls go through the connector gateway and need
  `LOVABLE_API_KEY`. When Lovable does not provide one, users can bring their
  own key and store it in Secrets.
- **Secrets.** Lovable manages them. Do not put secret values in the repo.
<!-- LOVABLE:END -->

## Game architecture
- Keep layout, ports, station coordinates and region bounds in config/rooms.ts; rendering, collisions and minimap share them to avoid mismatched navigation.
- Keep simulation in game/sim and energy rules in engine; frame updates consume the existing campaign state rather than introducing a second resource model.
- Separate floors, walls and roof material groups; cutaway must never hide walkable floors.
- Fetch NASA and n8n only through lib/*.functions.ts; resolve Sol dates server-side and preserve snapshot/sample provenance.
- Use the existing dev-only window.__game controls for state, teleport, simulate, quality, roofs and overview verification; no runtime imports of game modules from browser tests.
- Keep navigation, first-walk progression and speech queues in game/presentation.ts, separate from campaign state, so presentation never changes mission rules or resources.
- Use config/theme.ts for scene material roles and mirror those roles in semantic CSS tokens; shared NASA cards reuse existing service responses, and Html portals explicitly receive the existing QueryClient.

<!-- AGENT-MEMORY:BEGIN -->
- Normalize n8n image responses in lib/webhook-response.ts and keep per-Sol media in a presentation-only store shared by Observatory views; this preserves NASA provenance and existing narrator validation.
- Use tasks/ChallengePanel for station overlays and read-only ChallengeReadings for snapshot/training cards; shared light styling stays isolated from telescope roles so presentation cannot change mission rules.
- Control Room desk, screen, levers and paired seats share the console transform in config/rooms.ts so scene pieces stay aligned and the central approach remains clear.
- Terrain relief and geological exclusion use game/terrain.ts with existing room/path bounds; procedural maps and selective PBR stay in scene code so polish never changes collisions or campaign weather.
- Tactile task assignments, scoring and numeric origins stay in pure task modules; campaign state owns successful results and sealing so UI retries cannot change energy rules or grant duplicate rewards.
- Keep task components lazy-loaded by their registry IDs, pause Sol/alarm clocks in overlays, and evaluate a Sol only through confirmed report sealing to prevent alternate completion paths.
- Dev-only window.__game.tasks.preview(id, solIdx) opens task variants for input checks without importing game modules; normal Sol flow verification must still use USE and actual puzzle completion.
- World-mounted NASA HTML displays use depth-blended occlusion below the HUD so foreground characters remain visible over the screen.
- NASA library images may be fetched independently of sample Sol dates with explicit archive provenance; shared read-only NASA queries call generated RPCs without router-dependent hooks because Drei Html creates a separate React root.
- Sample FPS from unclamped frame intervals in the existing stepper and read renderer triangle metrics in the HUD at low frequency, so performance counters do not change simulation timing or trigger per-frame React updates.
- Keep greenhouse cosmetic harvesting and energy-to-visual mapping in game/greenhouse.ts, outside campaign rewards; scene indicators consume the existing allocation and demand.
- Keep the greenhouse vault and instanced growing systems in its focused scene module, with ribs and glass in the roof group so cutaway never hides the floor.
- Keep telescope placement and approach parameters in config/telescope.ts; the single stepper follows pure sim/telescope paths using the existing mover, and observation resets when leaving play.
- Keep educational resource exercises in tasks/context.ts separate from campaign resources; console reward previews show the existing clamped reward, and briefing/loading time never counts as attempt time.
- Keep dose, deterministic storm, panel-dust and consequence calculations pure in engine/consequences.ts; campaign applies them only at confirmed sealing, while alert queues, audio and cleaning robots remain presentation driven by accepted results.
- Keep optional greenhouse training in game/greenhouseTrainer.ts with integer-milliliter accounting and session-only state; its lazy UI pauses clocks and never writes campaign resources or narrator facts.
- Base layout is radial: satellite angles, hub/corridor/satellite radii and outdoor LANDMARKS live in config/rooms.ts, doorways use the port direction (not axis), and the Solar Park keeps its original local frame via solarFrame/SOLAR_YAW so solar decor and cleaning paths need no rewrites.
- Hub tree, shield bubble, rocket pad, rover and scatter live in scene/BaseLandmarks.tsx as decoration that only reads campaign progress.
<!-- AGENT-MEMORY:END -->

## n8n integration
- All n8n access goes through `src/lib/n8n.ts` (URL resolution, `x-api-key`, response validation, cache) and is called only from `lib/*.functions.ts`; never read `N8N_*` secrets in browser code.
- Webhooks: `POST /bubble-narrator` (narrator) and `GET /nasa-sol?sol=N&mission=<id>` (facts, APOD, library photo). Workflow: `n8n/mars-mission-trainer_v3.workflow.json` (mission-aware; the older `n8n/mars-bubble-groq-nasa_workflow.json` still works without `mission`); guide: `docs/n8n/workflow.md`. Never commit real keys inside the exported workflow.
- Mission id: `useCampaign().missionId` is a random UUID created in `start()` (new game = new mission). The game never sends or picks dates: the workflow derives a seeded 5-Sol real window (Earth dates, Curiosity sols) and a random APOD day per Sol from the id. n8n results are cached per mission+Sol (`N8nSolCache`); every `["observatory", missionId, sol]` query must carry the id.
- Fallback order is always n8n -> direct NASA -> local snapshot; keep `source`/`photoSource` provenance truthful. Live facts from `getSolFacts` do not drive campaign rules until explicitly wired.
- Opening the game with `?debug=n8n` tags Bubble lines with `via n8n` or `via template` for webhook testing; it changes no rules.
- By the owner's explicit choice the n8n URLs and `N8N_WEBHOOK_SECRET` live in `.env` (server-only names, no `VITE_` prefix). Rotate the secret if the repository is ever made public.
