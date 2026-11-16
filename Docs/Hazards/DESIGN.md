# Eventos ambientais — desenho e limites

Implementação de 16/11/2026. A pedido do proprietário, a sessão foi limitada a uma hora a partir de 00:15:41 UTC, com encerramento até 01:15:41 UTC. Este documento distingue código implementado de aceitação visual e operacional ainda pendente.

## Origem dos eventos

| Evento | Campo necessário | Regra | Procedência |
|---|---|---|---|
| Erupção | `facts.flare.class/magnitude` | C e abaixo: informação; M: médio; X: alto | Real |
| CME | `facts.cme.speed_kms` + `facts.mars_impact.level` | <500, 500–1000, >1000 km/s; efeito danoso somente direto/raspão | Real |
| Radiação | classe X ou impacto direto em Marte | Alternativa autorizada ao SEP; dose do jogo, não dosimetria NASA | Derived |
| Poeira | risco medium/high, panel_factor válido <1, Ls válido, resposta não fallback | Derivada da estação/seed; direção e velocidade visuais simuladas | Derived; vento Simulated |
| Frio | `facts.weather.temp_avg_c` | média ≤−65, ≤−80, ≤−95 °C | Real |
| Hipóxia | oxigênio do tanque existente | Atenção ≤40%; alerta ≤25%; colapso a zero após aviso | Telemetria simulada |
| Falha elétrica | choque não absorvido pela blindagem/reserva | Desliga primeiro estufa, depois suporte à vida; reparo ou expiração | Telemetria simulada |

Campos ausentes não ativam eventos. APOD, fotos e o índice do catálogo nunca são evidência suficiente de um evento. Não há vento Curiosity inventado. O endpoint histórico InSight respondeu com HWS e WD em sete Sols; não foi combinado automaticamente com Curiosity, pois representa outro local/período. SEP e notifications não foram acrescentados.

O catálogo contém observações públicas verificadas e não contém chaves. O construtor usa janelas de 120 dias, cache retomável e shortlist anual; o diretor reconsulta os candidatos pelo serviço NASA existente, verifica data e Sol Curiosity, evita dias/famílias repetidos e pode rejeitar sequências pelo estimador de viabilidade. O perfil em `Resources/ARES/Hazards/ScenarioProfile.asset` é editável. Curated é explicitamente uma seleção de cinco dias reais não consecutivos; Consecutive preserva a alternativa. Há 12 sequências de observações reais salvas para offline. Após 60 s sem concluir a preparação online, utiliza-se esse pacote, identificado como snapshot.

## Energia e exposição

- O relógio Unity existente continua com **600 segundos ativos/Sol**. Os 480 s do protótipo não substituíram esse comportamento.
- A fórmula original `Rules.Shield` continua intacta, inclusive limite 75. `Rules.Thermal`, passos de 5 e margem de 5 também foram preservados.
- Geração: `roundTo5(100 × max(solarFloor,panel_factor) − min(20,poeiraMédiaPercentual × .2))`. O parque e a bateria são os mesmos objetos `SolarPlant`; não há bateria paralela. A reserva nova é configurada em 30/10 no modo NASA; os fixtures legados continuam usando 160/160.
- Reserva cobre déficits, na ordem escudo → suporte à vida → estufa, e é debitada ao confirmar o Sol. Excedentes acima da margem podem recarregá-la. Choques e respostas de rádio usam essa reserva física existente.
- Poeira realça o desgaste dos 32 painéis existentes, e o regador/irrigação permanecem na implementação anterior. A limpeza do parque altera a geração real.
- O modelo de massa de oxigênio permanece em kg, com água, CO₂, consumo e eletrólise. Seu fornecimento nominal é normalizado ao orçamento pedagógico por Sol; a energia física por ciclo não é debitada uma segunda vez da bateria. Essa normalização precisa de aprovação de design: game-kWh não é uma escala física única entre todos os subsistemas.
- Penalidade de alocação: `I=missingShield`; `O=missingLife + .5×missingGreenhouse`; `Dose=.8×missingShield + 35 se abrigo perdido`. O ledger distribui a penalidade no tempo e contabiliza somente o restante ao confirmar. Replanejamento pode devolver a parcela de exposição de alocação, para que a alocação final permaneça autoritativa. Exposição ambiental adicional tem ledger e limites separados.
- Perda de produção de oxigênio durante blackout é contabilizada pelo tanque uma vez; a contabilidade adicional limita esse efeito sem subtrair novamente a mesma perda.
- Abrigo protege 90% da exposição ambiental à radiação; interior com escudo suficiente protege 60%, interior insuficiente 20%. Esses fatores são simplificações provisórias.
- O fim do relógio não conclui automaticamente o Sol. É necessário cumprir objetivos e confirmar no botão **Encerrar Sol**, no computador central. Missão e exercícios continuam no ambiente principal.

## Tripulante e recuperação

Estados Nominal → Caution → Warning → Incapacitated. A contagem de 15 s só avança quando o HUD de aviso/remédio está visível e o aplicativo tem foco. Menus que ocultam o aviso não contam como exposição a esse aviso. Não há morte instantânea no caminho novo.

