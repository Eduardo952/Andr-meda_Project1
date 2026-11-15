# Ícones do HUD — Casa Marte

Quatro imagens originais geradas com a ferramenta **imagegen integrada**, sem CLI. Fontes PNG RGBA de 1254 × 1254 preservadas. Unity importa em até 256 × 256, sem mipmaps, com transparência, filtro bilinear e clamp. Uso no HUD: 44 × 48 unidades de UI, com rótulos nativos e barras separadas.

Paleta: contorno ameixa, creme, menta, azul e ouro. Formas arredondadas, contorno grosso, sombra simples. O símbolo de radiação é uma advertência, não uma recompensa: menos dose é melhor. Sem logotipos oficiais ou sugestão de endosso.

| Arquivo | Significado |
|---|---|
| `integrity-v1.png` | Estado da base: habitat e escudo |
| `oxygen-v1.png` | Oxigênio: bolhas e movimento de ar |
| `dose-v1.png` | Radiação: trevo amarelo/ameixa |
| `energy-v1.png` | Energia guardada: bateria e raio |

## Prompts enviados

Prefixo comum:

> Use case: stylized-concept. Create ONE polished cartoon game HUD icon for Casa Marte, a cozy low-poly Mars habitat educational game for children 7–14. Square canvas. Genuine transparent background. Large clean silhouette, centered with 10% empty margin, cream highlights, thick smooth dark plum outline, soft cel-shaded depth, slightly rounded chunky shapes, readable when displayed at 40 pixels. No lettering, no numbers, no background, no badge frame, no extra floating decorative stars.

Assuntos de cada chamada:

- Integridade: small ivory space habitat dome with mint protective shield, physical sturdiness, teal/mint accents, integrated symbol.
- Oxigênio: friendly sky-blue air bubble with two small connected bubbles and a white swoosh, breathable air, not a water droplet.
- Dose: standard three-lobed gold-yellow radiation trefoil, plum lobes and center, orange rim, educational caution, no skull.
- Energia: chunky gold lightning over teal battery silhouette, cream cap, outline and highlights.

## Conferência e procedência

`asset_report.py --require-alpha --json` aprovou os quatro arquivos: canal alfa real, pixels transparentes e nenhum problema reportado. A folha de contato e o HUD a 1920 × 1080 foram inspecionados. Relatório: `Tools/ArtEvidence/hud-icon-report.json`. As imagens não são material científico da NASA; foram geradas para o jogo. Os arquivos-fonte não foram recortados ou retocados.
