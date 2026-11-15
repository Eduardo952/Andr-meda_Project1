# Light challenge interface verification

## Scope
- Shared `ChallengePanel` wraps all 14 lazy-loaded sector variants, Energy Panel and report sealing.
- Read-only cards show existing snapshot or training inputs with registered numeric origins and localized explanations.
- Light off-white surfaces, regional pills, yellow actions, pale green/coral feedback and internal scrolling replace the dark task console.
- Bubble hints reuse localized task hints and the existing presentation speech API; narrator transport is unchanged.
- Task assignments, timing, scoring, retry rules, rewards, allocations, data services and sealing conditions were not changed.

## Checks
- All 84 existing tests passed, including locale key parity, numeric provenance, scoring, assignments and sealing.
- All 14 variants opened and began at 1174 × 858 and touch-emulated 390 × 844. No horizontal overflow; puzzle buttons measured at least 56 × 56 CSS pixels.
- Irrigation wrong-answer feedback, Bubble hint and successful completion verified at both sizes.
- Complete Sol 1 through real USE: energy 40/40/20, SortFlares, WireShield, PipeFlow, SmartFarmCare and TiltSun, followed by matching the confirmed allocation and sealing. Each task recorded success and three stars; summary appeared.
- Confirmed task surfaces use the light token and Observatory uses its violet regional token. Settled feedback and sealing screenshots inspected.
- Preview build log reports build OK; no browser page errors observed.
- Physical-phone testing and authenticated/live service verification were not part of this presentation-only change; prior roadmap blockers remain unchanged.