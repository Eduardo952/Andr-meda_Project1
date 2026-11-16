// Optional public-distribution path. Not deployed or used by this private project.
// Cloudflare bindings: GROQ_API_KEY secret; BUBBLE_LIMITER Rate Limiting binding
// (for example 10 requests / 60 seconds per IP). CORS origin is configured separately.
// Binding setup: https://developers.cloudflare.com/workers/runtime-apis/bindings/rate-limit/
export default {
  async fetch(request, env) {
    const cors = { "Access-Control-Allow-Origin": env.ALLOWED_ORIGIN || "null", "Vary": "Origin" };
    const reply = (text, status) => new Response(text, { status, headers: cors });
    if (new URL(request.url).pathname !== "/bubble") return reply("Not found", 404);
    const origin = request.headers.get("Origin");
    if (origin && origin !== env.ALLOWED_ORIGIN) return reply("Forbidden", 403);
    if (request.method === "OPTIONS") return new Response(null, { headers: { ...cors, "Access-Control-Allow-Methods": "POST", "Access-Control-Allow-Headers": "Content-Type" } });
    if (request.method !== "POST") return reply("Method not allowed", 405);
    const ip = request.headers.get("CF-Connecting-IP");
    if (!ip || !(await env.BUBBLE_LIMITER.limit({ key: ip })).success) return reply("Rate limit", 429);
    if (Number(request.headers.get("Content-Length")) > 16384) return reply("Body too large", 413);
    let length = 0, chunks = [];
    const reader = request.body?.getReader(); if (!reader) return reply("Missing body", 400);
    while (true) { const part = await reader.read(); if (part.done) break; length += part.value.length; if (length > 16384) { await reader.cancel(); return reply("Body too large", 413); } chunks.push(part.value); }
    const bytes = new Uint8Array(length); let at = 0; for (const chunk of chunks) { bytes.set(chunk, at); at += chunk.length; }
    let body; try { body = JSON.parse(new TextDecoder().decode(bytes)); } catch { return reply("Invalid JSON", 400); }
    if (!Array.isArray(body.messages) || body.messages.length !== 2 || body.messages.some(m => !["system", "user"].includes(m.role) || typeof m.content !== "string")) return reply("Invalid messages", 400);
    const upstream = await fetch("https://api.groq.com/openai/v1/chat/completions", {
      method: "POST", headers: { "Content-Type": "application/json", "Authorization": `Bearer ${env.GROQ_API_KEY}` },
      body: JSON.stringify({ model: "openai/gpt-oss-120b", temperature: 0.7, max_tokens: 1500, response_format: { type: "json_object" }, messages: body.messages })
    });
    if (!upstream.ok) return reply("Upstream unavailable", upstream.status);
    return new Response(upstream.body, { headers: { ...cors, "Content-Type": "application/json" } });
  }
};
