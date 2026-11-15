import { describe, expect, it } from "vitest";
import { MODULES, PATHS } from "@/config/rooms";
import { terrainClearance, terrainHeight, CRATERS } from "@/game/terrain";

describe("cosmetic terrain relief",()=>{
  it("keeps every module center and station on the original flat plane",()=>{
    for(const m of MODULES){expect(terrainHeight(m.x,m.z)).toBe(-.04);expect(terrainHeight(...m.entrance)).toBe(-.04);}
  });
  it("keeps connecting routes flat",()=>{
    for(const p of PATHS)for(let i=0;i<=20;i++){const t=i/20;expect(terrainHeight(p.a.x+(p.b.x-p.a.x)*t,p.a.z+(p.b.z-p.a.z)*t)).toBe(-.04);}
  });
  it("places crater relief outside buildings and routes",()=>{
    for(const c of CRATERS)expect(terrainClearance(c.x,c.z)).toBeGreaterThan(2);
    expect(terrainHeight(-55,-30)).not.toBe(terrainHeight(-55,-25));
  });
});