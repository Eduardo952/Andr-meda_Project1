# Consequence system verification

## Intentional rule changes
- `BALANCE.marsWeatherModifier` is now enabled. Storms are always labeled simulated events. InSight wind above the provisional 12 m/s threshold can trigger one; missing wind uses fixed training Sols 2 and 4. No random outcome or dust-opacity measurement was added.
- Four array dust levels persist between Sols. Average dust reduces the next Sol's allocation pool, rounded to the existing 5 kWh steps and capped at 20% (80 kWh minimum). Existing demand and outcome formulas are unchanged.
- Crew dose is cumulative and uses game units: shield missing × 0.8 plus 35 for a missed shelter timer. A worst-case first Sol adds 95, so the fatal 100 threshold cannot occur without advance warning. Victory now also requires dose below 100.
- Required-task sealing and once-only accepted rewards remain intact. Failed/cancelled attempts still do not write campaign results; unfinished tasks cannot bypass the existing seal gate.

## Automated checks
- 103 Vitest tests pass across 14 files.
- `src/test/consequences.test.ts`: wind threshold, deterministic fallback, bounded accumulation, 1/2/3-pass cleaning bands, exact 20% cap, dose/surplus rules, first-Sol safety, numeric causes, perfect reward, severity and success tiers.
- Campaign tests confirm dose only at sealing, the dust-capped pool, once-only confirmed cleaning and the radiation ending.
- Narrator tests accept whitelisted consequence numbers and reject invented dose or absent dust storms. EN and pt-BR nested keys remain identical.

## Browser checks
- Real flow completed at 1280×1800: Play → report → Control allocation 40/40/20 → all five actual USE tasks → confirmed seal → Sol 1 summary → Next Sol. Five successful results read back; dose 0 and the actual surplus consequence were shown.
- Dev-only `window.__game.events.force('solar')` displayed the shelter alarm. `force('dust')` displayed a labeled simulated storm and set the test pool to 90 kWh for dust [80,60,40,20]. Alerts were acknowledged through Recovery and cleared.
- CleanPanels was exercised with the forced dust state. Desktop mouse and 390×844 touch-emulated runs completed all required passes, confirmed success, set `cleanedThisSol` and finished the 3D robot animation. Before/after output readouts showed 84/88/92/96% before cleaning and restoration afterward.
- Screenshots: `/tmp/browser/consequences/screenshots/` and `/tmp/browser/consequences/debug-cleaning.png`, `mobile-cleaning.png`. Temporary sandbox images are not durable deliverables.

## Performance and limitations
- A Low-quality Sol 1 summary sample reported 36,156 rendered triangles and 3 FPS in the software-rendered sandbox. Camera and scene differ from earlier samples, so this is not a controlled before/after benchmark. No new dependency or full-screen SSAO pass was added; particles are instanced and Low quality omits the extra emergency point light.
- Real-device frame rate, touch feel, speaker output and siren/chime audibility remain unverified. The sandbox exercised the audio code paths but cannot establish physical sound quality.
- Existing Drei-root teardown warnings appeared in console during navigation; no page exception interrupted the successful focused cleaning runs. Live n8n/NASA credentials and physical-phone checks remain outside this verification.
