import { describe, expect, it } from "vitest";
import { parseWebhookResponse } from "@/lib/webhook-response";
import { useWebhookMedia } from "@/services/narrator/media";

describe("webhook images", () => {
  it("preserves image_url alongside narrator text", () => {
    expect(parseWebhookResponse({ text: "Look at Mars.", mood: "excited", image_url: "https://images-assets.nasa.gov/mars.jpg" })).toEqual({ text: "Look at Mars.", mood: "excited", image: { url: "https://images-assets.nasa.gov/mars.jpg", title: undefined, credit: undefined } });
  });
  it("reads nested n8n APOD outputs with attribution", () => {
    const result = parseWebhookResponse([{ json: { output: JSON.stringify({ text: "Look.", apod: { url: "https://apod.nasa.gov/mars.jpg", title: "Mars", copyright: "NASA", media_type: "image" } }) } }]);
    expect(result.image).toEqual({ url: "https://apod.nasa.gov/mars.jpg", title: "Mars", credit: "NASA" });
    expect(result.text).toBe("Look.");
  });
  it("accepts image-only output and camel-case fields", () => {
    expect(parseWebhookResponse({ imageUrl: "https://example.org/mars.jpg" }).image?.url).toBe("https://example.org/mars.jpg");
    expect(parseWebhookResponse({ image: { url: "https://example.org/mars.jpg" } }).image?.url).toBe("https://example.org/mars.jpg");
  });
  it("uses a video thumbnail rather than embedding a video as an image", () => {
    expect(parseWebhookResponse({ media_type: "video", url: "https://example.org/video", thumbnail_url: "https://example.org/thumb.jpg" }).image?.url).toBe("https://example.org/thumb.jpg");
  });
  it("rejects unsafe URLs and credential-bearing images", () => {
    for (const image_url of ["javascript:alert(1)", "http://example.org/mars.jpg", "https://user:secret@example.org/mars.jpg"]) expect(parseWebhookResponse({ image_url }).image).toBeUndefined();
  });
  it("keeps images isolated by Sol", () => {
    useWebhookMedia.setState({ images: {} });
    useWebhookMedia.getState().accept(1, { url: "https://example.org/one.jpg" });
    useWebhookMedia.getState().accept(2, { url: "https://example.org/two.jpg" });
    expect(useWebhookMedia.getState().images[1]?.url).toBe("https://example.org/one.jpg");
    expect(useWebhookMedia.getState().images[3]).toBeUndefined();
  });
});