# Mapa panorâmico

O botão **Mapa [M]**, no lado direito do HUD, substitui o antigo diagrama pequeno. Clique ou pressione **M** para abrir; **M**, **Escape** e **Voltar** fecham. O mapa ocupa toda a tela e mostra uma câmera ortográfica olhando verticalmente para o cenário real. Não é uma ilustração inventada do terreno.

O ponto menta indica a localização e a ponta dourada indica a orientação do personagem. Ao pilotar o rover, ambos acompanham o rover e a legenda identifica NOMAD 06. O norte do mapa corresponde a **+Z da Unity**, com +X à direita. O mapa não gira com a câmera de jogo.

## Zoom e navegação

- Use a **roda do mouse** sobre o mapa para ampliar/reduzir em torno do cursor, ou os botões **+ / −** para ampliar pelo centro. A escala vai de **100% a 600%** por padrão.
- **Segure o botão esquerdo e arraste** para explorar o mapa ampliado. O mapa acompanha o arrasto e respeita os limites da vista geral. A captura do ponteiro mantém o gesto ao sair da área e termina ao soltar, cancelar, perder a captura, redimensionar ou fechar.
- O mapa abre enquadrando os setores do layout atual. **Ver base** recupera esse enquadramento. **Localizar** centraliza o astronauta/rover e garante pelo menos 200% de zoom (respeitando `maxZoom`). **Visão geral** mostra toda a área explorável em 100%.
- Pelo teclado: **+ / −** ajustam o zoom; **Home** restaura a vista geral. Clique no mapa ou use Tab para focá-lo e navegue com as **setas**. Com foco em um botão, as setas mantêm a navegação nativa entre controles.
- Se o jogador ficar fora do recorte, o marcador é ocultado e aparece a orientação para usar Localizar. Rótulos e marcador conservam o tamanho e acompanham a mesma projeção da câmera.

## Responsabilidades

| Módulo | Responsabilidade |
| --- | --- |
| `World/MapCapture.cs` | Câmera independente, limites, RenderTexture e descarte; contém `MapSettings` serializável. |
| `World/MapProjection.cs` | Ajuste de proporção sem recortar limites e conversão mundo → UI. |
| `World/MapNavigation.cs` | Estado independente de entrada/renderização: zoom ancorado, centro, limites, localizar e restaurar. |
| `UI/MapViewportInput.cs` | Roda, arrasto e ciclo de captura/liberação do ponteiro. |
| `UI/WorldMapView.cs` | Cabeçalho, panorama, nomes dos setores, legenda, foco e redimensionamento. |
| `UI/MapMarker.cs` | Ponto com ponta, desenhado em tamanho constante por UI Toolkit. |
| `UI/GameFrontend.cs` | Entrada M/Escape, abertura/fechamento, pausa e bloqueio dos controles de fundo. |
| `UI/MissionHud.cs` | Botão ilustrado; não captura o cenário nem calcula coordenadas. |
| `World/ReferenceExplorer.cs` | Expõe `ControlledPosition` e `ControlledForward`, inclusive para o rover. |
| `Resources/ARES/UI/Map.uss` | Aparência e disposição dos elementos. |

`PlayerMotion.Heading` está em radianos no espaço de origem. O vetor Unity é `(sin(heading), 0, -cos(heading))`. Não utilize `astronaut.forward`: o modelo visual tem convenção própria. A projeção UI inverte o eixo vertical uma única vez. A câmera e os marcadores usam os mesmos limites/proporção.

## Ajustes no Inspector

Em `ReferenceExplorer.mapSettings`:

- `padding`: margem em metros, padrão 8.
- `altitude`: altura da câmera, padrão 250; aumente para cenários com construções muito altas.
- `maxTextureSize`: maior dimensão da textura, padrão 2048, entre 512 e 4096.
- `maxZoom`: ampliação máxima, padrão 6 (600%), entre 1 e 12.

Os limites incluem toda a área permitida ao rover (`roverSettings.worldLimit`) e todos os setores do layout. A abertura e **Ver base** usam os limites dos setores, com margem, para destacar o cenário atual. Mantém a escala uniforme em outras proporções de tela. Fora da malha de terreno aparece o fundo terracota da captura. Os telhados são ocultados somente durante o desenho da câmera do mapa; seu estado é restaurado antes da câmera principal. Colisões e materiais são preservados. Planetas e estrelas ficam na camada 30 e o diorama do título na 31: ambas são excluídas da captura.

