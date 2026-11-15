import { create } from "zustand";
import { BALANCE } from "@/config/balance";
import { DERIVED, SOLS } from "@/data/campaign";
import { freeEnergy, type Allocation, type SystemId } from "@/engine/demand";
import { applyDelta, evaluateSol, type SolOutcome } from "@/engine/outcome";
import { tasksForSol, type TaskId } from "@/tasks/registry";
import { runtime } from "./runtime";
import { useGame } from "./store";
import type { RoomId } from "@/config/rooms";
import { isMiniTask } from "@/tasks/assignments";
import { scoreTask, type AttemptResult } from "@/tasks/rules";

export type Phase = "report" | "plan" | "tasks" | "summary" | "gameover" | "victory";

export interface TaskResult { id: TaskId; success: boolean; seconds?: number; mistakes?: number; stars?: number }

interface CampaignState {
  phase: Phase;
  solIdx: number;
  alloc: Allocation;
  integrity: number;
  oxygen: number;
  results: Record<number, TaskResult[]>;
  alarm: null | "sep" | "cme" | "flare";
  alarmStartedAt: number;
  outcome: SolOutcome | null;
  openTask: TaskId | null;
  energyOpen: boolean;
  reportOpen: boolean;
  sealOpen: boolean;
  confirmedAlloc: Allocation | null;
  alarmElapsed: number;
  start: () => void;
  closeReport: () => void;
  openReport: () => void;
  setAlloc: (k: SystemId, v: number) => void;
  setEnergyOpen: (v: boolean) => void;
  startSol: () => void;
  setOpenTask: (t: TaskId | null) => void;
  completeTask: (id: TaskId, success: boolean) => void;
  finishMiniTask: (id: TaskId, result: AttemptResult) => void;
  setSealOpen: (value: boolean) => void;
  sealSol: (values: Allocation) => void;
  tickAlarm: (seconds: number) => void;
  clearAlarm: (reachedShelter: boolean) => void;
  endSol: () => void;
  nextSol: () => void;
}

const EMPTY: Allocation = { shield: 0, life: 0, greenhouse: 0 };

