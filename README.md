# Base ARES — migração para Unity

## Computador central e oxigênio

O computador da **sala central, perto do spawn**, recebe uma indicação cartoon de **E** quando o astronauta se aproxima da frente do console. E inicia uma aproximação de câmera de 1,65 s até a tela física, que exibe o mesmo painel de oxigênio da interface. **Escape** ou **Voltar** reverte a transição e devolve o controle, inclusive se acionado durante a aproximação. Movimento, órbita e rover ficam bloqueados durante a sessão. Movimento reduzido usa uma dissolução curta sem voo de câmera.

O painel foi simplificado para crianças de **7 a 14 anos**: tanque com porcentagem, duas barras para comparar produção/uso e uma orientação curta que muda conforme a situação. **Poupar / Equilibrar / Reforçar** usam botões com arte gerada pela skill imagegen e textos editáveis; a escolha explica seu efeito sobre água, energia e reserva. **Saiba mais** reúne explicações, números e fontes em seções opcionais. Organização dos módulos e validação visual focada: [OXYGEN_UI.md](Assets/ARES/Runtime/UI/OXYGEN_UI.md). O cálculo continua usando cargas metabólicas publicadas pela NASA, consumo de água pela eletrólise e retirada separada de CO₂. Sem água não há geração; sem energia o gerador e o filtro perdem capacidade. Parâmetros, unidades, fórmulas e limites: [OXYGEN_SCIENCE.md](Assets/ARES/Runtime/Domain/OXYGEN_SCIENCE.md).

A reserva física alimenta o percentual de oxigênio da campanha/HUD. Recompensas e penalidades de tarefas continuam sob autoridade de `Campaign`. Os controles e os totais persistem ao fechar/reabrir e entre Sols; uma nova missão reinicia o sistema. **Energia só pode ser gerenciada no computador físico**: a seta à direita desliza até Energia; a seta à esquerda retorna ao Oxigênio. Movimento reduzido troca as páginas imediatamente.

Cada Sol dura **600 segundos (10 minutos) de jogo ativo**, contados a partir de Iniciar Sol no computador. Tarefas e uso do computador contam; o menu de pausa congela relógio e suporte à vida. Ao terminar o tempo, a atividade aberta é encerrada sem premiar tarefas incompletas e aparece o resumo. Continuar abre o relatório do dia seguinte. Planejamento, relatórios entre dias e resumos não consomem o tempo do próximo Sol. `MissionTime` centraliza a escala; o HUD mostra a contagem regressiva.

Responsabilidades: `Domain/OxygenPlant.cs` contém configurações e balanço de recursos; `Campaign.TickOxygen` integra reserva/HUD e custo real em `SolarPlant.DrawEnergy`. `World/Computer/ComputerTerminal` coordena proximidade e sessão; `ComputerCamera` possui a câmera durante a transição; `ComputerScreen` desenha a interface em uma textura sobre o monitor existente. `UI/ComputerPrompt`, `ComputerPanel`, `OxygenView` e `OxygenTankArt` cuidam da apresentação, com estilo em `Resources/ARES/UI/Oxygen.uss`. A letra E reutiliza o alfabeto cartoon autoral do projeto. Ajuste posições, alcance e duração em `ReferenceExplorer.computerSettings`, e taxas em `oxygenSettings`.

Validação focada: **ARES → Checks → Computer and oxygen**, ou `Temp/ares-computer-check.request`. Resultado em `Logs/ares-computer.txt`; capturas `Logs/ares-computer-{prompt,transition,oxygen,energy,science}.png`. Inclui conservação de matéria, referência metabólica, água/energia, filtro de CO₂, 600 s/pausa/tarefa pendente, sorteio das imagens, setas e bloqueio de acesso remoto. Não executa o rover nem abre todas as tarefas.

## Rover NOMAD 06

Em Play, **Y** alterna entre o astronauta e a pilotagem remota do rover. **W** acelera, **S** freia e engata a ré, **A/D** esterçam. Arrastar o cenário com o mouse orbita a câmera, a roda ajusta a distância e **R** centraliza atrás do veículo. O ângulo manual acompanha as curvas. Y estaciona o rover e devolve o controle ao astronauta na posição original. Menus interrompem movimento e emissão de poeira; tarefas a pé ficam bloqueadas durante a pilotagem.

O modelo 3D autoral é construído por malhas paramétricas: carroceria chanfrada, seis pneus com sulcos em V, cubos e parafusos, braços e molas de suspensão, para-lamas, painéis solares com células individuais, faróis, mastro de visão e antena. Peças fixas são agrupadas por material; rodas têm pivôs separados para giro e esterçamento. Não depende de serviço externo ou imagens. A suspensão é visual, com condução arcade cinemática; não é uma simulação de WheelColliders.

Configuração no Inspector de `ReferenceExplorer`, em `roverSettings`: velocidade máxima (12 m/s), ré (4 m/s), aceleração (2,4 m/s²; cerca de 5 s até a máxima), frenagem, direção, posição inicial, câmera e poeira. O rover circula pelo terreno externo, respeitando os volumes da base, corredores cobertos, parque solar, rochas e limite do mapa.

Código em `Assets/ARES/Runtime/World/Rover/`: `RoverMotion` contém a simulação independente de entrada/renderização; `RoverNavigation` reserva volumes; `RoverGround` adapta o terreno importado; `RoverCamera` combina órbita manual e orientação; `RoverMesh`/`RoverModel` constroem e animam a arte; `RoverDust` apresenta os efeitos; `RoverController` reúne os módulos. `ReferenceExplorer` mantém a autoridade de entrada e câmera e `MissionHud` exibe controles e velocidade. Ao substituir o cenário, adapte `RoverGround` e os limites, sem alterar a simulação.

