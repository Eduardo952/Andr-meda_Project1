# Vento e poeira marciana

`MartianWind` cria um único sistema de partículas em espaço global, com limite de 260 partículas. `ReferenceExplorer.Initialize` instala o componente depois do rover, permitindo reutilizar o collider de terreno já existente. A poeira acompanha a região de `ControlledPosition`, funcionando tanto com o astronauta quanto com o rover.

`ReferenceExplorer.windSettings` / `MartianWindSettings` configuram ativação, raio, altura, emissão, velocidade, opacidade e direção. O movimento combina deriva lateral, pequenas variações verticais e rajadas suaves; grãos pequenos são misturados a poucos véus de poeira de baixa opacidade. Não representa dados reais de vento nem altera as regras de clima ou recursos.

O shader `Resources/ARES/Effects/WindDust.shader` desenha partículas suaves, com profundidade de cenário respeitada e desaparecimento próximo da câmera. As partículas não projetam sombras. A emissão rejeita áreas internas, e partículas que entram em construções são removidas usando os mesmos limites do layout. O parque solar permanece ao ar livre. O terreno é amostrado apenas na emissão, com o collider existente; não são adicionados colliders para partículas.

Menus e sessão do computador limpam o efeito. Movimento reduzido usa 20% da emissão e 45% da velocidade. Teletransportes limpam partículas distantes. Material e objeto de partículas pertencem ao componente e são descartados com ele. A poeira das rodas e da corrida continua em seus módulos próprios.

Verificação opcional: gravar `wind-preview` em `Temp/ares-computer-check.request` atualiza os assets e executa somente `MartianWindPreview`. O resultado fica em `Logs/ares-wind.txt` e a captura em `Logs/ares-wind.png`. Verifica emissão, shader e exclusão de interiores; não dirige o rover nem abre tarefas.

O HUD não exibe mais data, hora ou contagem regressiva. O relógio de 600 segundos da campanha continua funcionando internamente.
