# Botão do gerenciador de oxigênio

Asset final: `button-cream-v1.png`. Gerado com a skill **imagegen**, pela ferramenta integrada `image_gen`, em 15/11/2026; não foi usado o CLI/API. A cópia final está neste diretório e é carregada por `OxygenControls`. O original foi preservado em `.codex/generated_images/01a1de62-1895-78f1-82f8-df2f9e9adb37/exec-111a2427-1f06-47bc-9d51-0adc7dd02dc1.png` no perfil do usuário.

Uma pele de botão reutilizável mantém as três escolhas consistentes. Títulos, explicações, foco, seleção e ação são elementos UI Toolkit, separados da imagem. O estado selecionado usa tonalidade verde e o texto **EM USO**; não depende só de cor. A importação específica preserva transparência, remove mipmaps e evita compressão com perdas. Não substitua o texto por texto rasterizado.

## Prompt final, sem alterações

```text
Use case: stylized-concept. Asset type: a single reusable raster button skin for a polished children's space-base management game (ages 7–14), intended for Unity UI. Generate ONE empty horizontal rounded rectangular push-button, front view, centered, with actual transparent background and very tight transparent padding. Wide landscape canvas, about 3:1 aspect ratio. Friendly handcrafted cartoon interface, refined and readable rather than babyish. Warm ivory face, subtle paper-like surface, elegant muted dark teal outer outline, generous soft rounded corners, a thick lower bevel in desaturated sage that makes it feel pressable, tiny warm highlight along upper edge, two very small recessed decorative corner screws. Flat orthographic composition with restrained soft shading, crisp clean silhouette, no perspective. The central 75% is completely blank and quiet, suitable for overlaying editable dark text and a small symbol in the game. Keep all edges inside the image. No words, no letters, no numbers, no icons, no stars, no logos, no watermark, no surrounding panel, no checkered backdrop, no drop shadow far outside the silhouette. One button only, not a screenshot or a contact sheet. Match cream, sage-green, teal and warm-sand cartoon menus.
```

Para uma nova versão, gere um arquivo irmão (`button-cream-v2.png`) e ajuste o caminho em `OxygenControls` e no importador. Confira os três estados com os textos do jogo e em largura reduzida; preserve o arquivo e o `.meta` existentes até concluir a troca.
