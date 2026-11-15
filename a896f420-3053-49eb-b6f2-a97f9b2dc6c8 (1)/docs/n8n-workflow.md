# n8n workflow for the Bubble narrator

The game calls a server-side proxy (`narratorProxy` in `src/lib/narrator.functions.ts`), which POSTs to your n8n webhook with a 6-second timeout and the header `x-api-key: <N8N_WEBHOOK_SECRET>`. Only game facts are sent: no names, nicknames or device data (the device id stays on the server for rate limiting and is not forwarded).

## Nodes

1. **Webhook**
   - HTTP Method: `POST`, Path: e.g. `bubble-narrator`
   - Authentication: **Header Auth**, header name `x-api-key`, value = the same secret you store as `N8N_WEBHOOK_SECRET` in Lovable.
   - Respond: **Using 'Respond to Webhook' node**.
2. **Code (basic input check)**: JavaScript:
   ```js
   const b = $json.body ?? $json;
   const ok = ["en", "pt-BR"].includes(b.lang) && typeof b.sol === "number" && b.facts && b.phase;
   if (!ok) throw new Error("bad input");
   return [{ json: { payload: JSON.stringify(b), lang: b.lang } }];
   ```
3. **LLM node** (OpenAI / Anthropic / any Chat Model + Basic LLM Chain)
   - System message: the contents of `docs/n8n-prompt.md` (below the line).
   - User message: `{{ $json.payload }}`
   - Low temperature (0.3–0.5) and a JSON / structured output parser with `{ text: string, mood: enum }`.
4. **Respond to Webhook**
   - Respond With: JSON
   - Body: `{ "text": "{{ $json.text }}", "mood": "{{ $json.mood }}" }`

Any error, timeout (> 6 s), non-200 status or invalid JSON makes the game use its local templates.

Sample requests: `docs/samples/narrator-request-sol-start.json`, `docs/samples/narrator-request-summary.json`.
