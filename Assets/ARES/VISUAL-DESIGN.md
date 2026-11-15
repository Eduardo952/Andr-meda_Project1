# Ares Base — presentation system

The Unity presentation is a playful educational Mars base: warm ceramic, midnight blue structure, coral, turquoise, gold, green horticulture and lavender astronomy. Fredoka titles and Nunito body text remain native UI text. Important feedback uses words and icons as well as color.

## Ownership

The visible product name is **Ares Base** in both languages. `Mission*` code/file names describe presentation modules and do not define the public brand.

`PresentationLocalizer` bridges older hardcoded copy to the existing catalogs plus `presentation-translations.json`. It supports anchored parameter templates, live values and switching back to Portuguese. The final refresh runs after terminal updates to avoid language flicker. Editable text fields are excluded; mission records and research sources remain unchanged. New screens should prefer keyed `Localization.T` entries. Add presentation-only source/translation pairs for older views, never translate identifiers or gameplay state.

- `Runtime/UI/Presentation/MissionVisualStyle.cs`: palette, sector icons and reduced-motion-aware popup transitions.
- `Resources/ARES/UI/MissionTheme.uss`: shared rounded controls, activity boards, computer, maps, reports and menu. Loaded after existing feature styles. Keep feature class names and interaction contracts stable.
- `MissionMainMenu`: illustrated entry screen; callbacks still belong to `GameFrontend`.
- `MissionReportView`: renders the actual `SolOutcome`. Its scrollable body and fixed footer keep the next action accessible. Sending the report to Earth still gates day advancement. There is no food inventory in the model; the view does not invent food rewards.
- `Runtime/World/Presentation/MissionHabitatArt`: shared runtime material variants and sector portal finishes. Runs before `ReferenceWorldBatches`; imported meshes, navigation, physical colliders and interaction coordinates remain authoritative.
- `MissionAstronautArt`: face, communications pods, suit details and contextual helmet motion on the existing rig. Crew expressions use their original local coordinates.
- `MissionStormPresentation`: reads the real alarm and integrity. A 2.5-second onset and 4-second recovery drive the existing shield renderer. No gameplay writes or per-frame material allocation.
- `MissionGeometry`: reusable low-resolution round detail mesh and removal of zero-area authoring cap triangles. New objects are cosmetic; they do not obstruct doors or change the navigation graph.

## Rendering budget

Sector decorations are grouped by sector/material into three meshes; the doorway opening is six metres wide. Local lights have no shadow maps and are restricted to inhabited sectors. Existing world static batching remains enabled. Material variants are cached by source material and destroyed with the scene. Rounded character details share one 520-triangle sphere. The shield uses its original single surface draw with anti-aliased hex lines and restrained impact waves; no additional full-dome mesh layers or particle bursts.

The toon material supports soft lighting bands, controlled highlights and URP additional lights. The terrain keeps imported shape/collision data, reducing fine-grain bump noise. The sky uses a cool upper gradient and warm Martian horizon. Storms reduce daylight gently rather than blacking out navigation. Reduced motion stops ambient UI drift, blinking and shield-wave travel while retaining static warning information.

## Art provenance

`Resources/ARES/UI/Presentation/mission-mars-title-v1.png` is an original illustration generated with the **imagegen skill, built-in tool**. Full prompt and provenance: `Tools/ArtEvidence/mission-mars-hero-manifest.json`. It contains no baked UI text. The tutorial speech bubble has its own manifest at `Tools/ArtEvidence/tutorial-speech-manifest.json` and preserves alpha. Existing imported world assets and source snapshots are retained.

## Review entry points

- `Temp/ares-computer-check.request` with `mission-visual`: integrated captures of the title, settings, briefing, HUD, energy, SOL 142 comparisons, compact report, astronaut and calm/storm states. Checks actual report deficits, visible action, shader compilation and new triangle areas.
- `Temp/ares-challenges-check.request`: existing fourteen-activity presentation/control review.
- `Temp/ares-scenery-check.request`: imported geometry, floor coverage, station reachability, batches, shaders and habitat captures.
- `Temp/ares-computer-check.request` with `tutorial`: spawn offer, real energy commit, four watering rows, water accounting and input/HUD restoration.
- `Temp/ares-scenery-build.request`: Windows build.

Review screenshots in `Logs/ares-mission-*.png`, `Logs/ares-challenge-*.png`, `Logs/ares-scenery-*.png` and `Logs/ares-tutorial-*.png`. These are focused integration checks, not claims of exhaustive platform or performance testing.
