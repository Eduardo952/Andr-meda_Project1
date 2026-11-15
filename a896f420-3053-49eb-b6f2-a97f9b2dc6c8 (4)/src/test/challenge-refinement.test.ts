import { describe, it, expect } from "vitest";
import { SECTOR_TASKS, assignTasks } from "@/tasks/assignments";
import { IRRIGATION, irrigationNeeds, irrigationCorrect, rewardPreview, priorityFor, observationImpact } from "@/tasks/context";
import { telescopeRoute, stepTelescope } from "@/game/sim/telescope";
import { TELESCOPE } from "@/config/telescope";
import { SOLS } from "@/data/campaign";
import { isWalkable } from "@/game/sim/world";

describe("contextual station rules",()=>{
 it("has at least two different variants for every sector",()=>{for(const tasks of Object.values(SECTOR_TASKS))expect(new Set(tasks).size).toBeGreaterThanOrEqual(2);});
 it("keeps five reproducible assignments on all five Sols",()=>{for(let sol=1;sol<=5;sol++){expect(assignTasks(sol)).toHaveLength(5);expect(assignTasks(sol)).toEqual(assignTasks(sol));}});
 it("waters only missing training moisture within three available drops",()=>{expect(irrigationNeeds()).toEqual([2,0,1]);expect(irrigationNeeds().reduce((a,b)=>a+b,0)).toBe(IRRIGATION.water);expect(irrigationCorrect([2,0,1])).toBe(true);expect(irrigationCorrect([1,1,1])).toBe(false);});
 it("shows existing plus-two rewards without exceeding maximum",()=>{expect(rewardPreview(80,99)).toEqual({integrity:{before:80,after:82},oxygen:{before:99,after:100}});expect(rewardPreview(100,100).oxygen.after).toBe(100);});
 it("prioritizes the current oxygen shortfall before heating",()=>{const sol=SOLS[0];if(!sol)throw Error("fixture");expect(priorityFor(99,sol)).toBe("air");expect(priorityFor(100,sol)).toBe("heat");});
 it("does not infer an arrival from an absent forecast",()=>{const sol=SOLS[0];if(!sol)throw Error("fixture");expect(observationImpact({...sol,marsImpact:{...sol.marsImpact,value:null}})).toBe("unknown");expect(observationImpact({...sol,marsImpact:{...sol.marsImpact,value:{impact:false,glancing:false,arrival:null}}})).toBe("no");});
 it("walks to the eyepiece from either side without changing the walkable floor",()=>{for(const offset of [-3,3]){const mover={x:TELESCOPE.x+offset,z:TELESCOPE.z,vx:0,vz:0,heading:0,speed:0};const path=telescopeRoute(mover.x,mover.z);let arrived=false;for(let i=0;i<1200&&!arrived;i++){arrived=stepTelescope(mover,path,.02);expect(isWalkable(mover.x,mover.z)).toBe(true);}expect(arrived).toBe(true);expect(Math.hypot(mover.x-TELESCOPE.standX,mover.z-TELESCOPE.standZ)).toBeLessThanOrEqual(.15);}});
});
