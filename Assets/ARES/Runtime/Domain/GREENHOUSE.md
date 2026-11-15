# Mestre da Estufa

The greenhouse station routes both `SmartFarmCare` and `IrrigationPlan` to the new child-paced Portuguese activity. The original reference sources, exported meshes and historical puzzle fixtures remain unchanged. This feature uses a generated four-shelf illustration and native UI Toolkit controls, not a newly generated 3D model.

## Model

**Manual watering integration:** the management panel prepares a locked plan in `GreenhouseWatering`; it does not call `Decide` directly. The player must pick up/fill the can and deliver water to all four marked cultivation groups. Only the last delivery commits the simulation decision. See [manual care architecture and water ledger](../World/MANUAL_WATERING.md). Reward validation also requires `CompletedManualCycles == CompletedCycles`.

`GreenhouseSimulation` owns all decisions and stores water as integer millilitres: initially 11000; Low 1360, Medium 2040, High 2320 per cycle for the entire greenhouse. The optional adjustment changes the final level before cost validation. No water, climate, history or cycle changes on rejected decisions. There is no preselected answer, timer, automatic irrigation, background AI service or refill on reopening.

The four logical shelves are Fruta, Legume, Verdura, Verdura. They share the specified soil and lighting system, so they currently share health outcomes; no undocumented crop sensitivities are invented. Initial soil/light are Moderate. The full nine-case irrigation table is explicit. High irrigation on Dry deterministically yields Moderate, one of the two allowed outcomes. High on Wet also records worsening waterlogging.

Two consecutive completed cycles with any nonideal factor count as repeated problems and produce `AtRisk`; both factors outside the ideal also produce `AtRisk`. Ideal conditions restore `Healthy`. Presenting a climate event does not itself count as another completed problem cycle.

Weather is a deterministic teaching sequence, not live Martian measurements: stable, dust storm, strong sun, clear sky, stable, dust storm, clear sky, strong sun. Dust reduces light; strong sun increases light and dries soil by one step; clear sky returns external light to Moderate; stable leaves conditions unchanged. Weather is applied once before the new decision. The previous result is an immutable-to-consumers snapshot, so the narration does not mix consequences of the last decision with the next weather event.

Only meaningful optional adjustments are offered: more water on dry soil unless already High; less water on Wet unless already Low; more/less light for Low/Excessive light. One adjustment can accompany a selected irrigation level. Selecting another level clears the prior adjustment. The UI shows final cost, never a predicted answer or an automatically selected solution.

## Ending and campaign

When remaining water is below 1360 ml, no legal full cycle is possible. End the activity and accurately display any residual water (never round it down to zero or deduct a partial cycle). This also handles a zero-water ending. The final reflection is optional and limited to 240 characters, held in local session memory.

`Campaign.Greenhouse` survives closing/reopening the activity during the same Sol. Each new Sol starts a **new cultivation activity** with 11 L; starting a new mission also resets it. This is not an in-round refill. Each greenhouse assignment is complete only after the cultivation activity ends and the player records the learning. `FinishGreenhouseTask` validates that state and awards the ordinary mission reward once, with three participation stars independent of speed or making the optimal choices. The generic timed `FinishTask` cannot bypass the greenhouse rules. The final reflection is not graded.

## Presentation and sources

`GreenhouseNarrator` owns curated PT-BR copy, cycle format, arithmetic and reflection prompts. `GreenhouseView` renders only; `Greenhouse.uss` extends the existing warm challenge style. The `imagegen` skill produced `Resources/ARES/UI/Greenhouse/veggie-rack.png`; its final prompt is recorded in `ART_DIRECTION.md`. Four shelves and alpha transparency were inspected. Illustration and controls remain separate so text stays readable and accessible.

The inspiration is [NASA's Veggie cultivation system](https://www.nasa.gov/exploration-research-and-technology/growing-plants-in-space/), which uses plant pillows and LED lighting. The four-shelf setup, Mars weather sequence and irrigation levels are game rules. The user supplied the 34/51/58 ml experimental-average rationale for scaling to 1.36/2.04/2.32 L; it has not been independently established by the linked overview, so the UI makes no claim that those levels are official NASA recommendations. No NASA logo or official-hardware claim is used.

## Tests

Create `Temp/ares-greenhouse-check.request` with BaseARES open. `GreenhouseChecks` verifies all soil transitions, exact arithmetic, final-level adjustment consumption, valid contextual controls, climate, repeated-risk rules, rejected choices, residual-water termination, campaign reward guards, actual E/Escape/reopening, native button decisions, reflection and viewport bounds. Results: `Logs/ares-greenhouse.txt`; screenshots: `Logs/ares-greenhouse-{start,weather,end}.png`.

`GreenhouseCheckFixture` adapts historical campaign trace fixtures to complete the new activity without bypassing its domain rules. Runtime code never uses that helper. `FrontendPlayChecks` now exercises the new greenhouse controls inside the existing five-Sol campaign regression. Historical pure `TaskAttempt` tests still validate the original imported puzzle models, which are no longer the greenhouse station's runtime UI.

Verified 2026-11-15: greenhouse model and Play checks passed; initial/weather/end screenshots were visually inspected. Reference parity and historical puzzle checks passed. The updated native UI campaign regression also passed all 25 assigned activities through five Sols and Victory (`Logs/ares-ui.txt`), including the new greenhouse controls. No standalone build was produced.
