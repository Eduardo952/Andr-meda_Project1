# Ares Base — ciência e decisões do protótipo

Público principal: crianças de 7–14 anos. A missão dura cinco dias simulados. A interface prioriza equipamentos, indicadores visuais e instruções curtas; unidades, fontes e limites ficam nos detalhes opcionais do sistema correspondente. Direção e módulos em `VISUAL_LEARNING.md`.

## O que a criança decide

- **Ar e energia:** aumentar a produção de oxigênio usa mais energia e água; experimentos também consomem recursos. Gerar oxigênio e retirar CO₂ são processos diferentes.
- **Proteção:** a campanha compara energia destinada ao escudo com a necessidade do cenário. A exposição acumulada é um índice educativo, não uma dose médica medida.
- **Alimentos:** quatro prateleiras dividem 11 L durante a missão. Observar o solo evita desperdício; pouca luz e luz excessiva prejudicam as plantas.
- **Comunicação:** uma confirmação curta custa menos energia que pedir orientação. A ajuda leva tempo para ir e voltar. Uma bateria dedicada protege o envio dos cinco relatórios essenciais.

## Demandas conflitantes durante a exploração

`Runtime/Domain/ConflictingDemands.cs` sorteia a agenda uma vez por campanha, com uma semente reproduzível. São dois dias distintos com poeira e pelo menos um dia com erupção. Cada evento só é anunciado depois de **duas tarefas obrigatórias concluídas naquele dia**. A regulagem opcional não entra nessa contagem nem em `AllTasksDone`.

- **Poeira:** aviso de 20 segundos, soterramento dos 32 painéis e geração zero. A bateria existente sustenta a base até acabar; então a produção de oxigênio deixa de atender ao consumo. A vassoura recupera cada painel pelo sistema de manutenção existente. Poeira não desaparece ao mudar de dia. Oxigênio realmente zerado encerra a partida imediatamente; o percentual arredondado não decide a morte.
- **Erupção:** sorteada somente nos dias **2 e 5**, quando existe prevenção. Aviso de 60 segundos; o impacto danifica a antena e aumenta a radiação. Sem regulagem naquele dia, o impacto é fatal. Com regulagem, acrescenta 12 pontos ao índice educativo de exposição e permite continuar. O reparo da antena usa suas três etapas existentes no computador.
- **Regular Escudo Eletromagnético:** missão opcional disponível desde o começo dos dias 2 e 5, no setor do escudo. Ajustar três controles e ativar a proteção registra a regulagem apenas para o dia atual. Estar dentro da sala não substitui o ajuste. A tarefa tem botão próprio na lista e também aparece na interação do setor após a tarefa obrigatória local.
- **Tempo e apresentação:** pausa e tutorial congelam eventos; minigames e computador continuam consumindo tempo. Um aviso iniciado perto do fim do dia mantém sua duração inteira, prorrogando o encerramento. Selar não ignora um aviso pendente. O impacto visual dura 30 segundos; reparos continuam necessários depois. Alertas ficam visíveis também sobre minigames/computador, sem capturar cliques.

`Campaign` aplica as consequências aos sistemas reais. `ShieldRegulationView`, `MissionEventBanner`, vento, domo e película de areia apresentam o estado. `DailyBaseSnapshot` e o arquivo local de comunicações preservam evento, regulagem, painéis soterrados e antena no fechamento do dia. O sorteio substitui o alarme fixo de `SolData.alarm` e a antiga pane automática da antena no dia 3; leituras de exemplo continuam sendo usadas pelos desafios científicos.

Verificação integrada: solicitar `mission-events` em `Temp/ares-computer-check.request`. Evidências em `Logs/ares-mission-events.txt` e `Logs/ares-events-*.png`: 64 sementes, contagem obrigatória, pausa, aviso tardio, regulagem, reparo, recuperação por vassoura, mortes, relatórios e UI PT/EN em tela normal/compacta. A agenda é simulação educativa, sem previsão real de clima marciano ou solar.

## O que é real e o que é simulado

### Apresentação da tempestade e console

`DustStormWeather` fornece intensidade e velocidade simuladas para a mesma rajada usada no visual e no instrumento externo. No auge, o modelo varia entre 19 e 34 m/s; a interface converte para km/h. Esse valor não é uma medição NASA. O visor aparece durante a tempestade ao sair para áreas abertas, incluindo o parque solar, e se oculta nos módulos e corredores cobertos. `HabitatShelter` compartilha essa classificação com as partículas.

