> Cenário atual (15/11/2026): a referência ativa é **casa-marte-missao-n8n**, com base radial, árvore central, marcos externos e 40 recipientes na estufa. Consulte [SCENERY.md](SCENERY.md) para arquitetura, comandos de atualização e evidências. Referências à pasta `(4)` abaixo são histórico da migração anterior.

# Base ARES — port nativo para Unity

## Tutorial guiado pelo Ares

Ao iniciar uma missão, o jogador nasce na base e escolhe **Fazer tutorial** ou **Jogar sem tutorial**. A primeira tarefa é dividir e confirmar toda a energia no computador. A segunda é **Regar plantas**: pegar o regador, encher no reservatório e cuidar das quatro fileiras, reabastecendo entre elas.

Durante o tutorial, o HUD e os marcadores de outras tarefas ficam ocultos; a trilha acompanha somente o alvo atual. As explicações bloqueiam o controle, aproximam o pet original em um retrato 3D flutuante e mostram o balão gerado com ImageGen. O botão da fala devolve o controle. O relógio da missão permanece pausado durante todo o aprendizado. É possível sair durante as etapas de ação; a campanha continua com o progresso real já realizado.

Os módulos ficam em `Runtime/Domain/TutorialFlow.cs` e `Runtime/UI/Tutorial/`:

- `TutorialFlow`: transições de estados; observa ações reais, sem distribuir recursos ou concluir tarefas artificialmente.
- `TutorialDirector`: integra computador, regador, proximidade, rota e registro da tarefa pelas regras existentes.
- `TutorialView`: balão, falas, objetivo atual, botões e foco. Usa as fontes Fredoka/Nunito e o alfabeto cartoon existente.
- `AresMentorPortrait`: aproximação, flutuação, câmera e textura próprias; restaura a visibilidade do pet e libera os recursos ao sair.
- `Resources/ARES/UI/Tutorial.uss`: layout amplo/compacto. O asset está em `Resources/ARES/UI/Tutorial/ares-speech-v1.png`, com alpha preservado.

O identificador de campanha `SmartFarmCare` permanece estável; a lista de tarefas apresenta **Regar plantas**. A conclusão exige as quatro fileiras realmente cuidadas e usa `Campaign.FinishGreenhouseTask`. O tutorial não altera doses, estoque ou regras do cultivo. Novas etapas devem entrar no fluxo e no diretor; não adicionar regras de campanha à interface ou ao retrato.

Conferência focada: **ARES > Checks > Ares guided tutorial**, ou escrever `tutorial` em `Temp/ares-computer-check.request`. Relatório: `Logs/ares-tutorial.txt`; capturas: `Logs/ares-tutorial-*.png`. O editor usa o relógio controlado de rega após as interações reais, para que perder foco não interrompa a conferência. As verificações antigas usam `StartMission(false)` explicitamente; o botão Jogar continua oferecendo o tutorial.

Proveniência e prompt completo da imagem: `Tools/ArtEvidence/tutorial-speech-manifest.json`, na raiz do repositório. As falas são texto nativo, fora da imagem, e podem ser editadas em `TutorialView`.

**Estado: migração em andamento. Menu e campanha nativa integrados; fidelidade completa ainda pendente.**

## Mestre da Estufa — 15/11/2026

O **observatório APOD** também está integrado: telescópio clicável/E, cinco descobertas liberadas por Sol e diário no HUD. As imagens reais têm resumos em PT-BR, datas, fontes e créditos. `ApodService` valida a API e preserva cópias locais quando ela falha; `DiscoveryJournal` mantém o progresso. Leia [OBSERVATORY.md](Runtime/World/OBSERVATORY.md) para configuração, arquitetura, coleção histórica, proveniência e testes.

**Atualização: a rega agora é manual no cenário.** Comece pelo [guia de manutenção na raiz](../../README.md) e por [MANUAL_WATERING.md](Runtime/World/MANUAL_WATERING.md). O painel prepara a dose; o jogador pega o regador com E, abastece e segura o clique em cada uma das quatro áreas. `Domain/GreenhouseWatering` conserva a água e só conclui o ciclo após as quatro entregas. O HUD mostra água, saúde, clima, solo, luz e progresso. Fechar o painel não reinicia o estado.

