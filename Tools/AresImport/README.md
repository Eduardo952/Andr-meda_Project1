# Extração offline da referência (4)

Executar na raiz do projeto Unity:

```powershell
npm.cmd ci --prefix Tools/AresImport --cache Temp/npm-cache
node Tools/AresImport/export.mjs
```

A ferramenta usa Node, Three.js, TypeScript e `@napi-rs/canvas`, com versões resolvidas em `package-lock.json`. O jogo Unity não depende dessas ferramentas. A origem é exclusivamente `a896f420-3053-49eb-b6f2-a97f9b2dc6c8 (4)/src`; não há fallback para a versão antiga.

`export.mjs` transpila os módulos locais necessários, executa os geradores de geometria e adapta JSX/refs/callbacks de animação. O adaptador Canvas executa as texturas procedurais originais e grava PNGs. Não executa o servidor da referência, carrega `.env`, consulta NASA ou chama o narrador.

O resultado fica em `Assets/ARES/Import`. Posições permanecem no sistema Three.js; a reflexão Z e a inversão de winding ocorrem uma única vez no importador C#. Personagens mantêm hierarquia/pivôs em `characters.json`. Cenário inclui UV, cor por vértice, mapas e propriedades de material. Malhas invisíveis na pose extraída são omitidas. Animações dinâmicas de cenário ainda precisam de módulos próprios em Unity.

`consequence-fixtures.mjs` gera o oráculo de consequências e sequências da campanha executando os arquivos TS de origem. A campanha usa um adaptador mínimo do estado Zustand; remove apenas os imports e chamadas de apresentação/narração. As ações de orçamento, tarefas, alarme e selagem são as originais. Os casos de teste não implementam puzzles nem concedem conclusões na cena do jogo.

O `manifest.json` é escrito por último e contém hashes SHA-256 dos arquivos de origem e dos resultados. Uma exportação interrompida pode deixar arquivos intermediários; a validação dos hashes impede importar um conjunto misturado. Reexecutar o exportador após qualquer mudança da referência. Os arquivos em `Import` são gerados e não devem ser corrigidos manualmente.

No editor Unity existente, executar **ARES > Import reference geometry and create scene**. Salvar antes qualquer alteração da cena. O importador regenera `BaseARES.unity`; não colocar trabalho autoral nessa cena sem incorporá-lo ao importador. A automação aceita os arquivos fixos `Temp/ares-import.request` e `Temp/ares-validate.request`, aguarda refresh/compilação e registra o resultado em `Logs/ares-request-status.txt`.

**ARES > Validate port against reference** verifica dados, navegação, regras, consequências, campanha, personagens e geometria importada. **ARES > Validate controls in Play** usa dispositivos virtuais e sai de Play ao terminar. Logs em `Logs/ares-parity.txt` e `Logs/ares-play.txt` são locais; resultados e limitações relevantes devem ser registrados também nos READMEs versionados.

Limites atuais: shaders/iluminação URP ainda diferem da referência; texturas Canvas são rasterizadas por Skia local; animações de estufa/robô, UI, puzzles, céu e serviços remotos continuam pendentes. Checks de geometria/fórmulas não substituem comparação visual e funcional do jogo inteiro.
