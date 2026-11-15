# Cenário atual: casa-marte-missao-n8n

A cena de entrada é `Assets/ARES/Scenes/BaseARES.unity`. A fonte é `casa-marte-missao-n8n/src`, preservada sem alterações. As skills locais **create-game-assets**, **level-design** e **game-ui-ux** orientam geometria, circulação e apresentação.

## Integração

O layout radial original contém o hub, cinco corredores, observatório, escudo, suporte de vida, estufa e parque solar. Inclui árvore central, bancos, monitores, hologramas, partículas, cúpula magnética, gerador, mastro, plataforma, foguete e o rover original com seis rodas.

A estufa usa quatro fileiras de dez recipientes. Regador e reservatório são originais; as áreas de interação vêm das malhas. Os 32 painéis, vassoura e robô usam o referencial solar rotacionado. Telescópio, tripulação, radar e vapor acompanham seus setores.

O mapa mostra os setores e marcos externos; exclui céu e cúpula magnética. Os telhados são ocultados somente durante sua captura. Planetas começam longe do spawn no prefab e usam identidade explícita para não depender de nomes automáticos da Unity.

## Organização para manutenção

Os caminhos abaixo são relativos a `Assets/ARES/Runtime`, salvo quando indicados.

| Módulo | Responsabilidade |
| --- | --- |
| `Tools/AresImport/export.mjs` (raiz) | Extração offline; manifesto publicado por último. |
| `Tools/AresImport/visual-adapters.mjs` (raiz) | Separa peças móveis em cópias na memória e extrai alvos de rega; não edita a referência. |
| `Tools/AresImport/mesh-quality.mjs` (raiz) | Indexação preservando atributos e remoção de triângulos inválidos/repetidos. |
| `Assets/ARES/Editor/AresImporter.cs`, `CharacterImporter.cs` | Geram assets, prefabs e cena, sem regras de campanha. |
| `Data/AresData.cs`, `GameDefinition` | Coordenadas, portas, referencial solar, marcos e alvos da estufa. |
| `World/ReferenceRig.cs`, `ReferenceWorldPart.cs` | Identidades estáveis dos rigs e peças; não vincular comportamento a nomes automáticos de assets. |
| `World/WorldNavigation.cs`, `ReferenceWorldGuidance.cs` | Colisão, portas orientadas e rotas baseadas no layout. |
| `World/ReferenceLandmarks.cs` | Copa, hologramas, partículas, luz e cúpula; leitura da campanha. |
| `World/ReferenceGarden.cs`, `ReferenceAmbience.cs` | Jardim, tripulação, painéis, robô, radar e efeitos ambientais. |
| `World/GreenhouseCare.cs` | Entrada, proximidade e encaminhamento à simulação. |
| `World/GreenhouseWorldView.cs`, `GreenhouseCanView.cs` | Rótulos, áreas, água e pose do regador original. |
| `World/Rover/ReferenceRoverBinding.cs` | Liga o modelo importado a seis pivôs, preservando controlador e câmera. |
| `World/MapCapture.cs`, `UI/WorldMapView.cs` | Captura, enquadramento e descarte separados da interface. |
| `UI/MapLabelLayout.cs` | Posiciona os rótulos conforme projeção e espaço disponível, evitando sobreposição em janelas menores. |
| `Assets/ARES/Resources/ARES/Effects/ReferenceShield.shader` | Fresnel e faixa animada conforme a referência. |

As animações não escrevem energia, oxigênio, estrelas ou conclusão de tarefas; essas regras pertencem à campanha e às simulações. Movimento reduzido suspende os efeitos intensos. Malhas e materiais importados são compartilhados; variações por instância usam `MaterialPropertyBlock`.

## Geometria e custo

São **1.657 malhas**, **288.526 vértices** e **204.788 triângulos** de cenário. A limpeza reduziu os vértices de 516.517 para 288.526 (**44,1%**), removendo 865 triângulos degenerados e 108 duplicados, sem simplificar silhuetas ou fundir costuras de UV/normais distintas. Personagens preservam hierarquia e pivôs.

Usam-se índices de 16 bits quando possível, materiais compartilhados, static batching para peças opacas imutáveis e pools limitados de efeitos. Pisos permanecem separados dos grupos de paredes/tetos. Peças móveis e transparentes ficam fora do batching estático. A navegação usa volumes simples do layout.

## Regeneração

1. Salvar alterações autorais antes de regenerar. A cena é saída do importador: mudanças duráveis pertencem aos geradores, adaptadores e componentes.
2. Executar `node Tools/AresImport/export.mjs` na raiz.
3. Executar `node Tools/AresImport/check-world.mjs`.
4. Escrever `scenery` em `Temp/ares-import.request` para importar e verificar a geometria sem a suíte geral de campanha.
5. Usar **ARES > Checks > Reference scenery and geometry** para conferência focada e capturas.
6. Escrever `build` em `Temp/ares-scenery-build.request` para gerar `Builds/ReferenceScenery/ARES.exe`.

Não corrigir `Assets/ARES/Import/*.json` manualmente. O manifesto contém hashes SHA-256 das fontes e saídas; versões misturadas impedem a importação. Adaptadores interrompem a extração se os trechos esperados ou as quatro fileiras/40 recipientes deixarem de existir. O exportador não carrega `.env`, executa servidores ou consulta NASA/n8n.

Backup anterior à migração: `Temp/BaseARES-before-casa-marte.unity`.

## Evidências e limites

- `Logs/ares-request-status.txt`: conferência dos vértices, winding, UVs, cores e texturas importados.
- `Logs/ares-world-geometry.json`: **75.132 amostras de piso/passagem, zero lacunas detectadas e zero normais nulas**; respeita o contorno circular dos domos.
- `Logs/ares-scenery.txt`: 9.672 amostras de navegação da referência, acesso às sete estações, vínculos, shaders e batching.
- `Logs/ares-scenery-*.png`: capturas por setor e dos marcos externos.
- `Logs/ares-scenery-build.txt`: resultado da build.

Build Windows de 15/11/2026 concluída: **0 erros, 2 avisos**, 136.292.981 bytes. O mapa passou na conferência de tela cheia e janela compacta, incluindo zoom, navegação, alinhamento do marcador, pausa e descarte da câmera (`Logs/ares-map.txt`).

A cobertura é amostral; não prova ausência de lacunas em todo ponto arbitrário. Vidro e abóbadas têm superfícies abertas intencionais. URP e Three.js diferem em iluminação, transparência, antialiasing e tone mapping; igualdade pixel a pixel não foi certificada. Funcionalidades pequenas não receberam uma bateria individual, conforme solicitado. A suíte antiga completa de campanha tem uma divergência conhecida no traço de oxigênio (78 versus 79); a migração não altera regras para mascará-la.
