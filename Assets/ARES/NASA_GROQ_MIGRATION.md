# Migração nativa NASA e Bubble / Groq

Implementada a partir de **Anexo/prompt_migracao_nasa_groq_unity.md**, incluindo os cinco blocos JavaScript do Apêndice A. Nenhuma chamada a n8n é necessária no jogo Unity.

## 1. Operações migradas, por nó

| Nó original | Implementação nativa |
| --- | --- |
| CONFIG do NASA-Sol | `MissionPlanner.Plan`, validação de mission/sol, RNG xmur3/mulberry32 e datas UTC |
| DONKI FLR (bloco) | `NasaSolService`, bloco de 120 dias compartilhado pelos cinco Sols |
| Escolher janela | `MissionPlanner.Select` e `MarsCalendar`, três motivos de seleção e fórmula Mars24 |
| APOD | Consulta do intervalo de dez dias, primeira imagem HTTPS não vídeo |
| DONKI CME | Consulta do dia escolhido e maior velocidade |
| DONKI WSA-Enlil | Consulta do dia anterior ao posterior e impactos em Marte |
| Clima Marte (Curiosity) | Consulta pelo Sol do Curiosity calculado, sem chave |
| NASA Images | Busca da página determinística e primeiro preview HTTPS |
| Montar resposta do Sol | `SolDataAssembler` e `EffectsCalculator`, mesmos campos, nulos e `effects-v2` |
| CONFIG do narrador / Montar pedido Groq | `PromptBuilder`, sete fases, tabela bilíngue de 12 perguntas, limpeza da pergunta livre |
| AI Agent / Groq Chat Model | `GroqLlmClient`, chat completions, modelo efetivamente executado no workflow |
| Ler resposta Groq | `NarratorResponseParser`, remoção de think/fences, extração de JSON e limites originais |
| Respostas dos Webhooks | `GetSolAsync(mission, sol)` e `BubbleNarrator.SayAsync`, sem servidor intermediário |

## 2. Scripts e integração no projeto

Em `Runtime/Infrastructure/Nasa/`:

- `MissionPlanner.cs`: RNG, calendário e seleção da janela.
- `SolModels.cs`, `SolDataAssembler.cs`: DTOs Newtonsoft, montagem e efeitos.
- `ApiConfiguration.cs`: configuração sem segredos, `ApiCredentials`, `ICredentialProvider` e provedor local.
- `HttpJsonClient.cs`, `UnityHttpTransport.cs`: deduplicação concorrente, cancelamento, timeout, retry 429/5xx e Retry-After. Credenciais nunca são registradas; URLs autenticadas são redigidas.
- `SolDataCache.cs`, `NasaSolService.cs`: cache por missão/Sol, bloco FLR, âncora UTC persistente, prefetch apenas de N+1, fallback da mesma identidade.
- `ImageLoader.cs`: cache LRU e disco por hash de URL, vínculo missão/Sol, leases para impedir destruição de texturas em uso, placeholder e descarte.
- `BubbleNarrator.cs`, `NarrationValidator.cs`, `LocalNarratorTemplates.cs`: transporte Groq, parser fiel, validação de fatos/números/classes/idioma e templates locais existentes.
- `NasaAsyncRunner.cs`: host para verificações isoladas das corrotinas.
- `link.xml`: preservação dos DTOs para serialização por reflexão em builds com stripping/IL2CPP.

Alterações de integração: `MissionIntegration.cs`, `World/ApodService.cs`, `UI/ObservatoryView.cs`, `UI/GameFrontend.cs` e `Domain/Campaign.cs`. A campanha registra apenas métricas reais de tarefas concluídas. As regras, recompensas e exercícios continuam sob controle de `Campaign`; nenhuma resposta da IA os modifica.

O observatório usa o APOD canônico do Sol, oferece as perguntas prontas e livres e exibe a foto adicional da biblioteca NASA. Metadados, créditos e textura são trocados juntos. Se o APOD falhar, a imagem curada mantém seus próprios créditos e data. A comunicação educativa com a Terra continua independente do narrador.

`Resources/ARES/Narrator/bubble-system.txt` e `questions.json` foram extraídos literalmente de A4. `StreamingAssets/integrations.json` centraliza os parâmetros públicos.

Dependência: pacote oficial Unity `com.unity.nuget.newtonsoft-json` **3.2.2**, incorporado em `Packages/`, com licença e configurações de importação Mono/AOT originais. O arquivo baixado foi conferido pelo SHA-1 do registro oficial. A incorporação permite resolver a dependência sem acesso ao registro durante a abertura do projeto.

## 3. Endpoints e parâmetros

