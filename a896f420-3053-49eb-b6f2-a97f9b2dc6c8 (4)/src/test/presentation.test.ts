import { describe,it,expect } from "vitest";
import { advanceWalk,alertStage,enqueueLine,FIRST_WALK,guidedTarget,labelCandidate,orderAlerts,type ActiveAlert } from "@/game/presentation";
describe("presentation rules",()=>{
 it("prioritizes an urgent task over a tapped map destination",()=>expect(guidedTarget("shield","solar","control",true)).toBe("shield"));
 it("allows map guidance without replacing the campaign destination",()=>expect(guidedTarget("observatory","greenhouse","control",false)).toBe("greenhouse"));
 it("selects the nearest station instead of a second region label",()=>expect(labelCandidate("control","observatory")).toBe("control"));
 it("visits Energy then each of the five regions without skipping stops",()=>{expect(FIRST_WALK).toEqual(["control","observatory","shield","lifeSupport","greenhouse","solar"]);expect(advanceWalk(0,"shield")).toBe(0);expect(advanceWalk(0,"control")).toBe(1);});
 it("queues speech sequentially without stacking duplicate messages",()=>{expect(enqueueLine([{id:1},{id:2}],{id:3})).toEqual([{id:1},{id:2},{id:3}]);expect(enqueueLine([{id:1}],{id:1})).toEqual([{id:1}]);});
 it("prioritizes radiation above life support, dust and information",()=>{expect(orderAlerts([{id:"i",kind:"info" as const},{id:"d",kind:"dust" as const},{id:"s",kind:"solar" as const},{id:"l",kind:"lifeSupport" as const}]).map(a=>a.id)).toEqual(["s","l","d","i"]);});
 it("holds a shelter alarm in emergency, then recovers for three seconds",()=>{const active:ActiveAlert={id:"solar-alarm",kind:"solar",hold:true,elapsed:3,released:false};expect(alertStage(active)).toBe("emergency");expect(alertStage({...active,elapsed:20})).toBe("emergency");expect(alertStage({...active,released:true,elapsed:5.6})).toBe("recovery");expect(alertStage({...active,released:true,elapsed:8.6})).toBeNull();});
});
