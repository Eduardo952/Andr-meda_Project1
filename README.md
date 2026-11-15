# Base ARES — migração para Unity

## Objetivo do usuário

Recriar **todo** o jogo da pasta `a896f420-3053-49eb-b6f2-a97f9b2dc6c8 (1)` em Unity, com fidelidade visual e funcional, abandonando a versão anterior somente após substituir suas funções. O usuário reforçou **"deixe idêntico"**, pediu código organizado/modular e este documento para outra IA continuar se a sessão acabar. Não reduzir o escopo a uma demonstração de cenário. Não afirmar que está concluído quando ainda faltam sistemas.

## Estado atual — 2026-11-14

### Continuação — personagens e câmera

- Preferência reafirmada pelo usuário: **rotação manual da câmera como no original**, código limpo/modular e fidelidade total. Não adicionar giro automático atrás do jogador.
- Implementado e reimportado no editor: extração de hierarquia local dos personagens para `Import/characters.json`, prefabs com pivôs originais, animação de caminhada/repouso/aceno do astronauta e animações internas do Bubble. A geometria estática anterior achatava os membros, causando o deslizamento sem animação relatado pelo usuário.
- Controles separados em `CameraInput`: arrastar com botão esquerdo no cenário ou direito, zoom, R para recentralizar; H fica como atalho de inspeção para tetos. C alterna a câmera isométrica, que intencionalmente tem ângulo fixo como na referência.
- Módulos novos: `ReferenceSpace`, `CharacterAnimation`, `AstronautAnimator`, `BubbleAnimator`, `CameraInput` e `Editor/CharacterImporter`. O importador agora recusa regenerar cenas com alterações não salvas.
- **Verificado no Unity**: 3.843 vértices da pose inicial contra a geometria original; 200 quadros do astronauta (repouso, caminhada, aceno, salto, observação e movimento reduzido); 128 quadros do Bubble (humores, fala, aceno, piscar e bolhas); 120 quadros de câmera; todos os checks anteriores de navegação/campanha. Os resultados esperados executam os callbacks TSX originais, não uma segunda cópia das fórmulas C#. Resultado em `Logs/ares-parity.txt`.
- **Verificado em Play**: cena renderizada com os dois personagens; arrasto manual do cenário conferido visualmente. Teste integrado com dispositivos virtuais do Input System: W deslocou 5,20 m, balanço do braço de 40,1°, oscilação do corpo de 0,179 m; arrasto esquerdo mudou yaw em -0,945 rad e R recentralizou. Resultado `PASS` em `Logs/ares-play.txt`. O teste remove os dispositivos virtuais e sai de Play automaticamente.
- Salto/observação/humores são testados como poses, mas ainda não ligados aos eventos da campanha. Poeira dos passos e áudio ainda pendentes. Touch/pinch implementados, mas não testados em dispositivo.
- Recompilar durante Play revelou perda do estado não serializado do explorador. `ReferenceExplorer` agora reinicializa esse estado se necessário. A primeira execução do teste integrado ficou pausada e expirou; a repetição passou. `PlayControlsChecks` desativa a pausa ao iniciar seus testes. Não interpretar esses registros históricos como falha da execução que passou.
- Uma atualização parcial do Unity não descobriu o arquivo novo dos testes e produziu CS0103. Foi resolvida após atualizar/renomear o arquivo para `CharacterAnimationChecks.cs` e recompilar; **não é erro corrente**. Não confundir entradas históricas do log com o resultado atual.

### Situação geral

A migração está **incompleta**. Há uma cena Unity de inspeção, geometria extraída da referência, dados de campanha e parte do domínio em C#. Ainda não há menu/HUD, minijogos ou campanha jogável integrados. A referência original permanece intacta e deve continuar disponível para comparação.

Leia [Assets/ARES/README.md](Assets/ARES/README.md) para a arquitetura e os limites de fidelidade. Código novo em `Assets/ARES`; ferramenta de extração em `Tools/AresImport/export.mjs`.

### Entregue e verificado

