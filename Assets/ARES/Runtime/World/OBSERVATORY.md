# Observatório e diário APOD

O jogador pode clicar no telescópio importado ou pressionar E perto dele. O botão **Diário** no HUD permite rever as descobertas sem voltar à sala. O diário também está disponível no encerramento e retorna a essa tela ao fechar. Uma imagem diferente é liberada por Sol, até cinco. Os dias futuros ficam desativados. A campanha mantém os registros ao avançar de Sol; iniciar outra missão cria um diário vazio.

## Organização

| Arquivo | Responsabilidade |
| --- | --- |
| `Domain/DiscoveryJournal.cs` | Cinco dias, bloqueio de dias futuros e histórico de observações |
| `Data/ApodData.cs` | Contratos da API/coleção e validação de imagem/data/HTTPS |
| `World/ApodService.cs` | Requisições assíncronas, timeout, repetição explícita, imagens em memória e alternativa local |
| `World/ObservatoryInteraction.cs` | Clique e E no telescópio existente, sem criar outra malha |
| `UI/ObservatoryView.cs` | Fotografia, resumo em PT-BR, créditos, fonte e navegação entre dias |
| `Resources/ARES/Observatory/collection.json` | Datas, textos revisados, créditos, URLs e recursos locais dos cinco dias |
| `Resources/ARES/UI/Observatory.uss` | Complementos do estilo compartilhado `ChallengePanel` |
| `Editor/ObservatoryChecks.cs` | Contratos, dados reais locais, interação em Play e verificação de falha da conexão |

`GameFrontend` instala o serviço e a interação; `Campaign.Discoveries` possui o estado do diário. `ReferenceExplorer` impede que o clique no telescópio inicie o arrasto da câmera. Posição Unity do telescópio: `(7,1.8,44)`; ponto de observação: `(7,0,41.8)`; alcance: 4,5 m. A área de clique mede 2,8 × 3,6 × 2,8 m. A geometria já importada é reutilizada. Não há novo modelo 3D nem imagem espacial gerada por IA.

A exploração do APOD é educativa e independente das recompensas das tarefas existentes do observatório. Ela não conclui puzzles automaticamente nem consome recursos. O diário usa o dia atual da campanha; não avança por tempo real nem modifica o cronômetro. Os overlays pausam a simulação pelo mecanismo já existente.

## API e falhas reais encontradas

Integração: `GET https://api.nasa.gov/planetary/apod?date=YYYY-MM-DD&api_key=...`. [Portal NASA](https://api.nasa.gov/) e [contrato mantido pela NASA](https://github.com/nasa/apod-api). A documentação atual descreve `url` como possível permalink e `hdurl` como imagem; o código aceita também o formato anterior, em que `url` apontava diretamente para a imagem. Vídeos/iframes não são tratados como fotografias.

Na verificação em 15/11/2026, a API respondeu HTTP 200 com título **NASA Science** e URL do **logotipo** para datas recentes e históricas. Essas respostas são inválidas para o jogo e são rejeitadas. Não confundir sucesso HTTP com uma imagem válida. As cinco imagens locais foram conferidas e baixadas das páginas oficiais do arquivo APOD, com data e créditos preservados. A UI identifica quando está usando a cópia local; não anuncia conexão bem-sucedida nessa situação.

O serviço consulta cada data apenas na primeira visualização, guarda a textura válida em memória e oferece repetição explícita em **Consultar NASA**. Metadados têm timeout de 12 s; imagens, 15 s. Uma falha não remove a fotografia local nem impede a missão. Não há tentativas automáticas infinitas ou uma chamada por quadro. Fechar o painel remove sua assinatura de eventos; a requisição pode terminar e ficar disponível para a próxima abertura. Texturas baixadas são destruídas com o serviço; recursos locais pertencem à Unity.

Por padrão usa-se a chave pública `DEMO_KEY`, sujeita às cotas da NASA. Para desenvolvimento, a variável de ambiente `NASA_API_KEY` pode fornecer outra chave; ela nunca é gravada nos assets nem exibida nos logs. Para distribuição, configure `collection.json.endpoint` com um proxy HTTPS de sua infraestrutura que aceite `date` e retorne o contrato APOD, mantendo a chave no servidor. O cliente só acrescenta a chave se o host for `api.nasa.gov`; não a envia a um proxy. Nenhum servidor ou credencial particular foi criado nesta etapa.

## Coleção educativa e tradução

Os cinco Sols sorteiam, sem repetição, os APOD de **13 a 17 de julho de 2022**. `DiscoveryDeck` usa Fisher-Yates; `ApodService.BeginMission` embaralha os registros completos e limpa downloads/tentativas da missão anterior. A foto de cada Sol permanece estável quando o diário é reaberto; uma nova missão faz outro sorteio. A semente opcional serve apenas para verificações reproduzíveis. Isso é uma seleção histórica explícita, não o APOD do dia civil atual. As páginas oficiais, imagens e créditos estão em `collection.json`. Datas, resumos, perguntas e licenças acompanham a imagem sorteada. Os textos em português são adaptações educativas revisadas das páginas oficiais; não são traduções automáticas da API.

Para trocar a coleção, atualize juntos: data, título, resumo, pergunta, créditos, fonte, URL da imagem e recurso local. Mantenha cinco datas/imagens distintas e revise todos os resumos. Não altere somente a data deixando uma legenda antiga. A imagem usa `ScaleToFit`, sem recortar o conteúdo científico. Preserve `.meta` ao substituir assets.

Os créditos de terceiros continuam pertencendo aos autores; **estar no APOD não significa que toda imagem seja de domínio público**. Consulte a [proveniência dos assets](../../Resources/ARES/Observatory/README.md) antes de redistribuir a coleção. Nenhuma licença global da NASA é atribuída a fotos de terceiros.

## Testes

Com BaseARES aberta, crie `Temp/ares-observatory-check.request`. Na primeira instalação de um novo script de teste, `Temp/ares-validate.request` solicita refresh ao hook já carregado. Aguarde o término antes de editar scripts ou iniciar outro teste. Resultado: `Logs/ares-observatory.txt`; capturas: `Logs/ares-observatory-day1.png` e `Logs/ares-observatory-day5.png`.

A suíte valida cinco imagens distintas, formato antigo/novo da API, rejeição de HTML/logotipos/vídeos/datas incorretas, fotos offline, clique real e E/Escape com dispositivos virtuais, captura da câmera, bloqueio de dias futuros, navegação dos cinco dias, reinício da missão e ausência de requisições duplicadas. Uma consulta real registra o estado de conexão e exige que a cópia local permaneça disponível em caso de falha. O avanço de dia nessa suíte é uma fixture de interface; a campanha completa tem sua própria suíte `Temp/ares-ui-check.request`.

Verificado em 15/11/2026: suíte do observatório passou, incluindo diário na vitória e retorno ao encerramento; as cinco imagens e as capturas dos Sols 1 e 5 foram inspecionadas. Paridade e regressão da campanha de cinco Sols/25 resultados também passaram. O teste online confirmou a alternativa local diante da resposta inválida; não houve validação de um download de fotografia válido pela API nessa execução. Nenhuma build standalone foi gerada.
