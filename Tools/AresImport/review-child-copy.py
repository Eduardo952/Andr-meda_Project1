"""Rebuild the reviewed PT-BR layer without editing the reference export.
Short labels stay; technical explanations belong in optional details. Preserve placeholders.
"""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[2]
ui = root / 'Assets/ARES/Resources/ARES/UI'
entries = {}
for suffix in ('', '.unity'):
    entries.update({x['key']: x['value'] for x in json.loads((ui / f'pt-BR{suffix}.json').read_text(encoding='utf-8-sig'))['entries']})

# Explicit editorial choices, kept next to the generator for future writers.
copy = '''
title.tagline|Sua casa em Marte por 5 dias
hud.integrity|Base
hud.oxygen|Oxigênio
hud.dose|Radiação
hud.energy|Bateria
hud.doseDetail|Radiação recebida pela equipe. Menos é melhor. A escala de 0 a 100 é do jogo, não uma medida médica.
data.barsNote|Base: estado dos equipamentos. Oxigênio: ar guardado. Radiação: quanto a equipe recebeu no jogo. Bateria: energia guardada.
howTo.move|WASD ou setas: andar.
howTo.use|Chegue perto de um equipamento e aperte E. Planeje a energia no computador.
howTo.solar|Pegue a vassoura com E. Chegue perto de um painel sujo e segure o clique para limpar. Painéis limpos geram mais energia.
howTo.sprint|Segure Shift para correr.
howTo.bubble|A Bubble ajuda quando você precisar.
bubble.room.greenhouse|As plantas precisam de água e luz. Vamos cuidar delas!
bubble.room.solar|Limpe a poeira para os painéis captarem mais luz.
bubble.room.shield|O abrigo tem paredes reforçadas. O escudo magnético é uma ideia em estudo, usada aqui como fantasia científica.
bubble.room.lifeSupport|Aqui fazemos oxigênio e limpamos o ar que respiramos.
bubble.plan|Vá ao computador e divida a energia entre os equipamentos.
bubble.report|Um novo dia em Marte! Vamos ver o plano?
bubble.sumBad|Faltou energia em alguns equipamentos. Amanhã podemos ajustar.
bubble.alarm.sep|Alerta de radiação! Vá ao abrigo!
bubble.alarm.cme|Uma nuvem do Sol pode chegar! Vá ao abrigo!
bubble.alarm.flare|Alerta no Sol! Vá ao abrigo!
sys.life|Ar e calor
rooms.lifeSupport|Ar e calor
areas.lifeSupport|Ar e calor
rooms.airlock|Porta de saída
areas.airlock|Porta de saída
report.sample|SIMULAÇÃO DO JOGO
report.noReading|Sem medição
report.noCme|Sem nuvem solar registrada
report.cme|Nuvem de partículas do Sol (CME)
report.sep|Partículas rápidas do Sol
report.sepYes|Partículas rápidas detectadas!
report.flare|Explosão de luz no Sol
report.weatherWhere|Medição da sonda InSight em outro lugar de Marte.
report.fallback|Sem medição: usamos uma temperatura de treino.
report.dateNote|O dia {{n}} do jogo usa registros de {{date}}. É uma história de treino, não o clima de agora.
info.api|Serviço de dados
info.endpoint|Endereço da fonte
info.field|Nome na fonte
info.captured|Quando foi salvo (UTC)
info.snapshot|Registro salvo
info.yes|Sim
info.sample|Valor de treino
energy.title|Dividir energia
energy.pool|Para dividir: {{n}} kWh
energy.free|Ainda sem destino
energy.need|Precisa de {{n}}
energy.start|Começar o dia
energy.hint|Divida toda a energia para começar.
energy.budgetNote|kWh mede energia. Os 100 kWh deste plano são uma regra do jogo.
energy.dustNote|A poeira tira {{loss}}% da energia do próximo dia. Limpe os painéis.
tasks.badgeReal|ARQUIVO REAL: {{src}}
tasks.sampleNote|Valores de treino, não medições da NASA.
tasks.T03.name|A viagem da nuvem solar
tasks.T03.goal|Ajuste a velocidade da nuvem e veja o tempo de viagem.
tasks.T04.goal|A previsão diz que a nuvem chega a Marte?
tasks.T06.name|Notícias da Terra
tasks.T10.sampleArt|Desenho do jogo, não uma foto da NASA.
tasks.T14.goal|Olhe o solo antes de regar.
alarm.title|Alerta do Sol!
summary.integrity|Estado da base
summary.dose|Radiação (escala do jogo)
summary.surplus|Sobrou aqui
summary.sparks|Faltou energia no escudo do jogo. Os equipamentos sofreram danos.
consequences.simulated|SIMULAÇÃO DO JOGO
consequences.allClear|Equipamentos prontos de novo!
consequences.storm.title|Poeira chegando!
consequences.storm.cause|Vento do treino: {{wind}}. Esta tempestade foi criada para o jogo.
consequences.storm.effect|A poeira cobre os painéis. Limpe primeiro os mais sujos.
consequences.events.shield-deficit.title|Faltou energia no escudo
consequences.events.shield-deficit.cause|Escudo: {{allocated}} de {{demand}} kWh. Faltaram {{missing}} kWh. Registro solar: {{flare}}.
consequences.events.shield-deficit.effect|Radiação: +{{dose}}. Fora do abrigo: +{{penalty}}. Estado da base: {{integrity}}. São efeitos do jogo.
consequences.events.life-deficit.cause|Ar e calor: {{allocated}} de {{demand}} kWh. Registro de temperatura: {{temp}} °C.
consequences.events.life-deficit.causeNoTemp|Ar e calor: {{allocated}} de {{demand}} kWh. Sem medição, usamos uma temperatura de treino.
consequences.events.life-deficit.effect|A base esfriou. No jogo, isso reduz o oxigênio: {{oxygen}}.
consequences.events.greenhouse-deficit.cause|Estufa: {{allocated}} de {{demand}} kWh. Faltaram {{missing}} kWh.
consequences.events.greenhouse-deficit.effect|As luzes enfraqueceram. As plantas sofrem. Efeito no oxigênio do jogo: {{oxygen}}.
consequences.events.solar-dust.cause|Vento de treino: {{wind}}. Poeira nos painéis: {{dust}}%.
consequences.events.solar-dust.effect|Próximo dia: {{loss}}% menos energia. O limite de 20% é uma regra do jogo.
consequences.events.solar-cleaned.cause|Painéis limpos! Poeira restante: {{dust}}%.
consequences.events.solar-cleaned.effect|Perda de energia no próximo dia: {{loss}}%.
consequences.events.energy-surplus.cause|Este equipamento recebeu mais energia do que precisava.
consequences.events.energy-surplus.effect|A sobra poderia ajudar outro equipamento.
consequences.events.perfect-sol.cause|Todos receberam a energia necessária!
consequences.events.perfect-sol.effect|Recompensa do jogo: +{{integrity}} para a base e +{{oxygen}} de oxigênio.
gameover.evacuated|A equipe saiu em segurança. Podemos tentar outro plano!
gameover.radiation.body|A barra de radiação chegou ao limite do jogo. Dê energia ao escudo e vá ao abrigo durante os alertas.
gameover.suffocation.body|O ar acabou. Confira a água, a energia e a produção de oxigênio antes de tentar de novo.
gameover.powerCollapse.title|A base precisa de reparos
gameover.powerCollapse.body|A base chegou a zero. A equipe saiu em segurança. Cuide dos equipamentos e do plano de energia.
cert.stats|Base {{i}}% · Oxigênio {{o}}% · Radiação {{d}} na escala do jogo
narrator.cme|Uma nuvem do Sol viaja a {{v}} km/s.
narrator.cmeNoImpact|A nuvem viaja a {{v}} km/s, mas a previsão não indica chegada a Marte.
narrator.dustStorm|Vento de treino: {{v}}. Limpe a poeira dos painéis.
narrator.consequences.shield-deficit|Escudo: {{a}} de {{d}} kWh. A radiação subiu {{dose}} na escala do jogo.
narrator.consequences.life-deficit|Ar e calor: {{a}} de {{d}} kWh. A base esfriou.
narrator.consequences.greenhouse-deficit|Estufa: {{a}} de {{d}} kWh. As plantas ficaram sem energia.
narrator.summary.reveal|O plano necessário era: escudo {{s}}, ar e calor {{l}}, estufa {{g}} kWh.
observatory.unavailable|Sem foto para este registro.
observatory.archiveNote|Foto real do arquivo NASA. Não é uma imagem de hoje.
refine.nasaMore|APOD reúne imagens do espaço. A biblioteca NASA guarda fotos das missões. Veja a fonte de cada imagem.
garden.cosmetic|Colher é uma brincadeira: não muda a energia ou as recompensas.
mini.practice|Treino com outro registro.
mini.practiceLearned|Você treinou com outro registro. Ele não mostra o clima deste dia do jogo.
mini.rulesNote|Alvos, tempo e estrelas são regras do jogo.
mini.constantSpeed|Conta de treino: a velocidade não muda. Não é uma previsão de chegada.
mini.returnControl|Volte ao computador.
mini.sealTitle|Conferir o plano
mini.sealGoal|Copie o plano de energia que você escolheu.
mini.seal|Confirmar plano
mini.SortFlares.goal|Toque da mais fraca para a mais forte.
mini.SortFlares.learned|O registro mostra uma explosão solar {{value}}.
mini.SortFlares.instruction|Toque nas cartas para colocá-las em ordem.
mini.SortFlares.readingHint|A letra indica a força. O número dá mais detalhes.
mini.PhotoDetective.learned|Você comparou a imagem com a legenda: {{value}}.
mini.CmeCountdown.name|Viagem da nuvem solar
mini.CmeCountdown.goal|Copie a velocidade e escolha o tempo de viagem.
mini.CmeCountdown.learned|A nuvem solar do registro viajava a {{value}} km/s.
mini.CmeCountdown.reading|Velocidade registrada
mini.CmeCountdown.arrival|Quanto tempo a viagem levaria? Escolha em horas.
mini.PipeFlow.name|Ligar os canos
mini.PipeFlow.goal|Gire os canos e ligue o caminho até os tanques.
mini.PipeFlow.instruction|Cubra a linha pontilhada com o cano.
mini.PipeFlow.keepGoing|Ainda há canos fora do caminho.
mini.ThermostatDial.name|Aquecer a base
mini.ThermostatDial.goal|Ajuste o aquecedor para o valor mostrado.
mini.ThermostatDial.learned|A sonda InSight mediu {{value}} °C em outro lugar de Marte.
mini.SmartFarmCare.name|Cuidar da horta
mini.SmartFarmCare.learned|Água na medida e luz ajudam as plantas a crescer.
mini.CleanPanels.name|Limpar os painéis
mini.CleanPanels.goal|Limpe primeiro o conjunto com mais poeira.
mini.CleanPanels.instruction|Toque na escova destacada para limpar.
mini.CleanPanels.alreadyClean|Tudo limpo! Veja quanta energia voltou.
mini.IrrigationPlan.goal|Complete cada canteiro até a marca. Economize gotas!
mini.ResourcePriority.name|O que cuidar primeiro?
mini.ResourcePriority.goal|Confira o ar. Depois compare o calor e a estufa.
mini.PowerBalance.goal|A energia dá para todos os equipamentos?
mini.PowerBalance.learned|Quando a energia é pouca, precisamos escolher onde usar.
mini.WindReading.goal|Toque na direção mostrada na bússola.
mini.WindReading.learned|O vento foi medido em outro lugar de Marte.
challenge.available|Temos
challenge.demand|Precisamos
challenge.matches|Dá para todos
challenge.shortfall|Vai faltar energia
challenge.air|Cuidar do ar
challenge.heat|Aquecer a base
challenge.water|Cuidar da água
challenge.rewardNote|Aperte Continuar para guardar a recompensa.
challenge.impactQuestion|Só a imagem prova que a tempestade chegará a Marte?
challenge.impactAnswer|Não. Precisamos de uma previsão para Marte.
challenge.dataNotes.flare|Registro de explosão solar. Sem dados, usamos um treino identificado.
challenge.dataNotes.wind|Vento medido pela InSight. Sem medição, usamos um treino identificado.
challenge.briefings.AlignCoils.happening|Os ponteiros precisam de ajuste.
challenge.briefings.WireShield.happening|Alguns fios estão soltos.
challenge.briefings.CmeCountdown.happening|Uma nuvem de partículas saiu do Sol.
challenge.briefings.ThermostatDial.happening|O frio faz a base perder calor.
challenge.briefings.ThermostatDial.hint|Copie o valor de energia do aquecedor.
challenge.briefings.ThermostatDial.error|Confira o valor pedido para o aquecedor.
challenge.briefings.CleanPanels.happening|A poeira impede parte da luz de chegar aos painéis.
challenge.briefings.TiltSun.importance|Virar os painéis ajuda a captar luz. O alvo é uma regra do jogo.
challenge.briefings.IrrigationPlan.hint|Complete as marcas de cada canteiro com as gotas disponíveis.
challenge.briefings.ResourcePriority.hint|Ar abaixo do máximo? Cuide dele. Senão, escolha entre calor e água para a estufa pelo maior valor mostrado.
challenge.briefings.ResourcePriority.error|Veja primeiro o ar. Depois compare calor e estufa.
challenge.briefings.PowerBalance.hint|Compare o que temos com o total que precisamos.
challenge.briefings.PowerBalance.error|Some os três equipamentos antes de comparar.
challenge.briefings.WindReading.happening|A sonda registrou de onde o vento veio.
challenge.briefings.WindReading.hint|Escolha a direção mostrada.
challenge.briefings.WindReading.error|Confira a direção da bússola.
solar.critical|Bateria abaixo de {{threshold}} kWh. Limpe os painéis para recuperar energia.
solar.generation|Produzindo {{n}} kWh a cada {{seconds}} s · {{dirty}} painéis sujos
solar.enterZone|Chegue perto de um painel com poeira.
solar.clean|Painel {{n}} limpo! Procure outro com poeira.
solar.return|[E] Guardar vassoura
station.needsPlan|Divida a energia no computador.
station.completed|Tarefa pronta por hoje!
station.approach|Chegue perto de um equipamento.
'''
overrides = dict(line.split('|', 1) for line in copy.strip().splitlines())
unknown = set(overrides) - set(entries)
assert not unknown, unknown
reviewed = {}
for key, original in entries.items():
    value = overrides.get(key, original)
    # Replace jargon only in prose; do not rename the Sun or machine data keys.
    value = value.replace('snapshot', 'registro salvo').replace('Snapshot', 'Registro salvo')
    value = value.replace('Sol {{', 'Dia {{').replace('Sol seguinte', 'dia seguinte').replace('próximo Sol', 'próximo dia').replace('neste Sol', 'neste dia').replace('do Sol {{', 'do dia {{').replace('5 Sóis', '5 dias em Marte')
    assert set(re.findall(r'{{.*?}}', value)) == set(re.findall(r'{{.*?}}', original)), key
    reviewed[key] = value
(ui / 'pt-BR.child.json').write_text(json.dumps({'entries': [{'key': k, 'value': v} for k,v in reviewed.items()]}, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print(f'Reviewed {len(reviewed)} keys; changed {sum(v != entries[k] for k,v in reviewed.items())}; placeholders intact.')
