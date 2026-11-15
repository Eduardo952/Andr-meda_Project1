# Base ARES — one connected base, five regions

## What changes

Replace the archive’s 23 scattered modules, including bedrooms, kitchen and other extra rooms, with one substantial connected station. Keep its vibrant rounded low-poly art, astronaut, Bubble narrator, five-Sol campaign, data provenance, English/Portuguese and existing camera controls. All new base art is original code-built geometry; NASA pictures are scientific display content, not reused base artwork.

The archive already includes partial interiors and fading, but its tunnels are elevated exterior objects, not walkable rooms. Rebuild these as real continuous interior routes rather than just resizing the current buildings.

## Layout and proposed sizes

Dimensions below are artistic game units, not NASA engineering measurements. North is up; coordinates are `(x,z)`.

````text
                        OBSERVATORY (0,-42)
                       24 × 18; telescope cap
                      APOD + Image Library wall
                               ║
                      19-unit sealed tunnel
                               ║
 MAGNETIC SHIELD        CONTROL ROOM HUB          GREENHOUSE
    (-44,0)  ═════════      (0,0)      ═════════    (44,0)
     26 × 18          28 × 28 cylinder             24 × 24
 coil/emitter tower    glass ring skylight        geodesic dome
 reinforced refuge     ONLY 3 energy sliders      grow beds
                               ║
                      19-unit sealed tunnel
                               ║
                      LIFE SUPPORT (0,42)
                      26 × 18 tank/recycler hall
                               ║
                     AIRLOCK transit (0,57)
                       6 × 10; not a region
                               |
                    SOLAR POWER PARK (0,82)
                  48 × 26 panels + junction box

                 Hexagonal landing pad (43,76)
               exterior prop, not a room or task area
````

- Four main wings, one hub and one exterior source region; exactly five named regions besides Control Room. Airlock is a transition, not an additional station.
- Main tunnels have a 5-unit clear walkable width and approximately 4-unit interior height. Module doors line up with tunnel ends; station consoles sit inside, clear of walking lanes.
- At the unchanged 5.2-unit/second walking speed, hub-to-wing center is roughly 8 seconds; opposite wings roughly 17 seconds before turns. Confirm emergency routes remain feasible under the existing 60-second rule.
- Spawn inside Control Room. Solar Park can be explored on foot through Life Support’s Airlock. Landing pad, cables and berms do not introduce rooms or mechanics.

## Appearance and visibility

- Broad supported cylinders and rounded armored utility blocks, thick floors, ribbed seams, instanced bolts and struts, ramps, pipes, ladders, antennas, chunky berms and path lights. Turquoise hub, blue shield, cyan Life Support, green greenhouse and yellow Solar Park; Observatory uses a contrasting warm accent from the existing palette.
- Observatory: telescope dome and a large dual-picture display. Shield: coil tower and restrained blue protective bubble covering the pressurized station. Life Support: O2/water tanks, pipes, recycler and vents. Greenhouse: geodesic glass roof, planted beds and grow lights.
- Glass roofs remain distinct from solid walls. **Auto:** fade occupied roofs and camera-obstructing walls; isometric/high-pitch views make every roof ghost-transparent. **Off:** hide all roofs while preserving low wall outlines, floors and door frames.
- A single saved roof setting drives HUD and Settings controls. All new visible labels, statuses and explanatory text live in the two locale files.
- Region labels fade by distance; the task trail follows the actual reachable tunnel/path graph, not a line through walls.

## Tasks and unchanged rules

| Region | Existing tasks |
|---|---|
| Observatory | T02 flare classes; T10 photo album; T11 APOD question, only if the optional task setting permits it |
| Magnetic Shield | T03 CME; T04 WSA+Enlil; T05 radiation refuge; T06 notifications |
| Life Support | T07 thermal; T15 recycling |
| Greenhouse | T14 plantation |
| Solar Power Park | Retain T08 wind activity here, to preserve existing gameplay without a sixth region |
| Control Room | Energy allocation only; no Observatory slider |

T11 is mentioned in the brief but is not implemented in the archive. Add its optional APOD interaction without enabling optional tasks by default or changing rewards/formulas. Move T05’s existing refuge completion to the Shield interior; remove the old shelter building and update every shelter-dependent action and label.

Keep the 100 kWh baseline, slider step, demands, margins, damage, task rewards and Sol timers exactly as the source implements them. Shield glow/flicker, Life Support tint and plant growth/wilting read the existing allocations and demands, not a second model. Solar panels follow the artistic sun direction and display the same pool used in Control Room. Leave the optional weather modifier unimplemented/off; record it as PROVISIONAL, “to confirm,” rather than alter the pool this step.

## Observatory data design

