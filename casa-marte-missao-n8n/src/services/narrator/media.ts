import { create } from "zustand";
import type { WebhookImage } from "@/lib/webhook-response";

/** Presentation-only images, keyed by Sol; never overwrite NASA or campaign data. */
export const useWebhookMedia = create<{ images: Record<number, WebhookImage>; accept: (sol: number, image: WebhookImage) => void }>(set => ({
  images: {},
  accept: (sol, image) => set(state => ({ images: { ...state.images, [sol]: image } })),
}));