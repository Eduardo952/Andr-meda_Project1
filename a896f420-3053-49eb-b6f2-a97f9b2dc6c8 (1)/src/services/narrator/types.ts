import type { Lang } from "@/i18n";

export type Severity = "calm" | "attention" | "alert";
export type NarratorPhase = "sol_start" | "alert" | "sol_summary" | "hint" | "answer" | "task_complete";
export type ResultStatus = "deficit" | "ideal" | "surplus";
export type QuestionId = "what_is_a_flare" | "why_shelter" | "why_cold_energy";
export const QUESTION_IDS: QuestionId[] = ["what_is_a_flare", "why_shelter", "why_cold_energy"];

export interface NarratorFacts {
  flare: { class: string; magnitude: number; source: "DONKI/FLR" } | null;
  cme: { speed_kms: number; source: "DONKI/CMEAnalysis" } | null;
  mars_impact: { level: "none" | "minor" | "glancing_blow" | "direct"; source: "DONKI/WSAEnlilSimulations" } | null;
  weather: { temp_avg_c: number; source: "InSight/AT" } | null;
  severity: Severity;
}

type Kwh = { shield: number; life: number; greenhouse: number };

/** Request sent to n8n. Game facts only, never personal data. */
export interface NarratorRequest {
  lang: Lang;
  sol: number;
  facts: NarratorFacts;
  demand_kwh: Kwh;
  player_kwh: Kwh;
  result: { shield: ResultStatus; life: ResultStatus; greenhouse: ResultStatus } | null;
  formula_version: string;
  phase: NarratorPhase;
  question_id: QuestionId | null;
  task_results?: { id: string; seconds: number; mistakes: number; stars: number }[];
  assigned_tasks?: string[];
}

export type Mood = "happy" | "worried" | "excited" | "proud";
export interface NarratorResponse { text: string; mood: Mood }
export interface NarratorLine extends NarratorResponse { source: "n8n" | "template" }
