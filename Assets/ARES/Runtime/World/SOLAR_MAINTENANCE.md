# Solar maintenance

The physical park contains **32 independent panels**. This Unity gameplay extension reuses their imported meshes and cell textures; it does not modify the reference web source or generated geometry.

## Balance and units

`ReferenceExplorer.solarSettings` exposes the balance in the scene Inspector. Defaults:

| Setting | Default |
| --- | --- |
| Clean panel production | 0.4 kWh per game cycle |
| Game energy cycle | 30 seconds of active play |
| Total clean production | 12.8 kWh per cycle |
| Base consumption | 9.6 kWh per cycle |
| Initial / maximum stored reserve | 160 / 160 kWh |
| Critical reserve | Below 100 kWh |
| First dust accumulation | After 180 seconds |
| Stagger between panels | 4 seconds, spatially permuted |
| Time from initial dust to full coverage | 120 seconds |
| Continuous cleaning hold | 4 seconds per panel |

Production decreases linearly with dust. The reserve integrates production minus consumption and stays between zero and capacity. Cleaning restores the selected panel's production immediately, but does not award instant battery energy. Enough clean panels must produce more than consumption to recharge. These are gameplay timing defaults, not real astronomical measurements. Neglect eventually soils all 32 panels and depletes the reserve; it does not repeatedly spawn alerts or require constant intervention from the start.

The original Sol allocation budget and its four-array weather/training puzzle are separate from the new physical storage simulation. Completing the legacy `CleanPanels` puzzle does **not** automatically clean the 32 world panels. Physical maintenance has no star or resource rewards and remains available independently of the assigned task. Reserve depletion currently triggers the requested critical warning, not an additional defeat rule. Change these domain policies explicitly if the campaign design later needs a unified allocation budget.

## Ownership

- `Domain/SolarPlant.cs`: pure production, dust, reserve and uninterrupted cleaning rules. `Campaign` owns one plant for the whole mission; report, task overlays, pause and summary suspend its clock. Moving to another Sol preserves it; starting a new mission resets it.
- `SolarMaintenance`: binds cells using expected positions and textured mesh bounds, validates all 32, reads world interaction and coordinates views. If the reference layout changes, update this adapter and its checks. No mesh name indices are used.
- `SolarPanelView`: per-renderer sand overlay through `MaterialPropertyBlock`; imported shared materials remain untouched. Outlines mark cleaning areas; the selected outline is green.
- `SolarBroomView`: reusable code-built broom and rack. The padded handle is anchored to the imported right glove after animation; `HandSocket` and `GripSocket` define the two attachment points. The holding pose overrides the idle wave while the broom is visible.
- `UI/SolarMaintenanceHud`: reserve, generation, dusty count, critical warning, context hint and cleaning progress. Text lives in the Unity locale extensions.

At the park entrance (Unity coordinates `3,0,-69`), E picks up or returns the broom before station interactions. Returning cancels any active cleaning hold and places the same prop back on its rack. With the tool, enter an outlined panel area and hold the left mouse button. Releasing, leaving the area, changing panels or opening an overlay cancels the current hold. A completed clean resets only that panel's dust timer. Menus and UI pointer interactions must not clean panels.

## Verification

For pickup/return or grip changes, use only `Temp/ares-broom-check.request` (`Logs/ares-broom.txt`). It verifies real E pickup/return/re-pickup without task overlays, cancellation on return, rack placement and glove alignment, and captures the held/walking/cleaning poses. Broader solar or campaign checks are needed only when their behavior changes.

With `BaseARES` open in the editor, create `Temp/ares-solar-check.request`. `Editor/SolarMaintenanceChecks.cs` runs pure balance and cancellation cases, then virtual keyboard/mouse checks in Play: panel binding, tool requirement, E pickup priority, held-click progress, camera conflict, critical HUD visibility and mission restart. Results: `Logs/ares-solar.txt`; visual captures: `Logs/ares-solar-*.png`. It does not send operating-system input.

Verified on 2026-11-15: solar checks passed, including clearing the critical state after recharging above the threshold. Clean, dusty, sweeping and restored-panel captures were inspected. The existing station/sprint/Exit regression also passed after integration (all 25 area/Sol policies; actual E at the five centers and station points; walking 4.68 m versus sprinting 8.19 m; movement dust and overlay blocking; Exit returned to Edit mode). Reference parity and puzzle checks passed. Standalone packaging was not exercised.