Verificação: menu **ARES → Checks → Rover driving and presentation**, ou crie `Temp/ares-rover-check.request`. Resultado em `Logs/ares-rover.txt`, capturas em `Logs/ares-rover-{model,rear,dust,hud}.png`. O teste usa dispositivos virtuais temporários e sai de Play ao terminar.

## Comece aqui — manutenção do projeto

Abra o projeto com **Unity 6000.6.9f1**, carregue `Assets/ARES/Scenes/BaseARES.unity` e entre em Play. `GameFrontend` instala os sistemas e a interface em tempo de execução. Não é necessário anexar os novos componentes à cena. As seções datadas abaixo registram a evolução; esta seção e os documentos dos módulos descrevem o comportamento atual.

### Observatório e diário de descobertas

**Observatório atualizado:** clique no telescópio ou use E perto dele para explorar uma imagem real do APOD por Sol. O botão **Diário** permite rever os dias já liberados. A seleção de cinco datas históricas possui resumos em português, perguntas, créditos e cópias locais para falhas de rede. A API está integrada, mas as consultas verificadas retornaram logotipos inválidos; o jogo identifica e usa o arquivo conferido. Arquitetura, configuração de chave/proxy, dados, limites e testes: [OBSERVATORY.md](Assets/ARES/Runtime/World/OBSERVATORY.md).

### Rega manual e gerenciamento da estufa

1. Na estufa, pressione **E** perto do regador para pegá-lo. Na estação **ÁGUA**, E transfere água do reservatório para o regador.
2. Use E na área da estufa para abrir o gerenciamento, escolher Baixa/Mediana/Alta e preparar a rega. E no suporte devolve o regador; E no abastecimento cheio mantém a interação ali, sem abrir o gerenciamento.
3. Entre nas quatro áreas delimitadas e **segure o clique esquerdo** para regar. Cada prateleira leva 2,5 segundos. Soltar, afastar-se ou abrir um menu interrompe o fluxo e conserva o progresso.
4. O ciclo só avança após regar as quatro prateleiras. O HUD acompanha saúde, solo, luz, clima, água carregada, reservatório e progresso. Ao encerrar a atividade, abra o painel e registre o aprendizado para concluir a tarefa.

São 11 L por atividade e doses de 1,36/2,04/2,32 L para **toda a estufa**, divididas igualmente entre as quatro prateleiras. Encher o regador apenas transfere água; não a consome novamente. O estado sobrevive ao fechamento do painel; um novo Sol inicia outra atividade. Os níveis são uma adaptação educativa, não recomendações oficiais da NASA.

### Onde alterar cada comportamento

| Responsabilidade | Arquivo ou documento |
| --- | --- |
| Solo, luz, clima, saúde e doses | `Assets/ARES/Runtime/Domain/GreenhouseSimulation.cs` e [regras da estufa](Assets/ARES/Runtime/Domain/GREENHOUSE.md) |
| Capacidade, duração, água entregue e retomada | `Assets/ARES/Runtime/Domain/GreenhouseWatering.cs` |
| Pegar, abastecer, áreas e prioridade de E/clique | `Assets/ARES/Runtime/World/GreenhouseCare.cs` |
| Posição do regador, pose, gotas e marcadores | `Assets/ARES/Runtime/World/GreenhouseWorldView.cs` |
| Visibilidade do jogador atrás das plantas | `Assets/ARES/Runtime/World/GreenhouseOcclusion.cs` |
| Painel, HUD e textos educativos | `GreenhouseView`, `GreenhouseCareHud` e `GreenhouseNarrator`, em `Runtime/UI` |
| Recompensas e reinício por Sol | `Assets/ARES/Runtime/Domain/Campaign.cs` |
| Manutenção dos 32 painéis solares | [SOLAR_MAINTENANCE.md](Assets/ARES/Runtime/World/SOLAR_MAINTENANCE.md) |
| Arquitetura geral da interface | [README da UI](Assets/ARES/Runtime/UI/README.md) |

Leia [MANUAL_WATERING.md](Assets/ARES/Runtime/World/MANUAL_WATERING.md) antes de alterar a rega: ele explica o balanço de água, as coordenadas, as dependências, a orientação do regador e as verificações. Mantenha regras no domínio, entrada/apresentação no mundo e recompensas somente em `Campaign`. Não chame `GreenhouseSimulation.Decide` diretamente pela UI: a entrega manual deve concluir o ciclo através de `GreenhouseWatering`.

O regador usa arte **2.5D**, presa pela alça à mão direita e inclinada durante a rega; não é uma nova malha 3D. Os quatro grupos cultiváveis reutilizam as malhas importadas; outras plantas do cenário são decorativas. Para trocar a arte, ajuste também o pivô da alça e a origem das gotas. A conversão de orientação do personagem está documentada no módulo.

O [alfabeto cartoon](Assets/ARES/Resources/ARES/UI/CartoonAlphabet/README.md), gerado com a skill `imagegen`, contém **52 caracteres**: A–Z, 0–9, acentos e símbolos. `CartoonText` permite reutilizá-los em títulos; textos longos continuam com fontes legíveis. São sprites com mapa de recortes, não uma fonte TTF. Os prompts finais e as instruções para atualizar os recortes estão junto aos assets. A [arte do regador](Assets/ARES/Resources/ARES/UI/Greenhouse/WATERING_CAN_ART.md) também registra sua origem e seus prompts.

