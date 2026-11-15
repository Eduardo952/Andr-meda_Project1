# Rega manual — guia de manutenção

O painel prepara um plano; a água é aplicada pelo jogador no cenário. E pega/devolve o regador no suporte e abastece no ponto ÁGUA. A coleta e a devolução também funcionam antes do planejamento e após encerrar o cultivo, quando a exploração está liberada. E no restante da estufa abre o gerenciamento. Com uma dose preparada, segurar o clique esquerdo em cada área delimitada rega aquela prateleira. O resultado e o próximo clima aparecem depois das quatro entregas. Ao terminar a atividade, o jogador ainda precisa registrar o aprendizado no painel para concluir a tarefa da campanha.

## Responsabilidades

| Módulo | Responsabilidade |
| --- | --- |
| `Domain/GreenhouseSimulation` | Solo, luz, clima, saúde, saldo total e consequências por ciclo |
| `Domain/GreenhouseWatering` | Ferramenta, plano bloqueado, transferência, entrega progressiva e conclusão manual |
| `World/GreenhouseCare` | Proximidade, E, clique, bloqueio por menus e sessão atual da campanha |
| `World/GreenhouseWorldView` | Sprite do regador, pose, gotas, contornos, rótulos e cores das plantas |
| `World/GreenhouseOcclusion` | Transparência temporária da geometria que esconde o jogador |
| `UI/GreenhouseView` | Escolha do plano, ajuste contextual e reflexão final |
| `UI/GreenhouseCareHud` | Água, clima, solo, luz, saúde e progresso das quatro prateleiras |
| `Domain/Campaign` | Reinício da atividade por Sol e concessão única da recompensa |

`GameFrontend` instala `GreenhouseCare`, chama sua atualização e dá prioridade à coleta/devolução/transferência antes da tarefa da estação. No ponto de abastecimento, E é consumido mesmo se `Fill()` retornar `false`, evitando abrir o gerenciamento por acidente. `ReturnCan()` interrompe a rega e conserva a água no regador, o plano e as doses já entregues. `ReferenceExplorer` reserva o clique esquerdo para rega dentro da área cultivável; arrasto direito e zoom continuam disponíveis. `MissionHud` mostra o cartão da estufa e oculta o cartão solar enquanto o jogador está dentro dela. A vassoura carregada fica oculta nesse local para não ocupar a mesma mão.

## Configuração e coordenadas

Os valores abaixo são coordenadas **Unity**, já com Z convertido da referência. Altere os pontos em `GreenhouseCare`; não edite a pasta de referência para reposicionar a nova interação.

| Ponto | Posição `(x,y,z)` |
| --- | --- |
| Regador | `(35,0,-2)` |
| Abastecimento | `(48,0,-2.8)` |
| 1 — Fruta | `(43,0,-4.5)` |
| 2 — Legume | `(61,0,-4.5)` |
| 3 — Verdura | `(43,0,4.5)` |
| 4 — Verdura | `(61,0,4.5)` |

O alcance de coleta é 1,8 m; o de abastecimento é 1,7 m. Cada área de rega mede 4,2 × 1,8 m no chão. O reconhecimento da estufa usa X entre 32 e 72 e Z entre −16 e 16. Se mudar o layout, mantenha esses limites, as posições, a seleção das plantas e os contornos sincronizados.

`GreenhouseWatering.CapacityMl` define 2320 ml; `SecondsPerShelf` define 2,5 segundos por prateleira. As doses da estufa inteira continuam em `GreenhouseSimulation`: 1360/2040/2320 ml. Cada prateleira recebe um quarto: 340/510/580 ml. A capacidade comporta a maior dose; preserve essa relação ao rebalancear ou adapte o fluxo para múltiplos abastecimentos.

## Balanço de água e conclusão do ciclo

Durante um plano, `GreenhouseSimulation.WaterMl` mantém o total anterior ao ciclo. `GreenhouseWatering` expõe:

```text
ReservoirMl = WaterMl - CanMl - DeliveredMl
AvailableMl = WaterMl - DeliveredMl
```

Encher transfere água do reservatório para o regador. Regar reduz `CanMl` e aumenta `DeliveredMl`. A quarta entrega chama `GreenhouseSimulation.Decide` uma única vez, desconta a dose total e avança o clima. Só então os contadores de entrega são zerados. A água que sobrou no regador é preservada. Exemplo: abastecer 2,32 L e aplicar uma dose mediana de 2,04 L deixa 8,68 L no reservatório e 0,28 L carregados: 8,96 L disponíveis.

Não desconte água ao abastecer e novamente ao concluir. Não chame `Decide` diretamente pela UI. O domínio rejeita substituir um plano em andamento ou regar novamente uma prateleira já concluída. Soltar o botão, sair da área ou abrir um menu interrompe o fluxo sem apagar a água já entregue. Frações de mililitro são acumuladas para que a taxa de quadros não altere a dose.

