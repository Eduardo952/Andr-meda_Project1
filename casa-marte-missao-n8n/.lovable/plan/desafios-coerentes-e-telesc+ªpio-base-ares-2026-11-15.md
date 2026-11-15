# Desafios coerentes e telescópio — Base ARES

## Preservar o que funciona
- Manter a base, câmera, missões de cinco Sóis, uma tarefa obrigatória por setor, distribuição de 100 kWh, pontuação, recompensas e selagem final.
- Reaproveitar os desafios, imagens, consultas NASA e narrador existentes. Sem recriar o jogo nem alterar fórmulas de energia, dano ou vitória.
- Código em inglês; textos próprios em inglês e português nos arquivos de tradução existentes.

## 1. Desafios por setor
**Antes:** alguns desafios são abstratos; a estufa tem apenas uma variação; dados e importância da ação nem sempre ficam claros.

**Depois:** cada setor terá pelo menos duas variações contextualizadas, mantendo atividades úteis já existentes.

| Setor | Variações e controles | Origem dos dados |
|---|---|---|
| Parque Solar | Limpar poeira por arraste/toque; comparar geração disponível e demanda em barras e distribuir blocos; interpretar direção predominante do vento numa rosa dos ventos | Orçamento e demanda atuais = REGRA DO JOGO; direção = InSight WD já carregado; poeira continua regra, sem inventar geração medida |
| Suporte de Vida | Ajustar aquecimento pela temperatura externa; interpretar a barra atual de oxigênio e priorizar ar/água/calor numa associação ou decisão | InSight AT; oxigênio e prioridades = estado/regras atuais do jogo, não medição NASA |
| Plantação | Refinar cuidado da horta com irrigação seletiva; nova distribuição de água entre canteiros com umidades diferentes | Umidade e água disponível não existem nos dados NASA carregados: valores de treino identificados como REGRA DO JOGO, sem mudar recursos da campanha |
| Escudo Magnético | Calibrar bobinas e conectar circuitos contextualizados pelo alerta; ajustar velocidade de CME e interpretar sua previsão para Marte | DONKI FLR, CMEAnalysis e WSA+Enlil existentes; calibração e circuitos = regras; tempo calculado mantém nota de velocidade constante |
| Observatório | Associar imagem à descrição de sua fonte; identificar observação e distinguir imagem do acervo de evento solar que tem previsão para Marte | Imagem/título/descrição NASA existentes e DONKI do Sol, sem deduzir impactos apenas pela aparência da imagem |
| Controle | Manter planejamento de energia e selagem com os três valores confirmados; aplicar o mesmo acabamento escuro | Distribuição e valores atuais da campanha |

As novas variações entram na seleção determinística por Sol. A exigência continua sendo uma tarefa por setor, não todas as variações.

### Briefing e feedback
- Antes da interação, três linhas: **o que acontece**, **o que fazer**, **por que importa**; botão para iniciar a tentativa. O cronômetro começa após o briefing, não durante leitura ou carregamento.
- Erro: mensagem específica sobre a diferença entre a escolha e os dados, ícone e som existentes; permitir corrigir ou reiniciar sem dano.
- Acerto: aprendizado curto e consequência visual honesta, mostrando as barras atuais e a recompensa já existente. Confirmar em Continuar aplica a recompensa uma única vez.
- Se a barra já estiver no máximo, mostrar que foi mantida, não alegar aumento inexistente. Não aumentar energia nem criar efeitos econômicos novos.
- Dados ausentes usam o fallback existente, explicitamente identificado como treino/snapshot. Os arquivos atuais de campanha são amostras; nunca serão apresentados como novas observações reais.

## 2. Console espacial escuro
**Antes:** cabeçalho escuro, mas corpo e cartões claros; rodapé e explicação de erro não são uniformes.

**Depois:** cabeçalho com ícone e nome do setor, faixa de objetivo, dados, interação e rodapé com dica, Tentar de novo e Continuar.
- Superfícies escuras com cores semânticas da identidade atual, texto claro e destaque do setor; sem fundos brancos nos desafios.
- Controles com pelo menos 56 px, mouse/toque/teclado, foco visível e leitura confortável no celular.
- Feedback imediato com ícone, cor e som; animações curtas respeitando Menos movimento e Som desligado.
- Manter carregamento leve por tarefa e pausa dos relógios durante os desafios.

## 3. Telescópio e observação
**Antes:** pedestal com tubo simples; astronauta se inclina onde está; lente não tem zoom nem pergunta posterior.

**Depois:** pedestal, montagem em forquilha, tubo comprido, lente frontal, buscador e ocular alinhados à janela do domo, na posição atual.
- Aproximação mostra Visualizar imagem.
- Ao ativar, o astronauta anda até a posição da ocular com o movimento e os limites de caminhada existentes, vira e se inclina. Abrir a lente apenas após chegar; cancelar restaura os controles.
- Lente circular escura com deriva discreta, ajuste de zoom acessível e título/crédito discretos. Zoom inicial prioriza a imagem inteira; respeitar Menos movimento.
- Após observar, uma pergunta curta ligada à imagem e à fonte. A resposta sobre efeito na base depende dos dados DONKI, nunca de uma fotografia isolada.
- Essa pergunta de exploração não concede recompensa extra nem encerra automaticamente a tarefa obrigatória: a atividade do Observatório continua no fluxo de missões existente.
- Fechar, trocar de Sol ou sair do jogo limpa observação e animação, sem deixar o personagem travado.

## Decisões adotadas
- Umidade dos canteiros, água de treino e geração do parque não estão disponíveis como sensores reais: usar e identificar regras de treino, sem acrescentar serviços ou inventar medições.
- Consequências exibem as recompensas atuais, não mudanças nas fórmulas.
- Nenhuma pergunta adicional é necessária para estas decisões; o plano pode ser ajustado antes da implementação.

## Detalhes técnicos
- Manter assignments/registry e carregamento lazy; atualizar testes para pelo menos duas variações por setor e atribuições reproduzíveis.
- Contexto e números dos desafios em módulos puros com origens registradas; estado da campanha continua sendo o único responsável por recompensas.
- Compartilhar o acabamento e briefing no MiniFrame; reutilizar controles e sons existentes.
- Coordenadas físicas e aproximação do telescópio permanecem na configuração e simulação existentes; pose visual não altera colisão ou recursos.
- Reutilizar as mesmas respostas e cache das imagens NASA; nenhuma mudança no serviço n8n.

## Verificação
- Testes de variantes, atribuição por Sol, números/origens, fallback, regras de recompensa sem duplicação, traduções e aproximação da ocular.
- Concluir um Sol pelo fluxo real: energia, tarefas nos setores e selagem no Controle.
- Computador e celular emulado: briefing, erro explicado, tentativa novamente, sucesso, zoom, pergunta de observação, cancelamento e retorno ao movimento.
- Conferir cenas não vazias, controles sem sobreposição e contagem de chamadas/triângulos antes/depois. Som, sensação e desempenho em celular físico ficam para conferência no aparelho, pois o ambiente de teste não os reproduz.