# Casa Marte — aprender pelas ações

## Público e direção

Público principal: 7–14 anos. Uma instrução curta abre a atividade; o equipamento aparece imediatamente. O botão **Começar** habilita as ações e o cronômetro. Antes dele, a prévia não muda a tentativa. Erros dão uma dica localizada. Explicações, origem dos dados e aprendizado após a tarefa ficam em detalhes opcionais.

O plano diário começa pelos três equipamentos e suas necessidades de energia. O clima detalhado fica recolhido. A reserva da bateria no HUD é a carga real de `SolarPlant`, não a porcentagem de cobertura do plano de distribuição. O painel de limpeza só aparece perto do parque solar ou com a vassoura equipada. A seta de navegação continua indicando o destino.

Não reduzir a precisão reduzindo o número de palavras: distinguir reserva de O₂ de concentração no ar, radiação do jogo de dose médica, cenário inventado de arquivo real, previsão de impacto de velocidade observada.

## Responsabilidades

| Módulo | Responsabilidade |
|---|---|
| `UI/ResourceGauge` | Imagem + nome + barra; nunca calcula recursos |
| `UI/TaskView` | Equipamento visível, começo explícito, dicas e fontes opcionais |
| `UI/CampaignScreens` | Plano principal e clima em detalhes |
| `UI/ScienceLibrary` | Biblioteca opcional com fontes oficiais |
| `World/HabitatStoryProps` | Geometria low-poly e indicadores que apenas leem a campanha |
| `Data/ScienceArchive` | Registros oficiais de arquivo, validação e seleção por dia |
| `Data/ApodResponse` | Contrato APOD, resposta única/lista e data exata |
| `Tools/AresImport/review-child-copy.py` | Catálogo PT-BR revisado, sem editar o export original |

O catálogo revisado contém 630 chaves; 191 diferem do catálogo original combinado. As demais mantêm nomes, números, unidades ou textos já curtos. Placeholders são validados ao gerar. Textos de comunicações, estufa, relatório e equipamentos também foram revisados diretamente nos seus componentes. Inglês permanece no catálogo existente; a nova camada editorial é PT-BR.

## Narrativa ambiental

- Ar e calor: três cilindros com ombros facetados, cintas, válvulas e tubos compartilhados. Cinco segmentos mostram a reserva de oxigênio.
- Sala central: armário de bateria com células, terminais e cabos; os segmentos acompanham a bateria. Mesa de refeições e armários pessoais sugerem uma tripulação vivendo ali.
- Observatório: bancada de amostras, bandejas, rochas facetadas e instrumentos.
- Abrigo: caixas de suprimentos e luz que acompanha a conexão da antena.

Materiais creme/grafite com acentos específicos por sistema. Malhas agrupadas por material; peças dinâmicas independentes; recursos destruídos em `OnDestroy`. Sem objetos primitivos individuais ou dependência do controlador do rover: apenas o utilitário de autoria de malhas existente. Colocação nas bordas das salas, fora das rotas de cinco unidades. Indicadores mostram nível por altura/quantidade, além de cor. A estufa detalhada existente é preservada.

## Trocas que a criança pode observar

Reforçar a produção de O₂ aumenta o consumo de água e energia; poupar pode reduzir o ar guardado. A bateria e os tanques mostram as consequências. A comunicação conserva seus custos diferentes para confirmar recebimento e pedir dica. Os textos curtos acompanham as decisões; dados completos continuam acessíveis.

Não tratar decoração como uma nova mecânica: refeições, armários e amostras são ambientais, não sistemas de fome/sono/mineração. Não prometer um benefício de gameplay que não existe.

## Validação focada

`Editor/VisualLearningChecks` verifica os quatro ícones, bateria seguindo a reserva, arte procedural, contratos/fontes e uma prévia de canos. Também confere janela de 1280 × 720 e captura HUD, equipamento, tanques e relatório. Não percorre todas as tarefas nem testa o rover. Sentinela: `Temp/ares-computer-check.request` contendo `visual-learning`.

As evidências ficam em `Logs/ares-visual-*.png` e `Logs/ares-visual-learning.txt`. É uma verificação técnica e visual, não um estudo de compreensão com crianças.