Fechar/reabrir o painel conserva a sessão. Um novo Sol cria outra atividade com 11 L e reinicia a ferramenta. Não há abastecimento infinito durante a mesma atividade. `Campaign.FinishGreenhouseTask` exige atividade encerrada e `CompletedManualCycles == CompletedCycles`; a UI não concede estrelas. As fixtures de teste também passam pela entrega manual do domínio, sem serem usadas pelo runtime.

## Saúde e apresentação

As quatro prateleiras compartilham as regras de solo e luz solicitadas para a atividade. Durante a entrega, cada grupo mostra seu progresso e uma prévia de saúde; o ciclo completo aplica as consequências e o histórico de problemas. Não existem sensibilidades científicas específicas por cultivo nem crescimento individual independente. As regras detalhadas estão em [GREENHOUSE.md](../Domain/GREENHOUSE.md).

As malhas importadas são reutilizadas para quatro grupos cultiváveis; outras plantas são decorativas. `MaterialPropertyBlock` aplica cores leves de saúde sem alterar materiais compartilhados. `GreenhouseOcclusion` seleciona os candidatos pela posição das malhas. A obstrução usa `MeshSightline`: os limites rejeitam peças distantes, e os triângulos confirmam se existe geometria no segmento entre câmera e torso. Espaços vazios entre folhas e peças atrás do jogador não provocam transparência. Os vértices/índices ficam em cache, sem colliders adicionais. Uma obstrução de 0,08 s inicia a transição até 35% de opacidade; 0,15 s de visão livre restaura a peça. Menus, controle do rover e saída da estufa desativam esse efeito. Os materiais originais são restaurados e as cópias temporárias são descartadas ao destruir o componente.

## Orientação do regador e troca de arte

O regador é um **sprite 2.5D**, não uma nova malha 3D. Sua arte foi gerada com `imagegen`, preservando transparência. O pivô normalizado `(0.37,0.91)` fica na alça superior. `HandPosition` usa `rightArm.TransformPoint(0,-0.55,0)`, a posição da mão no personagem importado. O regador acompanha esse ponto em `LateUpdate`, depois da animação do corpo, evitando agarrá-lo pelo lado do recipiente.

O personagem importado olha na direção `-transform.forward`, devido à conversão de coordenadas da referência. A pose usa `ReferenceSpace.Euler`. Ao regar, o corpo se orienta para o cultivo; a imagem espelha conforme a direção projetada na câmera e inclina 24 graus. As gotas partem do bico transformado junto com o regador. Ao substituir a imagem, revise **pivô, escala e posição do bico**, não apenas uma rotação global. O sprite acompanha a câmera; um modelo 3D futuro deverá substituir somente a apresentação e manter o domínio intacto.

Assets e prompts: [WATERING_CAN_ART.md](../../Resources/ARES/UI/Greenhouse/WATERING_CAN_ART.md). O [alfabeto de 52 caracteres](../../Resources/ARES/UI/CartoonAlphabet/README.md) é usado por `UI/CartoonText` no título do HUD. Preserve as fontes comuns nos textos pequenos e parágrafos.

## Verificação

Para corrigir apenas coleta/devolução e visibilidade, use `Temp/ares-greenhouse-tools-check.request`; resultado em `Logs/ares-greenhouse-tools.txt`. Essa verificação cobre E antes do planejamento, devolução/recoleta, conservação da água, prioridade no abastecimento cheio e falsos positivos da geometria. Não abre tarefas nem percorre a campanha. Execute as verificações amplas abaixo apenas quando a mudança atingir suas regras.

Com BaseARES aberta no editor, execute uma solicitação por vez:

```powershell
Set-Content Temp/ares-watering-check.request 'verificar rega manual'
```

`ManualWateringChecks` cobre as três doses, conservação/transferência, interrupções, bloqueio de menu, prevenção de duplicatas, sobra carregada e os 52 caracteres. Em Play, usa teclado/mouse virtuais para pegar, abastecer, preparar um plano e regar as quatro áreas. Verifica alinhamento da alça, captura do clique pela rega e saldo exato. Resultado: `Logs/ares-watering.txt`. Capturas: `Logs/ares-watering-{grip,action,complete}.png` e `Logs/ares-cartoon-alphabet.png`.

`Temp/ares-greenhouse-check.request` verifica regras, narração, clima, insuficiência, retomada e recompensas. `Temp/ares-ui-check.request` verifica a campanha de cinco Sols; suas soluções são fornecidas por fixtures e não comprovam uma caminhada manual completa. Ambas passaram, assim como a rega manual. As capturas de alfabeto, empunhadura e rega foram inspecionadas. Não foi gerada uma build standalone nesta etapa.

