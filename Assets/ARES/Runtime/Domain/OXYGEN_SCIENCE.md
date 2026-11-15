# Oxigênio: ciência, unidades e escolhas do jogo

## Fontes verificadas em 15/11/2026

- [NASA — Environmental Control and Life Support Systems](https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/): geração de oxigênio por eletrólise da água; retirada de CO₂ como uma função separada. O hidrogênio pode alimentar recuperação adicional ou ser descartado.
- [NASA — OCHMO-TB-004 Carbon Dioxide](https://www.nasa.gov/ochmo-tb-004-carbon-dioxide-2/), tabela “Crew-Induced Metabolic Loads for a Standard Mission Day with Exercise”, baseada no HIDH: 0,82 kg de O₂ usados e 1,04 kg de CO₂ produzidos por pessoa em 24 horas terrestres. São cargas de referência, não medições dos personagens.
- [NASA/JPL — MOXIE completes Mars mission](https://www.nasa.gov/solar-system/nasas-oxygen-generating-experiment-moxie-completes-mars-mission/): demonstrou extração de oxigênio do CO₂ atmosférico de Marte. Essa rota não é a eletrólise de água usada pelo gerador da base; a interface explica a diferença.
- [NASA Open APIs](https://api.nasa.gov/): APOD fornece imagens e metadados do telescópio. Não fornece telemetria de oxigênio da base fictícia.
- [CSA — Toolkits for educators and youth](https://www.asc-csa.gc.ca/eng/youth-educators/toolkits/): materiais de educação e produção de alimentos no espaço. Não foram tratados como uma tabela de produção de oxigênio das plantas deste jogo.

DONKI, RadLab/OSDR e Mars Trek são recursos úteis para clima espacial, pesquisas biológicas, dosimetria e terreno, respectivamente. Não são uma justificativa para inventar taxas de oxigênio em tempo real. Esta alteração não muda os módulos de radiação, terreno ou cultivo com dados dessas fontes.

Este projeto é independente. A NASA não endossa o jogo nem se responsabiliza por seu conteúdo. Os créditos e licenças individuais das imagens APOD continuam em `Resources/ARES/Observatory/collection.json`; o sorteio não altera a atribuição nem as imagens.

## Tempo e fórmulas

`MissionTime` define um Sol como 88.775,244 s físicos, representados por **600 s de jogo ativo**. As taxas metabólicas da referência terrestre são multiplicadas por `88775.244 / 86400` para obter a quantidade por Sol. O relógio, tarefas e computador compartilham essa escala. O menu de pausa e as fases de relatório/planejamento/resumo não avançam a simulação.

Para `dt` segundos ativos:

```text
fração_do_sol = dt / 600
demanda_equipe_kg = pessoas × 0,82 × (88775,244 / 86400) × fração_do_sol
CO₂_da_equipe_kg = O₂_entregue_à_respiração × (1,04 / 0,82)
O₂_gerado = min(taxa_do_modo × fração_do_sol × fração_de_energia, água_disponível / 1,125)
água_usada = 1,125 × O₂_gerado
hidrogênio_separado = água_usada − O₂_gerado
O₂_entregue = min(demanda_total, reserva_anterior + O₂_gerado)
nova_reserva = min(capacidade, reserva_anterior + O₂_gerado − O₂_entregue)
CO₂_retirado = min(CO₂_acumulado + CO₂_novo, capacidade_do_filtro × fração_do_sol × fração_de_energia)
```

A razão 1,125 usa massas molares arredondadas na reação `2 H₂O → 2 H₂ + O₂`: 36 unidades de água separam-se em 32 de oxigênio e 4 de hidrogênio. Não existe criação gratuita de oxigênio sem água ou energia. O filtro remove CO₂, mas não o converte em oxigênio. A circulação do ar não consome O₂; a antiga categoria “habitat” agora representa pequenas perdas nas vedações.

Todos os saldos de matéria usam **kg**. O painel principal mostra **g/min de jogo**, calculados como `kg_por_ciclo × 1000 × 60 / cycleSeconds`. Energia permanece em kWh. Evitamos litros de gás sem especificar pressão e temperatura. A porcentagem grande é o preenchimento do tanque, não a concentração de O₂ na atmosfera.

## Escolhas explícitas de simulação

`OxygenSettings` configura quatro pessoas, tanque de 8 kg, 30 kg de água dedicados ao gerador, produção de 2,8 / 3,8 / 5,2 kg por Sol, filtro de 5 kg de CO₂ por Sol, perdas de 0,03 kg por Sol e experimentos opcionais de 0,15 kg por Sol. Esses valores e o gasto elétrico são balanceamento da base fictícia, não especificações de equipamento da NASA. O gasto de energia inclui gerador e filtro.

A água do gerador não é o reservatório de 11 L da estufa. Não há recarga infinita nem recuperação Sabatier implementada. O hidrogênio é contabilizado como subproduto separado, sem recompensa ou retorno automático de água. Quando falta energia, produção e filtragem diminuem; quando falta água, a geração para e o filtro ainda pode funcionar. Demanda não atendida e excedente de O₂ liberado com o tanque cheio são contabilizados separadamente.

O modelo não calcula pressão parcial, temperatura da cabine, doenças ou qualidade médica do ar. `CarbonDioxideKg` registra a carga que aguarda remoção, não um limite clínico. A reserva baixa é um alerta de gerenciamento. Recompensas e penalidades discretas da campanha continuam sendo regras educativas, separadas da contabilidade física contínua.

## Organização e verificações

- `OxygenPlant`: domínio independente de Unity, sem UI ou consultas de rede.
- `Campaign.Tick`: relógio e atualização coordenada de geração solar/oxigênio; ao completar 600 s encerra tarefas pendentes sem conceder suas recompensas e abre o resumo.
- `OxygenView`: tanque visual, entradas/saídas, água e filtro; números completos e fontes no campo expansível.
- `ComputerPanel`: duas páginas com deslizamento; `ComputerTerminal` e `GameFrontend.BuildComputerEnergy` restringem energia ao computador físico.
- `DiscoveryDeck` / `ApodService.BeginMission`: sorteio sem reposição dos cinco registros completos. Datas, fotos, resumos e créditos viajam juntos; a descoberta de um Sol permanece estável no diário. Uma nova missão reinicia sorteio e cache.

`LifeSupportChecks.VerifyModel`, executado pela solicitação focada `Temp/ares-computer-check.request`, verifica 600 s e pausa, encerramento de tarefa pendente, cálculo metabólico, água/hidrogênio, CO₂, falta de água/energia, limites e conservação de massa em 30/120 Hz. A parte de Play verifica setas, transição, interação próxima e bloqueio remoto. Não é necessário executar o rover nem a campanha completa para validar esta alteração.