- Exportador local Node 26 + Three.js 0.187 + TypeScript 5.9.3 (lockfile incluído). Executa geradores visuais originais, sem executar servidor, ler `.env` ou carregar credenciais.
- 1.464 meshes extraídas: 1.428 de cenário e 36 em hierarquias articuladas de astronauta/Bubble. Inclui estrutura, interiores, corredores, estufa, parque solar, rochas/montanhas e chão.
- Sete módulos e cinco Sols, rotação das tarefas, traduções PT/EN e snapshots/proveniência importados.
- `GameDefinition` como ScriptableObject. Pisos, paredes, tetos, interiores e personagens separados no scene graph.
- `Campaign` e `Rules`: fases, orçamento, atribuições, validação de tarefa, recompensas sem duplicação, pausa e selagem no Controle. **Ainda não conectados a uma interface jogável.**
- Navegação traduzida com portas/corredores e reflexão Z.
- Cena `Assets/ARES/Scenes/BaseARES.unity` gerada e aberta no editor. Configurada em EditorBuildSettings.
- Testes executados pelo menu Unity: **9.672 pontos de navegação + 384 casos de fórmulas + 120 quadros de câmera + 328 quadros dos personagens + 3.843 vértices**, ciclo dos cinco Sols e bloqueios contra conclusão/recompensa duplicada. Resultado em `Logs/ares-parity.txt` (pasta ignorada pelo Git).
- Importação repetida com sucesso após separar assemblies e corrigir o ScriptableObject. Agora o log conta `1428 exact reference meshes` do cenário; os 36 meshes dos personagens são importados por `CharacterImporter`. Última validação: `ARES parity checks passed.`

### Verificação de Play

O primeiro teste em Play encontrou `NullReferenceException` em `ReferenceExplorer.Start` porque `GameDefinition` estava inicialmente num arquivo com outro nome e foi movido de assembly. Corrigido criando `Runtime/Data/GameDefinition.cs`, ajustando o `m_Script` do asset e **reimportando a cena inteira**. **Play foi repetido com sucesso após a correção**, com renderização da base e sem novas exceções no log; saiu de Play para continuar o desenvolvimento. Não confundir os erros históricos de `Logs/Editor.log` com erros novos. A captura mostrou cerca de 218 mil triângulos e 2.897 draw calls: otimização ainda necessária.

## Como continuar, em ordem

1. Manter os testes de paridade e de controles passando ao integrar a campanha. WASD move, arrastar o cenário gira, roda dá zoom, R recentraliza, H alterna tetos, C alterna isométrica e TAB mostra a visão geral. Não chamar isso de jogabilidade final.
2. Integrar as poses já portadas aos eventos de apresentação/campanha; portar poeira/áudio dos passos, primeira caminhada e falas. Astronauta/Bubble já têm prefabs articulados; não voltar a achatar esses modelos no exportador. Completar testes dos gestos touch e cutaway.
3. Portar menu, HUD e overlays com UI Toolkit/uGUI nativos, reutilizando textos, tokens, medidas e interações originais. Inspecionar referência em execução para comparação, não inventar novo design.
4. Portar cada um dos dez minijogos de `src/tasks/`, com regras e interações fiéis, separados em estado puro e apresentação. Integrar IDs de `src/tasks/assignments.ts` e fluxo da campanha. Não usar botão genérico de "concluir".
5. Integrar relatórios, energia, primeira caminhada, Bubble/falas, alarmes/abrigo, cinco Sols e telas finais.
6. Portar céu/ciclo solar, toon/outlines/transparência, textura procedural do terreno, animações/colheita da estufa, telescópio, áudio e dados remotos com fallback de snapshot.
7. Comparar visual e funcionalmente a versão web e Unity em cada tela/estado. Somente então remover/substituir o que for obsoleto.

## Fontes de verdade

Na pasta de referência, `src/config/rooms.ts` define layout, estações e portas; `theme.ts`, `palette.ts` e `styles.css` definem aparência; `balance.ts` contém regras de jogo. `src/game/campaign.ts` controla fases e recompensas. `src/engine` contém fórmulas. `src/game/presentation.ts` contém tour/falas. `src/tasks/rules.ts` e cada componente de tarefa contêm puzzles. `src/game/scene` e `src/game/characters` contêm os geradores visuais. Há testes originais em `src/test`.