`World/GreenhouseCare` adapta a entrada; `GreenhouseWorldView` posiciona a alça na mão e inclina o regador; `GreenhouseOcclusion` trata a visibilidade. A arte do regador é um sprite 2.5D, com [proveniência e prompts](Resources/ARES/UI/Greenhouse/WATERING_CAN_ART.md). O [alfabeto cartoon de 52 caracteres](Resources/ARES/UI/CartoonAlphabet/README.md) é reutilizável com `UI/CartoonText`; os corpos de texto continuam usando as fontes existentes. Reexecutar a interação com `Temp/ares-watering-check.request`; resultado em `Logs/ares-watering.txt`.

`Domain/GreenhouseSimulation` substitui a atividade temporizada da estufa no fluxo do jogador por ciclos com 11 L, quatro prateleiras, irrigação e ajustes contextuais. `UI/GreenhouseNarrator` mantém textos educativos em PT-BR; `UI/GreenhouseView` usa controles nativos e arte gerada pela skill `imagegen`. Modelo, formato das mensagens, limites, resets por Sol e proveniência estão em [Runtime/Domain/GREENHOUSE.md](Runtime/Domain/GREENHOUSE.md).

Passaram os testes em `Logs/ares-greenhouse.txt`: regras, água exata, clima, ajustes, risco, insuficiência, retomada, interface e conclusão sem recompensas duplicadas. Telas de início, clima e fim inspecionadas. Reexecutar com `Temp/ares-greenhouse-check.request`. Assets 3D originais permanecem; a nova ilustração de quatro prateleiras é um asset 2D da interface.

## Manutenção solar — 15/11/2026

`Campaign.Solar` mantém a reserva e a poeira dos 32 painéis ao longo da missão. `SolarCleaningSession` valida ferramenta e clique contínuo; `SolarMaintenance`, `SolarPanelView` e `SolarBroomView` cuidam do mundo; `SolarMaintenanceHud` apresenta geração, reserva, alerta e progresso. Balanceamento no Inspector de `ReferenceExplorer.solarSettings`. Regras, unidades, defaults e relação com o orçamento legado estão em [Runtime/World/SOLAR_MAINTENANCE.md](Runtime/World/SOLAR_MAINTENANCE.md).

Testes de domínio e interação real por dispositivos virtuais passaram no editor (`Logs/ares-solar.txt`); capturas de antes/depois e limpeza foram inspecionadas. Reexecutar por `Temp/ares-solar-check.request`. A reserva começa em 160 kWh, alerta abaixo de 100 kWh, e cada painel limpo gera 0,4 kWh a cada 30 segundos de jogo ativo. Manutenção física não concede estrelas nem limpa automaticamente os painéis ao concluir o puzzle legado.

## Interface e gerenciamento — continuação de 15/11/2026

Menu, relatório, HUD, energia, 14 puzzles com tentativas próprias, selagem, resumo e final estão conectados a `Campaign`. A UI se instala automaticamente em Play na BaseARES. Traduções e fontes vêm da referência (4); não houve alteração na referência. Leia [Runtime/UI/README.md](Runtime/UI/README.md) para responsabilidades, comandos de teste e limites.

Passaram no editor: suíte anterior de paridade e 70 combinações de puzzle/Sol. Passou em Play: fluxo de cinco Sols por eventos de botões/alavancas e soluções de teste dos 25 puzzles atribuídos. Passou também a regressão de movimento/câmera. Resultados em `Logs/ares-tasks.txt`, `Logs/ares-ui.txt` e `Logs/ares-play.txt`. A UI foi inspecionada em Play e em capturas, mas ainda não comparada lado a lado com a web. Permanecem diferenças de desenho, animação, áudio, gestos e serviços; não afirmar fidelidade total.

As seções seguintes são o histórico anterior à integração da interface.

## Continuação — 15/11/2026

Referência exclusiva: `a896f420-3053-49eb-b6f2-a97f9b2dc6c8 (4)`. O exportador e a cena já foram atualizados; a pasta anterior não deve ser restaurada. Leia o estado atual no README da raiz antes do histórico abaixo.

