# Watering can sprite

`watering-can.png` was generated with the built-in image generation tool and the `imagegen` skill. The final image is a transparent 2.5D world prop, not a 3D mesh. `GreenhouseWorldView` displays it as a camera-facing sprite, positions it beside the astronaut, tilts it when watering and animates separate droplets. Original greenhouse rack meshes are reused.

## Generation prompt

Use case: stylized-concept. Asset type: transparent game sprite of a handheld watering can for a cozy children's Mars greenhouse game in Unity. One single beautifully designed watering can, entire object in frame with generous clear margin. Side three-quarter view, spout points RIGHT, open curved carry handle on top and rear, rounded squat sage green body with ivory rim and a small sunny yellow inset water-drop emblem, warm charcoal-purple outline, subtle dimensional shading, crisp hand-crafted cartoon shapes matching a premium friendly educational space-colony game. The spout is long enough to clearly distinguish from a teapot and ends in a broad perforated shower rose. Functional believable proportions, no face, no eyes, no words, no letters, no numbers, no background, no floor, no ground shadow, no water droplets or pouring stream (water will be animated by the game). Actual transparent alpha. This will be displayed as a camera-facing world prop and a UI inventory icon, so the silhouette must read clearly at small sizes.

## Final edit prompt

Edit only the background and outer edges of this watering-can game sprite. Preserve the entire can, its proportions, drawing, colors, outline and details exactly. Remove ALL purple glow, halo, haze, vignette, drop shadow and any color outside the hard silhouette. Remove any such background from the open handle holes as well. Outside the can must be completely transparent alpha=0 with clean antialiased edges only, like a professionally cut-out sprite. No colored matte behind the object, no glow whatsoever. Keep the entire watering can in frame.

The background and handle opening were checked for zero alpha. RGB values in transparent pixels must not be treated as visible background color.
