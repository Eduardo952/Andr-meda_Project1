# Estufa — Casa Marte

Estufa educativa para crianças de 7–14 anos: quatro prateleiras, dez recipientes em cada uma, uma reserva de 11.000 mL e cinco dias de missão. Esta implementação substitui os antigos níveis de 1,36/2,04/2,32 L e o reinício do reservatório a cada Sol.

## Responsabilidades

| Arquivo | Responsabilidade |
| --- | --- |
| `GreenhouseRules.cs` | Constantes de projeto e conversão solo → quantidade de doses |
| `GreenhouseDayConditions` (no arquivo de regras) | Situações internas determinísticas por dia, sem ligação ao clima externo |
| `GreenhouseSimulation.cs` | Quatro estados de solo, luz compartilhada, saúde, reservatório e resultados diários |
| `GreenhouseWatering.cs` | Regador, retirada de água, ação de rega e conservação do saldo |
| `Campaign.cs` | Avanço dos dias e registro da tarefa diária; preserva as mesmas instâncias durante toda a missão |
| `../World/GreenhouseCare.cs` | Entrada por clique/E e proximidade |
| `../World/GreenhouseCropRacks.cs` | Quatro prateleiras com dez recipientes visíveis, lâmpadas e reservatório |
| `../World/GreenhouseWorldView.cs` | Regador, pose, gotas e indicadores no cenário |
| `../UI/GreenhouseView.cs` | Observação do solo, ajuste da luz e registro dos cuidados |
| `../UI/GreenhouseNarrator.cs` | Texto infantil e explicações científicas |

## Regras e ciclo de vida

- Uma rega completa usa 510 mL. Solo moderado: uma; seco: duas; muito úmido: nenhuma.
- Nas quatro prateleiras uniformes, os totais são 2.040, 4.080 e 0 mL, respectivamente.
- `RequiredDoses` é fixado no início do dia. Após completar as doses, o solo aparece moderado, mas isso não cria outra necessidade de rega no mesmo dia.
- Cada clique numa prateleira a observa e tenta iniciar uma dose. Uma prateleira muito úmida fica registrada como observada sem consumir água. Doses extras são bloqueadas.
- A luz começa moderada. Os botões alteram uma faixa por vez, permitindo aprender com pouca luz ou luz excessiva. Ambos podem prejudicar a saúde.
- `EndDay` aplica consequências de regas faltantes e iluminação inadequada uma única vez. Solo muito úmido sem água adicional drena ao longo do dia (simplificação educativa). Problemas em dois dias seguidos elevam o risco; condições adequadas restauram a saúde.
- `BeginDay` só aceita o próximo dia após o encerramento do anterior. As situações internas fornecem os desafios do novo dia. Não existe reposição automática de água.
- `CareReady` permite registrar a tarefa quando todas as prateleiras foram observadas/cuidas e a luz está adequada. Registrar não avança o relógio. O dia continua usando a duração global de 600 segundos.
- A missão da estufa termina após o quinto dia. Água insuficiente bloqueia novas doses, mas não bloqueia observar ou ajustar luz.

A sequência educativa inicial exige 2.040 mL/dia quando bem cuidada, incluindo dias com solos mistos; sobram 800 mL após cinco dias. Isso é balanceamento de jogo. O cenário inicial pode ser injetado no construtor para criar atividades alternativas; os próximos dias são definidos por `GreenhouseDayConditions.ForDay`.

## Base científica e limites

Fonte primária: [Pick-and-eat space crop production flight testing on the International Space Station — NASA NTRS](https://ntrs.nasa.gov/citations/20240000823), [artigo completo](https://ntrs.nasa.gov/api/citations/20240000823/downloads/2024%20Buncheck%20VEG-04.pdf).

O VEG-04A estudou mizuna no Veggie e registrou média de 51 mL por recipiente/dia no tratamento com maior proporção de luz vermelha; o outro tratamento apresentou valor diferente. Portanto, 51 mL não é uma necessidade universal de qualquer planta. O VEG-04B também estudou cultivo e manejo da água.

A multiplicação por dez recipientes fornece os 510 mL usados como referência didática. A NASA não definiu esse regador, a reserva de 11 litros, os três estados do solo, as doses por estado nem essa missão marciana de cinco dias. Frutas e legumes são parte da variedade visual do jogo, não uma descrição dos cultivos do VEG-04A/B.

## Verificação focada

Menu `ARES > Checks > Casa Marte - water and daily care`. Verifica contabilidade, doses, interrupções, reservatório insuficiente, cinco dias, persistência real entre dias da campanha, interação por clique, 40 recipientes, luz e registro da tarefa. Abre somente vistas da estufa; não percorre outras tarefas nem testa o rover. Relatório: `Logs/ares-casa-marte.txt`; imagens: `Logs/ares-casa-marte-world.png` e `Logs/ares-casa-marte-ui.png`.
