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
- Tactile task assignments, scoring and numeric origins stay in pure task modules; campaign state owns successful results and sealing so UI retries cannot change energy rules or grant duplicate rewards.
- Keep task components lazy-loaded by their registry IDs, pause Sol/alarm clocks in overlays, and evaluate a Sol only through confirmed report sealing to prevent alternate completion paths.
- Dev-only window.__game.tasks.preview(id, solIdx) opens task variants for input checks without importing game modules; normal Sol flow verification must still use USE and actual puzzle completion.
- World-mounted NASA HTML displays use depth-blended occlusion below the HUD so foreground characters remain visible over the screen.
- NASA library images may be fetched independently of sample Sol dates with explicit archive provenance; shared read-only NASA queries call generated RPCs without router-dependent hooks because Drei Html creates a separate React root.
- Sample FPS from unclamped frame intervals in the existing stepper and read renderer triangle metrics in the HUD at low frequency, so performance counters do not change simulation timing or trigger per-frame React updates.
- Keep greenhouse cosmetic harvesting and energy-to-visual mapping in game/greenhouse.ts, outside campaign rewards; scene indicators consume the existing allocation and demand.
- Keep the greenhouse vault and instanced growing systems in its focused scene module, with ribs and glass in the roof group so cutaway never hides the floor.
- Keep telescope proximity and model placement in config/telescope.ts; observing is presentation-only runtime state and must reset when entering or leaving play.
<!-- AGENT-MEMORY:END -->
