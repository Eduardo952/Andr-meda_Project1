# Base ARES — tactile station mini-games

## Confirmed choices
- One required task per **sector**, not per individual equipment: Observatory, Magnetic Shield, Life Support, Greenhouse and Solar Power Park. Control Room adds energy planning before departure and report sealing after those five tasks.
- Pause the Sol clock and alarm countdown while a mini-game is open. Exploration advances the clock, but reaching the end of the day cannot bypass required tasks or report sealing.
- Sealing repeats the player's **chosen energy allocation**, not ideal demand. It confirms rather than reallocates energy.
- Original names, code-built artwork and mechanics. No impostors, sabotage, violence, multiplayer chat or copied assets.
- English code; all authored player-facing text in `en.json` and `pt-BR.json`, English default. NASA titles/credits remain attributed source content.

## Before → after
| Area | Before | After |
|---|---|---|
| Assignments | Tasks are filtered by available data; some sectors offer several tasks | Exactly one seeded selection per sector each Sol, independent of missing readings |
| Interaction | Small modal quizzes and sliders, eagerly imported | Full-screen tactile task overlays, individually lazy-loaded, mouse/touch/keyboard |
| Progress | Pending tasks and guide arrow | Compact five-sector checklist, progress bar, pending station glow and trail; Control becomes the final target |
| Completion | Timeout can record failure; the Sol can end before all tasks | Retry without damage, cancellation without penalty; successful completion required before sealing |
| Energy | Three 5-kWh sliders, 100-kWh pool | Same allocation rules, presented as large tactile levers; Start Sol only at zero free energy |
| Report | Sol ends through the current clock/action flow | Return to Control, match three confirmed allocation values and press the sealing button; evaluate the existing energy outcome once |
| Bubble | Receives weather/events and energy outcomes | Also receives actual completed-task IDs, elapsed time and mistakes; cannot invent completion facts |

## Tasks per station
All names below are the English localization entries. Pointer gestures always have a keyboard and tap alternative. Badges and full provenance appear in an accessible detail popover, not over the playing surface.

| Sector / task file | Controls and completion rule | Data source / distinction |
|---|---|---|
| Observatory — `SortFlares.tsx`, “Sort the Flares” | Drag A/B/C/M/X cards into ascending order; tap-select then place, or keyboard move controls. A second short placement locates the Sol's observed class. Never cap X magnitudes at X10. | DONKI `FLR.classType` from the existing snapshot; classification order is a scientific reference, not five invented observations. Show snapshot/sample provenance. |
| Observatory — `PhotoDetective.tsx`, “Photo Detective” | Inspect the NASA image and select its matching caption card; correct placement completes the task. | Existing Observatory service: NASA Image Library/APOD response and its actual metadata. Correct caption derives from that displayed image, never an unrelated sample illustration label. Offline: bundled snapshot illustration with an explicit sample/art badge and its own metadata, not a claimed NASA photograph. |
| Shield — `AlignCoils.tsx`, “Align the Coils” | Rotate three dials onto visible target marks; large rotate buttons and arrow keys. Severity affects mark count/motion within a readable cap. | GAME RULE geometry/difficulty inspired by existing snapshot severity; actual flare/CME readings, if displayed, retain DONKI provenance. |
| Shield — `WireShield.tsx`, “Wire the Shield” | Connect paired endpoints by dragging or selecting both ends. Colors also have distinct symbols/patterns. | GAME RULE wiring puzzle, not a measurement or real shield engineering claim. Learning line cites only present Sol facts, or explicitly labels the game rule. |
| Shield — `CmeCountdown.tsx`, “CME Countdown” | Drag the speed marker onto the snapshot CME speed, then place/read the corresponding travel-time card. Adjustable range includes the actual reading. | DONKI `CMEAnalysis.speed`; travel time reuses `cmeTravelHours`, registered Sun–Mars distance and unit conversion. Explicit constant-speed simplification; not a predicted real arrival date and not a substitute for WSA/Enlil arrival metadata. |
| Life Support — `PipeFlow.tsx`, “Pipe Flow” | Rotate pipe tiles until two colored-and-symbolized routes reach oxygen/water tanks. Tap or keyboard rotate. | GAME RULE routes; no invented oxygen/water sensor readings. A present InSight temperature may supply the learning line, separately attributed. |
| Life Support — `ThermostatDial.tsx`, “Thermostat Dial” | Read outside temperature, turn the heating dial to the existing calculated requirement, confirm. Tap steps/arrow keys as alternatives. | InSight `AT.av`; heating requirement uses the existing provisional thermal formula in `balance.ts`, explicitly GAME RULE derived from that reading. No new thermal formula. |
| Greenhouse — `SmartFarmCare.tsx`, “Smart Farm Care” | Place drops on marked thirsty beds, set a light level, harvest marked ripe apples. Selection-and-placement and keyboard alternatives. | GAME RULE care targets, using existing greenhouse presentation conventions. Never imply real soil/light/moisture measurements. Mini-game harvest cannot grant a second campaign reward or alter the separate cosmetic basket. |
| Solar — `CleanPanels.tsx`, “Clean the Panels” | Wipe dust patches with pointer strokes; tap individual patches or keyboard clear selected patches. | GAME RULE dust amount. Weather modifier remains OFF. If enabled, only available snapshot fields can influence dust; badge: GAME RULE inspired by Mars weather. |
| Solar — `TiltSun.tsx`, “Tilt to the Sun” | Rotate panels to the highlighted target; drag dial or use step buttons/keys. | GAME RULE target from the existing simulated Sun direction, not a measured NASA solar angle. |
| Control — `EnergyPanel.tsx`, “Energy Panel” | Three large levers, step 5 kWh; allocation stays within 100 kWh. Start Sol enabled only when Free Energy = 0. | Existing `balance.ts` pool/steps and existing allocation logic; GAME RULE. |
| Control — `SealSolReport.tsx`, “Seal the Sol Report” | After all five successes, slide three confirmation levers to the recorded allocation, then press the large seal button. Closing cancels without evaluating the Sol. | GAME RULE confirmation of player values. Existing demand, damage, outcome and five-Sol victory proposal remain unchanged. |