- 1.782 malhas de cenário e 36 dos personagens. Relevo/geologia do novo `Terrain.tsx`, UVs, cores por vértice, texturas originais do solo/painéis e propriedades físicas dos materiais são extraídos do código original.
- `Shaders/ReferenceTerrain.shader`: iluminação URP do terreno com textura, cor de vértice e mapa de relevo. Iluminação/céu/toon/outlines globais ainda não equivalem ao original.
- `Runtime/Data/CampaignRules.cs`: contratos do `balance.json` e presença explícita das leituras do Sol. `GameDefinition.rules` é atualizado pelo importador.
- `Runtime/Domain/Consequences.cs`: avaliação pura de tempestades simuladas, poeira e limpeza, orçamento reduzido, déficits, dose, bônus e medalhas. `Campaign(data, rules)` aplica os efeitos na selagem, conserva estrelas históricas e registra o motivo de derrota.
- `Tools/AresImport/consequence-fixtures.mjs`: executa os módulos TS reais e a campanha original com adaptadores locais, sem rede, servidor ou credenciais. Não é uma reimplementação JS das fórmulas C#.
- `Editor/ConsequenceChecks.cs`: 640 casos, 9 limites de limpeza, 216 medalhas e 436 transições da campanha original passaram no editor.
- `Editor/WorldImportChecks.cs`: 395.249 vértices de cenário, winding/UV/cores, 33 malhas texturizadas, configuração dos mapas e shader validados no editor.
- `Editor/ReferenceProvenance.cs`: exige a referência (4) e compara os hashes do código e dos arquivos importados. Reexportar ao atualizar a referência. `ImportRequest` espera compilação/refresh e registra PASS/FAIL com horário em `Logs/ares-request-status.txt`.
- Mantidos os checks de navegação, personagens e câmera manual. A fixture de observação agora exige `observeArrived`, como o original atualizado.
- Play validado em 15/11: movimento de 5,13 m, braço 40,1°, corpo 0,178 m, arrasto -1,005 rad e recentralização com R. O teste removeu os dispositivos virtuais e saiu de Play. `Logs/ares-play.txt` registra PASS.

A rotação importada contém 14 minijogos; os quatro novos são ResourcePriority, IrrigationPlan, PowerBalance e WindReading. Isso registra suas atribuições, **não implementa seus puzzles nem a interface**. O domínio da campanha continua separado da cena de inspeção. Próximo passo: apresentação/UI e integração jogável, sem conceder recompensa na UI. Nenhuma equivalência visual total foi declarada; o controle visual do editor expirou nesta etapa.

## Organização

Personagens agora são exportados em `Import/characters.json` com hierarquia e geometria local. `Editor/CharacterImporter` gera os prefabs em `Generated/Characters`; `CharacterAnimation` contém as fórmulas do astronauta, `AstronautAnimator` aplica poses e `BubbleAnimator` anima o peixe e o acompanhamento. `ReferenceSpace` concentra reflexão Z e conversão de Euler XYZ. `CameraInput` cuida de arrastar/zoom, separado do estado matemático em `OrbitCamera`.

Preferência explícita do usuário: câmera manual idêntica à referência; não implementar follow automático de direção. R centraliza, H alterna tetos no explorador, C alterna isométrica e TAB abre a visão geral de inspeção. O modo isométrico conserva o ângulo fixo do original.

- `Import/`: dados intermediários extraídos da referência; não editar manualmente.
- `Generated/`: meshes, materiais e definição do jogo gerados pelo importador Unity.
- `Runtime/Data/`: contratos serializáveis e `GameDefinition` (ScriptableObject).
- `Runtime/Domain/`: estado da campanha, orçamento, pontuação e encerramento de Sol.
- `Runtime/World/`: navegação e controlador temporário de inspeção.
- `Editor/`: importação e comparação automatizada contra a referência.
- `Scenes/BaseARES.unity`: cena independente, sem dependência do navegador.

## Reproduzir a importação