`MartianWind` mantém um único conjunto limitado a 960 partículas: grãos rápidos e nuvens largas com ruído procedural. Céu, luz solar e neblina acompanham a frente de poeira; a visibilidade externa cai para cerca de 48 metros no auge e se recupera ao terminar. Movimento reduzido diminui o movimento e a quantidade de partículas, sem alterar a velocidade indicada ou as regras da campanha. Revisão visual: `storm-preview` em `Temp/ares-computer-check.request`; imagens e resultado em `Logs/ares-dust-*`.

Durante a missão, **`'` abre/fecha o console**, `Enter` executa e `Esc` fecha. A partida e seus atalhos ficam suspensos enquanto o console tem foco; a tela anterior é preservada. Comandos disponíveis: `/help`, `/event (WindStorm)` e `/event WindStorm` (sem distinção de maiúsculas). É preciso estar na exploração, após o tutorial e a divisão de energia. O acionamento manual dispensa as duas tarefas obrigatórias, inicia o impacto real e soterra os painéis; não concede missões ou recompensas. Um evento ativo não pode ser sobrescrito. Parser e consequências ficam em `MissionCommands`/`Campaign`; `MissionCommandConsole` cuida apenas da entrada, foco e mensagens PT/EN. Revisão: `command-console` pelo mesmo arquivo de solicitação.

| Elemento | Origem e limite |
| --- | --- |
| Fotografias do telescópio e desafio de fotos | Coleção real APOD de julho de 2022, com créditos e links por imagem em `Resources/ARES/Observatory/collection.json`. Consulta APOD usa o endpoint atual de NASA Science e valida a data exata, com cópia local como fallback. Não é uma transmissão ao vivo de Marte. |
| Desafios de explosões e nuvens solares | 21 registros reais de arquivo DONKI obtidos no novo endpoint oficial, referentes a 10/05/2024. As leituras identificam fonte/data e análise inicial quando aplicável. Não substituem o cenário de campanha nem estabelecem impacto em Marte. |
| Oxigênio | Referências técnicas da NASA sobre ECLSS e cargas metabólicas. Fórmulas, unidades e hipóteses em `Runtime/Domain/OXYGEN_SCIENCE.md`. Não é telemetria de uma base real. |
| Cultivo | VEG-04A/B e média de 51 mL por recipiente/dia em um tratamento do VEG-04A. Capacidade do regador, doses por estado do solo, cultivos variados e 11 L são adaptações educativas. Ver `Runtime/Domain/GREENHOUSE.md`. |
| DSN e demora do sinal | A rede e o atraso físico são reais; mensagens, custos, falha de antena e 12 segundos por trecho são regras de jogo. Ver `Runtime/Domain/Communications/COMMUNICATIONS.md`. |
| Clima espacial e ambiente de Marte da campanha atual | **Os cinco cenários em `Import/campaign.json` estão marcados `sample: true`.** Eles usam estruturas semelhantes a DONKI/InSight, mas contêm valores de exemplo, não observações verificadas. Não apresentar esses números como dados reais ou atuais da NASA. |
| Dormitório | Avaliação do ambiente a partir de oxigênio, energia e integridade do jogo. Ainda não há medição de horas de sono. |
| OSDR | Busca oficial retornou três estudos de plantas. Identificadores e temas estão na biblioteca opcional do computador, vinculados aos estudos. Não são taxas de crescimento inventadas para a estufa. |
| RadLab, Mars Trek e CSA | Documentação e recursos oficiais consultados e vinculados na biblioteca. RadLab não alimenta a barra de radiação; o cenário não é terreno Mars Trek. CSA é referência educativa, não tabela oficial de irrigação. |
| Escudo magnético | Tecnologia em pesquisa; a proteção ativa da base é ficção científica identificada, não equipamento de colonização estabelecido. |

As telas e relatórios devem continuar distinguindo dados de exemplo, referências científicas e registros da partida. Substituir cenários de exemplo por arquivos verificados exige atualizar a procedência dos dados, não apenas remover o rótulo `sample`.

O projeto é independente e não tem endosso da NASA. O arquivo de comunicação é local; ninguém recebe mensagens reais fora do jogo. A rastreabilidade específica de cada sistema está nos documentos e links acima.

URLs verificadas, respostas originais, datas de obtenção e hashes: `Tools/ScienceEvidence/README.md` e `manifest.json`. Ícones gerados são arte do jogo, não imagens científicas.
