# Mestre da Estufa

Added as an optional greenhouse activity, not a replacement for mission tasks.

## Rules and provenance
- Starts with 11,000 mL, moderate soil and moderate light; integer accounting avoids floating-point drift.
- Low, medium and high irrigation consume 1,360 / 2,040 / 2,320 mL respectively.
- Fruit, vegetable and leafy greens rotate; crop health and reservoir balance remain visible.
- Environmental lighting is deterministic training weather, not fetched NASA observations.
- Water volumes are explicitly labeled game adaptations of the supplied VEG-04 averages, not official greenhouse irrigation levels. No new NASA API or narrator call was introduced.
- Optional water adjustments redistribute the chosen dose without refilling or additional consumption. Light-temperature adjustments abstract lighting correction. These interpretations are disclosed in the interface because the brief supplies no extra volumes or physical conversion.
- Three consecutive problematic cycles trigger risk. This threshold is a training interpretation of the brief's “several cycles.”
- Below 4 L triggers conservation feedback. Unaffordable choices do not consume water. At zero, learning ends; when a positive remainder is below the minimum dose, the player can finish while preserving that remainder.
- The activity uses Portuguese throughout, with authored text in both locale dictionaries. English remains the default for the rest of the app.
- Session progress survives closing/reopening; restarting resets the activity. Campaign allocations, results, dose, rewards and narrator facts are untouched. Movement, Sol clock, alarm clock and emergency queue progression pause while open.

## Verification
- 119 tests pass, including 16 trainer tests for concrete water amounts, thresholds, soil/light, repeated stress, crop rotation and campaign isolation.
- Browser flow passed on 1280×1800 desktop and 390×844 touch-enabled emulation: open in greenhouse, choose levels, correct light, read exact successive balances (11 → 8.96 → 6.92 → 4.88 → 2.84 → 0.52 L), conclude, restart and close.
- Browser assertions confirmed unchanged campaign allocation, task results, dose, Sol time and alarm time while making decisions.
- Screenshots inspected: light challenge theme, crop scoreboard, low-water warning, readable controls; no horizontal overflow. The phone dialog scrolls to reach remaining controls.
- Build log reports build OK. Physical-device testing is not available.