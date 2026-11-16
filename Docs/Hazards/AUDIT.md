# P0 — Auditoria Unity / eventos ambientais

Auditoria iniciada antes de modificar o runtime. Referência: `Anexo/prompt_eventos_climaticos_unity.md`. As alterações locais de HUD, câmera, dormitório, irrigação e NASA/Groq já existentes serão preservadas.

## Plataforma e apresentação

| Área | Evidência encontrada | Integração / lacuna |
| --- | --- | --- |
| Unity | `ProjectSettings/ProjectVersion.txt`: 6000.6.9f1 | Usar compilador/API da instalação existente |
| Renderização | URP 17.6; QualitySettings seleciona nível 1 com asset URP; GraphicsSettings global está vazio, mas o override por qualidade existe | Não trocar pipeline. Confirmar pipeline efetivo nas capturas. Há `DefaultVolumeProfile.asset`; não há efeitos de condição ligados ao jogador |
| Interface | UI Toolkit: `GameFrontend`, UIDocument, PanelSettings, USS, `ResourceGauge`, `MissionEventBanner` | Reutilizar estilos e disposição; acrescentar banner/estoque e medidor, sem novo framework |
| Entrada | Input System, activeInputHandler=1 | `ReferenceExplorer`, `GameplayCursor` e `CameraInput`; não alterar câmera, colisões ou atalhos |
| Personagem | `PlayerMotion` usa navegação própria; `AstronautAnimator` aplica poses procedurais a membros; não depende de AnimatorController/Mecanim | Há caminhar, observar, gestos; não há clipe dedicado de cambalear/ajoelhar. Interpolar poses em três etapas, sem ragdoll |
| Repouso | `Dormitory` + `BedSequence` em `World/Dormitory.cs`: deitar, dormir, despertar, fade inicial | Hoje é somente visual; precisa de tratamento com custo, separado do despertar introdutório |
| Poeira | Shuriken em `MartianWind`, pool 960; shaders WindDust/ReferenceSky, `ReferenceEnvironment`, `DustStormWeather` | Preservar Medium antes de refatorar. Nenhum VFX Graph necessário |
| Pós-processamento | URP / Volume disponível, mas sem controle de condição | Volume leve somente no jogador, com reduced motion; não aplicar a câmeras de mapa |
| Áudio | AudioListener criado pelo importer; nenhum AudioMixer/HazardAudioProfile ou áudio de eventos encontrado em ARES | Criar mixer e sons procedurais locais; sem download de áudio |
| Localização | `UI/Localization.cs`, catálogos Resources/ARES/UI/en, pt-BR, unity e child; PresentationLocalizer | Adicionar uma única tabela bilíngue de termos de risco e explicações; reutilizar no narrador |
| Assemblies/testes | Ares.Runtime / Ares.Editor; checks do Editor por arquivo de solicitação e menus; testes NASA puros + harness .NET | Acrescentar testes NUnit EditMode/PlayMode sem mover todo o projeto; manter checks existentes |
| Alvo | `SceneryBuildRequest`: StandaloneWindows64; código NASA usa filesystem e Task.Run | Desktop Windows é o alvo verificado. Não declarar suporte WebGL; WebGL precisaria de adaptador sem threads e StreamingAssets remoto |

## Regras e fontes reais

- **A ligação está faltando:** `MissionIntegration.CurrentSol` contém dados NASA; `Campaign.Current` ainda usa `GameDefinition.campaign` com cinco snapshots/demandas/tarefas estáticos. `ObserveSol` carrega NASA para observatório/narrador, mas não alimenta as regras.
- `ConflictingDemands` sorteia poeira/flare por `System.Random`, após duas tarefas, e permite proteção somente nos dias 2/5. Não consulta NASA. Isso viola o novo contrato de ativação e será desligado na campanha com dados reais.
- `Campaign.ApplyDemand` aplica Dose=100 quando a blindagem não está regulada. `Die` termina imediatamente. Não há aviso mínimo, incapacidade, resgate, estoque médico ou recuperação.
- `Campaign.CompleteSol` concentra penalidades de alocação. `Consequences` já contém fórmulas, margem, dose e resultados oficiais; deve continuar sendo a autoridade na selagem. Não introduzir uma segunda reserva de oxigênio/integridade/dose.
- `SolarPlant` já tem 32 painéis, limpeza física e bateria **160 kWh**, geração e consumo contínuos. `OxygenPlant` já faz contabilidade de massa/energia/água/CO₂. Não substituir esses sistemas por modelos paralelos. O grid deve reutilizar a bateria e o oxigênio existentes.
- O modelo antigo de quatro bancos está em `Campaign.PanelDust`; os 32 painéis físicos devem continuar agregados em quatro bancos. O pool ainda deriva do depósito anterior.
- `MissionTime.RealSecondsPerSol` é **600 s**, apesar do balance importado conter 480 s. Preservar 600 s, já usado por oxigênio e missões; registrar como decisão provisória.
- O relógio atual sela automaticamente ao expirar. O novo requisito exige selagem confirmada: ao chegar ao fim, parar o relógio e pedir confirmação, sem conceder tarefas/recompensas.
- `Rules.Shield`, `Rules.Thermal`, `Consequences.Classify` e sliders existentes são os pontos a preservar. Algumas leituras didáticas (`TaskReadings`) usam valores de treino quando falta dado; nunca usar esses valores para ativar um risco.
- Comunicação, relatórios e mensagens da Terra estão em `CommunicationsSystem`; blecaute temporário deve bloquear ações sem inventar danos permanentes na antena.

## NASA e limites científicos

