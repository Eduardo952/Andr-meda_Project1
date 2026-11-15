# Base ARES — visual refinement

## Scope and safeguards

Refine the existing playable game, not replace it. Preserve all energy formulas, slider steps, rewards, demands, damage, Sol timers, task order, campaign data, NASA requests, and narrator backend. Preserve the bilingual system: English is default; all new authored interface text belongs in `src/locales/en.json` and `src/locales/pt-BR.json`. Original code-built artwork and existing camera controls remain.

## Before → after

### 1. One visual language
**Before:** Unrelated cyan consoles, dark roofs, pale/grey floors, and decorative emissive props compete with each other.

**After:** One shared theme: milk-white rounded hulls, subtly tiled warm sand floors, rust-orange terrain, and region accents: coral Control Room, violet Observatory, blue Shield, teal Life Support, green Greenhouse, yellow Solar Park. Three-tone toon materials, warm illumination, consistent doors and props. Only usable stations receive soft pulsing accent rings and icons; decorative props have no glow. Status indicators stay small and non-blooming. Remove unexplained pedestals, floating blocks, and decorative domes; preserve purposeful roofs and the Shield’s meaningful visual state without a distracting hub overlay.

### 2. Control Room
**Before:** Scattered cyan screens and a central pedestal crowd the walking space; the energy station is not visually obvious.

**After:** A circular command deck with a curved energy console against a solid wall segment, facing inward. Three oversized lever/knob forms mirror the existing allocations; the Energy Panel retains the exact same slider behavior. One wall display shows the Sol clock and next objective, plus two simple seats. Clear central walking floor and an understated center ring. Each doorway receives a region-colored arch, pictogram, and floor arrow. The Solar direction is signed through Life Support and the Airlock, rather than adding a nonexistent direct door.

### 3. Minimap
**Before:** Small loose shapes and thin lines without useful region icons, current-location text, or map navigation.

**After:** A real floor plan using the same bounds, shapes, and corridors as the world. Region pictograms, a rotating player arrow, a pulsing next-task marker, and the current region beneath the map. A rounded map card with safe phone sizing; an expand button opens a larger map where tapping a region sets a navigation-only target. North-up by default, with a camera-up toggle. Manual destinations never alter objectives, task order, or rewards; urgent objectives take priority and clearing the destination restores task guidance.

### 4. Labels and details
**Before:** All region labels remain visible from afar, another room label appears on screen, and floating energy/provenance badges crowd the scene.

**After:** One shared world-label controller, prioritizing the nearest usable station over a briefly displayed region-entry label. Smooth transitions, maximum two lines, larger high-contrast text, and edge/HUD exclusion zones. Remove duplicate room labels and the floating black energy badge. Put the small GAME RULE badge inside the Energy Panel; keep provenance in information popovers and Sol cards, except the explicitly requested NASA source badge inside image cards. If there is no safe screen position, hide the label rather than cover the player or controls.

### 5. NASA image presentation
**Before:** A basic two-column dialog, no image-failure handling or skeleton, and limited small-screen protection.

**After:** Reusable NASA media cards in the Observatory and Sol-start report: large contained images, rounded corners, loading skeleton, friendly unavailable/broken-image state, two-line title, three-line bilingual framing with Read more, original credit, clear APOD/Image Library source, and snapshot tag only when a snapshot is actually used. Video uses the existing thumbnail when available and an external link; never invent imagery. Scrollable image panels have a clear close button and reserved space around HUD and touch controls.

**Resolved:** Captions use short bilingual summaries authored in locales. Original NASA titles and credits remain external source content. Do not translate or invent scientific specifics that the existing data does not supply. Current campaign picture fields are empty, so fallback states remain honest.

### 6. One Bubble interface
**Before:** A second 3D fish canvas and a separate question button sit at the right edge; permanent speech competes with the world companion.

