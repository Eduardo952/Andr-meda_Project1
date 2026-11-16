# Integração e refinamentos — 15/11/2026

## Referência e implementação

Foram comparados diretamente no ZIP `Anexo/Mars_Trainer.zip`: `src/config/dormitory.ts`, `src/game/sim/rest.ts`, `src/game/characters/Astronaut.tsx`, `src/ui/Hud.tsx`, `src/lib/narrator.functions.ts`, `docs/n8n/workflow.md` e as amostras de requisição/resposta. Não foram importados arquivos de ambiente ou credenciais.

A implementação principal permanece na Unity. O alojamento ocupa o suporte de vida existente, com quatro camas pastel, travesseiros e cabeceiras arredondadas. `Dormitory` centraliza posições, colisões e alcance; `BedSequence` controla repouso, despertar, alongamento e retorno ao movimento. O personagem lógico permanece fora do móvel; a pose visual se desloca ao colchão. E permite deitar/levantar. Repousar não concede recursos nem adianta o Sol.

A missão começa deitada, desperta, libera o controle e oferece o tutorial apenas ao chegar a menos de 6 m do centro de controle. O tutorial existente foi preservado. Movimento reduzido encurta as transições. `StartMission(false)` mantém a entrada direta utilizada pelas verificações de campanha.

Oxigênio e energia continuam no computador. Comunicação usa a mesma `CommunicationsView`, em uma tela lateral independente, escura e compacta; preserva relatórios, histórico, reparos e respostas. Desafios continuam em overlays com o cenário visível. Fórmulas de viagem, aquecimento e balanço energético receberam destaque; as atividades táteis e a rega foram preservadas.

Portais usam equipamentos característicos: telescópio roxo, broto verde, escudo azul e cruz de suporte de vida. O modelo detalhado do rover já existia, mas o binding importado interrompia sua construção: foi reativado, recuperando rodas com sulcos, suspensão e células solares. O percurso pontilhado verifica a mesma navegação e as mesmas rochas da condução. A pilotagem permanece manual, com Y/WASD; não há piloto automático. O mapa conserva o cenário e recebe bordas arredondadas.

## Configuração

A integração foi migrada para NASA/Groq nativos, sem Webhooks n8n. Consulte [NASA_GROQ_MIGRATION.md](NASA_GROQ_MIGRATION.md) para endpoints, configuração, credenciais privadas, build, testes e pendências de FP. `Assets/StreamingAssets/integrations.json` contém somente configurações públicas; as chaves ficam no arquivo local ignorado `secrets.local.json`.

A comunicação educativa com a Terra continua independente do assistente. Nenhuma resposta da IA concede recompensas ou modifica as regras do jogo.

## Verificações

- Runtime e Editor compilados com as referências Unity, sem erros; somente avisos obsoletos existentes no fixture de computador.
- Smoke test: 25 verificações de repouso normal/reduzido, reinício, colisões, alcance e corredor livre.
- Play: `Logs/ares-refinement.txt` registra aprovação da pose inicial, despertar, controle livre antes do tutorial, gatilho no centro, colisões e formato do payload. Capturas: `ares-refinement-wake.png` e `ares-refinement-tutorial.png`.
- Comunicação: `Logs/ares-communications.txt` registra aprovação dos custos, atrasos, reparos, relatórios, persistência e painel independente. Captura: `ares-communications-computer.png`.
- Rover: `Logs/ares-rover.txt` registra aprovação de aceleração, frenagem, ré, colisões, limites, câmera, poeira, entrada real e devolução do controle. Captura: `ares-rover-model.png`.
- Irrigação: `Logs/ares-casa-marte.txt` registra aprovação de doses, balanço de água, cancelamento, abastecimento, cinco dias de persistência e interações. A visibilidade é verificada pelas plantas identificadas no cenário, excluindo personagens decorativos que aparecem por proximidade.
- Para repetir a sequência inicial, grave `refinement` em `Temp/ares-computer-check.request`; para comunicação, use `communications`. Execute uma solicitação por vez, fora de Play.

A validação real da migração NASA/Groq está registrada em NASA_GROQ_MIGRATION.md. Os fixtures de tutorial e comunicação foram adaptados às novas entradas; o teste de corredores do rover passou a consultar o layout radial em vez de uma coordenada antiga fixa.
