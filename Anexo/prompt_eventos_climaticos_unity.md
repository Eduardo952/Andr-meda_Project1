# TASK (UNITY / C#): Real-data environmental events, connected energy grid, player condition & recovery, event VFX and sound

## 0. CONTEXT — READ THIS FIRST

This is a **Unity project** ("Mars Mission Trainer / Base ARES"): a kids' (≈7–12) educational survival game on a Mars base, with a 5-Sol campaign. The project was **converted from a web (React + three.js) prototype** and the Unity version has **many more integrated systems** than that prototype. Everything in this brief must be implemented **in Unity with C#**, using the project's own conventions, pipeline and existing systems.

- The web prototype is only a **behavioral reference**. Folder/class names in Unity may be similar (Balance, Energy, Consequences, Shield, Greenhouse, Solar, Emergency/Alerts, HUD, Narrator…) but **may differ**. Never assume a file exists: search the project, and adapt. Section 1 lists the web rules/constants as the *design truth* to preserve.
- The NASA data layer was (or is being) migrated to native C# (`NasaSolService`-style facade: Sol plan, DONKI FLR/CME/WSA-Enlil, APOD, Curiosity weather, NASA image library; response shape = the old webhook's `SolResponse`: `facts`, `effects`, `data_dates`, `mission_window`, `apod`, `photo`). **Use that layer; do not create a second one.**
- Credentials: this task needs **no keys**. Never hardcode or print any key found in the project.