## Sol lifecycle and non-punitive retries
1. Read the existing Sol report and assign one task from each sector's fixed list, seeded by Sol number. Revisits/reopens do not reroll; a new Sol gets its own deterministic selection.
2. Walk to Control, allocate the same 100-kWh budget and Start Sol only at zero free energy. Store the confirmed allocation for the report.
3. Follow the pending checklist/trail. USE opens only that sector's assigned task. Completed sectors do not grant repeated rewards.
4. Each task targets 30–60 seconds of play. The provisional 60-second attempt limit gives a friendly retry state, never integrity/oxygen loss. Wrong moves add mistakes, not damage. Close/Escape discards an unfinished attempt without spending resources or recording completion.
5. Successful tasks save their result and award stars once. Preserve the existing success reward exactly once per selected task; do not introduce extra energy/damage effects. Failure never calls the existing permanent-completion path.
6. Keep existing environmental energy evaluation and shelter/alarm mechanics separate from mini-game failure. A shelter response is not a sixth checklist task and does not create an extra required/rewarded mini-game. Urgent guidance still takes priority while exploring.
7. At day-end, hold the clock at the end of the current Sol until the player finishes and seals; do not advance to another data snapshot or auto-evaluate. Pause clock/countdowns in the task overlays.
8. All five tasks complete → trail points to Control → seal the stored allocation → call the existing outcome evaluation exactly once → summary → existing next-Sol/victory rules.

## Provisional scoring and numbers
- Proposed scoring in `balance.ts`, clearly marked PROVISIONAL: **3 stars** for success within 30 seconds with zero mistakes; **2 stars** for success within 45 seconds with at most two mistakes; **1 star** for any other successful attempt within the 60-second limit. Failed/cancelled attempts award no stars and never lower a previous result.
- Energy setup and sealing are confirmation actions, not repeatable sources of stars. Five sector tasks give a maximum of 15 stars per Sol.
- New puzzle tolerances, target counts, difficulty caps and scoring constants belong in the same provisional rule configuration. They never overwrite NASA data or existing energy formulas.
- Every displayed numeric value uses a registered-origin entry: snapshot field/provenance, scientific reference, existing derived formula, player input/result or provisional GAME RULE. Include timers, mistakes, stars, counts, dial angles, energy values, fallback values and unit conversions.
- Test task view-model outputs against that registry; UI renders registered values rather than unrelated inline numeric literals. This verifies runtime values, not just text scanning.

