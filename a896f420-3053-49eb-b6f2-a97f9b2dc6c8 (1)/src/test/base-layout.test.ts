import {describe,it,expect} from "vitest";
import {MODULES,REGIONS,ROOMS,SPAWN} from "@/config/rooms";
import {isWalkable,routeBetween,stationPosition,roomAt} from "@/game/sim/world";
import {BALANCE} from "@/config/balance";
import {TASK_BY_ID} from "@/tasks/registry";
import {roofOpacity} from "@/game/sim/visibility";
describe("Connected Base ARES",()=>{
 it("has exactly the five requested regions",()=>expect(REGIONS.slice().sort()).toEqual(["greenhouse","lifeSupport","observatory","shield","solar"]));
 it("has no extra accommodation or service rooms",()=>expect(MODULES.map(m=>m.id).sort()).toEqual(["airlock","control","greenhouse","lifeSupport","observatory","shield","solar"]));
 it("starts inside the walkable Control Room",()=>{expect(isWalkable(SPAWN.x,SPAWN.z)).toBe(true);expect(roomAt(SPAWN.x,SPAWN.z)).toBe("control");});
 it("connects every interior station and Solar Park",()=>{for(const m of ROOMS){const route=routeBetween(SPAWN.x,SPAWN.z,...m.entrance);expect(route.length,m.id).toBeGreaterThan(0);for(const p of route)expect(isWalkable(p.x,p.z),m.id).toBe(true);}});
 it("leaves main doors open and walls solid",()=>{expect(isWalkable(0,-33)).toBe(true);expect(isWalkable(7,-33)).toBe(false);expect(isWalkable(0,52)).toBe(true);expect(isWalkable(0,62)).toBe(true);});
 it("retains the 100 kWh pool and disables weather modifier",()=>{expect(BALANCE.energyPool).toBe(100);expect(BALANCE.marsWeatherModifier).toBe(false);});
 it("places Observatory tasks in Observatory",()=>{for(const id of ["T02","T10","T11"] as const)expect(TASK_BY_ID[id].room).toBe("observatory");});
 it("places all four shield tasks in Shield",()=>{for(const id of ["T03","T04","T05","T06"] as const)expect(TASK_BY_ID[id].room).toBe("shield");});
 it("places life and plantation tasks correctly",()=>{expect(TASK_BY_ID.T07.room).toBe("lifeSupport");expect(TASK_BY_ID.T15.room).toBe("lifeSupport");expect(TASK_BY_ID.T14.room).toBe("greenhouse");expect(TASK_BY_ID.T08.room).toBe("solar");});
 it("keeps emergency refuge routes below 60 seconds",()=>{for(const r of ROOMS){const route=routeBetween(...r.entrance,...stationPosition("shield"));expect(route.length).toBeGreaterThan(0);const length=route.reduce((s,p,i)=>{const prev=route[i-1];return s+(prev?Math.hypot(p.x-prev.x,p.z-prev.z):0);},0);expect(length/BALANCE.walkSpeed+2).toBeLessThan(60);}});
 it("hides roofs in Off and ghosts roofs for isometric or occupied rooms",()=>{expect(roofOpacity("off",false,false,0)).toBe(0);expect(roofOpacity("auto",true,false,0)).toBe(.025);expect(roofOpacity("auto",false,true,0)).toBe(.025);expect(roofOpacity("auto",false,false,0)).toBe(.3);});
});