### Como verificar mudanças

Com BaseARES aberta no editor, crie **um arquivo de solicitação por vez**, espere o resultado e não edite scripts durante Play. O conteúdo desses arquivos não é executado; o editor reconhece apenas os caminhos fixos. Os testes entram e saem de Play automaticamente e usam dispositivos virtuais temporários.

| Solicitação na raiz do projeto | Resultado | Cobertura |
| --- | --- | --- |
| `Temp/ares-watering-check.request` | `Logs/ares-watering.txt` | Doses, conservação de água, interrupções, E, rega das quatro áreas, alinhamento da alça e 52 caracteres |
| `Temp/ares-observatory-check.request` | `Logs/ares-observatory.txt` | Telescópio, API/falhas, imagens reais offline, diário e liberação dos cinco dias |
| `Temp/ares-greenhouse-check.request` | `Logs/ares-greenhouse.txt` | Solo, clima, saúde, saldo insuficiente, textos e proteção de recompensas |
| `Temp/ares-ui-check.request` | `Logs/ares-ui.txt` | Controles da campanha, 25 resultados e vitória após cinco Sols |
| `Temp/ares-solar-check.request` | `Logs/ares-solar.txt` | Poeira, reserva, vassoura e limpeza |
| `Temp/ares-broom-check.request` | `Logs/ares-broom.txt` | Apenas pegar/devolver vassoura e encaixe na luva |
| `Temp/ares-greenhouse-tools-check.request` | `Logs/ares-greenhouse-tools.txt` | Apenas regador, água preservada e obstrução das plantas |
| `Temp/ares-station-sprint-check.request` | `Logs/ares-station-sprint.txt` | Áreas de interação, corrida e saída |

Exemplo em PowerShell: `Set-Content Temp/ares-watering-check.request 'verificar rega'`. Capturas de rega e alfabeto ficam em `Logs/ares-watering-*.png` e `Logs/ares-cartoon-alphabet.png`. O teste da campanha usa soluções de fixture e não substitui uma caminhada manual completa. Não foi validada uma build standalone nesta etapa.

Preserve os `.meta` dos assets para manter referências da Unity. `Import/` e `Generated/` vêm do exportador/importador; faça mudanças autorais em `Runtime/`, `Resources/` e ferramentas específicas, evitando perder trabalho numa reimportação. Preserve a pasta de referência **(4)**. O port continua em andamento; os limites de fidelidade descritos abaixo ainda se aplicam.

## Mestre da Estufa — 15/11/2026

A interação da estufa agora abre uma atividade por ciclos em português, com quatro prateleiras (Fruta, Legume, Verdura, Verdura), 11 L iniciais, solo e luz moderados. Irrigação Baixa/Mediana/Alta consome 1,36/2,04/2,32 L para toda a estufa; ajustes contextuais usam o custo do nível final. Clima simulado, risco por problemas repetidos, saldo exato e explicações curtas acompanham cada decisão. Não há cronômetro nem escolha automática. Fechar e reabrir preserva o cultivo; cada novo Sol inicia uma nova atividade.

O encerramento mantém qualquer sobra menor que o consumo mínimo e oferece uma reflexão. `Campaign.FinishGreenhouseTask` valida a conclusão e impede recompensa duplicada ou desvio pelo fluxo antigo. Modelo, narrador e apresentação são separados; detalhes em [GREENHOUSE.md](Assets/ARES/Runtime/Domain/GREENHOUSE.md).

Arte das quatro prateleiras gerada com a skill `imagegen`, salva no projeto e inspecionada em Play. Prompt em [ART_DIRECTION.md](Assets/ARES/Resources/ARES/UI/Greenhouse/ART_DIRECTION.md). Controles são UI Toolkit, usando a estética existente. Não foi criado um novo modelo 3D; as malhas da estufa importadas da referência foram preservadas.

Passaram os testes de regras, nove transições do solo, contas de água, custo após ajuste, clima, risco, saldo insuficiente, retomada com E/Escape, decisões por controles nativos, reflexão e proteção de recompensas (`Logs/ares-greenhouse.txt`). Paridade e puzzles históricos também passaram. As capturas `Logs/ares-greenhouse-{start,weather,end}.png` foram inspecionadas.

## Poeira, vassoura e reserva solar — 15/11/2026

Os 32 painéis agora acumulam poeira individualmente após um período inicial, perdem produção e recebem uma camada de areia levemente mais clara. E coleta a vassoura na entrada do parque; segurar o clique esquerdo por 4 segundos dentro do contorno limpa o painel. Soltar, sair da área ou abrir um menu cancela o progresso. A câmera continua permitindo arrasto direito e zoom, sem disputar o clique de limpeza.

Cada painel limpo gera 0,4 kWh por ciclo de jogo de 30 segundos. A reserva começa em 160 kWh e alerta abaixo de 100 kWh; o consumo padrão é 9,6 kWh/ciclo. Limpar recupera produção e permite recarga gradual. O tempo inicial de poeira é 180 segundos, escalonado por painel. Parâmetros no Inspector de `ReferenceExplorer`, em `solarSettings`. O orçamento original de tarefas continua separado da reserva física.