Ler `AGENTS.md` dentro da referência antes de modificá-la. Não editar migrations ou arquivos gerados da versão web. A pasta contém `.env`: não ler nem exibir segredos; `.gitignore` agora ignora `**/.env`.

## Ferramentas e comandos

```powershell
npm.cmd ci --prefix Tools/AresImport
node Tools/AresImport/export.mjs
```

Unity instalado em `D:/Unity/6000.6.9f1/Editor/Unity.exe`. Projeto em `C:/Users/dudu-/Documents/GitHub/Andr-meda_Project1`. Editor aberto pelo usuário. Não iniciar outro Unity com o mesmo projectPath. Tentativa em lote no mesmo projeto falhou por lock; tentativa isolada em `Temp/AresValidation` não conseguiu inicializar licença e foi interrompida. Usar o editor existente.

Menu **ARES > Import reference geometry and create scene** regenera a cena (não colocar trabalho manual nela que precise sobreviver à regeneração). Menu **ARES > Validate port against reference** roda os checks.

Menu **ARES > Validate controls in Play** testa movimento/animação/câmera através de dispositivos virtuais isolados do Input System. Requer a cena BaseARES aberta, inicia e encerra Play, e escreve `Logs/ares-play.txt`. Também pode ser iniciado por `Temp/ares-play-check.request`, consumido apenas fora de Play e da compilação. Não recompilar nem usar os controles durante os poucos segundos do teste. Os testes não editam a cena nem enviam entrada ao teclado/mouse do sistema operacional.

Alternativa de automação local: escrever `Temp/ares-import.request`. O hook `Editor/ImportRequest.cs` consome o arquivo quando o editor está disponível e fora de Play, importa a cena e roda os checks. Isto substitui a cena atual e agora é recusado se alguma cena aberta tiver alterações não salvas. Para apenas validar, usar `Temp/ares-validate.request`; os testes dos personagens usam uma cena de preview isolada. O hook só aceita esses caminhos fixos e não executa texto recebido.

O usuário autorizou controle do computador. Skill: `C:/Users/dudu-/.codex/plugins/cache/openai-bundled/computer-use/26.1111.21308/skills/computer-use/SKILL.md`. Usar `node_repl` com `@oai/sky`, selecionar a janela retornada e respeitar observação/ação. O usuário também usa a máquina; `user input detected` exige reobservar. Não controlar terminal/Codex por UI. Após reload o Unity pode ficar alguns segundos indisponível. Não capturar continuamente janelas por polling.

## Cuidados técnicos descobertos

- Node 26 `stripTypeScriptTypes` aceita apenas `mode:'strip'`; não usar `transform`.
- TypeScript 7 instalado inicialmente não fornece `transpileModule` na mesma API. Ferramenta fixada na série 5.9 (lockfile).
- `npm.ps1` bloqueado pela política PowerShell; usar `npm.cmd`. Instalação exigiu aprovação por cache fora do workspace.
- Three é destro; Unity recebe `(x,y,-z)` e winding invertido uma vez. Não inverter novamente.
- `ScriptableObject` precisa do arquivo `GameDefinition.cs`; não reunir classes Unity serializadas em arquivo arbitrário.
- `StartAssetEditing/StopAssetEditing` agora envolve a geração de meshes/materiais para evitar refresh por mesh. A primeira importação sem batching levou minutos.
- Atualmente materiais são URP/Lit, não o toon original; iluminação provisória e chão sem textura procedural. Não afirmar equivalência visual.
- Instâncias viraram meshes individuais. Precisam otimização sem alterar formas/proporções.
- O importador ainda usa SceneMode.Single e configura build settings. Tornar a regeneração mais segura antes de trabalho autoral nas cenas.
- Git tinha somente a referência não rastreada antes deste trabalho. O editor alterou também QualitySettings/URPProjectSettings e gerou `stove.blend.meta`; revisar diferenças sem apagar trabalho do usuário. Não houve commit/push.

## Regra de continuidade

Atualizar este README e `Assets/ARES/README.md` a cada etapa relevante. Registrar o que foi **testado**, o que foi apenas **implementado**, falhas correntes e próximo passo concreto. Continuar o objetivo completo; não encerrar dizendo que a cena de inspeção equivale ao jogo.
