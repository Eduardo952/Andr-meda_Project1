# Fontes oficiais verificadas — 15/11/2026

## Integrações efetivas

### APOD

- Portal oficial e aviso de migração: https://api.nasa.gov/
- Endpoint testado: https://science.nasa.gov/wp-json/wp/v2/apod-basic/220713
- APOD migrou em setembro de 2026. A consulta `?date=2022-07-13` retornou uma lista recente durante a verificação, portanto o código usa o caminho YYMMDD e exige a data exata.
- `ApodResponse` aceita objeto/lista, recusa HTML, registros de outro dia e respostas excessivas. `ApodService` mantém timeout, limite de tentativa por imagem/dia e coleção offline com créditos. Falhas não interrompem o jogo.
- `apod-contract.json` conserva os campos relevantes da resposta real, sem o HTML redundante. É uma fixture de contrato, não uma imagem nova sem licença.
- O desafio de fotos usa as imagens reais APOD já atribuídas na coleção, com título e crédito correspondentes.
- O portal marca **Mars Rover Photos como arquivada**; não foi adicionada dependência do endpoint antigo.

### DONKI

- Documentação/aviso oficial: https://ccmc.gsfc.nasa.gov/tools/DONKI/
- Novo serviço: https://ccmc.gsfc.nasa.gov/DONKI-API/get/
- FLR e CME de 10/05/2024 foram obtidos na API oficial. Respostas originais: `flares.json` e `cmes.json`; URLs exatas e SHA-256 em `manifest.json`.
- `build-science-archive.py` gera 21 registros selecionados em `Resources/ARES/Science/archive.json`. Os desafios SortFlares e CmeCountdown usam esses valores e exibem a fonte individual em detalhes.
- A análise de CME selecionada é a mais recentemente submetida entre as marcadas `isMostAccurate`. Nível 0 é identificado como análise inicial, não verdade definitiva.
- Registros históricos não são dados ao vivo. Uma explosão ou velocidade não estabelece impacto em Marte. Não foram convertidas em dose/alertas de campanha. O cenário importado continua explicitamente simulado.
- Atribuição: NASA CCMC, Moon to Mars Space Weather Analysis Office e pesquisadores responsáveis pelos registros vinculados. São produtos de pesquisa/prototipagem, não orientação operacional. Regras oficiais de uso e publicação estão na página do DONKI.

### OSDR

- Documentação oficial: https://www.nasa.gov/osdr-api/
- Busca real: https://osdr.nasa.gov/osdr/data/search?term=plants&from=0&size=3&type=cgene
- Resposta original `osdr.json`; três estudos de plantas identificados e vinculados na biblioteca opcional do computador (GLDS-177, GLDS-520, GLDS-134).
- A UI apresenta o tema, não inventa conclusões ou transfere resultados entre espécies. O título integral fica no tooltip e na fonte.

## Recursos consultados e vinculados, sem conversões inventadas

- RadLab: https://visualization.osdr.nasa.gov/radlab/gui/data-api/ — documentação distingue taxa de dose absorvida (µGy/h), equivalente (µSv/h), instrumento e localização. Não há conversão de uma medida da ISS para dose de astronautas em Marte neste jogo.
- Mars Trek: https://trek.nasa.gov/mars/ e https://trek.nasa.gov/mars/jpl/config/textAbout.html — exploração real de imagens e relevo. O cenário continua autoral; não é terreno Mars Trek importado.
- Agência Espacial Canadense: https://www.asc-csa.gc.ca/eng/youth-educators/toolkits/food-production/ — atividades e recursos oficiais para cultivo/alimentação. Referência educativa, não fonte dos parâmetros de irrigação do jogo.
- Pesquisa em escudo ativo: https://www.nasa.gov/centers-and-facilities/armstrong/space-and-hypersonics-technologies/ — escudos magnéticos para astronautas ainda são pesquisa. O escudo da base é identificado como ficção científica.

Domínios de novas referências são validados por `OfficialSources`: HTTPS e domínio exato/subdomínio de `nasa.gov` ou `asc-csa.gc.ca`; `nasa.gov.example.org` não é aceito. Isso evita links com aparência oficial; a conferência editorial da página continua necessária.

Projeto independente. Uso de dados não implica endosso da NASA ou da CSA. Fotos APOD mantêm créditos e condições específicas na coleção existente. Nenhuma mensagem é enviada à NASA.

## Atualização para desenvolvedores

Baixar novas respostas para `Temp/Science`, conferir datas/IDs/unidades/termos oficiais e executar `python Tools/AresImport/build-science-archive.py`. Revisar a diferença do JSON e preservar o manifesto. Não mudar `sample` de toda a campanha porque um minijogo usa um arquivo real. A atualização é deliberada, não uma chamada de rede por frame; a campanha funciona offline.
