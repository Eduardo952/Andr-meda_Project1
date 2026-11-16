# Relatório técnico — seção 11

## Correção adicional do telescópio solicitada durante a validação

Foi confirmado que os 60 Sols dos snapshots reutilizavam a seleção de imagens da missão de geração do catálogo: 18 tinham a mesma APOD, e os demais não tinham APOD. Isso podia repetir imagens apesar de o sorteador de missão estar correto.

`NasaImagery.cs` agora consulta APOD e Image Library pelo ID/Sol da missão atual, independentemente do catálogo climático. O mesmo Sol mantém sua imagem; uma nova missão tem uma nova seleção. Se APOD falhar e a imagem da biblioteca carregar, essa imagem passa a ser exibida na lente com seus próprios metadados/créditos, antes do fallback local.

Três checks adicionais confirmaram estabilidade do mesmo Sol, mudança entre missão/Sol e ausência de consultas climáticas nesse caminho. Na consulta real, duas missões receberam os IDs NASA **PIA16161** e **PIA19090**; APOD deu timeout nas duas. [Evidência](telescope-live-images.json). O Runtime final compilou e a regressão passou; a troca visual final da lente ainda não recebeu uma nova captura em Play Mode após esse ajuste, feito nos minutos finais do limite solicitado.

## Resultado e limite de entrega

Foi implementado o caminho integrado NASA → cenário de cinco Sols → eventos → energia/exposição → aviso → recuperação, com fallback offline, tarefas adaptadas, áudio procedural e confirmação do Sol. O HUD, mapa, câmera, dormitório, regador e modelos existentes foram aproveitados.

**A especificação integral ainda não está concluída.** O limite adicional de uma hora solicitado pelo proprietário foi respeitado priorizando funcionamento e validação. Os acabamentos e critérios de aceitação pendentes estão explicitados abaixo, sem apresentá-los como concluídos.

## Fases

| Fase | Entrega | Estado |
|---|---|---|
| P0 | Auditoria Unity, pipeline, recursos, animação, rede, efeitos e baseline | Concluída; [AUDIT.md](AUDIT.md) |
| P1 | Serviço NASA reutilizado, plano por campos, catálogo retomável, diretor, perfil, fallback | Implementada; 86 datas verificadas e 12 cenários offline |
| P2 | Bateria compartilhada, geração, choques, ledgers, condição e recuperação | Implementada e testada no domínio; normalização energética requer aprovação |
| P3 | HUD localizado, remédios, aviso visível de 15 s, colapso, resgate e encerramento médico | Implementada; caminho principal passou em Play Mode |
| P4 | Poeira preservada, direção simulada, pools e cores por evento, apresentação de escudo | Parcial: acabamento artístico, prefabs/controladores independentes e Volume URP pendentes |
| P5 | Loops procedurais, mixer/grupo Hazards, perfil substituível, ganho interno e ducking | Implementada funcionalmente; acabamento dos filtros/chimes/thud e avaliação auditiva pendentes |
| P6 | Cinco tarefas adaptadas aos dados, abrigo obrigatório, limpeza real e reparo elétrico | Implementada em versão inicial; rotação diversificada e objetivos pós-evento mais completos pendentes |
| P7 | Narrador, en/pt-BR, regressão, simulações, capturas e medições do Editor | Parcial: UTF formal, benchmark da build e duas partidas humanas completas pendentes |

## Arquivos principais

Novos componentes:

- `Assets/ARES/Runtime/Domain/Hazards/`: `HazardBalance`, `HazardPlanner`, `ScenarioDirector`, `EnergyGrid`/`ExposureModel`, `PlayerConditionModel`, `CampaignHazards` e `MissionDirector`/`ScenarioSolvability`.
- `Assets/ARES/Runtime/Infrastructure/Hazards/`: `HazardCatalogBuilder`, `MissionScenarioService`, `ScenarioProfile`.
- `Assets/ARES/Runtime/Infrastructure/Nasa/NasaArchive.cs`: extensão do serviço existente, sem cliente NASA paralelo.
- `Assets/ARES/Runtime/UI/Hazards/HazardHud.cs` e `Resources/ARES/UI/Hazards.uss`, `en.hazards.json`, `pt-BR.hazards.json`.
- `Assets/ARES/Runtime/World/Hazards/`: `DustStormEffect`, `WeatherVfxController`, `HazardPresentation`, `HazardAudioController`, `HazardAudioProfile`.
- `Assets/ARES/Resources/ARES/Hazards/`: catálogo, snapshots, regras do narrador, ScenarioProfile, AudioProfile e HazardMixer.
- `Assets/ARES/Editor/HazardAssets.cs`, `HazardCatalogMenu.cs`, `HazardChecks.cs`, `HazardPlayChecks.cs` e `Tools/Hazards/`.

Pontos existentes integrados:

- `Campaign`, `ConflictingDemands`, `SolarPlant`, `DustStormWeather` e `CommunicationsSystem`.
- `MissionIntegration`, extensão parcial de `NasaSolService`, `LocalNarratorTemplates`, `NarrationValidator`.
- `GameFrontend`, `ComputerPanel`, `EnergyView`, `MissionHud`, `Localization`, `StormWindHud`.
- `ReferenceExplorer`, `MartianWind`, `ReferenceEnvironment`, `MissionStormPresentation`, `TaskAttempt` e `TaskReadings`.
- `ApodService` utiliza os Sols selecionados pelo serviço nativo. Telescópio continua usando APOD/biblioteca NASA, com imagens locais e créditos quando o download falha; imagens não ativam eventos climáticos.