export const useCampaign = create<CampaignState>()((set, get) => ({
  phase: "report",
  solIdx: 0,
  alloc: EMPTY,
  integrity: BALANCE.initialIntegrity,
  oxygen: BALANCE.initialOxygen,
  results: {},
  alarm: null,
  alarmStartedAt: 0,
  outcome: null,
  openTask: null,
  energyOpen: false,
  reportOpen: true, sealOpen: false, confirmedAlloc: null, alarmElapsed: 0,
  start: () => {
    set({ phase: "report", solIdx: 0, alloc: EMPTY, integrity: BALANCE.initialIntegrity, oxygen: BALANCE.initialOxygen, results: {}, alarm: null, outcome: null, openTask: null, energyOpen: false, reportOpen: true, sealOpen: false, confirmedAlloc: null, alarmElapsed: 0 });
    runtime.solTime = BALANCE.startSolFraction;
    useGame.getState().say("bubble.report");
  },
  closeReport: () => {
    const s = get();
    set({ reportOpen: false, phase: s.phase === "report" ? "plan" : s.phase });
    if (s.phase === "report") useGame.getState().say("bubble.plan");
  },
  openReport: () => set({ reportOpen: true }),
  setAlloc: (k, v) => {
    if (!Number.isFinite(v)) return;
    const a = { ...get().alloc };
    const others = BALANCE.energyPool - (a.shield + a.life + a.greenhouse - a[k]);
    a[k] = Math.max(0, Math.min(others, Math.round(v / BALANCE.sliderStep) * BALANCE.sliderStep));
    set({ alloc: a, confirmedAlloc: get().phase === "tasks" ? { ...a } : get().confirmedAlloc });
  },
  setEnergyOpen: (energyOpen) => set({ energyOpen }),
  startSol: () => {
    const s = get();
    if (s.phase !== "plan" && s.phase !== "tasks") return;
    if (freeEnergy(s.alloc) !== 0) return;
    if (s.phase === "tasks") { set({ energyOpen: false }); return; }
    const d = DERIVED[s.solIdx]; if (!d) return;
    runtime.solTime = s.solIdx + BALANCE.startSolFraction;
    set({ phase: "tasks", energyOpen: false, confirmedAlloc: { ...s.alloc }, alarm: d.alarm, alarmStartedAt: performance.now(), alarmElapsed: 0 });
    void import("@/services/narrator/speak").then((m) => m.speak(d.alarm ? "alert" : "sol_start"));
  },
  setOpenTask: (openTask) => {
    const s = get();
    if (openTask && (s.phase !== "tasks" || !pendingTasks(s.solIdx, s.results).some(t => t.id === openTask))) return;
    set({ openTask });
  },
  completeTask: (id, success) => {
    if (isMiniTask(id) || !success) return;
    const s = get();
    const list = s.results[s.solIdx] ?? [];
    if (list.some((r) => r.id === id)) { set({ openTask: null }); return; }
    const R = BALANCE.taskReward;
    set({
      results: { ...s.results, [s.solIdx]: [...list, { id, success }] },
      openTask: null,
      integrity: success ? applyDelta(s.integrity, R.integrity) : s.integrity,
      oxygen: success ? applyDelta(s.oxygen, R.oxygen) : s.oxygen,
    });
    useGame.getState().say(success ? "bubble.taskDone" : "bubble.taskTry");
  },
  finishMiniTask: (id, result) => {
    const s = get();
    if (s.phase !== "tasks" || s.openTask !== id || !isMiniTask(id) || !pendingTasks(s.solIdx, s.results).some(t => t.id === id)) return;
    const stars = scoreTask(result.seconds, result.mistakes);
    if (!stars) return;
    const list = s.results[s.solIdx] ?? [];
    set({ results: { ...s.results, [s.solIdx]: [...list, { id, success: true, seconds: result.seconds, mistakes: result.mistakes, stars }] }, openTask: null,
      integrity: applyDelta(s.integrity, BALANCE.taskReward.integrity), oxygen: applyDelta(s.oxygen, BALANCE.taskReward.oxygen) });
    void import("@/services/narrator/speak").then(m => m.speak("task_complete"));
  },
  setSealOpen: (sealOpen) => {
    const s = get();
    if (sealOpen && (s.phase !== "tasks" || pendingTasks(s.solIdx, s.results).length || freeEnergy(s.alloc) !== 0)) return;
    set({ sealOpen });
  },
  sealSol: values => {
    const s = get(); const confirmed = s.confirmedAlloc;
    if (!s.sealOpen || !confirmed || !(["shield", "life", "greenhouse"] as const).every(k => values[k] === confirmed[k])) return;
    if (s.phase !== "tasks" || pendingTasks(s.solIdx, s.results).length || freeEnergy(s.alloc) !== 0) return;
    const derived = DERIVED[s.solIdx]; if (!derived) return;
    const outcome = evaluateSol(s.alloc, derived.demand);
    set({ outcome, integrity: applyDelta(s.integrity, outcome.integrityDelta), oxygen: applyDelta(s.oxygen, outcome.oxygenDelta),
      phase: "summary", openTask: null, energyOpen: false, sealOpen: false, alarm: null });
    void import("@/services/narrator/speak").then(m => m.speak("sol_summary"));
  },
  tickAlarm: seconds => {
    const s = get();
    if (!s.alarm || s.phase !== "tasks" || s.openTask || s.energyOpen || s.sealOpen || s.reportOpen) return;
    const alarmElapsed = Math.min(BALANCE.shelterSeconds, s.alarmElapsed + Math.max(0, seconds));
    set({ alarmElapsed });
    if (alarmElapsed >= BALANCE.shelterSeconds) get().clearAlarm(false);
  },
  clearAlarm: (reachedShelter) => {
    const s = get();
    if (!s.alarm) return;
    const sol = SOLS[s.solIdx]; if (!sol) return;
    set({ alarm: null });
    useGame.getState().say(reachedShelter ? "bubble.safe" : "bubble.alarmOver");
  },
  endSol: () => {
    // No remote action or elapsed clock can skip walking to Control and sealing.
    if (useGame.getState().nearStation === "control") get().setSealOpen(true);
  },
  nextSol: () => {
    const s = get();
    if (s.phase !== "summary") return;
    if (s.integrity <= 0 || s.oxygen <= 0) { set({ phase: "gameover" }); return; }
    if (s.solIdx + 1 >= BALANCE.campaignSols) { set({ phase: "victory" }); return; }
    runtime.solTime = s.solIdx + 1 + BALANCE.startSolFraction;
    set({ solIdx: s.solIdx + 1, phase: "report", reportOpen: true, alloc: EMPTY, outcome: null, sealOpen: false, confirmedAlloc: null, alarmElapsed: 0 });
    useGame.getState().say("bubble.report");
  },
}));

export function pendingTasks(solIdx: number, results: Record<number, TaskResult[]>) {
  const done = new Set((results[solIdx] ?? []).filter(r => r.success).map((r) => r.id));
  const sol=SOLS[solIdx];return sol?tasksForSol(sol).filter(t=>!done.has(t.id)):[];
}

/** Where the guide arrow points. */
export function currentTarget(): RoomId | null {
  const c = useCampaign.getState();
  if (c.phase === "plan") return "control";
  if (c.phase !== "tasks") return null;
  if (c.alarm) return "shield";
  const next = pendingTasks(c.solIdx, c.results).find((t) => !t.inWorld);
  return next?.room ?? "control";
}

export function totalTaskCount() {
  return SOLS.reduce((n, s) => n + tasksForSol(s).length, 0);
}
