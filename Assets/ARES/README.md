# Base ARES — port nativo para Unity

**Estado: migração em andamento. A cena atual é de inspeção, não o jogo completo.**

## Organização

Personagens agora são exportados em `Import/characters.json` com hierarquia e geometria local. `Editor/CharacterImporter` gera os prefabs em `Generated/Characters`; `CharacterAnimation` contém as fórmulas do astronauta, `AstronautAnimator` aplica poses e `BubbleAnimator` anima o peixe e o acompanhamento. `ReferenceSpace` concentra reflexão Z e conversão de Euler XYZ. `CameraInput` cuida de arrastar/zoom, separado do estado matemático em `OrbitCamera`.

Preferência explícita do usuário: câmera manual idêntica à referência; não implementar follow automático de direção. R centraliza, H alterna tetos no explorador, C alterna isométrica e TAB abre a visão geral de inspeção. O modo isométrico conserva o ângulo fixo do original.

- `Import/`: dados intermediários extraídos da referência; não editar manualmente.
- `Generated/`: meshes, materiais e definição do jogo gerados pelo importador Unity.
- `Runtime/Data/`: contratos serializáveis e `GameDefinition` (ScriptableObject).
- `Runtime/Domain/`: estado da campanha, orçamento, pontuação e encerramento de Sol.
- `Runtime/World/`: navegação e controlador temporário de inspeção.
- `Editor/`: importação e comparação automatizada contra a referência.
- `Scenes/BaseARES.unity`: cena independente, sem dependência do navegador.

## Reproduzir a importação

1. Na raiz, executar `npm.cmd ci --prefix Tools/AresImport` e `node Tools/AresImport/export.mjs` com Node 26.
2. No Unity 6000.6.9f1, executar **ARES > Import reference geometry and create scene**.
3. Executar **ARES > Validate port against reference**. O resultado fica em `Logs/ares-parity.txt`.

O exportador executa somente módulos locais de geometria e regras. Não carrega `.env`, servidor, Supabase ou credenciais. O jogo Unity não requer Node nem Three.js; essas dependências são ferramentas de migração.

As coordenadas Three.js são convertidas por `(x,y,z) -> (x,y,-z)`, com inversão única do winding dos triângulos. Normais são convertidas com a mesma reflexão. Os pisos, paredes, telhados e interiores permanecem em grupos separados. Os índices e posições das malhas são derivados diretamente dos geradores originais. Nos personagens, os pivôs e pais originais são preservados: quaternions recebem `(-x,-y,z,w)` e rotações Euler usam a ordem XYZ do Three, em vez da ordem padrão de `Quaternion.Euler`.

## Validação desta etapa

`CharacterAnimationChecks` executa em cena de preview isolada, sem alterar a cena aberta. `Import/character-parity.json` contém dados calculados pelos callbacks originais de `Astronaut.tsx` e `Bubble.tsx`. Passaram no editor: 3.843 vértices em pose inicial, 200 quadros do astronauta e 128 do Bubble. Também passaram 120 quadros de câmera, 9.672 pontos de navegação, 384 casos de regras e os checks de campanha.

`PlayControlsChecks` passou em Play com teclado/mouse virtuais do Input System: W deslocou 5,20 m, braços oscilaram 40,1°, corpo oscilou 0,179 m; arrasto esquerdo alterou yaw e R centralizou. Menu **ARES > Validate controls in Play**, ou arquivo fixo `Temp/ares-play-check.request`, executa o teste e grava `Logs/ares-play.txt`; encerra Play e remove os dispositivos ao terminar. Arrasto no cenário também conferido visualmente. Gestos touch ainda não verificados em dispositivo. O explorador reinicializa seu estado transitório quando scripts são recarregados durante Play.

Salto/observação, fala e humores foram portados e testados como poses, mas a cena de inspeção só integra repouso/caminhada/aceno inicial e Bubble em humor feliz. Ligar os demais parâmetros aos eventos originais quando portar a apresentação/campanha; não inventar recompensas na animação. Poeira dos passos, áudio e equivalência de shaders ainda pendentes.

## Regras de extensão

Não conceder recompensas na UI. `Campaign` valida a tarefa aberta, impede duplicatas e exige confirmação do orçamento no Controle para selar o Sol. A camada de apresentação deve chamar `Tick` com o estado de pausa dos overlays. Minijogos precisam calcular sucesso a partir das próprias entradas; nunca criar botão de conclusão automática. Os dados de exemplo mantêm data, proveniência e JSON original; não rotular os snapshots como dados NASA ao vivo.

Novos minijogos devem ter modelos de estado e validação independentes de MonoBehaviour, uma apresentação separada e registro por ID. Os IDs originais e a rotação de tarefas dos cinco Sols constam de `GameDefinition.campaign`. Serviços remotos devem ficar atrás de interfaces com fallback explícito para snapshots, mantendo chaves no servidor.

## Ainda necessário para equivalência

- Menu, HUD, textos localizados, configurações, acessibilidade, minimapa e interface de relatórios.
- Dez minijogos táteis completos, apresentação das tarefas legadas e integração com a campanha.
- Completar integração da apresentação, roteiro de primeira caminhada, falas, poeira/áudio dos passos e eventos das animações já portadas; validar gestos touch.
- Shader toon, outlines, textura procedural do terreno, céu, ciclo solar, transparência/cutaway e efeitos.
- Animação e colheita da estufa, observação do telescópio, telas NASA e áudio.
- Proxies de dados/narrador, verificações de interface e comparação visual lado a lado.
- Otimização de instâncias e agrupamento de materiais: atualmente as instâncias extraídas viram meshes individuais para inspeção.

A referência web e os assets anteriores foram preservados até que a equivalência funcional e visual seja validada. Não considerar o port concluído nem apagar a única referência antes disso.