Alterações anteriores já presentes no repositório não foram revertidas. Nenhuma chave foi acrescentada ao catálogo, aos snapshots ou à documentação.

## Verificação executada

- **Compilação:** Runtime e Editor compilados; os avisos de APIs obsoletas dos checks anteriores não impedem a compilação.
- **Regressão NASA/IA:** 228 verificações passaram, preservando RNG/calendário/paridade, transporte, validação e cache.
- **Hazards:** 170 verificações passaram. Incluem ativação por fonte, 32 combinações de null, procedência, thresholds, determinismo, ledger sem cobrança duplicada, caps, aviso visível, terceira evacuação, chaves en/pt-BR e respostas locais do narrador.
- **200 campanhas sintéticas × 5 Sols:** estratégia planejada concluiu 200/200, sem abortos nem evacuações. Estratégia descuidada sofreu colapso em 188/200 e abortou em 8/200. O teste usa Campaign real, relógio, água/irrigação, tarefas, energia e relatórios; abstrai deslocamento físico e resolução humana dos desafios. [Resultados](simulation-results.json).
- **Cenário com rede:** Curated/catalog-verified, cinco datas distintas, 36 rejeições/substituições de candidatos; simulação até Victory com zero evacuações. A consulta mostrou que substituições reais podem demorar; por isso a versão final impõe espera de 60 s e fallback identificado. O teste online foi realizado antes da adição desse limite. [Dados](live-scenario-results.json).
- **Offline:** 12 sequências derivadas de observações verificadas; fallback sem acesso à rede também atingiu Victory na simulação.
- **Play Mode:** passou a sequência de avisos, colapso, resgate para a cama, recuperação, idiomas, mixer/snapshots, confirmação do Sol e relatório. [Registro](playmode-results.txt).
- **Capturas:** [pasta Captures](Captures), incluindo 21 cenários tipo/nível, aviso pt-BR, recuperação e interface en. Níveis sem fonte que ative aquele risco continuam sem ativar o risco; as capturas não representam 21 efeitos artísticos completos.

## Visual e desempenho

A poeira Medium conserva as cores, shader, nuvens, grãos e fog originais. O baseline registrou 450 partículas, vento visual 92 km/h e visibilidade 48 m; a repetição registrou aproximadamente 448 partículas, 92 km/h e 48 m. A diferença de partículas é compatível com o sistema estocástico existente; não se afirma igualdade pixel a pixel.

No Play Mode, o cenário calmo registrou média de 35,11 ms/frame e aproximadamente 68 KB de alocação gerenciada por frame completo. As amostras com eventos ficaram próximas de 31–34 ms em várias capturas. **Não é uma medição isolada do custo dos efeitos nem prova de melhoria:** inclui Editor, UI e o próprio check. Os pools permaneceram limitados. Não foram medidos GPU, draw calls ou orçamento de frame em build final; esse critério está pendente.

## Configuração e reprodução

- Ajustes de eventos: `Assets/ARES/Resources/ARES/Hazards/ScenarioProfile.asset`.
- Áudio/clipes substitutos: `Assets/ARES/Resources/ARES/Hazards/AudioProfile.asset`.
- URLs e parâmetros NASA/Groq continuam em `Assets/StreamingAssets/integrations.json`; credenciais continuam no arquivo local já existente, sem alteração nesta entrega.
- Unity: `ARES > Checks > Hazard domain and 200 missions` e `ARES > Checks > Hazard integration and captures`.
- Catálogo: em Play Mode, `ARES > Hazards > Rebuild NASA catalog (Play Mode)`. Reutiliza o serviço existente e o cache retomável.
- Fórmulas, unidades, simplificações e **todos os valores PROVISIONAL** estão em [DESIGN.md](DESIGN.md).

## Pendências e perguntas para o proprietário

1. Confirmar os limiares de atenção/alerta/colapso: Dose 50/75/100, O₂ 40/25/0 e exposição térmica acumulada de 300 s.
2. Confirmar dois kits médicos, dois cilindros e reposição de um kit por Sol ideal; confirmar custo/benefício do repouso e do resgate.
3. Aprovar bateria compartilhada de 30 kWh, início em 10, piso solar de 80% e os caps adicionais de dano.
4. Aprovar interrupção na terceira evacuação e a lentidão temporária após tratamento.
5. Deseja acrescentar chamadas DONKI SEP/notifications, ou manter radiação derivada explicitamente identificada?
6. O catálogo deve continuar com 86 datas/shortlist de seis por ano ou receber curadoria/expansão maior? O endpoint FLR retornou 404 nesta execução; é necessário acompanhar sua disponibilidade.
7. São aceitos cinco dias reais não consecutivos no modo Curated? Há conflito entre essa exigência e a regra de não reordenar datas; o modo e a procedência ficam explícitos.
8. Manter vento visual simulado ou integrar uma fonte histórica específica? InSight respondeu, mas não representa o mesmo local/tempo do Curiosity.
9. Manter 600 s/Sol, como no Unity atual, ou migrar para 480 s do protótipo?
10. Aprovar a normalização entre orçamento pedagógico de energia e modelo físico de oxigênio, e o acerto do ledger pela alocação final? Ambos estão explicados no design.
11. Agendar uma etapa de acabamento para aurora/frentes/prefabs por evento, Volume URP, efeitos sonoros específicos, controles de depuração, diversidade das tarefas, UTF e benchmark na build final. Esses itens **não estão concluídos** nesta entrega limitada a uma hora.
