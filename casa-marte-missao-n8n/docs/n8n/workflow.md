# Integração com o n8n

O jogo conversa com **um único workflow** (`n8n/mars-bubble-groq-nasa_workflow.json`) que expõe dois webhooks. Toda chamada passa por server functions (`src/lib/*.functions.ts`) usando o módulo `src/lib/n8n.ts`; o navegador nunca vê a URL nem o segredo.

| Webhook | Método | Uso no jogo | Código |
|---|---|---|---|
| `/bubble-narrator` | POST | Fala do Bubble (Groq `llama-3.3-70b`) | `narrator.functions.ts` → `NarratorService` |
| `/nasa-sol?sol=N` | GET | Fatos NASA (DONKI, InSight) + APOD + foto do Sol N | `observatory.functions.ts` (Observatório) e `solfacts.functions.ts` (fatos) |

Só são enviados fatos do jogo: nada de nome, apelido ou dados do aparelho (o `deviceId` fica no servidor, para limite de requisições).

## 1. Configurar o n8n

1. **Import from file** → `n8n/mars-bubble-groq-nasa_workflow.json`.
2. Abra o node **CONFIG** e preencha só ali:
   - `NASA_API_KEY` (https://api.nasa.gov)
   - `GROQ_API_KEY` (`gsk_...`)
   - `WEBHOOK_SECRET` → invente um valor longo; é o mesmo que irá em `N8N_WEBHOOK_SECRET`.
   - `SOL_DATES` → data da Terra de cada Sol (define APOD, DONKI e foto).
3. **Ative o workflow** (toggle no topo). Só então valem as URLs de produção `https://SEU-N8N/webhook/...`. As URLs `/webhook-test/...` funcionam apenas enquanto você clica em *Listen for test event*.

> ⚠️ A autenticação é feita pelo código do node CONFIG (header `x-api-key`). Se `WEBHOOK_SECRET` continuar com o texto `COLE_AQUI...`, o webhook fica **aberto**. As chaves ficam dentro do JSON exportado: nunca exporte/commite o workflow com valores reais.

## 2. Configurar o jogo

Neste projeto as variáveis já estão no `.env` (nomes sem `VITE_`, portanto só o servidor as lê): `N8N_WEBHOOK_URL`, `N8N_NASA_URL` e `N8N_WEBHOOK_SECRET`. Se você criar Secrets com os mesmos nomes no Lovable, eles têm prioridade. Para trocar o segredo, altere-o **nos dois lados** (`WEBHOOK_SECRET` no node CONFIG e `N8N_WEBHOOK_SECRET`).

Tabela de referência:

| Nome | Obrigatório | Exemplo |
|---|---|---|
| `N8N_BASE_URL` | sim | `https://SEU-N8N/webhook` |
| `N8N_WEBHOOK_SECRET` | recomendado | mesmo valor de `WEBHOOK_SECRET` |
| `N8N_WEBHOOK_URL` | não | URL completa do narrador (config antiga continua valendo) |
| `N8N_NASA_URL` | não | URL completa do GET de NASA |
| `NASA_API_KEY` | não | fallback direto na NASA se o n8n cair |

Sem nada configurado, o jogo funciona offline (templates do Bubble + snapshots locais).

## 3. Testar

```sh
# NASA por Sol
curl -s -H "x-api-key: SEU_SEGREDO" "https://SEU-N8N/webhook/nasa-sol?sol=2"

# Narrador
curl -s -X POST -H "x-api-key: SEU_SEGREDO" -H "Content-Type: application/json" \
  -d @docs/n8n/samples/narrator-request-sol-start.json "https://SEU-N8N/webhook/bubble-narrator"
```

Formatos: `docs/n8n/samples/nasa-sol-response.json` (resposta) e `narrator-request-*.json` (pedidos). O prompt do Bubble está em `docs/n8n/prompt.md` (já embutido no workflow).

## 4. Como o jogo se comporta (e cai para o plano B)

- **Observatório (APOD + foto)**: n8n → NASA direta com `NASA_API_KEY` → snapshot local. Cada imagem informa de onde veio (`live` / `snapshot`).
- **Bubble**: n8n → validador (`services/narrator/validator.ts`) → se qualquer número/evento não bater com os fatos, usa o template local.
- **Cache**: respostas do `/nasa-sol` ficam 24 h por Sol no servidor; após uma falha o servidor espera 60 s antes de tentar de novo (o workflow faz até 7 chamadas em série, então um n8n lento não trava as telas). Timeout: 15 s no GET, 6 s no narrador.
- **Fatos ao vivo no motor**: `getSolFacts({ sol })` já devolve flare/CME/impacto/clima/vento do n8n, mas as **regras do jogo seguem nos snapshots** (`src/data/snapshots`), pois `SOLS`/`DERIVED` são constantes síncronas e trocar a fonte mudaria demanda de energia e alarmes. Ligar isso é uma decisão de design separada: converter o payload para `RawSnapshot` e carregar antes do início da campanha.

## Limitações conhecidas

- A API InSight é de uma missão encerrada em 2022: os dados de clima/vento são históricos e podem vir `null`; o jogo trata `null` como "sem dado".
- O workflow não foi executado aqui (sem acesso à internet/n8n): a validação cobre o código do jogo e o formato da resposta, não as APIs da NASA/Groq ao vivo.
