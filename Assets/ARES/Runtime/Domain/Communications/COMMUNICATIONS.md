# Comunicação — Casa Marte

Central local de mensagens educativas e relatórios dos cinco dias. Não há chat externo, conta, envio à NASA ou serviço de IA. A interface fica na terceira página do computador central; o encerramento do dia abre a revisão do relatório pela mesma interface.

## Arquitetura

| Módulo | Responsabilidade |
| --- | --- |
| `CommunicationsRules` | Conteúdo diário, custos de jogo, atraso reduzido e etapas de reparo |
| `CommunicationsSystem` | Mensagens, entregas, antena, rascunhos/relatórios enviados e histórico |
| `DailyBaseSnapshot` | Cópia imutável dos dados no fechamento do dia |
| `Campaign` | Débito de energia compartilhada, criação do relatório e bloqueio de `NextSol` |
| `UI/CommunicationsView` | Mensagens, respostas, reparo e histórico de relatórios |
| `UI/DailyReportView` | Exibição dos quatro sistemas a partir do snapshot |
| `Infrastructure/CommunicationsArchive` | Arquivo JSON local versionado por missão, com cópia de segurança |

Os diretórios de UI e infraestrutura são relativos a `Runtime`, não a esta pasta. Regras não dependem de Unity, disco ou rede. Só `Campaign.ReplyToEarth` pode enfileirar uma resposta e debitar a bateria, evitando consumo parcial quando falta energia.

## Estados e fluxo

1. `BeginDay` agenda uma orientação. Ela só fica legível depois de chegar.
2. Cada trecho demora 12 segundos de jogo. Pedir ajuda exige a viagem de ida e a de volta; a orientação não aparece instantaneamente.
3. Mensagens recebidas podem ter uma resposta curta ou pedido de orientação. Uma resposta por mensagem impede duplicação acidental.
4. Uma erupção solar anunciada pela campanha danifica a antena no impacto, mesmo quando o escudo foi regulado. Não existe pane automática ao iniciar o terceiro dia. Para recuperar a conexão, o jogador verifica o cabo, alinha a antena e testa o sinal; são três ações de dois segundos. A manutenção também funciona no fechamento do dia.
5. Sem conexão, novos envios ficam bloqueados. Sinais já enviados continuam viajando; novas mensagens recebidas aguardam a recuperação da antena.
6. `CompleteSol` captura plantas, água no reservatório/regador, consumo diário, energia armazenada, painéis sujos, proteção, índice de exposição e condições ambientais da base. Também preserva o evento que atingiu o dia, a regulagem do escudo, os painéis ainda soterrados e a conexão da antena.
7. `SendDailyReport` só aceita o relatório do dia em `Summary`, uma única vez e com antena conectada. `NextSol` exige envio, inclusive no quinto dia e no caminho para o fim da missão. Não é preciso esperar a confirmação da Terra para avançar.
8. A pausa explícita congela o rádio. Abrir um menu não interrompe a viagem do sinal. No resumo, o rádio continua funcionando mesmo com o relógio do dia encerrado.

## Escolhas e energia

Resposta curta: 0,2 kWh da bateria da base. Pedido de orientação: 0,5 kWh e uma mensagem de ajuda após a ida e a volta. Essa energia deixa de estar disponível para os demais sistemas, incluindo o oxigênio. São valores de balanceamento educativo, não especificações de antenas reais.

Os relatórios usam uma bateria dedicada inicial de 1 kWh, inacessível às conversas opcionais: cinco envios de 0,2 kWh. Essa reserva finita torna visível a prioridade de comunicação essencial e evita bloquear para sempre uma missão cuja bateria geral esteja vazia. A reserva é própria do rádio, não uma retirada oculta ou recarga da bateria solar. Cliques repetidos não gastam energia adicional.

## Dormitório e limites dos dados

O projeto ainda não mede sono nem possui um sistema dedicado de descanso. O relatório NÃO inventa horas dormidas. Ele mostra uma avaliação educativa do ambiente, baseada nos dados existentes: oxigênio ≥20%, integridade ≥50% e energia disponível ≥1 kWh. Esses limites são regras de jogo, não critérios médicos ou de habitabilidade da NASA. A interface informa essa limitação junto do resultado.

O escudo usa a alocação e o resultado do sistema existente. A exposição é chamada de **índice do jogo**, não dose em mSv. O snapshot é capturado após as consequências diárias; não muda ao abrir o histórico ou iniciar outro dia. Para integrar um futuro dormitório real, substitua a origem desses campos em `DailyBaseSnapshot`, preservando os relatórios anteriores.

## Persistência

Cada missão recebe um identificador próprio. O histórico em memória é preservado nos cinco dias; um JSON é atualizado quando mensagens/relatórios mudam. No Editor: `UserSettings/Communications/mission-<id>.json`. No jogo compilado: `Application.persistentDataPath/Communications`. Missões novas não sobrescrevem arquivos de missões anteriores. O arquivo inclui versão, mensagens, situação de entrega, créditos de energia e snapshots completos. Erros de gravação são informados na tela; o histórico da sessão continua disponível.

Esse arquivo é um **arquivo de correspondência**, não um salvamento completo da campanha. O menu mostra a missão em andamento; arquivos de missões anteriores permanecem no disco para inspeção, sem prometer retomada de uma campanha ou importação pelo menu. Nenhuma credencial ou mensagem real é armazenada.

## Base científica

- [NASA — Space Communications: 7 Things You Need to Know](https://www.nasa.gov/centers-and-facilities/goddard/space-communications-7-things-you-need-to-know/)
- [NASA — How an Atomic Clock Will Get Humans to Mars on Time](https://www.nasa.gov/missions/tech-demonstration/deep-space-atomic-clock/how-an-atomic-clock-will-get-humans-to-mars-on-time/)

A DSN participa da comunicação com missões distantes; a duração do sinal depende da distância entre os planetas. A faixa aproximada de 4–24 minutos por trecho é apresentada como contexto real. Não há consulta de distância atual nem simulação orbital. O atraso de 12 segundos, as mensagens, os custos e a falha no terceiro dia são explicitamente educativos. O jogo não é endossado pela NASA.

## Verificação

Menu `ARES > Checks > Communications and daily reports only`. Confere custos, insuficiência de energia, duplicação, atraso de ida/volta, pausa, antena, relatórios imutáveis, bloqueio dos cinco dias, serialização e gravação local. Na apresentação, abre apenas o computador e telas de comunicação/resumo. Relatório: `Logs/ares-communications.txt`; capturas `ares-communications-computer.png` e `ares-communications-report.png`.
