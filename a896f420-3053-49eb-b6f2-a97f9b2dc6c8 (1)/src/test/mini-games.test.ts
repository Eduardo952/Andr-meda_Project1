import { describe, it, expect, beforeEach, vi } from "vitest";
import { assignTasks } from "@/tasks/assignments";
import { scoreTask, taskData, advanceTaskClock, ORIGINS, numeric } from "@/tasks/rules";
import { SOLS } from "@/data/campaign";
import { useCampaign } from "@/game/campaign";
import { useGame } from "@/game/store";
import { parseFlareClass } from "@/engine/flare";
import { cmeTravelHours } from "@/engine/cme";
import { validateNarration } from "@/services/narrator/validator";
import { buildRequest } from "@/services/narrator/facts";
import { deriveSol } from "@/engine/normalize";
import { templateFor } from "@/services/narrator/templates";
import en from "@/locales/en.json";
import pt from "@/locales/pt-BR.json";
vi.mock("@/services/narrator/speak", () => ({ speak: vi.fn() }));

describe("Sol task assignments", () => {
  it("assigns exactly one task per five sectors on every Sol", () => {
    for (let sol = 1; sol <= 5; sol++) {
      expect(assignTasks(sol)).toHaveLength(5);
      expect(assignTasks(sol).map(t => t.room).sort()).toEqual(["greenhouse", "lifeSupport", "observatory", "shield", "solar"]);
      expect(assignTasks(sol)).toEqual(assignTasks(sol));
    }
    expect(assignTasks(1).map(t => t.id)).toEqual(["SortFlares", "WireShield", "PipeFlow", "SmartFarmCare", "TiltSun"]);
    expect(assignTasks(2).map(t => t.id)).toEqual(["PhotoDetective", "CmeCountdown", "ThermostatDial", "SmartFarmCare", "CleanPanels"]);
  });
  it("keeps a CME task even when that Sol has no CME reading", () => {
    expect(assignTasks(5).find(t => t.room === "shield")?.id).toBe("CmeCountdown");
    const sol = SOLS[4]; if (!sol) throw new Error("Missing fixture");
    const data = taskData({ ...sol, cme: { ...sol.cme, value: null } });
    expect(data.cmePractice).toBe(true);
    expect(data.speed.value).toBe(SOLS.find(s => s.cme.value)?.cme.value?.speed);
  });
});
describe("provisional scoring", () => {
  it("gives three stars at 30 seconds with zero mistakes", () => { expect(scoreTask(30, 0)).toBe(3); expect(scoreTask(30.1, 0)).toBe(2); });
  it("gives two stars through 45 seconds and two mistakes", () => { expect(scoreTask(45, 2)).toBe(2); expect(scoreTask(45.1, 2)).toBe(1); expect(scoreTask(30, 3)).toBe(1); });
  it("gives one star through 60 seconds and none after timeout/failure", () => { expect(scoreTask(60, 10)).toBe(1); expect(scoreTask(60.1, 0)).toBe(0); expect(scoreTask(12, 0, false)).toBe(0); expect(scoreTask(-1, 0)).toBe(0); expect(scoreTask(20, -1)).toBe(0); });
});
describe("campaign task boundaries", () => {
  beforeEach(() => { useCampaign.getState().start(); useCampaign.getState().closeReport(); useGame.getState().setNear("control"); });
  const start = () => { const c = useCampaign.getState(); c.setAlloc("shield", 40); c.setAlloc("life", 40); c.setAlloc("greenhouse", 20); c.startSol(); };
  it("starts only with zero free energy in the 100 kWh pool and 5 kWh steps", () => {
    useCampaign.getState().setAlloc("shield", 12); expect(useCampaign.getState().alloc.shield).toBe(10);
    useCampaign.getState().startSol(); expect(useCampaign.getState().phase).toBe("plan");
    start(); expect(useCampaign.getState().phase).toBe("tasks"); expect(useCampaign.getState().confirmedAlloc).toEqual({ shield: 40, life: 40, greenhouse: 20 });
    useCampaign.getState().setAlloc("shield", 100); expect(useCampaign.getState().alloc.shield).toBe(40);
  });
  it("does not punish or complete cancellation/failure", () => {
    start(); const id = assignTasks(1)[0]?.id; if (!id) throw new Error("Missing task");
    useCampaign.getState().setOpenTask(id); useCampaign.getState().completeTask(id, false); useCampaign.getState().setOpenTask(null);
    expect(useCampaign.getState().results).toEqual({}); expect(useCampaign.getState().integrity).toBe(100); expect(useCampaign.getState().oxygen).toBe(100);
  });
  it("rewards a success once and computes stars rather than trusting supplied stars", () => {
    start(); useCampaign.setState({ integrity: 80, oxygen: 80 }); const id = assignTasks(1)[0]?.id; if (!id) throw new Error("Missing task");
    useCampaign.getState().setOpenTask(id); useCampaign.getState().finishMiniTask(id, { seconds: 30, mistakes: 0, stars: 99 });
    useCampaign.getState().finishMiniTask(id, { seconds: 30, mistakes: 0, stars: 99 });
    expect(useCampaign.getState().results[0]).toEqual([{ id, success: true, seconds: 30, mistakes: 0, stars: 3 }]);
    expect(useCampaign.getState().integrity).toBe(82); expect(useCampaign.getState().oxygen).toBe(82);
  });
  it("requires all tasks before sealing the chosen values and evaluates once", () => {
    start(); useCampaign.getState().endSol(); expect(useCampaign.getState().sealOpen).toBe(false); expect(useCampaign.getState().phase).toBe("tasks");
    for (const task of assignTasks(1)) { useCampaign.getState().setOpenTask(task.id); useCampaign.getState().finishMiniTask(task.id, { seconds: 25, mistakes: 0, stars: 3 }); }
    useCampaign.getState().endSol(); expect(useCampaign.getState().sealOpen).toBe(true);
    useCampaign.getState().sealSol({ shield: 30, life: 50, greenhouse: 20 }); expect(useCampaign.getState().phase).toBe("tasks");
    useCampaign.getState().sealSol({ shield: 40, life: 40, greenhouse: 20 }); expect(useCampaign.getState().phase).toBe("summary");
    const first = useCampaign.getState().outcome; useCampaign.getState().sealSol({ shield: 40, life: 40, greenhouse: 20 }); expect(useCampaign.getState().outcome).toBe(first);
  });
  it("cannot remotely end a Sol away from Control", () => { start(); useGame.getState().setNear("solar"); useCampaign.getState().endSol(); expect(useCampaign.getState().sealOpen).toBe(false); });
  it("pauses alarm countdowns in overlays", () => { start(); useCampaign.setState({ alarm: "cme", openTask: "SortFlares" }); useCampaign.getState().tickAlarm(20); expect(useCampaign.getState().alarmElapsed).toBe(0); useCampaign.getState().setOpenTask(null); useCampaign.getState().tickAlarm(20); expect(useCampaign.getState().alarmElapsed).toBe(20); });
});
describe("data and origins", () => {
  it("pauses the Sol clock and holds at day end without switching Sols", () => { expect(advanceTaskClock(.3, 0, 60, true)).toBe(.3); expect(advanceTaskClock(.99, 0, 100, false)).toBe(.999); });
  it("supports X above X10 and reuses the constant-speed calculation", () => {
    const flare = parseFlareClass("X28.2"); expect(flare?.magnitude).toBe(28.2);
    const sol = SOLS[1]; if (!sol) throw new Error("Missing snapshot"); const d = taskData(sol); expect(d.hours.value).toBe(cmeTravelHours(d.speed.value));
  });
  it("registers every number in each task data model", () => {
    for (const sol of SOLS) for (const value of Object.values(taskData(sol))) if (value && typeof value === "object" && "origin" in value) {
      expect(ORIGINS[value.origin]).toBeTruthy(); expect(Number.isFinite(value.value)).toBe(true);
    }
    expect(() => numeric(Number.NaN, "input")).toThrow();
  });
  it("English and Portuguese contain exactly the same nested keys", () => {
    const keys = (value: unknown, prefix = ""): string[] => value && typeof value === "object" ? Object.entries(value).flatMap(([k, v]) => keys(v, `${prefix}.${k}`)) : [prefix];
    expect(keys(en).sort()).toEqual(keys(pt).sort());
  });
  it("Bubble accepts real task metrics and rejects invented or incomplete tasks", () => {
    const sol = SOLS[0]; if (!sol) throw new Error("Missing snapshot");
    for (const lang of ["en", "pt-BR"] as const) {
      const req = buildRequest({ lang, sol, derived: deriveSol(sol), alloc: { shield: 40, life: 40, greenhouse: 20 }, outcome: null, phase: "task_complete", taskResults: [{ id: "SortFlares", seconds: 25, mistakes: 0, stars: 3 }] });
      expect(validateNarration(templateFor(req).text, req)).toBeNull();
    }
    const req = buildRequest({ lang: "en", sol, derived: deriveSol(sol), alloc: { shield: 40, life: 40, greenhouse: 20 }, outcome: null, phase: "task_complete", taskResults: [{ id: "SortFlares", seconds: 25, mistakes: 0, stars: 3 }] });
    expect(validateNarration("You completed Wire the Shield.", req)).toBe("mentions_incomplete_task");
    expect(validateNarration("You completed Sort the Flares with 3 mistakes.", req)).toBe("unknown_task_metric");
  });
});