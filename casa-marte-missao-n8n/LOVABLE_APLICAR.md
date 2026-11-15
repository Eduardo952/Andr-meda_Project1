# Instruções para o Lovable (aplicar este ZIP)

Este ZIP já contém o projeto completo com a integração n8n. Aplique **copiando os arquivos como estão**, sem reescrever, adaptar nem "melhorar":

1. `.env` — acrescenta N8N_WEBHOOK_URL, N8N_NASA_URL e N8N_WEBHOOK_SECRET (mantenha as variáveis Supabase existentes).
2. `src/lib/n8n.ts` (novo), `src/lib/solfacts.functions.ts` (novo), `src/test/n8n.test.ts` (novo).
3. `src/lib/narrator.functions.ts`, `src/lib/observatory.functions.ts`, `src/ui/BubblePanel.tsx` (substituir).
4. `n8n/` (workflow), `.env.example`, `README.md`, `AGENTS.md`, `roadmap.md`.
5. Docs: `docs/n8n/` e `docs/verification/` (mover os .md antigos para lá, como no ZIP).

Não altere regras do jogo, missões, energia, snapshots, tema nem o Mestre da Estufa. Depois rode os testes e informe os arquivos alterados.
Para ver de onde vem a fala do Bubble, abra o jogo com `?debug=n8n` no endereço.