**After:** Keep one small 3D Bubble beside the astronaut. Replace the right-edge avatar and question button with one 56 px fish-face HUD button, bottom-right with safe margins separate from the Use button and joystick. It opens a compact slide-up panel containing the current tip, existing Ask Bubble questions, and a close button. Speech toasts anchor above this button, queue sequentially, and dismiss after a few seconds. A Settings switch mutes speech toasts without changing narrator requests or audio settings. No extra WebGL canvas for the HUD button.

### 7. Astronaut
**Before:** A narrow egg-like silhouette, thin limbs, basic backpack, and minimally readable visor.

**After:** An original rounded capsule suit with more balanced proportions, short thick limbs, chunky boots, a tinted reflective helmet visor with a soft face highlight, compact backpack/status indicator, arm mission patch, and a consistent accent stripe. Keep the current model height, world footprint, and collision. Refine existing idle breathing, limb swing, and completion jump; add a brief greeting wave for Bubble. Do not add new movement or jump mechanics, rewards, or suit-customization controls.

### 8. First-walk guidance
**Before:** A generic yellow beacon/trail points toward a station without explaining the base layout.

**After:** A short guided route introduces the Energy Panel and each of the five regions, with one-line localized hints and subtle directional trail. It runs alongside missions, is dismissible, and never gates tasks or grants rewards. Existing urgent objectives and the Next Task arrow take priority; tutorial stops are resumed only when they do not redirect an active objective. No pauses or changes to Sol timing.

**Resolved:** First-walk hints appear alongside normal missions, not as a separate pre-mission tour.

## Performance and verification

- Capture a fresh before baseline before scene edits, then measure after at identical spawn, hub interior, and overview positions, Sol time, camera settings, and Low/High quality.
- Previous reference measurements: Low spawn 107 calls / 26,868 triangles; Low interior 89 / 24,084; Low overview 147 / 33,196; High spawn 171 / 56,645; High interior 179 / 58,505; High overview 231 / 68,153. These are historical references, not substitutes for new measurements.
- Retain geometry merging, instancing, nearby-interior rendering, Low/High settings, and shadows off on touch. Batch tile seams, door markings, and static props; reuse materials. Avoid decorative point lights and extra UI canvases.
- Target approximately 100 calls and under 100,000 triangles in Low; disclose actual results and any views exceeding the target rather than claiming an unmeasured frame rate.
- Check desktop and phone layouts, all four door directions, energy-console interaction, expanded-map guidance, single-label selection, Bubble queue/mute, image loading/error/video states, both languages, and reduced motion.
- Run existing game-rule tests unchanged and add focused tests for label priority, navigation-only target precedence, sequential toast handling, and non-gating tutorial progression. Confirm energy allocation and task completion still follow the existing rules.
- Report build status, before/after rendering counts, and any remaining device-only checks. Real GPU frame rate, sound, and touch feel cannot be measured faithfully in the sandbox.

## Technical implementation

- Create `src/config/theme.ts` for scene palette/material presets and mirror the roles as semantic CSS tokens in `src/styles.css`. Keep geometry, region bounds, ports, and station coordinates authoritative in `src/config/rooms.ts`; synchronize the moved energy interaction point with its wall console.
- Separate shared world labels, navigation presentation, NASA media cards, and Bubble HUD into focused modules. Keep presentation state separate from campaign resources and outcomes; do not change the simulation’s resource model.
- Use the existing NASA server-function results and narrator interface unchanged. No new service, backend, data seeding, or generated captions.
- Keep all animations in the existing frame loop/ref-based rendering, honor reduced motion, and use existing design-system controls.
- Record structural decisions in `AGENTS.md` and keep the roadmap current.

## Open questions and limitations

- No further decisions block implementation after the two answers above.
- Live NASA images and narrator service verification remain dependent on authentic dates/images and the existing service credentials. This refinement will not fabricate or replace those values; unavailable-image states can be fully verified now.
- Route guidance to a tapped map region is a visual aid only; mission and alarm destinations remain authoritative.
- Suit stripe customization is deliberately deferred, as requested.