Verificados em Play: ligação dos 32 painéis, E para pegar a ferramenta, clique segurado, cancelamento, bloqueio da câmera, alerta no HUD e reinício da missão. Regras puras cobrem acúmulo escalonado, negligência, geração, consumo, recarga e limite de alerta. Capturas de painéis limpos, sujos e da limpeza foram inspecionadas. Resultado em `Logs/ares-solar.txt`. Arquitetura, balanceamento e limites: [manutenção solar](Assets/ARES/Runtime/World/SOLAR_MAINTENANCE.md). Nenhuma build standalone foi gerada nesta etapa.

## Interação por área, corrida e saída — 15/11/2026

- **E/Espaço/Enter:** `StationInteraction` agora resolve a tarefa dentro da área interna do setor, além do ponto original. Antes, o alcance fixo de 2,4 m deixava o centro de várias salas fora da interação. HUD, teclado e confirmação de selagem usam a mesma regra. Corredores não abrem tarefas; planejamento pendente e conclusão são indicados no HUD; recompensas continuam protegidas por `Campaign`.
- **Corrida:** segurar Shift esquerdo/direito aumenta a velocidade para 1,75×, configurável em `SprintSettings` no explorador. Caminhada, aceleração e colisões existentes permanecem. `SprintDust` emite pequenos tufos cartoon por distância percorrida, com pool limitado a 32 partículas, expansão e desaparecimento. Parar, soltar Shift ou abrir um menu interrompe a emissão; movimento reduzido diminui o efeito. Shader próprio em `Resources/ARES/Effects`, sem imagens/dependências externas.
- **Sair/Exit:** botão no menu principal encaminha a `ApplicationExit`. Salva preferências e fecha o jogador compilado; no editor, apenas sai de Play. Os textos específicos da Unity ficam em catálogos adicionais, preservados nas reexportações.
- **Verificado em Play:** 25 políticas área/Sol, E real por teclado virtual nos cinco centros e cinco pontos originais, Escape/reabertura, caminhada 4,68 m versus corrida 8,17 m, bloqueio de movimento/emissão por menu, saída de Play pelo botão Sair. Shader compilou; capturas do efeito e menu foram inspecionadas. Registro: `Logs/ares-station-sprint.txt`; capturas: `Logs/ares-sprint.png` e `Logs/ares-title-exit.png`. A chamada de saída do executável não foi exercitada numa build standalone.

Módulos novos: `World/StationInteraction`, `World/SprintSettings`, `World/SprintDust`, `UI/ApplicationExit`. Teste integrado em `Editor/StationSprintChecks`, acionado por `Temp/ares-station-sprint-check.request`. Sem alterações na pasta de referência.

## Revisão visual e funcional dos desafios — 15/11/2026

Comparação feita com os componentes e estilos da pasta **(4)**, que permaneceu intacta. A principal divergência era a apresentação: regras já portadas eram expostas por controles genéricos, sem os desenhos e o feedback do original.

| Área | Diferença encontrada | Alteração na Unity |
| --- | --- | --- |
| Janelas de estação | Painel genérico, sem hierarquia de contexto/dados | `ChallengePanel` compartilhado, fundo claro, setor/proveniência, cartões, grade, cabeçalho/rodapé fixos e corpo rolável |
| Inclinar para o Sol | Somente números e slider | Painel vetorial que gira com o slider, suporte fixo e guia tracejada do alvo; confirmação explícita |
| Canos | Seis mostradores genéricos | Dois circuitos de três tubos, alvos tracejados, giro de 90°, cor/texto de conexão e Liberar fluxo |
| Cabos | Botões sem fios ou arraste | Cores/símbolos, curvas das conexões e arraste; alternativa por clique/teclado |
| Bobinas | Sem marcações nem controles direcionais | Marcas do Sol, ponteiro/alvo, botões de giro, slider e indicador animado com movimento reduzido |
| CME | Horas não acompanhavam a velocidade | Diagrama Sol–Marte e cálculo ao mover o controle, inclusive velocidade zero |
| Irrigação/horta | Apresentação textual e botões sem bloqueio visual | Canteiros/plantas/frutos, orçamento, botões desativados nos limites e água por clique/arraste |
| Limpeza | Lista sem eficiência visual | Cartões em duas colunas, poeira, produção/eficiência, conjunto ativo e passes |
| Erupções | Sequência textual | Cinco espaços/cartas, ordem visual e cartas usadas desativadas |
| Outros desafios | Leituras pouco visuais | Barras de oferta/demanda/oxigênio, cartões e ilustrações locais das cinco categorias originais |
| Energia | Janela genérica | Mesmo painel claro das estações, alavancas agrupadas e ações fixas |
| HUD | Lista extensa; câmera apenas por atalhos | Checklist recolhível com progresso, botões de tetos/recentralização/câmera e estrelas separadas |

`TaskAttempt` continua sendo o estado da tentativa e `Campaign` a única autoridade das recompensas. `TaskView` compõe instruções/resultado/rodapé; quatro módulos de tabuleiro organizam os controles por família. Desenhos e gestos não alteram recursos. Veja [a arquitetura da interface](Assets/ARES/Runtime/UI/README.md).

Verificação em `Logs/ares-challenges.txt`: 14 desafios em Play, limites do painel/traduções, sliders/botões, ângulos 0°/45°/90°/alvo e arrastes sintéticos dos cabos/água. Capturas antes/depois em `Logs/ares-challenge-*.png`. A prévia offline (`Tools/AresPreview`) aceita `?view=task&task=TiltSun`, usando os componentes originais; build Vite passou. A suíte de campanha/controles tem resultados separados em `Logs/ares-ui.txt` e `Logs/ares-play.txt`.