**Phase 0 is mandatory**: before coding, audit the Unity project and write `Docs/Hazards/AUDIT.md` (Unity version, render pipeline URP/HDRP/Built-in, UI system uGUI/UI Toolkit/TextMeshPro, Input System, character controller + Animator controller and existing animations (walk, **lie-down / bed-rest, sit**, etc.), particle approach (Shuriken / VFX Graph), post-processing (Volume), audio setup (AudioMixer), localization approach, existing tests/assemblies, target platforms (if WebGL: no threads, check API support). Record where each concept below already lives, and the gaps.

---

## 1. REFERENCE RULES FROM THE WEB PROTOTYPE (preserve; verify against Unity)

| Topic | Reference behavior |
|---|---|
| Energy | 100 kWh per Sol (a **game budget**, ≈4.05 kW over a 24 h 39 m Sol), 3 systems: **Shield, Life Support, Greenhouse**; allocation in 5-kWh steps; all energy must be allocated; "ideal" if \|allocated − demand\| ≤ 5; below = deficit; above = surplus (wasted, no storage). |
| Shield demand (GDD v1, confirmed) | `clamp(flareBase + 2.5·magnitude + (cmeSpeed/400)²·4, 10, 75)` rounded to 5. `flareBase`: A5 B5 C10 M22 X40. No CME speed ⇒ CME term 0. |
| Life support demand | `10 + 0.15·max(0, 20 − T_external)` rounded to 5; fallback 25 if no temperature. |
| Greenhouse demand | 20 (provisional constant). |
| Damage at Sol seal | Integrity −1 per missing shield kWh; Oxygen −1 per missing life kWh, −0.5 per missing greenhouse kWh. Resources 0..100 (Integrity, Oxygen), crew **Dose** 0..100 (`+0.8` per missing shield kWh, `+35` if the radiation shelter was not reached; severe ≥ 75; limit 100). |
| Alarm | Flare class ≥ M, or SEP, or CME with Mars impact ⇒ radiation alarm; reach the Radiation Shelter within **60 s**. |
| Dust | Dust on 4 solar arrays (0–100); cleaning needs 1/2/3 passes by dust level; pool loss = average dust, capped at **20 kWh**, applied to the **next** Sol only. |
| Sol clock | 1 Sol = 24 h 39 m 35.244 s; real play time per Sol ≈ 480 s (provisional); sunrise .25, noon .5, sunset .75. |
| Alert stages | advisory → warning → emergency → recovery; presentation was deliberately **non-flashing**. |
| Win | Finish 5 Sols with Integrity > 0, Oxygen > 0, Dose < 100; tiers gold/silver/bronze by resources and dose. |

---

## 2. CRITICAL ANALYSIS — YOU MUST HANDLE THESE

1. **Are the rules really driven by live data?** In the web prototype the rules ran on 5 static sample snapshots and live facts were *not wired*. Confirm in Unity. If any rule still uses placeholders/sample data, wiring the real data (with provenance + offline `sample` fallback) is the first deliverable. Goal: events come **100% from real fields**.
2. **Source mismatch.** The old web types assumed InSight (wind, temp, pressure; mission ended 2022). The live source is **Curiosity REMS**: min/max temperature, pressure, opacity — **no wind**. So wind-triggered dust storms are impossible; trigger dust from `effects.dust_storm_risk` (Mars season Ls ∈ [180,330) = dust season + the mission-seeded roll already in the data layer) and `panel_factor`. Wind **direction** has no live source ⇒ use a mission+Sol seeded direction, labeled `simulated` (see §4A.3 for the wind/dust data limits).
3. **No SEP / notifications** in the live workflow. Either add DONKI `SEP`/`notifications` to the Unity data layer (same key/limits as the other DONKI calls) or derive a "radiation storm" from X-class flare or `mars_impact.level == direct`, labeled `derived`. Never invent an event.
4. **Damage timing.** In the prototype all damage happened only when the Sol was sealed. Warnings, a pulsing player indicator and a mid-Sol collapse need a **real-time exposure layer**. It must update the **same** Integrity/Oxygen/Dose values (no second resource model) and obey a **no-double-counting invariant**: `applied_realtime + applied_at_seal == the penalty the seal would have applied`; brand-new sources (outdoor exposure during radiation, blackouts, surges) have their own per-Sol caps. Keep the seal evaluation as the authority for allocation results.
5. **Energy is not physically connected** (no storage, same-Sol dust has no effect). Add the minimal model in §3 (not a full time-domain grid).
6. **Time mapping.** Data are Earth-dated UTC timestamps (flare `peak_time`, CME `arrival_time`); the game runs Mars Sols. X-rays reach Mars in ≈12.7 min (immediate); a CME takes `2.28e8 km / speed / 3600` h (1600 km/s ≈ 39.6 h). Map the UTC time-of-day fraction to the Sol fraction to schedule **when in the Sol** an event starts; if outside the Sol's Earth date use a deterministic seeded time. Document as a game simplification.
7. **Geometry is Earth-centric.** DONKI events are as seen from Earth. The only physically grounded Mars filter is WSA-Enlil `impactList[location=Mars]` (direct / glancing / none). Any refinement (CME `halfAngle`/`longitude`, FLR `sourceLocation`) only after verifying the fields in a real response. No ephemeris math.
8. **Data gaps.** REMS has gaps. `null` ⇒ "no reading", no hazard from that field, never an invented value. Offline ⇒ labeled sample data.
9. **Solvability & fairness.** For **every** data combination (worst case: X9+ flare + CME ≥ 2500 km/s direct + high dust + very cold) an optimal player must be able to finish with resources > 0 and without a mission abort; careless play must fail often enough to matter. Enforced by tests (§9).
10. **Photosensitivity / audience.** Warning pulses ≤ 2 Hz (never > 3 flashes/s), small area; honor the game's reduced-motion/accessibility setting (static red state + icon + text). Tone must stay **non-aggressive and non-graphic** (see §5).
11. **Narrator.** The narrator (Groq) only receives validated facts. Any new event/number Bubble may mention must be added to the facts payload, the validator and the local fallback lines (both languages), or lines will be rejected.

---

## 3. ENERGY MODEL — CONNECTED, NOT ISOLATED

Keep: 100 kWh budget, 3 systems, 5-kWh steps, margin 5, the GDD shield formula, the thermal formula, the allocation UI. Put every new number in the project's balance config (ScriptableObject or constants class), mark `PROVISIONAL`, and implement the logic as **pure C# classes (no UnityEngine dependency)** so it is unit-testable.

1. **Solar generation factor (same Sol):** `pool = roundTo5(100 × solarFactor − depositLoss)`; `solarFactor` from `effects.panel_factor` (+ Sol dust tier); `depositLoss` = existing dust-on-arrays accumulation (cap 20). Floor `solarFactor ≥ 0.6` unless you prove solvability otherwise.
2. **Demand coupling:** greenhouse = baseline 20 + a dust/opacity term (less natural light ⇒ LED supplement). Life support keeps the thermal formula; `heating_factor`/min temperature feed T_external when no mean exists. **Shield demand formula stays unchanged**; `shield_load_factor` (CME impact none/glancing/direct = 1.0/0.6/0.2 in the data layer's `effects`) scales **radiation intensity/duration**, not the confirmed demand.
3. **Battery reserve (minimal storage):** `reserve` kWh (capacity ≈ 30, start ≈ 10, PROVISIONAL), persisted in campaign state. At seal, surplus beyond the margin is **stored** (up to capacity) instead of wasted; the reserve **covers deficits first** (priority Shield > Life > Greenhouse) before consequences are computed; it absorbs energy shocks during the Sol; carries to next Sol. Show it in the existing energy panel as one more gauge in the same style.
4. **Energy shocks (event-driven, real time):**
   - *Flare surge* at the scheduled time: if `shield.allocated < shield.demand`, draw `surgeKwh(class, magnitude)` from reserve; if insufficient ⇒ **brownout** of the lowest-priority powered system (greenhouse, then life) until repaired (§8) or the shock expires. If the shield is adequately powered, the surge is absorbed (VFX + notice "shield absorbed the flare").
   - *CME impact* at arrival: load = `shield_load_factor × speed severity`, same logic.
   - *Dust*: generation factor + dust on arrays.
5. **Real-time ledgers** (capped per §2.4): blackout seconds per system ⇒ Oxygen/Integrity drain; exposure seconds ⇒ Dose. Calibrate so a whole Sol integrates to the existing seal magnitudes (ignoring the whole 60 s alarm ≈ +35 Dose; a fully unpowered life support for a Sol ≈ the existing worst Oxygen loss).
6. **Architecture suggestion:** pure `HazardPlanner` (data → `HazardPlan`), `EnergyGrid`, `ExposureModel`, `PlayerConditionModel`, `FailureDetector`; thin MonoBehaviours/ScriptableObject events for presentation; seeded PRNG only (seed = `missionId|sol|eventId`; **no `UnityEngine.Random`** in rules).

---

## 4. EVENT CATALOG — data source → tier → functional consequence

Tiers `Low | Medium | High` come from the data; a small seeded jitter (±1 tier, clamped) gives per-playthrough variation. Every event carries provenance `Real | Derived | Simulated` and its source field.

| # | Event | Real source → tier | Functional consequence | Counter-play |
|---|---|---|---|---|
| 1 | **Solar flare** | DONKI FLR class+magnitude (M medium, X high; ≤C advisory only), time `peak_time` | Shield demand (existing); **comm blackout** (`comm_disruption`): minimap/guide arrow/narrator hints offline, comm tasks blocked; **power surge** if shield under-powered | Power the shield ≥ demand; breaker repair; wait it out |
| 2 | **CME / Mars impact** | CME speed (buckets vs 400 km/s) + Enlil `mars_impact.level`, `arrival_time`; `none` = visible "passing" event, no damage | Radiation intensity/duration ∝ `shield_load_factor` and speed; short brownout risk; shield bubble absorbs visually | Shield power, shelter at arrival |
| 3 | **Radiation storm** (SEP or derived) | SEP if added, else X flare / direct CME (`Derived`) | Alarm; **dose rate** outdoors `base × tier × (1 − protection)`: shelter 0.9, interior with shield ≥ demand 0.6, interior with shield deficit 0.2, outdoors 0 | Radiation Shelter (60 s), shield power |
| 4 | **Dust storm** | `dust_storm_risk`, `panel_factor`, REMS opacity if present | Generation factor, dust on arrays, visibility, greenhouse light loss (+demand), cleaning passes | CleanPanels, TiltSun, reserve, shed greenhouse |
| 5 | **Extreme cold** | REMS min/avg temperature (+`heating_factor`) | Life-support demand ↑; if life under-powered ⇒ cabin cooling ⇒ Oxygen/Integrity ledger and player "cold" condition | ThermostatDial, PipeFlow, power life support |
| 6 | **Low oxygen** (derived) | Oxygen value / blackout of life or greenhouse | Staged warnings (50 / 25 / 10), drain from ledgers, player condition | SmartFarmCare, IrrigationPlan, restore power, emergency canister |
| 7 | **Critical system failure** (derived) | Integrity thresholds / unrepaired brownout | Systems trip offline in priority order | Repair objectives (§8) |

Pressure, APOD and library photos remain **informational**. If a real response exposes more fields (UV index, ground temperature…), inspect a real response first, then decide and document.

---

## 4A. DATA-DRIVEN ACTIVATION CONTRACT + SCENARIO DIRECTOR (curated real dates)

**Owner rule (hard):** environmental events exist **only if the API data says so**. The game never invents a hazard. The game's job is to *treat* real data (normalize, tier, schedule) and *simulate its consequences*; "simulation" = deterministic mapping of real fields to effects/visuals, never a made-up event.

### 4A.1 Activation contract
- A hazard activates iff, in the **actual response used for that Sol** (live, or the cached/snapshot copy of that exact response), its source field is non-null **and** passes its threshold (table in §4). Otherwise it does not exist for that Sol (the UI shows "no reading / no event").
- Any catalog or precomputed index (below) may only help to **choose dates**; it never activates an event by itself. If the real response for a chosen date disagrees with the index (empty, error, null fields), the event is not activated and the Scenario Director **re-picks that slot** (deterministically) or falls back.
- Every active event shows its provenance in the UI/glossary: source API, field, Earth date (e.g. "DONKI FLR · X2.3 · 2015-05-31") and `Real | Derived | Simulated`.
- Data treatment layer (pure C#): validation → normalization/units → thresholds → tier → schedule time → effect parameters. Document thresholds in `Docs/Hazards/DESIGN.md`; prefer **data-driven tiers** (e.g. terciles of the catalog's own distribution for cold/pressure) over magic numbers; flare tier by class (C low/info, M medium, X high) with continuous intensity from log X-ray flux for VFX scaling; CME tier by speed buckets (<500 / 500–1000 / >1000 km/s) and **damage only if WSA-Enlil says Mars is hit**.

### 4A.2 Date selection: the Scenario Director
Today's data layer picks 5 consecutive Sols from a seeded 120-day block that has at least one strong (M/X) and one calm day. Replace it with a richer, still deterministic and real-data-based **ScenarioDirector** (keep the old behavior as the `Consecutive` fallback mode):
1. **Hazard catalog (precomputed from real APIs, validated at runtime).** An **Editor tool** (`HazardCatalogBuilder`, reusing the native NASA client, chunked requests like the workflow's 120-day DONKI blocks, resumable, rate-limit aware, no keys in the output) scans the archive from `EARTH_MIN` to `today − 45 days` and writes a compact asset (TextAsset/ScriptableObject): per candidate Earth date → strongest flare class/magnitude/peak time, CME speed, WSA-Enlil Mars impact (none/glancing/direct + arrival), SEP flag (if SEP is added), Mars season Ls / dust-season flag (computed with the existing Mars24 formula), and REMS min/max temperature + pressure for **shortlisted candidates only** (REMS is one request per Sol; do not scan all ~4,700 sols). Store generation date, source versions and counts. Past archive data is stable, so the catalog is not "fake data": it is an index of real API results. Offline-fallback scenario snapshots (full real responses for a few ready-made 5-day scenarios, labeled `snapshot`) ship with the game.
2. **Slots with an interleaved escalation profile** (ScriptableObject `ScenarioProfile`, tunable). Default hierarchy for the 5 survival days, alternating *radiation-type* (flare/CME/SEP) and *atmosphere-type* (dust/cold) hazards with a recovery breather before the climax:
   - **Sol 1 — Orientation:** calm or advisory only (e.g. C-class flare, mild weather) so the player learns the systems.
   - **Sol 2 — Radiation A:** M-class flare (comm blackout + surge risk), no CME impact.
   - **Sol 3 — Atmosphere:** dust-season / cold Sol (generation and thermal stress), no strong flare.
   - **Sol 4 — Radiation B:** CME with WSA-Enlil Mars impact (glancing or direct) and/or SEP.
   - **Sol 5 — Climax:** the strongest *solvable* combination the data allows (e.g. X flare + CME impact, or flare + dust season + cold).
   Rules: no two consecutive Sols with the same dominant hazard; a breather between peaks; each slot has *required* hazards and an ordered list of *acceptable substitutes* if the catalog lacks a perfect date; the escalation must stay solvable (§9).
3. **Selection** = seeded (missionId) constraint search over catalog candidates that match each slot (≥ 10 candidates per slot when available so missions differ): distinct Earth dates and Curiosity sols, `Curiosity sol = floor((date − LANDING)/SOL_MS)`, respect `EARTH_MIN` and the 45-day margin. Then **validate by real request** (live data for the 5 chosen dates, cached per mission+Sol, request deduplication, same limits as the native NASA layer ≈ 5 calls per Sol); on mismatch re-pick the slot. Finally run the pure **solvability check** on the 5 derived demand/generation sets; reject and re-pick if the optimal bot cannot survive.
4. **Honesty about dates.** Curated Sols are **not consecutive calendar days**; the UI/glossary/narrator must say "five real days from NASA's archive, chosen to train different hazards" and show each Sol's Earth date (update `data_dates`, the narrator facts, the validator, the `why_real_data` explanation and fallback lines in both languages). Weather for each Sol must come from the Curiosity sol of **that same date**.
5. If the catalog asset is missing, use the `Consecutive` mode; if offline, use a shipped scenario snapshot; always set the provenance flag (`live | catalog-verified | snapshot | sample`).

### 4A.3 Known data limits (verify in Phase 0, report in AUDIT.md, do not paper over)
- **Wind:** the live Curiosity REMS source gives **no per-Sol wind** (the old workflow sets `wind = null`). InSight wind (HWS/WD) existed only for ~2018–2022 and the endpoint may be discontinued. Check whether any current endpoint returns real per-Sol wind. If none: wind-driven effects stay `Derived/Simulated` from the dust-season/`dust_storm_risk` data and are labeled so; do not claim real wind. If InSight is still reachable, an optional "wind Sol" may use an InSight-era date, but then **all of that Sol's weather must come from the same source** (never mix sites silently).
- **Dust:** REMS `atmo_opacity` is often uninformative; the reliable real signals are the Mars season (Ls 180–330) and, if the live response exposes it, UV/pressure behavior. Candidate dust windows (e.g. the documented 2018 global dust storm period) must be **verified against the real response** before they enter the catalog. Never hardcode unverified dates.
- **SEP/notifications:** not in the old workflow; add or derive (labeled), see §2.3.
- Solar cycle: strong X-class days cluster around solar maxima (≈2012–2014, 2024–2025); the catalog may offer few X days elsewhere; the Director's substitutes handle this.

---

## 5. PLAYER CONDITION, WARNINGS, INCAPACITATION AND RECOVERY (NOT A "YOU DIED")

**Tone requirement from the owner:** nothing aggressive. No "YOU DIED". The player becomes impaired and is **incapacitated (loses consciousness) with a fall-to-the-ground animation**, then there must be **recovery resources** so the game continues. Mission end is a calm, specific "medical evacuation", not a death.

### 5.1 Player condition (derived from Dose / Oxygen / cold exposure / Integrity)
`Nominal → Caution (early symptoms) → Warning (impaired) → Incapacitated (loss of consciousness)`, with the cause tag (Radiation, Hypoxia, Cold, Power failure). Internal enum names may differ, but **on-screen text must use the technical terminology of §5.4**. Thresholds in the balance config (suggested: Symptoms at Dose ≥ 50 / O2 ≤ 40; Sick at Dose ≥ 75 / O2 ≤ 25; Collapse at Dose ≥ 100 / O2 = 0 / hypothermia ledger full). Functional effects:
- **Caution (early symptoms):** mild screen vignette, Bubble warns; hazard banner appears.
- **Warning (impaired):** walk speed ×0.75, slight camera/vision wobble, breathing/heartbeat audio, stumble animation; HUD bar pulses red.
- **Incapacitated:** a short, **non-graphic, 3-beat sequence**: stagger → drop to the knees → lie down (reuse the project's existing lie-down/sit/bed-rest animations; if one is missing, cross-fade the existing poses or add a minimal Animator state; **no ragdoll, no blood, no vomiting**), with an eyes-closing vignette fade and muffled audio. Analyze in Phase 0 whether the existing animations make this convincing; if not, document the compromise in `DESIGN.md`.

### 5.2 Warnings
- Persistent non-modal **hazard banner** under the HUD in the existing HUD/alert style: e.g. `WARNING: RADIATION EXPOSURE`, cause line (`X2.3 flare + direct CME`), live values (`Dose 62/100, +3.2/min`) and an action line (`Go to the Radiation Shelter / Power the Shield`). Each hazard has its own title, icon, color and sound (radiation, low oxygen, power failure, comm blackout, cold).
- **Player indicator:** the matching HUD bar pulses red (≤ 2 Hz), thin red edge vignette scaled by hazard level, astronaut visor/suit red emissive pulse. Reduced motion ⇒ static.
- Modal alert card only for the first `emergency` stage of each hazard. **Grace rule:** no incapacitation without a `warning` stage shown for ≥ 15 s with the remedy visible.

### 5.3 Recovery resources (must exist, limited, tied to the energy/mission systems)
1. **Radiation Shelter** (prevention, existing).
2. **Med Kit** (limited stock, e.g. 2 per mission, +1 on a Perfect Sol): used from the HUD/inventory; Dose −25 and clears the Caution/Warning condition. Not usable while collapsed.
3. **Recovery Bed in the Dormitory** (reuse the existing bed-rest feature): a treatment/rest that lowers Dose (and restores Oxygen/warmth) but **costs Sol time and Life-Support energy**, so it is a real trade-off.
4. **Emergency oxygen canister** (limited, e.g. 1–2): +25 Oxygen and buys time during blackouts.
5. **Rescue sequence after collapse:** short non-graphic sequence (Bubble/crew drag/carry to the Dormitory bed), wake-up with **Dose set to a treated value** (e.g. 60, weakened for the rest of the Sol: walk ×0.85), the Sol clock advances N hours, unfinished tasks remain, lose the Sol's bonus stars, `medicalEvacuations += 1`.
6. **Mission abort (final, calm):** only after `maxMedicalEvacuations` (e.g. 3) or a collapse with no recovery resource left in an unrecoverable state. Screen in the existing end-screen style, headline format `MISSION ABORT — MEDEVAC: ACUTE RADIATION SYNDROME` (and the other causes in §5.4; Portuguese equivalents there too), the **real cause chain** (e.g. "X2.3 flare on 2015-05-31 → Shield received 20 of 45 kWh → dose rate +3.2/min → shelter not reached"), final values, provenance badges, retry/home buttons. Never a generic message when the cause is known. A configurable `AbortTone` (default calm) is allowed, nothing graphic.
Keep a safety net: if Dose ≥ 100 / O2 ≤ 0 / Integrity ≤ 0 is reached by any path, collapse/abort logic still fires exactly once.

### 5.4 Technical terminology (space-mission tone, still kid-readable)

The game is a space-mission trainer, so UI text uses **real aerospace/medical vocabulary**, never graphic, and **every technical term is paired with a one-line plain explanation** (second line, tooltip or glossary entry, and Bubble can explain it). English is the primary language; pt-BR must use the accurate technical equivalent. Define all strings as localization keys (one table, used consistently by HUD, banners, alerts, end screens, narrator facts, validator and fallback lines).

| Concept | English (primary) | pt-BR | Plain gloss (EN) |
|---|---|---|---|
| Alert classes | ADVISORY / CAUTION / WARNING / EMERGENCY / ALL CLEAR | AVISO / ATENÇÃO / ALERTA / EMERGÊNCIA / SITUAÇÃO NORMALIZADA | how urgent it is |
| Radiation, stage 1 | `CAUTION: ELEVATED RADIATION DOSE` | `ATENÇÃO: DOSE DE RADIAÇÃO ELEVADA` | more radiation than normal |
| Radiation, stage 2 | `WARNING: RADIATION EXPOSURE` | `ALERTA: EXPOSIÇÃO À RADIAÇÃO` | take shelter now |
| Radiation, incapacitation | `EMERGENCY: CREW INCAPACITATED — ACUTE RADIATION SICKNESS` | `EMERGÊNCIA: TRIPULANTE INCAPACITADO — DOENÇA AGUDA DA RADIAÇÃO` | too much radiation made the astronaut too sick to move |
| Oxygen, stage 1 | `CAUTION: LOW O₂ — HYPOXIA RISK` | `ATENÇÃO: O₂ BAIXO — RISCO DE HIPÓXIA` | not enough oxygen reaching the body |
| Oxygen, stage 2 | `WARNING: HYPOXIA SYMPTOMS` (dizziness, shortness of breath) | `ALERTA: SINTOMAS DE HIPÓXIA` | |
| Oxygen, incapacitation | `EMERGENCY: LOSS OF CONSCIOUSNESS — HYPOXIA` | `EMERGÊNCIA: PERDA DE CONSCIÊNCIA — HIPÓXIA` | |
| Cold | `CAUTION: HYPOTHERMIA RISK` → `EMERGENCY: CREW INCAPACITATED — HYPOTHERMIA` | `ATENÇÃO: RISCO DE HIPOTERMIA` → `TRIPULANTE INCAPACITADO — HIPOTERMIA` | body temperature too low |
| Power | `WARNING: BUS UNDERVOLTAGE / BROWNOUT` → `EMERGENCY: POWER BUS FAILURE — LIFE SUPPORT OFFLINE` | `ALERTA: SUBTENSÃO NO BARRAMENTO` → `FALHA NO BARRAMENTO DE ENERGIA — SUPORTE DE VIDA OFFLINE` | not enough electricity for a system |
| Comms | `CAUTION: RADIO BLACKOUT (FLARE-INDUCED)` | `ATENÇÃO: BLECAUTE DE RÁDIO (INDUZIDO POR ERUPÇÃO)` | the flare is jamming radio signals |
| Structure | `EMERGENCY: HABITAT INTEGRITY CRITICAL` | `EMERGÊNCIA: INTEGRIDADE DO HABITAT CRÍTICA` | |
| Recovery items | `MED KIT`, `CREW QUARTERS RECOVERY`, `EMERGENCY O₂ CANISTER` | `KIT MÉDICO`, `RECUPERAÇÃO NO ALOJAMENTO`, `CILINDRO DE O₂ DE EMERGÊNCIA` | |
| Rescue | `MEDEVAC` (medical evacuation) | `EVACUAÇÃO MÉDICA (MEDEVAC)` | taken to the infirmary to recover |
| Final abort | `MISSION ABORT — MEDEVAC: ACUTE RADIATION SYNDROME` / `… : HYPOXIA` / `… : HYPOTHERMIA` / `… : POWER BUS FAILURE` / `… : HABITAT INTEGRITY LOSS` | `MISSÃO ABORTADA — MEDEVAC: SÍNDROME AGUDA DA RADIAÇÃO` / `HIPÓXIA` / `HIPOTERMIA` / `FALHA NO BARRAMENTO` / `PERDA DE INTEGRIDADE DO HABITAT` | |

Rules: (a) never write "YOU DIED" or graphic detail; (b) dose stays in **game units** and the UI says so ("Dose (game units)") — do not present game numbers as real sieverts/grays; add a one-line "simplified for learning" note in the glossary; (c) the narrator's validator and fallback lines must accept/use exactly these terms in both languages; (d) keep terms short enough for the HUD banner (one line on mobile; the gloss goes on the second line).

---

## 6. VISUAL EFFECTS (same quality bar as the existing dust storm)

- **Dust storm: refactor, don't redesign.** Move the existing dust-storm look (particles, fog, tint, lighting) into its **own self-contained component/prefab** (`DustStormEffect`) driven by an `Intensity`/tier parameter. First step: **read the current values from the implementation and treat them as the Medium tier**, verify the look is unchanged (before/after screenshots/video), then enhance: gust waves along a wind vector (seeded direction), far veil layer, ground-level streaks, grains drifting toward the solar park, dimmed orange sun, dust haze over panels. Keep its palette family.
- Create one **prefab + controller per event** under a `Weather/` (or the project's equivalent) folder, orchestrated by a `WeatherController` that reads the `HazardPlan` and the scheduled times:
  1. **Solar flare:** sky flash + sun glare, expanding radial pulse ring, bright motes; comm-static scanlines on the HUD during comm blackout. Yellow → white → orange.
  2. **CME impact:** slow wave front across the sky, aurora curtains (Mars auroras are real; artistic), diagonal charged-particle streaks, ripples on the shield bubble proportional to absorbed load. Violet/teal.
  3. **Radiation storm:** sparse cold-green/white flickering specks, subtle film grain, scales with dose rate.
  4. **Cold snap:** frost crystals, breath vapor, frosted screen edges, cold-blue cabin lighting.
  5. **Blackout:** flickering amber/red emergency lights, sparks at panels, lights dim.
  6. **Low-oxygen/sickness:** tunnel vignette, desaturation, slight blur (Volume overrides).
- Each event: distinct palette, movement pattern, intensity tiers, instantly readable danger. Use the project's render pipeline correctly (Volume post-processing, `RenderSettings` fog, light animation); pool particles; scale counts by the project's quality levels; honor reduced motion; report draw-call/GPU deltas. Dev-only debug menu to force any event/tier.

---

## 7. SOUND

Use the project's AudioMixer (create `Hazards` group if missing): per-event loops with intensity → volume/filter, ducking under narrator speech and modal screens, stop on pause/phase change/collapse, respect the sound setting. **Do not depend on external audio assets:** generate procedural `AudioClip`s (noise buffers + filters/LFO via `AudioClip.Create` or `OnAudioFilterRead`) and expose a `HazardAudioProfile` ScriptableObject so real clips can be dropped in later without code changes.
- Dust storm: brown/pink noise, band-pass 300–900 Hz, slow gust LFO.
- Flare: rising filtered-noise swell + radio-static crackle during comm blackout.
- CME: sub-bass 40–60 Hz swell and filter sweep at arrival.
- Radiation: **Geiger clicks** (Poisson-timed ticks), rate ∝ dose rate.
- Blackout: power-down sweep + relay clicks; restore ⇒ rising chime.
- Low oxygen: two-tone beep that speeds up with stage; heartbeat/heavy breathing at the Warning condition.
- Cold: low wind + faint creaks; collapse: soft thud + muffled fade.

---

## 8. MISSIONS — DYNAMIC, HAZARD-AWARE FIVE SOLS

Replace any fixed task rotation with a deterministic `MissionDirector(sol, hazardPlan, seed)` (same mission id ⇒ same assignments) that keeps the existing number of tasks per Sol/sector and the "Sol is evaluated only via confirmed sealing" rule:
- Dust Medium/High ⇒ CleanPanels (+ TiltSun); low dust ⇒ PowerBalance / WindReading only if data exists.
- Flare M/X or direct CME ⇒ shield tasks (AlignCoils, WireShield, CmeCountdown) and SortFlares in the observatory.
- Radiation storm ⇒ the shelter run is a **mandatory** objective.
- Cold ⇒ ThermostatDial / PipeFlow; low O2 or greenhouse deficit ⇒ SmartFarmCare / IrrigationPlan; resource crunch ⇒ ResourcePriority.
- **Repair objectives after events:** brownout ⇒ breaker/wiring repair; dusty arrays ⇒ cleaning. Completing a repair **changes real state** (clears the brownout, restores generation), not just stars.
- Calm Sols (no events in the data) get an optimization/learning set so five Sols are never repetitive. Use the seeded real-data window (it already picks one strong and one calm day) so progression has rhythm; never reorder real dates.
Adapt to the task/mini-game names that actually exist in the Unity project.

---

## 9. TESTS, SIMULATION, ACCEPTANCE (Unity Test Framework)

Keep existing tests green. Add **EditMode** tests for the pure logic and **PlayMode** smoke tests:
0. **Scenario Director:** determinism per mission id; distinct dates/Curiosity sols; slot profile respected (no two consecutive Sols with the same dominant hazard; breather before the climax); substitutes when the catalog lacks a perfect date; re-pick when the real response disagrees with the catalog; solvability rejection; `Consecutive`/snapshot fallbacks; **no event activates when its API field is null**.
1. Hazard plan from facts (all `null` combinations), tier/jitter determinism, grid (reserve, surplus, deficit priority), exposure and oxygen rates, **no-double-count invariant**, condition state machine, collapse/recovery rules, mission director determinism.
2. **Simulation harness:** ≥ 200 seeded synthetic missions × 5 Sols incl. worst cases. Bot "optimal" never aborts and finishes ≥ 99% with resources > 0; bot "careless" (ignores alarms, random allocation) aborts/collapses in ≥ 30% of missions; **no collapse without a prior ≥ 15 s warning**; recovery resources always reachable when the player is collapsed-but-not-aborted.
3. Provenance test: every event has `Real|Derived|Simulated` + source field, and no event exists when its source is `null`.
4. Localization: every new string exists in both languages (en, pt-BR) via the project's localization mechanism.
5. Visual/performance: dust before/after parity at Medium tier, one capture per event per tier, reduced motion, target-platform frame time.
6. Narrator: new facts pass the validator; no rejected lines for new events.
**Definition of done:** a full 5-Sol playthrough on live data and on offline fallback where each Sol shows different data-derived events with working consequences, warnings, sounds, the sick → collapse → recovery path, a calm specific mission-abort screen reachable only via careless play, and a perfect-play path to victory.

---

## 10. CONSTRAINTS

- Preserve the current UI layout, colors, fonts, components and tone; new elements reuse existing prefabs/styles. No new UI framework.
- Don't change level layout, collisions, camera, the GDD shield formula, the 100 kWh budget, slider step/margin, narrator validation rules or NASA provenance badges.
- Respect the project's architecture and assembly definitions; pure logic in non-Unity classes, presentation in MonoBehaviours/ScriptableObjects; no per-frame allocations in effects; no new heavy packages without justification.
- Every new tunable is `PROVISIONAL` in the balance config; document formulas, units and simplifications in `Docs/Hazards/DESIGN.md`.

## 11. DELIVERABLES

1. `Docs/Hazards/AUDIT.md` (Phase 0) and `Docs/Hazards/DESIGN.md` (event table, formulas, calibration, simplifications).
2. Phases, in order, each leaving the game playable: **P0** audit → **P1** live-data wiring + hazard plan + hazard catalog builder + Scenario Director (§4A) → **P2** grid/battery + exposure ledgers + player condition + failure detection → **P3** warnings/HUD + collapse/recovery/abort screen → **P4** weather VFX (dust refactor first) → **P5** procedural sound + mixer → **P6** mission director → **P7** narrator/localization/tests/perf.
3. Final report: files created/modified, test results, captures, performance deltas, every PROVISIONAL value with rationale, and the **open design questions** for the owner (collapse thresholds, Med Kit/canister counts, battery capacity, solar floor, max medical evacuations, whether to add DONKI SEP/notifications calls, catalog scope and size, whether Sols may be non-consecutive dates, wind source decision).
