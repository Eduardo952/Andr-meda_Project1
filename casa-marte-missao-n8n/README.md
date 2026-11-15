# Mars Mission Trainer — Base ARES

Jogo educativo 3D para crianças de 7 a 12 anos: o jogador treina como astronauta júnior na Base ARES, em Marte, equilibra a energia da base com dados reais da NASA (DONKI, InSight, APOD) e conversa com **Bubble**, um peixinho astronauta narrado por IA (Groq via n8n).

Feito no [Lovable](https://lovable.dev) com TanStack Start, React 19, React Three Fiber, Tailwind 4 e zustand.

## Rodando

```sh
bun install      # ou npm i
bun run dev      # vite dev
bun run test     # vitest
bun run build
```

Sem configurar nada, o jogo funciona offline. Para ligar o n8n, veja abaixo.

## Backend n8n (dados + Bubble)

Um workflow n8n (`n8n/mars-bubble-groq-nasa_workflow.json`) fornece:

- `GET  /nasa-sol?sol=N` → fatos NASA + APOD + foto do Sol N
- `POST /bubble-narrator` → fala do Bubble

Passo a passo completo: [`docs/n8n/workflow.md`](docs/n8n/workflow.md). Resumo:

1. Importe o workflow no n8n, preencha as chaves **só** no node `CONFIG` e ative-o.
2. Crie os Secrets no Lovable: `N8N_BASE_URL` (`https://SEU-N8N/webhook`) e `N8N_WEBHOOK_SECRET`. Modelo em [`.env.example`](.env.example).

## Estrutura

```
n8n/                     workflow importável no n8n
docs/
  n8n/                   guia, prompt do Bubble e exemplos de request/response
  verification/          relatórios de verificação por funcionalidade
  IMPLEMENTATION_PROGRESS.md
src/
  lib/n8n.ts             ÚNICO ponto de integração n8n (URLs, headers, validação, cache)
  lib/*.functions.ts     server functions: narrator, observatory, solfacts
  services/narrator/     Bubble: pedido, validador anti-alucinação, templates locais
  engine/ config/ data/  regras de energia, balanceamento e snapshots dos 5 Sols
  game/ tasks/ ui/       cena 3D, mini-games e interface
  locales/               en / pt-BR
  test/                  vitest
.lovable/                planos e metadados do Lovable (não editar à mão)
roadmap.md               status das entregas
AGENTS.md                regras de arquitetura para agentes/contribuidores
```

## Pendências (ver `roadmap.md`)

- Verificar o n8n/NASA ao vivo com credenciais reais.
- Testar todos os mini-games em um celular físico.
