# Greenhouse: a living space garden

## Boundaries
Preserve all energy formulas, demands, rewards, campaign data, narrator requests, controls and T14 Plant Care rules. English code; all authored text in the English and Brazilian Portuguese locale files. Harvesting is cosmetic only, with no balance changes.

## Before → after

### 1. Larger glass-vaulted sector
**Before:** A 24 × 24 circular module with an opaque-looking faceted dome and sparse beds.

**After:** A 40 × 32 arched greenhouse, clearly larger than the hub and other indoor modules. Keep the existing west tunnel connection at x=32; extend the garden eastward, centered at x=52. Use transparent glass panels, milk-white structural ribs, green trim and a warm tiled floor. Update shared room bounds so the floor plan, walking area and guidance remain aligned. Roof glass **and ribs** fade together inside and disappear in top-down/cutaway views; the floor remains visible.

### 2. Orchard plaza and sensible growing systems
**Before:** Eight identical beds with small isolated plant shapes.

**After:** A broad central walkway leads from the west entrance to an orchard plaza with a round-canopied dwarf apple tree and visible red apples. Long growing rows flank the walkway: stacked hydroponic trays with water channels and nutrient pipes, tower gardens, lettuce/herbs, tomato and bean trellises, and raised pots containing smaller apple trees. Rounded foliage in several greens, ripe fruit and small flowers distinguish crops without cluttering the walking route.

### 3. Alive, but readable
**Before:** Plant height scales with the existing allocation; there are no visible growing systems.

**After:** Pink/violet grow strips, tiny water/light/nutrient pictograms, capped ceiling mist and a friendly slow-patrolling watering drone. Warm glass tint suggests Martian sunset without adding a new time model. Restrained grow-strip and sensor illumination are functional greenhouse indicators, not unrelated decorative glow. The orchard station remains the only pulsing interaction marker.

### 4. Energy-driven visual states
**Before:** One allocation-to-demand ratio stretches plants vertically.

**After:** The same `alloc.greenhouse` and existing Sol demand drive every visual signal:
- **Ideal/surplus:** lush foliage, warm bright LEDs, more visible ripe apples, active mist/drone and a few pollen particles.
- **Moderate:** reduced foliage fullness, some dimmer racks and fewer ripe apples.
- **Deficit:** drooping yellow-green leaves, cold dim LEDs, fewer apples and stopped mist; never frightening.

Reuse the existing energy classification. Moderate is only a visual transition band within under-supply, not a new campaign status or formula. Surplus visuals do not imply extra rewards or energy storage.

### 5. Orchard interaction and harvest
**Before:** A generic nearby station opens the existing Plant Care task.

**After:** Place the existing T14 interaction at the orchard plaza, using the shared station coordinate. Keep its task availability, thirsty-plant sequence and rewards unchanged. Add a localized learning card and GAME RULE badge to its presentation. Nearby ripe apples can be tapped/clicked; a small basket display increments and the collected apple disappears. The basket is session-only cosmetic feedback, never written to energy, mission results or campaign data. Harvest interaction remains available independently of T14's scheduled availability.

### 6. Learning card and language
**Before:** No greenhouse-specific explanation.

**After:** A two-sentence localized card: plants provide food and help keep air fresh. No scientific quantities or invented citations. Mark the explanation **Source pending** until an authentic reference is supplied. Localize basket labels, harvest feedback, sensor descriptions and accessible control names in both languages.

### 7. Performance and verification
**Before:** Small interior geometry, with nearby-interior culling and Low/High settings already present.

**After:** Instance repeated foliage, fruit and rack elements; merge static pipes/ribs by material; share three-tone materials; avoid large textures and additional shadow lights. Cull dense interior detail at distance, cap particles (lower on Low), preserve shadows-off-touch and reduced-motion settings.

Capture **before and after draw calls/triangles** at matching greenhouse entrance, orchard and overview camera positions in Low and High. Report actual counts, including any target overruns; aim for under 100k triangles in Low, and minimize the additional calls.

Verify desktop and phone rendering, the existing tunnel-to-plaza walk, roof Auto/Off/top-down, allocation-driven visual changes, apple collection without campaign changes, T14 completion and both languages. Run existing tests plus focused tests for visual-state mapping and cosmetic harvesting. Sandbox checks cannot establish real-device frame rate, touch feel or audible sound.

## Technical implementation
- Keep expanded bounds, connection and station position in `config/rooms.ts`; extend map bounds and represent the real vault footprint in the minimap.
- Add a focused greenhouse scene module for the shell/interior, pooled materials, instanced crops, drone and particles, integrated with the existing Base lifecycle and cutaway controls.
- Keep cosmetic state and pure visual mapping outside campaign/engine resource logic; consume the existing allocation/demand rather than create another resource pool.
- Keep greenhouse material roles in `config/theme.ts` and mirror any UI roles in semantic CSS tokens.
- Preserve the original code-built toon art direction; no downloaded tree models or large textures.
- Leave NASA, n8n, scientific data and game formulas untouched.

## Open questions / proposed defaults
No blocking questions. Use an **arched rib vault**, keep the **west entrance**, and reset the **cosmetic basket on a fresh game session**. Use **Source pending** for the learning card rather than invent a reference. The allocation thresholds will be documented as presentation-only and verified against existing classification; they will not change outcomes.