## Data honesty and learning lines
- Current five campaign snapshots are **samples**, with sample dates and mostly placeholder image metadata. They must remain visibly sample/snapshot data; do not relabel them as live observations.
- Task selection is never filtered out for missing readings. Show localized “no reading this Sol” and use an explicitly labeled snapshot practice scenario when needed. A fallback's origin is separate from the current Sol; its learning sentence cannot claim it was observed today.
- Photo tasks use verified response metadata for a loaded archive image. If neither image nor matching caption metadata is available, use the labeled snapshot practice illustration; never leave the task blocked by a broken image request.
- One sentence on success: a fact in the task's actual data, or a clearly marked game-rule learning statement. No invented horticulture, weather, solar or shield science. Keep scientific references distinct from measured values.

## Accessibility and presentation
- Full-screen, lightweight DOM/SVG overlays with original regional art and the current theme. Stable layout on phone/desktop; primary targets at least **48 × 48 px**.
- Focus enters the dialog, stays inside, then returns to USE on close. Escape and a visible close button cancel. Named controls, visible focus and live completion feedback.
- All drag/wipe/rotate gestures have tap and keyboard equivalents. Never rely on color alone. Respect reduced motion; cap animation speed and avoid flashes.
- Compact checklist/progress UI must not obscure the Observatory images, joystick or Bubble. Hide irrelevant gameplay controls while a task overlay is open; restore on exit.

## Technical implementation
- Extend the existing task registry with sector lists and a pure deterministic assignment function. Individual task components live in `src/tasks/<id>.tsx` and load through `React.lazy` with a lightweight fallback.
- Shared task frame handles focus, attempt timer, mistakes, cancellation, retry and success; pure TypeScript modules own scoring, puzzle validation and numeric provenance.
- Extend the existing campaign's authoritative state with assignments, confirmed allocation and successful task results. Reuse `pendingTasks`, USE dispatch and guidance; no second energy/resource model.
- Update the existing stepper's time/auto-end checks and report actions to enforce the sealed-report gate on every path, including debug simulation. Do not change `engine/demand`, `engine/outcome` or the win-condition formula.
- Add bounded task result facts to narrator types, fact builder, server input validation, templates and post-response validator. Reject references to incomplete/unassigned tasks, invented mistake/time values or absent readings. Keep the n8n endpoint, secret handling and local fallback intact.
- Update `docs/n8n-prompt.md` and `docs/samples` for start, task feedback and summary examples. These are ready-to-use documents; changing the external n8n workflow itself is outside this code update.
- Keep all public text in the two locales and ensure equal nested leaf keys. Keep existing scene/material architecture and route metadata.

## Verification
- Unit tests: seeded assignments for all five Sols; exactly one task per sector even with missing data; scoring boundary values; no penalties/rewards on cancel or failure; no duplicate success reward; 100-kWh/5-kWh and zero-free-energy start rules; sealing matches the recorded allocation and requires all five successes; clock pause and no premature end.
- Scientific/data tests: X classes above X10 remain valid; CME calculation reuses its existing reference; missing-reading practice provenance; displayed numeric origins cover every task view model; narrator rejects nonexistent completions and invented metrics.
- Locale-key parity test for `en.json` and `pt-BR.json`.
- Browser checks: one full Sol from energy setup through five real UI tasks and sealing; retries/cancel; an attempt of every task variant; desktop and phone layouts; keyboard-only alternatives; English and Portuguese; missing-image fallback.
- Run all relevant existing tests. Measure renderer draw calls/triangles and task loading before/after; overlays add no 3D assets. Real-device frame rate and tactile feel remain device checks, not claims based on software-rendered sandbox FPS.

## Open questions / approval
The three flow questions are resolved above. No further answer is required before implementation. Approving this plan also approves the **provisional** star thresholds and the hold-at-day-end behavior; existing scientific data and energy/victory formulas stay unchanged.