| Endpoint | Parâmetros | Timeout |
| --- | --- | --- |
| `https://api.nasa.gov/DONKI/FLR` | startDate, endDate, api_key | 20 s |
| `https://api.nasa.gov/DONKI/CMEAnalysis` | startDate=endDate=earth_date, mostAccurateOnly=true, catalog=ALL, api_key | 10 s |
| `https://api.nasa.gov/DONKI/WSAEnlilSimulations` | startDate=d_prev, endDate=d_next, api_key | 10 s |
| `https://api.nasa.gov/planetary/apod` | start_date, end_date, api_key | 10 s |
| `https://api.maas2.apollorion.com/{curiosity_sol}` | Sem chave | 10 s |
| `https://images-api.nasa.gov/search` | q=Mars surface Curiosity, media_type=image, page_size=1, page=photo_page | 10 s |
| `https://api.groq.com/openai/v1/chat/completions` | Bearer; system/user; response_format=json_object | 10 s |

Groq: `openai/gpt-oss-120b`, temperatura **0,7**, máximo **1500 tokens**. Modelo, temperatura, tokens, URLs, timeouts, cache e retries ficam em `Assets/StreamingAssets/integrations.json`. Não se usam os valores mortos llama/0,4/300 de A4.

## 4. Progressão e variação dos Sols

O chamador informa somente missão e Sol; datas não são recebidas da interface. A missão é validada pela regex original, e o Sol fica entre 1 e 5. Cada tag inicializa um RNG próprio a partir de `mission|tag`.

O bloco terrestre respeita EARTH_MIN, margem de 45 dias e 120 dias de extensão. A seleção prioriza cinco Sols consecutivos com atividade variada e pelo menos um evento M/X quando disponível. Sem isso, usa `calm_period` ou `no_flare_data`, exatamente como A2. A data terrestre, o Sol Curiosity, Ls e risco de poeira seguem a mesma matemática UTC e o mesmo arredondamento JS.

O intervalo APOD e a página da biblioteca variam por Sol. O algoritmo original **não garante imagens diferentes** entre Sols, por isso a interface não promete ausência de repetição. As datas terrestres consecutivas são distintas. A âncora UTC fica persistida por missão para manter sua identidade ao atravessar a meia-noite.

Cache: `Application.persistentDataPath/NasaSolCache`; imagens: `NasaImages`. Validade padrão de um dia. Dados parciais válidos também são armazenados. Ordem: cache fresco, rede, cache anterior da mesma missão/Sol, amostra estática `is_fallback:true`. Nunca se atribuem dados de outro Sol ao atual. Nova missão gera novo ID; `ResumeMission(id)` permite reutilizar a identidade e seu cache sem introduzir um novo sistema de saves da campanha.

## 5. Credenciais, build e rotação

As duas chaves fornecidas já estão em **`Assets/StreamingAssets/secrets.local.json`**. O arquivo e seu `.meta` estão ignorados pelo Git; o Markdown de origem foi sanitizado. Nenhuma chave foi incluída em C#, cenas, prefabs, ScriptableObjects ou `integrations.json`.

Formato do arquivo local (preencher os valores apenas no arquivo ignorado):

```json
{ "NASA_API_KEY": "", "GROQ_API_KEY": "" }
```

- **Build de desktop:** Unity copia StreamingAssets para o player. As chaves são lidas automaticamente em outro computador, sem variáveis de ambiente. Distribuir a pasta completa da build, não apenas o executável.
- **Rotação:** um `secrets.local.json` em `Application.persistentDataPath` tem prioridade sobre o arquivo distribuído. Esse arquivo é uma substituição completa; fornecer ambas as chaves. Também é possível atualizar o arquivo em StreamingAssets da build.
- **Git/clones:** ignorar o arquivo protege o repositório, não a build. Um clone novo precisa receber o arquivo local separadamente antes de gerar uma build com acesso direto.
- **Uso privado:** `PrivateTrustedBuild:true` permite a chamada Groq direta também no player final, conforme a autorização do projeto. As chaves distribuídas podem ser extraídas. NASA tem limites; Groq pode gerar custos/abuso.
- **Desenvolvimento:** somente o fallback de variável `GROQ_API_KEY` usa `UNITY_EDITOR || DEVELOPMENT_BUILD`; o carregamento das chaves do arquivo local não depende de build de desenvolvimento.
- **Distribuição ampla:** configurar `GroqProxyBaseUrl` e `PrivateTrustedBuild:false`; retirar a chave Groq do arquivo que será distribuído. O proxy detém o segredo. `ILlmClient` permite outra implementação sem alterar gameplay. `NasaProxyBaseUrl` também é opcional; recebe os mesmos caminhos NASA, sem api_key no cliente, e deve inserir a chave no servidor.
- O provedor por filesystem atende ao player desktop deste projeto. Android/WebGL requerem um adaptador de StreamingAssets e políticas específicas; esses alvos não foram declarados como validados.