Reutilizar `Infrastructure/Nasa/NasaSolService`, HttpJsonClient, UnityHttpTransport, MissionPlanner/MarsCalendar, cache e DTO SolResponse. Não criar outro cliente HTTP nem outro provedor de credenciais.

- `facts.wind` é sempre null. A resposta Curiosity já verificada na migração fornece min/max, pressão, opacity e data; não fornece vento por Sol. PDS possui produtos históricos REMS, o que **não** equivale a uma API operacional diária intercambiável. A [documentação InSight](https://api.nasa.gov/assets/insight/InSight%20Weather%20API%20Documentation.pdf) descreve HWS/WD históricos; a disponibilidade atual do endpoint será verificada e registrada antes de decidir qualquer fonte. Não misturar sítios/fontes meteorológicas.
- `effects.dust_storm_risk` combina Ls calculado da data real com sorteio determinístico. **É risco derivado**, não observação de tempestade. A direção/velocidade visual deve ser identificada como simulada. Opacity null não ativa nada.
- Não existem SEP/notifications no cliente. Usar a alternativa explicitamente autorizada: radiação **Derived** de X ou CME direto, sem chamadas SEP novas nesta implementação.
- `SolDataAssembler` fornece shield_load_factor **none=.2, glancing=.6, direct=1**. A frase invertida na seção 3 do pedido conflita com o código e a física pretendida: preservar o código, nunca inverter a intensidade e nunca mudar a fórmula de demanda.
- A NASA pode retornar 404/timeout/coleções vazias. Catálogo é apenas índice de candidatos; validar a resposta real antes de ativar qualquer evento.
- APOD e pressão permanecem informativos. Não inferir tempestades de uma foto, de um índice ou de um valor substituto de exercício.

## Contradições resolvidas para implementação

1. Seção 4A exige datas curadas não consecutivas, enquanto seção 8 diz não reordenar datas. Implementar Curated com aviso explícito de cinco dias reais de arquivo e modo Consecutive preservado; pedir confirmação no relatório final.
2. Seção 4 pede jitter ±1; 4A fixa classes/limiares. Jitter só altera apresentação/duração dentro da classe; nunca cria um evento ou promove C a flare danoso.
3. Bateria de 30/10 é sugestão provisória, mas já existe bateria 160/160 ligada ao suporte de vida. A integração deverá usar **uma só bateria**, com configuração explícita e teste de solvabilidade antes de escolher capacidade final.
4. Worst-case exige suprimento maior que o orçamento. Não enfraquecer a fórmula de blindagem nem inventar energia: testar prioridade, reserva, abrigo, tratamento e recursos limitados. Catálogo recusa sequências inviáveis; testes sintéticos também exercitam os extremos.
5. O aviso de 15 s precisa ser efetivamente apresentado (e não apenas tempo interno acumulado por um salto de relógio). Clock de resgate/tratamento não deve contornar essa garantia.

## Baseline visual Medium (antes de P4)

`MartianWind`: raio 25→32, altura 7→13, velocidade de fundo 3.2 m/s; emissão 22→260/s, máximo 18/frame e pool 960; grãos .07–.18, nuvens 5–11; alpha de tempestade .82/.48; paleta (.74,.43,.26)→(.94,.69,.42). `ReferenceEnvironment`: fog 65/230→9/48, interior ×.45; suavização .7/s; sol ×.42; cor #dfb57d; fog #ae8157; fill .32→.19. `DustStormWeather`: frente 20 s, impacto 30 s; velocidade visual 19–34 m/s. Esses valores serão o Medium, sem alegação de vento medido.

## Ordem de trabalho e controle

P0 auditoria → P1 dados/plano/catálogo/diretor → P2 grid/ledgers/condição → P3 interface/recuperação → P4 poeira e demais VFX → P5 áudio → P6 tarefas → P7 integração do narrador, testes, capturas e desempenho. Ao fim de cada fase: compilação e verificações relevantes, sem deixar uma etapa seguinte necessária para iniciar/jogar a campanha.

Estado da auditoria: inspeção estática concluída; verificações de rede, capturas baseline e resultados por fase serão acrescentados conforme executados. Nenhuma credencial será escrita neste documento.

## Resultados complementares executados

- A sonda real de InSight respondeu: sete Sols, com HWS e WD presentes (`wind-probe.json`). São observações históricas de outro sítio; não foram misturadas com a meteorologia Curiosity. Continua ausente um campo de vento na resposta Curiosity usada pelo jogo.
- Catálogo em 16/11/2026: 44 blocos de 120 dias, 87 datas na shortlist, 86 observações verificadas. NASA FLR retornou 404 em consultas desta execução; CME/WSA-Enlil, biblioteca de imagens e parte das consultas REMS responderam. Algumas datas REMS responderam 500; APOD teve timeouts. Nenhuma dessas falhas foi substituída por observações inventadas.
- Foram geradas 12 sequências offline a partir das observações públicas verificadas. A execução com rede também confirmou cinco dias selecionados, com procedência `catalog-verified`; a simulação correspondente terminou em Victory, sem evacuações. Detalhes em `live-scenario-results.json`.
- Compilação do Runtime e Editor concluída. O harness de regressão NASA manteve 228 verificações; o de hazards passou 170 verificações, incluindo 32 combinações de campos nulos, localização/narrador, ledger e 200 missões de cinco Sols.
- Play Mode: `playmode-results.txt` registra capturas por tipo/nível, limites de partículas, aviso visível, colapso, recuperação, deslocamento para a cama, idiomas, mixer, snapshots, confirmação do Sol e relatório de comunicação.
- A aceitação completa de arte, perfis/prefabs por evento, Volume URP, benchmark GPU e partidas humanas online/offline continua pendente. Consultar `REPORT.md`; não confundir a simulação de domínio com uma partida humana completa.