Kit médico reduz Dose; cilindro repõe o mesmo tanque; repouso exige cama e energia disponível, uma vez/Sol. Colapso bloqueia deslocamento, apresenta uma transição procedural para joelhos/chão e conduz ao dormitório. Recuperação restaura os valores configurados, avança o relógio, aplica lentidão até o próximo Sol e preserva a campanha. A terceira evacuação interrompe a missão com linguagem médica e ações de recuperação. A animação é procedural, pois não havia controlador Mecanim ou clipe dedicado.

## Valores novos PROVISIONAL

Todos os campos de `HazardBalance` são provisórios e editáveis pelo ScenarioProfile. Não são constantes científicas NASA.

| Campos | Valores padrão | Motivo / unidade |
|---|---|---|
| solarFloor | .8 | Preserva um caminho de sobrevivência com a fórmula original |
| coldCaution / coldWarning / coldEmergency | −65 / −80 / −95 | °C da média diária, não mínima |
| greenhouseDustMedium / High | 5 / 10 | game-kWh adicional |
| warningGrace / shelterDeadline | 15 / 60 | segundos para ler/reagir |
| eventDuration | 90 | segundos de janela pedagógica |
| commMediumSeconds / High | 45 / 75 | duração de interferência |
| batteryCapacity / Start | 30 / 10 | game-kWh, mesma bateria |
| surgeMedium / High / cmeShockMax | 4 / 8 / 10 | game-kWh por choque |
| blackoutOxygenCap / blackoutIntegrityCap | 12 / 8 | perda adicional máxima por Sol |
| outdoorDoseCap / coldIntegrityCap | 20 / 6 | teto adicional; frio usa principalmente ledger térmico, sem dano estrutural separado atualmente |
| cautionDose / warningDose | 50 / 75 | porcentagem pedagógica |
| cautionOxygen / warningOxygen | 40 / 25 | porcentagem do tanque |
| warningSpeed / treatedSpeed | .75 / .85 | multiplicadores de velocidade |
| treatedDose / Oxygen / Integrity | 60 / 60 / 45 | estado de recuperação; integridade é um mínimo |
| medKits / oxygenCanisters / maxMedicalEvacuations | 2 / 2 / 3 | estoque limitado; Sol ideal repõe um kit |
| medKitDose / canisterOxygen | 25 / 25 | pontos por uso |
| restSeconds / restDose / restOxygen / restEnergy | 60 / 25 / 20 / 5 | repouso real, recursos, game-kWh |
| rescueSeconds | 75 | custo no relógio da missão |
| collapseAnimationSeconds / rescueAnimationSeconds | 4 / 5 | duração da apresentação |
| pulseHz | 1 | reservado; apresentação atual evita pulsação luminosa |
| dustDepositMedium / High | .35 / .55 | fração de sujeira adicionada |
| lowCmeSpeed / highCmeSpeed | 500 / 1000 | km/s |
| radiationDosePerMinute | 8 | base de dose simulada/min, multiplicada por tier/intensidade/proteção |
| coldLedgerSeconds | 300 | exposição térmica acumulada para incapacidade |
| candidatesPerSlot / maxRepicksPerSlot | 10 / 20 | limite de busca; candidatesPerSlot reservado |
| catalogShortlistPerYear / catalogRequestConcurrency | 6 / 3 | custo de coleta limitado |
| minEventSeconds / latestEventSeconds | 60 / 450 | início no Sol de 600 s |
| tierDurationJitter | .15 | ±15% de duração, sem promover classe científica |
| scenarioWaitSeconds | 60 | fallback offline para não bloquear a partida |

Também provisórios: poeira Medium preserva os parâmetros originais descritos na auditoria; High aumenta a emissão em 25% e vento visual em 20%; pool poeira 960; outros efeitos 192 partículas por tipo, emissão máxima 55/s, vida 2.2 s; redução de partículas .2 e de poeira .4 em movimento reduzido. Áudio mono 24 kHz, loops de 4 s, ganho .38, ganho interno .35 e sob interface/narrador .2; perfil `AudioProfile.asset` aceita clipes substitutos. Esses valores visuais/sonoros não alteram os eventos ou o dano.

## Limites e dívida explícita

- O filtro de viabilidade do diretor é uma estimativa conservadora; o teste de 200 campanhas usa o modelo real de Campaign, mas abstrai percurso físico e resolução humana dos exercícios.
- O conjunto visual usa pools Shuriken e a apresentação de escudo existente. Ainda não reproduz todos os efeitos artísticos pedidos: aurora em cortinas, frente radial CME, filme granular, frost completo e Volume de desaturação/blur. Não há um prefab independente por evento; `DustStormEffect` é um adaptador sobre o renderizador preservado, e não a extração completa solicitada.
- Os loops procedurais, grupo de mixer e perfil estão integrados. A equalização/faixa exata da poeira e os sons específicos de restabelecimento/colapso ainda requerem acabamento auditivo.
- Não foi adicionada uma interface de depuração no jogo para forçar cada evento; os fixtures de apresentação são exclusivos do Editor.
- A migração dos checks para Unity Test Framework/EditMode/PlayMode ainda não está pronta: utilizou-se a infraestrutura de checks do projeto mais harness .NET. Isso não deve ser apresentado como uma suíte UTF concluída.
- As capturas e médias do Editor não substituem benchmark GPU/draw calls em build final nem duas partidas humanas completas, online e offline.