1. Na raiz, executar `npm.cmd ci --prefix Tools/AresImport` e `node Tools/AresImport/export.mjs` com Node 26.
2. No Unity 6000.6.9f1, executar **ARES > Import reference geometry and create scene**.
3. Executar **ARES > Validate port against reference**. O resultado fica em `Logs/ares-parity.txt`.

O exportador executa somente módulos locais de geometria e regras. Não carrega `.env`, servidor, Supabase ou credenciais. O jogo Unity não requer Node nem Three.js; essas dependências são ferramentas de migração.

As coordenadas Three.js são convertidas por `(x,y,z) -> (x,y,-z)`, com inversão única do winding dos triângulos. Normais são convertidas com a mesma reflexão. Os pisos, paredes, telhados e interiores permanecem em grupos separados. Os índices e posições das malhas são derivados diretamente dos geradores originais. Nos personagens, os pivôs e pais originais são preservados: quaternions recebem `(-x,-y,z,w)` e rotações Euler usam a ordem XYZ do Three, em vez da ordem padrão de `Quaternion.Euler`.

## Validação anterior — 14/11/2026

`CharacterAnimationChecks` executa em cena de preview isolada, sem alterar a cena aberta. `Import/character-parity.json` contém dados calculados pelos callbacks originais de `Astronaut.tsx` e `Bubble.tsx`. Passaram no editor: 3.843 vértices em pose inicial, 200 quadros do astronauta e 128 do Bubble. Também passaram 120 quadros de câmera, 9.672 pontos de navegação, 384 casos de regras e os checks de campanha.

`PlayControlsChecks` passou em Play com teclado/mouse virtuais do Input System: W deslocou 5,20 m, braços oscilaram 40,1°, corpo oscilou 0,179 m; arrasto esquerdo alterou yaw e R centralizou. Menu **ARES > Validate controls in Play**, ou arquivo fixo `Temp/ares-play-check.request`, executa o teste e grava `Logs/ares-play.txt`; encerra Play e remove os dispositivos ao terminar. Arrasto no cenário também conferido visualmente. Gestos touch ainda não verificados em dispositivo. O explorador reinicializa seu estado transitório quando scripts são recarregados durante Play.

Salto/observação, fala e humores foram portados e testados como poses, mas a cena de inspeção só integra repouso/caminhada/aceno inicial e Bubble em humor feliz. Ligar os demais parâmetros aos eventos originais quando portar a apresentação/campanha; não inventar recompensas na animação. Poeira dos passos, áudio e equivalência de shaders ainda pendentes.

## Regras de extensão

Não conceder recompensas na UI. `Campaign` valida a tarefa aberta, impede duplicatas e exige confirmação do orçamento no Controle para selar o Sol. A camada de apresentação deve chamar `Tick` com o estado de pausa dos overlays. Minijogos precisam calcular sucesso a partir das próprias entradas; nunca criar botão de conclusão automática. Os dados de exemplo mantêm data, proveniência e JSON original; não rotular os snapshots como dados NASA ao vivo.

Novos minijogos devem ter modelos de estado e validação independentes de MonoBehaviour, uma apresentação separada e registro por ID. Os IDs originais e a rotação de tarefas dos cinco Sols constam de `GameDefinition.campaign`. Serviços remotos devem ficar atrás de interfaces com fallback explícito para snapshots, mantendo chaves no servidor.

## Ainda necessário para equivalência

- Menu, HUD, textos localizados, configurações, acessibilidade, minimapa e interface de relatórios.
- 14 minijogos táteis completos, apresentação das tarefas legadas e integração com a campanha.
- Completar integração da apresentação, roteiro de primeira caminhada, falas, poeira/áudio dos passos e eventos das animações já portadas; validar gestos touch.
- Shader toon, outlines, céu, ciclo solar, transparência/cutaway e efeitos. Terreno/texturas da versão (4) já importados.
- Animação e colheita da estufa, observação do telescópio, telas NASA e áudio.
- Proxies de dados/narrador, verificações de interface e comparação visual lado a lado.
- Otimização de instâncias e agrupamento de materiais: atualmente as instâncias extraídas viram meshes individuais para inspeção.

A referência web e os assets anteriores foram preservados até que a equivalência funcional e visual seja validada. Não considerar o port concluído nem apagar a única referência antes disso.
