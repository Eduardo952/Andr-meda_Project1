# Interface de oxigênio — público de 7 a 14 anos

## Hierarquia

A tela principal responde: **quanto temos**, **o que está acontecendo** e **o que posso escolher**. Mostra uma porcentagem grande no tanque, duas barras comparáveis de produção/uso, uma orientação contextual e os botões Poupar/Equilibrar/Reforçar. Água, energia e filtro aparecem como estados curtos. A ação secundária permite pausar os experimentos.

Os números técnicos, totais e fontes continuam acessíveis em **Saiba mais**, com duas subseções inicialmente fechadas: **Ver os números da base** e **A ciência por trás do jogo**. Isso preserva as informações sem exigir sua leitura para tomar a primeira decisão. As barras usam a mesma escala por atualização; seus valores em g/min aparecem no tooltip e nos números detalhados.

## Responsabilidades

| Arquivo | Responsabilidade |
| --- | --- |
| `OxygenView.cs` | Composição, tanque, barras e estados de apoio |
| `OxygenGuidance.cs` | Prioridades dos avisos e orientações; nunca altera o domínio |
| `OxygenControls.cs` | Botões com imagem gerada, foco/seleção e chamadas da API do domínio |
| `OxygenDetails.cs` | Conteúdo educativo, telemetria completa e links científicos |
| `OxygenTankArt.cs` | Desenho vetorial do tanque |
| `Resources/ARES/UI/Oxygen.uss` | Estilo da tela e navegação do computador |
| `Domain/OxygenPlant.cs` | Cálculos científicos e regras, sem alteração nesta simplificação |

As orientações priorizam água esgotada, falta de energia, reserva baixa e saldo. Com tanque cheio no modo Reforçar, a sugestão é reduzir o ritmo. Com reserva baixa já se recuperando, a mensagem reconhece a recuperação; não manda acelerar um gerador que já está no máximo. O filtro com CO₂ acumulado e energia disponível é descrito como limpando o ar, sem diagnosticar falta de energia inexistente.

Mantenha frases curtas e ações existentes. Não invente um botão de recarga de água, um filtro desligável ou um tratamento médico que o domínio não oferece. Todos os controles essenciais continuam visíveis no tamanho normal; larguras menores reorganizam tanque e escolhas e mantêm rolagem disponível. Cor é reforçada por texto e estados de seleção.

## Arte e verificação focada

Botões: [arte e prompt](../../Resources/ARES/UI/Oxygen/ART_DIRECTION.md). Os textos são editáveis e não fazem parte do PNG.

Use **ARES → Checks → Oxygen screen only**, ou grave `oxygen-ui` em `Temp/ares-computer-check.request`. Esse seletor executa apenas `OxygenPresentationChecks`: layout, textura, controles e avisos. Resultado: `Logs/ares-oxygen-ui.txt`; capturas: `Logs/ares-oxygen-ui-*.png`. Não executa a campanha inteira, puzzles, rover nem a bateria de cálculos do computador.