**Passaram nesta revisão:** compilação/paridade, 70 combinações puzzle/Sol, fluxo dos cinco Sols até vitória, 25 resultados de tarefas aceitos pela campanha e regressão de movimento/câmera (W 5,22 m; braço 40,1°; corpo 0,177 m; arrasto −0,780 rad; R recentralizou). O editor encerrou Play ao terminar os testes. As cores das estações são exportadas dos tokens originais para USS/C#, incluindo as misturas por setor e as quatro cores dos cabos.

**Não equivale a fidelidade integral do jogo.** Capturas da Unity foram inspecionadas, mas o controle do navegador foi interrompido por não conseguir determinar sua URL: a comparação visual web/Unity lado a lado não foi concluída. Permanecem diferenças anteriores em iluminação/materiais 3D, áudio/voz, telescópio, serviços NASA remotos, minimapa expandido/guia manual, joystick e efeitos de consequências no cenário. PhotoDetective mantém a identificação de ilustração de amostra. Dispositivos touch físicos não foram testados. Não houve commit/push.

## Continuação — interface e gerenciamento, 15/11/2026

A cena agora inicia com **menu nativo**, e a campanha está conectada à interface. A referência continua sendo exclusivamente a pasta **(4)**, sem alterações nela. **A fidelidade total ainda não está concluída**: os limites abaixo são parte do estado de entrega.

- UI Toolkit: menu PT/EN, ajuda, configurações de câmera/movimento reduzido, relatório, HUD com cobertura da demanda de energia, tarefas, relógio, estrelas, minimapa, painel de energia, confirmação de selagem, resumo, derrota e certificado com apelido salvo localmente.
- Fontes originais Fredoka/Nunito, licenças OFL incluídas. Menu com diorama nativo de Marte, estrelas determinísticas e clone animado do Bubble; iluminação/material ainda precisam de comparação com Three.js.
- `GameFrontend` instala a UI ao entrar em Play na cena BaseARES; não é preciso anexar componentes manualmente. `ReferenceExplorer` bloqueia movimento e novas entradas da câmera durante overlays. A suíte de controles desativa o menu explicitamente durante seu próprio teste.
- As **14 tarefas** possuem estado de tentativa e controles específicos. `TaskAttempt` não concede recursos; somente `Campaign.FinishTask` aceita o resultado e aplica a recompensa, uma única vez. Instruções não consomem tempo da tentativa; expiração bloqueia sucesso; repetir cria novo estado. PhotoDetective usa ilustração local identificada, sem NASA remota.
- Planejamento respeita passo, limite e orçamento reduzido por poeira. E/Espaço usa estações a 2,4 m. Entrar na sala do Escudo registra abrigo. O fim do Sol continua exigindo as tarefas e confirmação no Controle. Nenhum atalho genérico de conclusão foi adicionado ao jogador.
- Orientação para a próxima sala, textos da primeira caminhada e falas locais de entrada do Bubble ficam em `MissionGuidance`, separados das regras de recursos.

### Verificação desta etapa

- Compilação no editor, reimportação da definição e suíte anterior de paridade passaram.
- **70 combinações de puzzle/Sol** passaram: solução, instruções sem consumo do relógio, expiração, congelamento de resultado e isolamento de recursos. Isso é teste de integração das regras portadas, não oráculo visual da referência.
- **Fluxo dos cinco Sols em Play passou**, usando eventos nativos dos botões/alavancas e soluções programáticas para os 25 puzzles atribuídos. Não comprova uma caminhada manual completa nem todos os gestos. `Logs/ares-ui.txt` registra a execução e `Logs/ares-*.png` as capturas.
- **Controles em Play passaram** após a integração: W 5,19 m, braço 40,1°, oscilação 0,180 m, arrasto -0,915 rad, R recentralizou. Dispositivos virtuais removidos e Play encerrado.
- Inspeção inicial por computer-use confirmou menu e relatório em Play. Capturas renderizadas do Unity foram inspecionadas para tipografia/layout. Não houve comparação lado a lado com a versão web em execução.

### Organização e continuidade

Leia [arquitetura da interface](Assets/ARES/Runtime/UI/README.md). Regras de campanha em `Runtime/Domain`; regras de tentativa/adaptação de leituras em `Runtime/Tasks`; composição/telas em `Runtime/UI`; estilos, traduções e fontes em `Resources/ARES/UI`. `Tools/AresImport/export-ui.py` exporta os textos originais. Os hooks de teste só consomem caminhos fixos, não executam comandos recebidos.

Próximo trabalho: comparação visual tela por tela, completar desenhos/animações/arrastes dos puzzles, fala/áudio e painel de perguntas do Bubble, joystick e acessibilidade, efeitos de consequências na base, estufa/robôs, telescópio e serviços com fallback. O relatório já inclui SEP, impacto, pressão e vento; conferir normalização/proveniência contra os adaptadores web antes de trocar os snapshots. Não tratar a campanha jogável como migração integral finalizada.

Não houve commit/push. Os blocos abaixo registram etapas anteriores e suas pendências históricas.

## Objetivo do usuário

Recriar **todo** o jogo da pasta atualizada `a896f420-3053-49eb-b6f2-a97f9b2dc6c8 (4)` em Unity, com fidelidade visual e funcional, abandonando a versão anterior somente após substituir suas funções. O usuário reforçou **"deixe idêntico"**, pediu código organizado/modular e este documento para outra IA continuar se a sessão acabar. Não reduzir o escopo a uma demonstração de cenário. Não afirmar que está concluído quando ainda faltam sistemas.