A câmera e a textura só existem durante a consulta. Redimensionar recria a textura apenas se suas dimensões mudarem. Fechar, reconstruir o HUD, retornar ao título, desativar ou destruir o frontend libera os recursos. A resolução é limitada para controlar memória/custo; o mapa fechado não renderiza uma câmera adicional.

Zoom e deslocamento alteram a projeção/câmera, sem recriar a RenderTexture. Redimensionar conserva o zoom e ajusta o centro aos novos limites. O arrasto usa deltas normalizados pela área útil da UI, preservando o comportamento com o escalonamento do painel. A âncora sob o cursor é mantida até encontrar um limite de navegação.

## Fluxo e manutenção

A consulta pausa o relógio e os recursos da campanha, bloqueia movimento/interação/órbita e desabilita os controles do HUD. O foco começa em Voltar, pode circular pelos controles do mapa e retorna ao botão Mapa ao fechar. Não abre sobre tarefas, relatórios, configurações ou sessão do computador. O HUD deixa de atualizar controles enquanto o mapa está aberto, evitando que um botão de fundo seja reabilitado.

Os textos estão nos catálogos `pt-BR.unity.json` e `en.unity.json`, sob `map.*`. Os nomes vêm de `rooms.*`; não são gravados na imagem. Para novos setores, mantenha o layout como fonte das coordenadas e ajuste somente os afastamentos de rótulos em `Map.uss` quando necessário.

## Verificação focada

Com BaseARES aberta, execute **ARES → Checks → Fullscreen world map**, ou escreva `world-map` em `Temp/ares-computer-check.request`. O editor entra e sai de Play automaticamente. Não edite scripts durante a execução.

Resultado: `Logs/ares-map.txt`. Capturas: `Logs/ares-map-{button,fullscreen,compact,rover}.png`. Relatório de transparência/importação: `Logs/ares-map-art.json`.

A extensão de navegação verifica âncora do zoom, limites mínimo/máximo, sentido/limites do deslocamento, redimensionamento, botões, roda e arrasto por eventos UI Toolkit, interrupção ao soltar, localizar/restaurar, alinhamento do marcador ampliado, pausa e reutilização da textura. Captura adicional: `Logs/ares-map-zoom.png`. Os eventos de ponteiro são sintéticos; não substituem validação em hardware de toque.

Verifica projeção nos quatro sentidos, reflexão do heading, proporções, M pressionado/segurado/fechamento, Escape, botões, foco, bloqueio do HUD/movimento/rover, pausa, sobreposição de rótulos, coordenadas do marcador contra `Camera.WorldToViewportPoint`, redimensionamento, impedimento sobre relatório/configurações e descarte das câmeras. O modo compacto restringe o painel a 960 × 600 unidades dentro da Game View; não representa uma build móvel. Não houve validação de build standalone.

## Arte e skills

O botão usa `Resources/ARES/UI/HudIcons/map-v1.png`, gerado pela ferramenta **imagegen integrada**, sem CLI. Original PNG com alfa preservado; importação pelo `HudIconImporter` em até 256 px, sRGB, filtro bilinear, clamp e sem mipmaps. Arte exibida a 62 px no HUD e 72 px no cabeçalho. O marcador é geometria nativa para continuar nítido e girar sem arte adicional.

Direção visual: mapa dobrado creme, terreno terracota, rota menta, ponta dourada e contorno ameixa. A skill local `Tools/Skills/game-ui-ux/SKILL.md` orientou hierarquia, layout adaptável, foco e isolamento modal. As fontes Fredoka/Nunito e a paleta vêm da interface existente.

Prompt final enviado à ferramenta integrada:

> Use case: stylized-concept. Asset type: production game HUD button icon for Casa Marte, a cozy low-poly Mars habitat educational game. Create ONE polished illustration of a chunky folded ivory map with subtle terracotta terrain patches, a simple mint route and a gold navigation pointer resting above the map. Match the project's established cream, mint, warm gold and dark plum palette, thick smooth plum outlines, softly rounded forms, restrained cel shaded highlights, clean readable silhouette at 56 pixels. Square canvas, centered with 10 percent clear margins. Genuine transparent background. No lettering, no numbers, no background, no badge frame, no extra floating decorations, no logos. This is the illustrated map button asset, not the actual gameplay map.

Referência técnica: [Unity Camera.targetTexture](https://docs.unity.com/en-us/engine/6000.7/script-reference/unityengine/camera/targettexture).
