# Greenhouse redesign verification

## Result
40 × 32 arched glass garden with paired hydroponic racks, nutrient channels, tower gardens, trellises, orchard trees, ripe apples, flowers, grow strips, capped mist and a patrol drone. The west tunnel remains at (32, 0); the orchard station is (48, 0). Existing campaign formulas, balance, NASA data, narrator and task availability were not modified.

## Comparable rendering counts
1280 × 1800 desktop, Sol fraction .3, same world positions, quality settings and 45-frame settling per view. Full-scene counts, not greenhouse-only. Entrance (33, 0), orchard (52, 0), overview anchored at (52, 0).

| Quality | View | Calls before → after | Triangles before → after |
| --- | --- | --- | --- |
| Low | Entrance | 75 → 85 | 22,880 → 49,612 |
| Low | Orchard | 72 → 84 | 22,176 → 51,272 |
| Low | Overview | 161 → 146 | 37,182 → 63,594 |
| High | Entrance | 146 → 167 | 59,617 → 126,981 |
| High | Orchard | 131 → 156 | 55,505 → 125,969 |
| High | Overview | 221 → 218 | 72,819 → 136,595 |

Low close views remain below 100 calls; overview exceeds that aspirational target. All Low views remain below 100k triangles. Added geometry is intentionally concentrated in the larger garden. Counts do not establish device frame rate.

## Checked
- All 54 tests pass, including six focused greenhouse tests; current automatic build signal is OK.
- Actual Energy Panel values 0 / 10 / 20 yield deficit / moderate / lush visuals respectively, using the existing 20 demand. Moderate is presentation-only, not an engine status.
- Walked through the west entrance using the existing movement stepper; reached the orchard and opened existing T14 through USE.
- Collected a ripe apple through the localized harvest control; basket read back 1 and allocations remained unchanged.
- Completed the unchanged five thirsty plants in T14; basket stayed 1 afterwards.
- English and Brazilian Portuguese learning card, basket and controls verified.
- Desktop 1280 × 1800 and touch phone 390 × 844 screenshots inspected. Additional 813 × 858 checks cover the current preview size, exterior glass vault and Roof Off.
- Roof cutaway removes overhead glass/ribs in top-down or Off while leaving the floor visible. Occupied Auto uses .025 opacity.
- Session-only harvest resets when the game screen starts. Duplicate, unripe and out-of-range collection rejected by tests; no campaign write.
- No page exceptions in the final interactive checks; shadows remain disabled on touch devices.

## Limits and provenance
The general learning explanation is explicitly marked Source pending; no quantities or invented scientific references were added. Live NASA/n8n service verification remains blocked by the pre-existing missing credentials/authentic dates.

The bundled generic 3D harness timed out at the title/report transition at 813 × 858 and logged React root-unmount warnings there; custom interaction checks at that same viewport subsequently rendered the vault and Roof Off with no page exceptions. This harness limitation is not counted as a clean harness pass.

Real-GPU appearance, frame rate, physical touch feel and audible sound cannot be established here. Open the preview on the target device, walk to the greenhouse, orbit the orchard, harvest an apple and compare Low/High. Use Settings → Reduced motion to disable drone/mist animation if desired.

## Architecture
Shared bounds and station coordinates remain in config/rooms.ts. Greenhouse shell/interior rendering is focused in its scene module; repeated crops/fruit are instanced, static hardware merged by material, particles capped at 12 Low / 28 High. Cosmetic harvesting and visual mapping are isolated in game/greenhouse.ts. Dev-only __game.greenhouse.state reports visual bands and basket; __game.view(yaw, pitch, distance) sets a verification camera without importing extra game module instances.