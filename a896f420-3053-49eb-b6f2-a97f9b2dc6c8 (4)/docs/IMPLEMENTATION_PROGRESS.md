# Base ARES visual polish

## Scope
Preserve campaign mechanics, NASA provenance, narrator, controls, collisions, station locations and cutaway floors. Keep the original code-built, colorful toon identity. Improve terrain first, then the existing atmosphere and material systems; no additional dependencies.

## Stages
- Completed: deterministic layered regolith color/bump maps, wind-combed grain, broad surface tint, shallow exterior relief, instanced mineral fragments, irregular crater rims and distant geology.
- Completed: existing wind dust refined, pooled fading boot traces and walking dust, warmer sun/cooler fill, softer shadow filtering, selective physical trim/glass/visor materials, photovoltaic-cell texture and tapered greenhouse leaves.
- Completed: desktop/phone-emulated screenshots, actual USE → energy allocation → Solar task → success readback, 84 passing tests and current preview build OK.

## Constraints
The later consequence-system brief intentionally enables PROVISIONAL simulated dust storms and a next-Sol energy loss capped at 20%. Real InSight wind above the provisional threshold can trigger the simulation; missing wind uses a deterministic training schedule. These are labeled game events, never NASA dust-opacity measurements, and solar-particle alarms remain separate from Martian weather. Physical-device performance, audio and feel cannot be measured in the sandbox.

## Consequence system update
- Completed: pure tested consequence evaluation, cumulative crew dose, revised endings, dust-capped allocation pools, deterministic four-array cleaning and accepted-result robot animation.
- Completed: prioritized accessible alerts, reduced-motion behavior, synthesized optional audio, cause/effect summary cards, perfect-Sol rewards, success tiers and bilingual narrator facts/validation.
- Verified: 103 tests, a real five-task Sol 1 seal, forced solar/dust events, desktop cleaning and touch-emulated phone cleaning. See docs/consequences-verification.md.

## Modified files
- src/config/theme.ts, src/styles.css: geological and lighting roles; mid-width mission-list spacing.
- src/game/terrain.ts: cosmetic relief and exclusion bounds.
- src/game/scene/surfaceTextures.ts: generated regolith and photovoltaic maps.
- src/game/scene/Terrain.tsx: textured relief and instanced geology.
- src/game/scene/Ambience.tsx: pooled wind dust, walking dust and fading traces.
- src/game/scene/Construction.ts, Decor.tsx, GameCanvas.tsx, Greenhouse.tsx: selective material response, cell details, lighting and foliage.
- src/test/terrain.test.ts: route/module flat-plane invariants and exterior crater placement.
- AGENTS.md, roadmap.md: architecture and completion tracking.

## Verification
- All 84 tests passed on the final clean rerun. An earlier run reported an intermittent asynchronous narrator-import teardown; no narrator code was changed.
- Browser screenshots: /tmp/browser/visual-polish/after.png, after-high.png, garden.png, after-mobile.png, current-820.png, dusk.png and task-complete.png. Temporary screenshots do not persist across sandbox restarts.
- Actual Sol 1 allocation 40/40/20 and Solar USE task completed; successful TiltSun result read back with 3 stars.
- Initial same-view sample: Low 107 calls / 28,430 triangles; High 169 / 56,513. After polish: Low 108 / 39,658; High 166 / 113,577. These immediate-switch samples include camera/material warmup and are approximate, not a stable benchmark.
- Settled final Solar view after 16 rendered frames: Low 105 / 38,524; High 155 / 108,553. No comparable settled pre-change sample exists. High adds relief resolution; Low remains below 100K triangles but is above the preferred 100-call phone budget.
- Sandbox FPS is software-rendered and is not a real-device performance claim. Physical-phone smoothness, GPU appearance, audio and input feel remain unverified; use Settings → Low/High and the FPS/polygon counter while walking around Solar Park on the intended device.
- Bundled verification harness could not pass the two-step Play → Plan energy entry gate within its budget; focused Playwright flow passed instead. The harness captured no page exceptions or failed network requests, but emitted existing Drei-root teardown warnings during that attempt.
- No camera-control change, global SSAO pass or dependency was introduced. The explicitly requested later storm simulation is documented above; it does not infer dust opacity. Terrain and lighting provide depth without a full-screen SSAO cost. Existing gameplay-driven greenhouse health/color/growth is preserved.