Exemplo de Worker em `Tools/NasaMigration/groq-proxy.example.js`, não implantado: rota `/bubble`, HTTPS, corpo máximo de 16 KiB inclusive em envio chunked, origem configurável e rate limit por IP. Criar o secret `GROQ_API_KEY` no serviço e o binding `BUBBLE_LIMITER` de rate limiting, por exemplo 10 pedidos a cada 60 segundos. O contador desse binding é aproximado e local a cada região Cloudflare; ver a [documentação oficial](https://developers.cloudflare.com/workers/runtime-apis/bindings/rate-limit/). Não há credencial de proxy inventada. O exemplo fixa o mesmo modelo/parâmetros e não registra pedidos autenticados.

Pendências de contrato: `formulaVersion` permanece vazio; o payload envia null. **TODO(confirm):** significado, fonte, tipo/unidade e obrigatoriedade de FP não constam no apêndice. Não foi criado campo especulativo. As APIs NASA e Groq já possuem as credenciais fornecidas.

## 6. Dependência restante de n8n

**Nenhuma dependência de execução no Unity.** Removidos os caminhos de Webhook, URLs antigas e header x-api-key. Não se usa WEBHOOK_SECRET.

`UseLegacyN8n:false` fica por uma versão. Como o servidor será desligado, o modo de comparação lê **exportações locais**, sem reativar o servidor: `Application.persistentDataPath/LegacyN8nResponses/{sha256(mission + "|" + sol)}.json`. Aceita somente um SolResponse com a mesma missão e Sol. Sem exportação correspondente, retorna a amostra offline. Remover essa opção após a versão de comparação.

O diretório de referência web `casa-marte-missao-n8n` foi preservado e pode conter código histórico de n8n. Ele não é compilado nem utilizado pelo jogo Unity.

## 7. Diferenças conhecidas e validação

- A chamada Groq é direta, sem AI Agent intermediário. Usa os parâmetros do nó realmente executado e preserva SYSTEM, perguntas, payload e parser. O parser conserva inclusive a limitação original de contar chaves dentro de strings JSON.
- Mantido o validador de cliente, cujo limite de 400 caracteres pode rejeitar texto que passaria pelo parser original de 600 caracteres. Respostas rejeitadas usam os templates locais existentes; o parser mantém sua frase de interferência original.
- Templates locais respondem às três perguntas que já existiam no cliente. Para as nove novas perguntas sem template local, a falha de rede usa a frase de interferência, sem inventar conteúdo.
- Mantidos resultados medidos de tarefas; a irrigação não produz segundos/erros, portanto não se inventam métricas para ela. Demanda, alocação e consequências reais são enviadas, separados dos fatos NASA. `facts.wind` permanece sempre null.
- Persistência, prefetch, retries e LRU foram acrescentados como solicitado. A âncora UTC por missão evita mudança de identidade após a meia-noite; a comparação com o original usa a mesma data UTC e as mesmas respostas NASA.
- APOD do pipeline substitui a antiga restrição às datas curadas. A coleção curada continua disponível offline, identificada como tal.

**Verificações executadas:**

- 228 asserções de lógica: vetores RNG a 1e-15, calendário, seleção nos três motivos, regex/clamp/arredondamento, campos e nulos A1–A5, sete fases, SYSTEM literal, sanitização, validadores, templates bilíngues, falhas individuais, JSON inválido, timeout, 403, 429/503/Retry-After, cancelamento, deduplicação e persistência.
- Comparação C# × JavaScript executando literalmente o apêndice: 14 casos sintéticos e dez respostas obtidas com dados reais; campos e valores equivalentes.
- Teste real: Sols 1–5 de `mission-ABC12345` e `test_mission_001`, UTC de referência 2026-11-15. Datas distintas entre Sols, respostas idênticas ao recarregar do disco. Um bloco FLR por missão e prefetch limitado ao próximo Sol.
- Groq respondeu HTTP 200; texto passou no parser e no validador. Clima, biblioteca de imagens, CME e WSA-Enlil responderam HTTP 200. **APOD atingiu o timeout original de 10 s; FLR retornou 404 nos blocos históricos sorteados.** Não são credenciais pendentes: após corrigir a extração do comentário junto à chave NASA, os endpoints autenticados CME/WSA funcionaram. Dados dessas fontes ausentes permaneceram nulos.
- Unity PlayMode em projeto temporário isolado: download HTTPS real, segundo acesso em memória, reabertura pelo disco, URL inválida com placeholder, retenção de textura em uso, LRU e destruição das texturas.
- Runtime e Editor completos compilados com as referências Unity. A instância temporária preservou a cena aberta do usuário. Não foi executada a build final em outro computador.

Para repetir, a partir da raiz do repositório:

```text
node Tools/NasaMigration/reference.mjs
dotnet run --project Tools/NasaMigration/NasaMigration.csproj -- --unit
dotnet run --project Tools/NasaMigration/NasaMigration.csproj -- --live
node Tools/NasaMigration/compare-live.mjs
```

O comando `--live` usa as chaves locais e faz chamadas reais. No Editor: menu **ARES > Checks > Native NASA and Groq**, que entra em PlayMode para validar texturas. Relatórios locais ignorados pelo Git: `Logs/ares-nasa-unit.txt`, `Logs/ares-nasa-play.txt`, `Logs/nasa-live.json`; registro resumido sem credenciais: `Tools/NasaMigration/validation.json`.
