import { z } from "zod";

const ImageUrl = z.string().max(4096).url().refine(value => {
  const url = new URL(value);
  return url.protocol === "https:" && !url.username && !url.password;
});
export interface WebhookImage { url: string; title?: string | undefined; credit?: string | undefined }

/** n8n may wrap node outputs in arrays, json objects or JSON strings. */
export function parseWebhookResponse(input: unknown): { text?: string | undefined; mood: string; image?: WebhookImage | undefined } {
  let text: string | undefined, mood = "happy", image: WebhookImage | undefined;
  function visit(value: unknown, depth: number) {
    if (depth > 5) return;
    if (typeof value === "string") {
      try { visit(JSON.parse(value), depth + 1); } catch { /* Not structured output. */ }
      return;
    }
    if (Array.isArray(value)) { value.slice(0, 10).forEach(item => visit(item, depth + 1)); return; }
    if (!value || typeof value !== "object") return;
    const item = value as Record<string, unknown>;
    if (typeof item["text"] === "string" && !text) text = item["text"].slice(0, 600);
    if (["happy", "worried", "excited", "proud"].includes(String(item["mood"]))) mood = String(item["mood"]);
    const candidate = item["image_url"] ?? item["imageUrl"] ?? (typeof item["image"] === "string" ? item["image"] : undefined) ?? (item["media_type"] === "video" ? item["thumbnail_url"] : item["url"] ?? item["hdurl"]);
    const parsed = ImageUrl.safeParse(candidate);
    if (!image && parsed.success) image = {
      url: parsed.data,
      title: typeof item["title"] === "string" ? item["title"].slice(0, 200) : undefined,
      credit: typeof (item["copyright"] ?? item["credit"]) === "string" ? String(item["copyright"] ?? item["credit"]).slice(0, 200) : undefined,
    };
    for (const key of ["apod", "image", "output", "data", "json", "body", "result"]) visit(item[key], depth + 1);
  }
  visit(input, 0);
  return { text, mood, image };
}