## Estado atual — 2026-11-15, referência (4)

O usuário substituiu a referência antiga e confirmou que **a versão (4) é a fonte de verdade**. A ausência da pasta (1) é uma alteração anterior a esta etapa; não restaurá-la nem usar seus dados para novos portes. O histórico abaixo descreve a etapa anterior.

- Importador retomado usando exclusivamente `a896f420-3053-49eb-b6f2-a97f9b2dc6c8 (4)/src`. Nenhum arquivo da referência foi editado. Não houve commit/push.
- Cena Unity regenerada com **1.782 malhas de cenário + 36 dos personagens (1.818 total)**. Estruturas, console, interiores e vegetação vêm dos geradores atualizados. O terreno usa o componente `Terrain.tsx` original: relevo, geologia, cores por vértice, UVs e texturas procedurais. Painéis solares recebem a textura original. Metais, rugosidade, faces duplas e materiais sem iluminação são preservados na importação quando aplicáveis.
- `@napi-rs/canvas` foi adicionado à ferramenta local (lockfile incluído) para executar as funções originais de textura. Nenhuma dependência Node é usada pelo jogo Unity. O shader nativo `ARES/Reference Terrain` usa as cores e os mapas exportados; isso ainda não prova equivalência da iluminação entre Three.js e URP.
- Dados dos cinco Sols atualizados com a rotação dos **14 minijogos**, traduções e leituras normalizadas com presença explícita dos campos. Os minijogos ainda não têm apresentação jogável em Unity.
- `CampaignRules` carrega os parâmetros de `balance.json`. `Consequences` porta tempestades determinísticas, poeira/limpeza, redução do orçamento, classificação de déficits, dose, bônus de Sol perfeito e medalhas. `Campaign` integra esses resultados exclusivamente na selagem, registra abrigo perdido, mantém pontuação dos Sols anteriores e aplica os três motivos de derrota.
- Testes do astronauta atualizados: a pose de observação só começa após a chegada ao telescópio. Aproximação ao telescópio e lente ainda precisam de integração jogável.
- `manifest.json` identifica a referência (4), hashes SHA-256 do código de origem e dos arquivos exportados. `ReferenceProvenance` recusa importação/validação de dados divergentes ou exportação incompleta. O manifesto é escrito por último pelo exportador.
- `ImportRequest` agora aguarda refresh/compilação antes de processar pedidos e grava resultado com horário em `Logs/ares-request-status.txt`. Em caso de falha, substitui o resultado antigo de paridade por `FAIL`.

### Verificação da etapa atual

- **Passou no editor:** 9.672 pontos de navegação, 384 fórmulas, 120 quadros de câmera, 200 quadros de astronauta, 128 do Bubble e 3.843 vértices dos personagens.
- **Passou no editor:** 640 casos de consequências, 9 limites de limpeza, 216 combinações de medalhas e 436 transições de campanha. O oráculo executa `engine/consequences.ts` e as ações reais de `game/campaign.ts`, isolando apenas Zustand/apresentação/narração. Verifica vitória, derrotas, pausas de alarme durante tarefas e rejeição de duplicatas.
- **Passou no editor:** 1.782 malhas / 395.249 vértices de cenário, winding, UVs, cores, 33 malhas texturizadas, importação sRGB/linear dos mapas, definição atualizada e compilação do shader do terreno.
- **Passou em Play (15/11):** W deslocou 5,13 m, braço oscilou 40,1°, corpo 0,178 m, arrasto esquerdo alterou yaw em -1,005 rad e R recentralizou. Dispositivos virtuais removidos e Play encerrado automaticamente. Resultado em `Logs/ares-play.txt`.
- A inspeção visual atual não foi concluída: `sky.activate_window` expirou também na tentativa de recuperação. Importação e testes continuam pelo hook do editor existente. Não tratar testes numéricos como comparação visual lado a lado.

### Próximo passo concreto

Integrar a apresentação/campanha e a UI nativa a partir dos arquivos da versão (4), mantendo as novas verificações. Há **14** minijogos (`ResourcePriority`, `IrrigationPlan`, `PowerBalance` e `WindReading` são novos); portar suas interações reais sem conclusão genérica. Continuam pendentes menu/HUD/overlays, primeira caminhada/falas, animações de estufa e robô de limpeza, lente do telescópio, áudio, céu, toon/outlines e serviços com fallback. A cena ainda é de inspeção; **a migração completa não está concluída**.

## Histórico da etapa anterior — 2026-11-14

### Continuação — personagens e câmera

