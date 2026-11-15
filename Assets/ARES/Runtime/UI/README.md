# Interface nativa da Base ARES

Fonte: `a896f420-3053-49eb-b6f2-a97f9b2dc6c8 (4)/src/ui`, `src/tasks` e `src/styles.css`.

## Responsabilidades

- `GameFrontend`: composição e navegação entre telas. Instalado no início de Play na raiz de `ReferenceExplorer`; não depende de alterações manuais na cena gerada. Controla bloqueio de movimento/câmera e encaminha comandos à campanha.
- `TitleBackdrop`: câmera e RenderTexture exclusivos para Marte, estrelas determinísticas e clone do rig Bubble. Libera objetos, materiais e textura ao sair do menu.
- `MissionHud`: barras derivadas da campanha, relógio, tarefas, próxima estação, estrelas e minimapa. A barra de energia mostra cobertura da demanda, conforme `Hud.tsx`. `MissionGuidance` mantém orientação, progresso do tour e textos locais de salas fora da campanha.
- `CampaignScreens`: relatório e resumo. Dados de amostra são identificados; valores ausentes não são apresentados como zero medido.
- `EnergyView`: alavancas com passo original e limite do orçamento efetivo. `Campaign.SetAllocation` continua sendo a autoridade para aceitar mudanças.
- `ChallengePanel`: estrutura compartilhada pelos desafios e planejamento, com cabeçalho/rodapé fixos e corpo rolável.
- `TaskView`: contexto, instruções, dicas e resultado. `TaskBoard` encaminha cada ID a `SolarTaskBoard`, `MechanicalTaskBoard`, `FarmTaskBoard` ou `ReadingTaskBoard`. `ChallengeReadings` apresenta cartões somente de leitura.
- `ChallengeArt`/`SnapshotArt`: desenhos vetoriais sem alterações de estado. `TaskTransferGesture`: arraste com captura/cancelamento de ponteiro; clique/teclado continuam disponíveis. As animações de bobinas respeitam movimento reduzido.
- `Runtime/Tasks/TaskAttempt`: estado das tentativas; `TaskReadings` adapta snapshots e fallbacks de treino. Resolver uma tentativa não altera recursos. Só `Campaign.FinishTask` aceita a recompensa, após Continuar.
- `Localization`: catálogo achatado sem dependência de JSON externo; exportado por `python Tools/AresImport/export-ui.py`.
- `Resources/ARES/UI/Game.uss`: estilos comuns; `Challenges.uss`: estilos isolados das estações; `RuntimeTheme.tss`: controles padrão do UI Toolkit. Fredoka e Nunito têm licenças OFL incluídas em `Fonts`.
- `ChallengePalette.cs` e `ChallengeColors.uss`: cores sRGB geradas dos tokens OKLCH originais por `python Tools/AresImport/export-challenge-colors.py`. Regenerar esses arquivos pelo script; manter cores e misturas por setor na fonte de referência, sem duplicar uma paleta manual.

## Contratos de integração

Relatório → planejamento → tarefas nas estações → confirmação de selagem no Controle → resumo → próximo Sol/final. Fechar um puzzle cancela apenas a tentativa. Não chamar `FinishTask` de botões genéricos nem conceder recursos em componentes visuais. Alarmes e relógio do Sol param em overlays; o relógio da tentativa só começa depois de Começar desafio. As consequências são aplicadas exclusivamente em `Campaign.Seal`.

E/Espaço/Enter e o botão USAR consultam `World/StationInteraction`: a área interna do setor é válida, além do raio configurado do ponto original. Corredores e a câmara de ar não iniciam tarefas. O mesmo resolvedor atende a confirmação de selagem; planejamento pendente e tarefa concluída aparecem no HUD, sem oferecer ações inválidas. Entrar no Escudo durante o alarme registra abrigo. Escape abre configurações ou fecha um overlay cancelável.

WASD, câmera manual, zoom, R, C e H permanecem em `ReferenceExplorer`/`CameraInput`. Shift esquerdo/direito usa `SprintSettings` (multiplicador padrão 1,75). `SprintDust` consome o deslocamento efetivo, emite partículas reutilizadas por distância e mantém o efeito separado da simulação. `Resources/ARES/Effects/CartoonDust.shader` desenha os tufos sem texturas externas. Movimento reduzido diminui emissão/tamanho. Overlays bloqueiam corrida e nova emissão.

`ApplicationExit` é o único ponto de saída: salva preferências, encerra Play no editor e chama `Application.Quit` no jogador compilado. Os textos exclusivos da Unity ficam nos catálogos `pt-BR.unity.json`/`en.unity.json`, carregados depois dos textos exportados, sem modificar a referência.

## Verificação

- `Temp/ares-station-sprint-check.request`: políticas de interação nos cinco Sols, teclado virtual E/Escape nos centros/pontos dos cinco setores, velocidade de corrida, emissão somente em movimento, bloqueio por menu e saída real de Play pelo botão Sair. Resultado em `Logs/ares-station-sprint.txt`; capturas `ares-sprint.png` e `ares-title-exit.png`.

- `Temp/ares-challenges-check.request`: suíte em Play dos 14 desafios, limites do painel/traduções, controles nativos, arrastes sintéticos e capturas antes/depois em `Logs/ares-challenge-*.png`. Resultado com data em `Logs/ares-challenges.txt`. Não substitui teste da campanha nem de touch físico.

- `ARES > Validate task interactions`: 14 puzzles × 5 snapshots, incluindo instruções sem consumo de tempo, expiração, congelamento após sucesso e isolamento dos recursos.
- `Temp/ares-ui-check.request`: teste em Play com eventos nativos de botões/alavancas. Percorre os cinco Sols; usa soluções programáticas dos puzzles e não comprova navegação espacial. Resultado `Logs/ares-ui.txt`; capturas `Logs/ares-*.png`.
- `Temp/ares-play-check.request`: teste separado de movimento/animação/câmera. Desativa explicitamente o menu apenas durante esse teste.
- `Temp/ares-validate.request`: suíte existente de paridade e testes de tarefas. Não emitir vários pedidos de Play simultaneamente nem editar scripts durante os testes.

## Limites desta implementação

Não representa ainda equivalência visual/funcional completa. Os diagramas e controles agora seguem os componentes da referência, incluindo painel inclinado, circuitos, cabos e cultivo. A comparação lado a lado no navegador não foi concluída; diferenças de fontes/SVG, blur do cenário e gestos em dispositivos físicos ainda precisam de revisão. PhotoDetective utiliza ilustração local identificada, sem consulta remota NASA. O relatório inclui SEP/impacto/pressão, mas seu adaptador deve ser confrontado com a normalização web antes de trocar snapshots. O tour e as falas de entrada são textuais; painel de perguntas/voz, som, joystick, acessibilidade completa, telescópio, confete do certificado e efeitos de consequências no mundo permanecem pendentes. Não remover a referência nem tratar os testes de lógica como prova de igualdade visual.
