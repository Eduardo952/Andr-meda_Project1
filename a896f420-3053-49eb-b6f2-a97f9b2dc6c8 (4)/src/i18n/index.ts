import { useCallback } from "react";
import en from "@/locales/en.json";
import ptBR from "@/locales/pt-BR.json";
import { useGame } from "@/game/store";

export type Lang = "en" | "pt-BR";
export const LANGS: Lang[] = ["en", "pt-BR"];
type Dict = Record<string, unknown>;
const DICTS: Record<Lang, Dict> = { en, "pt-BR": ptBR };

function lookup(dict: Dict, key: string): string | undefined {
  const v = key.split(".").reduce<unknown>((o, k) => (o && typeof o === "object" ? (o as Dict)[k] : undefined), dict);
  return typeof v === "string" ? v : undefined;
}

export type Vars = Record<string, string | number>;

/** Translate with English fallback when a pt-BR key is missing. */
export function translate(lang: Lang, key: string, vars?: Vars): string {
  let s = lookup(DICTS[lang], key) ?? lookup(DICTS.en, key) ?? key;
  if (vars) for (const [k, v] of Object.entries(vars)) s = s.split(`{{${k}}}`).join(String(v));
  return s;
}

export function formatNumber(lang: Lang, n: number, opts?: Intl.NumberFormatOptions): string {
  return new Intl.NumberFormat(lang, opts).format(n);
}

export function useT() {
  const lang = useGame((s) => s.lang);
  const t = useCallback((key: string, vars?: Vars) => translate(lang, key, vars), [lang]);
  const n = useCallback((v: number, opts?: Intl.NumberFormatOptions) => formatNumber(lang, v, opts), [lang]);
  return { t, n, lang };
}