- Preferência reafirmada pelo usuário: **rotação manual da câmera como no original**, código limpo/modular e fidelidade total. Não adicionar giro automático atrás do jogador.
- Implementado e reimportado no editor: extração de hierarquia local dos personagens para `Import/characters.json`, prefabs com pivôs originais, animação de caminhada/repouso/aceno do astronauta e animações internas do Bubble. A geometria estática anterior achatava os membros, causando o deslizamento sem animação relatado pelo usuário.
- Controles separados em `CameraInput`: arrastar com botão esquerdo no cenário ou direito, zoom, R para recentralizar; H fica como atalho de inspeção para tetos. C alterna a câmera isométrica, que intencionalmente tem ângulo fixo como na referência.
- Módulos novos: `ReferenceSpace`, `CharacterAnimation`, `AstronautAnimator`, `BubbleAnimator`, `CameraInput` e `Editor/CharacterImporter`. O importador agora recusa regenerar cenas com alterações não salvas.
- **Verificado no Unity**: 3.843 vértices da pose inicial contra a geometria original; 200 quadros do astronauta (repouso, caminhada, aceno, salto, observação e movimento reduzido); 128 quadros do Bubble (humores, fala, aceno, piscar e bolhas); 120 quadros de câmera; todos os checks anteriores de navegação/campanha. Os resultados esperados executam os callbacks TSX originais, não uma segunda cópia das fórmulas C#. Resultado em `Logs/ares-parity.txt`.
- **Verificado em Play**: cena renderizada com os dois personagens; arrasto manual do cenário conferido visualmente. Teste integrado com dispositivos virtuais do Input System: W deslocou 5,20 m, balanço do braço de 40,1°, oscilação do corpo de 0,179 m; arrasto esquerdo mudou yaw em -0,945 rad e R recentralizou. Resultado `PASS` em `Logs/ares-play.txt`. O teste remove os dispositivos virtuais e sai de Play automaticamente.
- Salto/observação/humores são testados como poses, mas ainda não ligados aos eventos da campanha. Poeira dos passos e áudio ainda pendentes. Touch/pinch implementados, mas não testados em dispositivo.
- Recompilar durante Play revelou perda do estado não serializado do explorador. `ReferenceExplorer` agora reinicializa esse estado se necessário. A primeira execução do teste integrado ficou pausada e expirou; a repetição passou. `PlayControlsChecks` desativa a pausa ao iniciar seus testes. Não interpretar esses registros históricos como falha da execução que passou.
- Uma atualização parcial do Unity não descobriu o arquivo novo dos testes e produziu CS0103. Foi resolvida após atualizar/renomear o arquivo para `CharacterAnimationChecks.cs` e recompilar; **não é erro corrente**. Não confundir entradas históricas do log com o resultado atual.

### Situação geral

A migração está **incompleta**. Há uma cena Unity de inspeção, geometria extraída da referência, dados de campanha e parte do domínio em C#. Ainda não há menu/HUD, minijogos ou campanha jogável integrados. A referência original permanece intacta e deve continuar disponível para comparação.

Leia [Assets/ARES/README.md](Assets/ARES/README.md) para a arquitetura e os limites de fidelidade. Código novo em `Assets/ARES`; ferramenta de extração em `Tools/AresImport/export.mjs`.

### Entregue e verificado

- Exportador local Node 26 + Three.js 0.187 + TypeScript 5.9.3 (lockfile incluído). Executa geradores visuais originais, sem executar servidor, ler `.env` ou carregar credenciais.
- 1.464 meshes extraídas: 1.428 de cenário e 36 em hierarquias articuladas de astronauta/Bubble. Inclui estrutura, interiores, corredores, estufa, parque solar, rochas/montanhas e chão.
- Sete módulos e cinco Sols, rotação das tarefas, traduções PT/EN e snapshots/proveniência importados.
- `GameDefinition` como ScriptableObject. Pisos, paredes, tetos, interiores e personagens separados no scene graph.
- `Campaign` e `Rules`: fases, orçamento, atribuições, validação de tarefa, recompensas sem duplicação, pausa e selagem no Controle. **Ainda não conectados a uma interface jogável.**
- Navegação traduzida com portas/corredores e reflexão Z.
- Cena `Assets/ARES/Scenes/BaseARES.unity` gerada e aberta no editor. Configurada em EditorBuildSettings.
- Testes executados pelo menu Unity: **9.672 pontos de navegação + 384 casos de fórmulas + 120 quadros de câmera + 328 quadros dos personagens + 3.843 vértices**, ciclo dos cinco Sols e bloqueios contra conclusão/recompensa duplicada. Resultado em `Logs/ares-parity.txt` (pasta ignorada pelo Git).
- Importação repetida com sucesso após separar assemblies e corrigir o ScriptableObject. Agora o log conta `1428 exact reference meshes` do cenário; os 36 meshes dos personagens são importados por `CharacterImporter`. Última validação: `ARES parity checks passed.`

### Verificação de Play

O primeiro teste em Play encontrou `NullReferenceException` em `ReferenceExplorer.Start` porque `GameDefinition` estava inicialmente num arquivo com outro nome e foi movido de assembly. Corrigido criando `Runtime/Data/GameDefinition.cs`, ajustando o `m_Script` do asset e **reimportando a cena inteira**. **Play foi repetido com sucesso após a correção**, com renderização da base e sem novas exceções no log; saiu de Play para continuar o desenvolvimento. Não confundir os erros históricos de `Logs/Editor.log` com erros novos. A captura mostrou cerca de 218 mil triângulos e 2.897 draw calls: otimização ainda necessária.

## Como continuar, em ordem

1. Manter os testes de paridade e de controles passando ao integrar a campanha. WASD move, arrastar o cenário gira, roda dá zoom, R recentraliza, H alterna tetos, C alterna isométrica e TAB mostra a visão geral. Não chamar isso de jogabilidade final.
2. Integrar as poses já portadas aos eventos de apresentação/campanha; portar poeira/áudio dos passos, primeira caminhada e falas. Astronauta/Bubble já têm prefabs articulados; não voltar a achatar esses modelos no exportador. Completar testes dos gestos touch e cutaway.
3. Portar menu, HUD e overlays com UI Toolkit/uGUI nativos, reutilizando textos, tokens, medidas e interações originais. Inspecionar referência em execução para comparação, não inventar novo design.
4. Portar cada um dos 14 minijogos de `src/tasks/`, com regras e interações fiéis, separados em estado puro e apresentação. Integrar IDs de `src/tasks/assignments.ts` e fluxo da campanha. Não usar botão genérico de "concluir".
5. Integrar relatórios, energia, primeira caminhada, Bubble/falas, alarmes/abrigo, cinco Sols e telas finais.
6. Portar céu/ciclo solar, toon/outlines/transparência, animações/colheita da estufa, telescópio, áudio e dados remotos com fallback de snapshot. O terreno da versão (4) já foi atualizado nesta etapa.
7. Comparar visual e funcionalmente a versão web e Unity em cada tela/estado. Somente então remover/substituir o que for obsoleto.