1. At each Sol start, request Observatory content using the selected campaign Sol, not an arbitrary client-supplied API URL. Validate and resolve its Earth date on the server.
2. Add a typed APOD server function. Read `NASA_API_KEY` only inside its handler; never create a fake key or put one in browser code. Use `DEMO_KEY` only when an explicit server configuration allows it; otherwise use the snapshot.
3. Fetch `https://api.nasa.gov/planetary/apod?date=YYYY-MM-DD&thumbs=true&api_key=…`. Validate response fields, set a timeout, deduplicate concurrent same-date requests and cache successful results by date with bounded retention. Keep caching in the server instance initially and describe its cold-start limitations honestly.
4. On missing key, invalid/sample date, network failure, invalid response or rate limit, return the selected local snapshot’s `apod` field with `source: snapshot`. Never label invented material as live NASA data. If no authentic snapshot is supplied, show a localized unavailable/sample state instead of fabricating an APOD.
5. Extend raw/normalized data with optional APOD and richer Image Library metadata without modifying demand derivation. Use the NASA Image and Video Library public search/detail endpoints server-side, with cached image metadata and local fallback.
6. Display title, short kid-friendly explanation, credit/copyright and provenance. APOD videos show the provided thumbnail and safe external link; absent thumbnails show a localized video state. Label authentic APOD content **REAL DATA: APOD** with a separate **snapshot** marker when appropriate; sample content remains visibly sample.
7. Keep localized explanatory framing in locale files; fetched NASA titles and credits remain attributed source data. Prefer curated bilingual short explanations for known snapshots rather than silently pretending untranslated NASA text is localized.

Cloud is required for secret-backed NASA and the existing n8n proxy. Enable it after plan approval; retain n8n transport and local narrator fallback. Missing keys block live verification, not the base preview.

## Components and technical work

- Shared `rooms.ts` layout: hub, regions, transit Airlock, bounds, door ports, tunnel links and interior station coordinates.
- Focused base scene pieces: Hub, HabitableModule, WalkableTunnel, ObservatoryDisplay, ShieldEmitter/Bubble, LifeSupportProps, GeodesicGreenhouse, SolarPark and instanced infrastructure.
- Roof/wall visibility utility with separate material groups, so removing roofs never fades floors or unrelated modules.
- Update `world.ts`, camera clearance, task registry/actions, minimap and guidance to consume the same layout. Keep runtime movement outside React and preserve the existing camera rig.
- APOD/Image Library function module, validated DTOs, snapshot fields and Observatory UI; saved roof preference in the existing settings store.
- Import only safe source; exclude repository metadata, dependencies, generated files and secrets. Retain the target project’s pinned TanStack versions. Record structural decisions in `AGENTS.md`.

## Performance and verification

- Before replacement, measure archive baseline Low/High at spawn, an interior and the same elevated overview. Save camera/time conditions and renderer draw calls/triangles; repeat after replacement. These measurements cannot be honestly supplied until the archive runs.
- Instance panels, bolts, rocks, lights and struts; merge static geometry by shared material. Separate fadeable shells from merged floors and props. Use a small number of emissive materials, not a light per lamp.
- Mount detailed interiors only near the player with hysteresis; ensure isometric view still shows simple floors and station silhouettes in distant modules. Shadows stay off on touch devices; keep Low/High settings.
- Targets: Low under 100 calls/100,000 triangles, High under 300 calls/500,000 triangles, subject to measured complete-scene results. Minimize transparent bubble/roof overdraw.
- Run existing engine, campaign, narrator and camera tests. Add concrete tests for five regions, task assignments, unchanged 100 kWh pool, connected interior routes, Airlock access, walls/doors, roof visibility and APOD date/cache/fallback behavior.
- Verify real UI interactions: start campaign, walk hub → each wing → Airlock → Solar Park, open station tasks, adjust energy, read Observatory, toggle roofs/cameras/quality/language, and inspect desktop/mobile screenshots. Report live NASA/n8n checks separately from sample-mode checks.
- Sandbox metrics and screenshots are verifiable; real-phone touch feel, GPU frame rate and audible sound remain device checks.

## Open questions and defaults

- The separately mentioned **Context Skill v3.0** and **reference image** are not present in the two attachments. Proceed using the code’s current rules and the written composition; attach those files if exact comparison is required.
- All five campaign Earth dates are sample placeholders, and there are no APOD snapshots. Preserve their sample status until authentic dates/snapshots are supplied; do not invent a campaign date mapping.
- NASA and n8n credentials will be requested securely when needed for live checks. No placeholder secrets will be stored.
- Default resolution: T08 remains at Solar Park; T11 stays optional/off by default. No extra rooms, online accounts, data-store changes or core formula changes.