## Fontes de verdade

Na pasta de referência, `src/config/rooms.ts` define layout, estações e portas; `theme.ts`, `palette.ts` e `styles.css` definem aparência; `balance.ts` contém regras de jogo. `src/game/campaign.ts` controla fases e recompensas. `src/engine` contém fórmulas. `src/game/presentation.ts` contém tour/falas. `src/tasks/rules.ts` e cada componente de tarefa contêm puzzles. `src/game/scene` e `src/game/characters` contêm os geradores visuais. Há testes originais em `src/test`.

Ler `AGENTS.md` dentro da referência antes de modificá-la. Não editar migrations ou arquivos gerados da versão web. A pasta contém `.env`: não ler nem exibir segredos; `.gitignore` agora ignora `**/.env`.

## Ferramentas e comandos

```powershell
npm.cmd ci --prefix Tools/AresImport
node Tools/AresImport/export.mjs
```

Unity instalado em `D:/Unity/6000.6.9f1/Editor/Unity.exe`. Projeto em `C:/Users/dudu-/Documents/GitHub/Andr-meda_Project1`. Editor aberto pelo usuário. Não iniciar outro Unity com o mesmo projectPath. Tentativa em lote no mesmo projeto falhou por lock; tentativa isolada em `Temp/AresValidation` não conseguiu inicializar licença e foi interrompida. Usar o editor existente.

Menu **ARES > Import reference geometry and create scene** regenera a cena (não colocar trabalho manual nela que precise sobreviver à regeneração). Menu **ARES > Validate port against reference** roda os checks.

Menu **ARES > Validate controls in Play** testa movimento/animação/câmera através de dispositivos virtuais isolados do Input System. Requer a cena BaseARES aberta, inicia e encerra Play, e escreve `Logs/ares-play.txt`. Também pode ser iniciado por `Temp/ares-play-check.request`, consumido apenas fora de Play e da compilação. Não recompilar nem usar os controles durante os poucos segundos do teste. Os testes não editam a cena nem enviam entrada ao teclado/mouse do sistema operacional.

Alternativa de automação local: escrever `Temp/ares-import.request`. O hook `Editor/ImportRequest.cs` consome o arquivo quando o editor está disponível e fora de Play, importa a cena e roda os checks. Isto substitui a cena atual e agora é recusado se alguma cena aberta tiver alterações não salvas. Para apenas validar, usar `Temp/ares-validate.request`; os testes dos personagens usam uma cena de preview isolada. O hook só aceita esses caminhos fixos e não executa texto recebido.

O usuário autorizou controle do computador. Skill: `C:/Users/dudu-/.codex/plugins/cache/openai-bundled/computer-use/26.1111.21308/skills/computer-use/SKILL.md`. Usar `node_repl` com `@oai/sky`, selecionar a janela retornada e respeitar observação/ação. O usuário também usa a máquina; `user input detected` exige reobservar. Não controlar terminal/Codex por UI. Após reload o Unity pode ficar alguns segundos indisponível. Não capturar continuamente janelas por polling.

## Cuidados técnicos descobertos

- Node 26 `stripTypeScriptTypes` aceita apenas `mode:'strip'`; não usar `transform`.
- TypeScript 7 instalado inicialmente não fornece `transpileModule` na mesma API. Ferramenta fixada na série 5.9 (lockfile).
- `npm.ps1` bloqueado pela política PowerShell; usar `npm.cmd`. Instalação exigiu aprovação por cache fora do workspace.
- Three é destro; Unity recebe `(x,y,-z)` e winding invertido uma vez. Não inverter novamente.
- `ScriptableObject` precisa do arquivo `GameDefinition.cs`; não reunir classes Unity serializadas em arquivo arbitrário.
- `StartAssetEditing/StopAssetEditing` agora envolve a geração de meshes/materiais para evitar refresh por mesh. A primeira importação sem batching levou minutos.
- Materiais ainda são majoritariamente URP/Lit, não o toon original; iluminação provisória. O terreno da versão (4) já tem relevo, cores por vértice e textura procedural. Não afirmar equivalência visual total.
- Instâncias viraram meshes individuais. Precisam otimização sem alterar formas/proporções.
- O importador ainda usa SceneMode.Single e configura build settings. Tornar a regeneração mais segura antes de trabalho autoral nas cenas.
- Git tinha somente a referência não rastreada antes deste trabalho. O editor alterou também QualitySettings/URPProjectSettings e gerou `stove.blend.meta`; revisar diferenças sem apagar trabalho do usuário. Não houve commit/push.

## Regra de continuidade

Atualizar este README e `Assets/ARES/README.md` a cada etapa relevante. Registrar o que foi **testado**, o que foi apenas **implementado**, falhas correntes e próximo passo concreto. Continuar o objetivo completo; não encerrar dizendo que a cena de inspeção equivale ao jogo.
