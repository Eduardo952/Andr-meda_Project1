import fs from 'node:fs';
import path from 'node:path';
import ts from 'typescript';

// Oracles run the reference functions and campaign actions, with only UI/narration adapters replaced.
export async function exportConsequenceFixtures({source,output,get,emit,BALANCE,sols}) {
 const c=await get('engine/consequences'), outcome=await get('engine/outcome'), demand=await get('engine/demand');
 const {normalizeSnapshot,deriveSol}=await get('engine/normalize');
 const assignments=await get('tasks/assignments');
 const normalized=sols.map(s=>normalizeSnapshot(JSON.parse(s.raw)));
 const portableEvent=e=>({...e,cause:{...e.cause,hasAllocation:'allocated' in e.cause,hasCmeSpeed:'cmeSpeed' in e.cause,hasAirTempMean:'airTempMean' in e.cause,hasWindSpeed:'windSpeed' in e.cause,hasDust:'dust' in e.cause,hasAlarmPenalty:'alarmPenalty' in e.cause}});
 const portableStorm=s=>({...s,hasWindSpeed:s.windSpeed!==null,windSpeed:s.windSpeed??0});
 const portableOutcome=o=>({...o.systems,integrityDelta:o.integrityDelta,oxygenDelta:o.oxygenDelta});
 const cases=[];
 const dustSets=[[],[0,0,0,0],[55,45,35,25],[100,100,100,100],[-10,37.5,140,60,100]];
 for(let i=0;i<640;i++){
  const solIndex=i%5,wind=[null,0,12,12.01,25][Math.floor(i/5)%5],panelDust=dustSets[Math.floor(i/25)%5];
  const allocation=[{shield:40,life:40,greenhouse:20},{shield:0,life:0,greenhouse:0},{shield:50,life:30,greenhouse:20},{shield:55,life:25,greenhouse:20}][Math.floor(i/125)%4];
  const needed=i%4===0?allocation:sols[solIndex].demand, facts={flareClass:i%2?'M2.5':null,cmeSpeed:i%3?900:null,airTempMean:i%4?-65:null,windSpeed:wind};
  const storm=c.stormForSol(solIndex,wind),cleaned=i%3===0,shelterMissed=i%7===0,currentDose=i%101;
  const evaluated=outcome.evaluateSol(allocation,needed),events=c.evaluateConsequences({outcome:evaluated,allocation,demand:needed,facts,shelterMissed,currentDose,panelDust,storm,cleaned});
  const nextDust=c.panelDustAfterCleaning(panelDust,storm,cleaned);
  cases.push({solIndex,wind:wind??0,hasWind:wind!==null,panelDust,allocation,demand:needed,facts:{...facts,hasCmeSpeed:facts.cmeSpeed!==null,hasAirTempMean:facts.airTempMean!==null,hasWindSpeed:wind!==null},cleaned,shelterMissed,currentDose,
   storm:portableStorm(storm),outcome:portableOutcome(evaluated),events:events.map(portableEvent),totals:c.consequenceTotals(events),nextDust,loss:c.energyLossPct(panelDust),pool:c.effectiveEnergyPool(panelDust),nextPool:c.effectiveEnergyPool(nextDust)});
 }
 const cleaning=[-1,0,10,10.001,35,35.001,70,70.001,100].map(dust=>({dust,passes:c.cleaningPasses(dust)}));
 const tiers=[];for(const integrity of [0,49,50,79,80,100])for(const oxygen of [0,49,50,79,80,100])for(const dose of [0,24,25,59,60,100])tiers.push({integrity,oxygen,dose,tier:c.successTier(integrity,oxygen,dose)});
 let code=fs.readFileSync(path.join(source,'game/campaign.ts'),'utf8').replace(/^import[^;]+;\r?\n/gm,'').replace(/^.*void import\([^\n]+\n/gm,'');
 const scoring=fs.readFileSync(path.join(source,'tasks/rules.ts'),'utf8');
 emit('tasks/score','import { BALANCE } from "@/config/balance";\n'+scoring.slice(scoring.indexOf('export function scoreTask'),scoring.indexOf('export const ORIGINS')));
 const {scoreTask}=await get('tasks/score');
 code=ts.transpileModule(code,{compilerOptions:{target:ts.ScriptTarget.ES2022,module:ts.ModuleKind.CommonJS}}).outputText;
 const create=()=>initializer=>{let state;const set=patch=>{state={...state,...patch};};const get=()=>state;state=initializer(set,get);return {getState:get};};
 const names=['create','BALANCE','DERIVED','SOLS','freeEnergy','applyDelta','evaluateSol','tasksForSol','runtime','useGame','isMiniTask','scoreTask',...Object.keys(c)];
 const values=[create,BALANCE,normalized.map(deriveSol),normalized,demand.freeEnergy,outcome.applyDelta,outcome.evaluateSol,s=>assignments.assignTasks(s.sol),{}, {getState:()=>({say(){},nearStation:'control'})},assignments.isMiniTask,scoreTask,...Object.values(c)];
 const traces=[];
 for(const scenario of ['balanced','unprotected','no-oxygen','no-integrity']){
  const exports={};new Function('exports',...names,code)(exports,...values);
  const campaign=exports.useCampaign,steps=[];
  const record=(action,args={})=>{const s=campaign.getState();steps.push({action,...args,expected:{phase:s.phase,solIndex:s.solIdx,integrity:s.integrity,oxygen:s.oxygen,dose:s.dose,bonusStars:s.bonusStars,energyPool:c.effectiveEnergyPool(s.panelDust),panelDust:s.panelDust,cleanedThisSol:s.cleanedThisSol,shelterMissed:s.shelterMissed,alarm:s.alarm??'',alarmSeconds:s.alarmElapsed,failureCause:s.failureCause??'',events:s.consequences.map(portableEvent)}});};
  campaign.getState().start();record('start');
  while(campaign.getState().phase==='report'){
   campaign.getState().closeReport();record('closeReport');
   let s=campaign.getState(),pool=c.effectiveEnergyPool(s.panelDust);
   const allocation=scenario==='unprotected'?{shield:0,life:pool-20,greenhouse:20}:scenario==='no-oxygen'?{shield:pool,life:0,greenhouse:0}:scenario==='no-integrity'?{shield:0,life:pool-20,greenhouse:20}:{shield:40,life:pool-60,greenhouse:20};
   for(const [key,value] of Object.entries(allocation))campaign.getState().setAlloc(key,value);record('allocate',{allocation});
   campaign.getState().startSol();record('startSol');
   if(scenario==='unprotected'){campaign.getState().tickAlarm(60);record('tick',{seconds:60});}
   else{campaign.getState().clearAlarm(true);record('shelter');}
   for(const task of assignments.assignTasks(s.solIdx+1)){
    campaign.getState().setOpenTask(task.id);record('begin',{id:task.id,room:task.room});
    campaign.getState().tickAlarm(20);record('tick',{seconds:20});
    campaign.getState().finishMiniTask(task.id,{seconds:10,mistakes:0,stars:3});record('finish',{id:task.id});
    campaign.getState().finishMiniTask(task.id,{seconds:10,mistakes:0,stars:3});record('duplicate',{id:task.id});
   }
   campaign.getState().setSealOpen(true);campaign.getState().sealSol(allocation);record('seal',{allocation});
   campaign.getState().sealSol(allocation);record('doubleSeal',{allocation});
   campaign.getState().nextSol();record('next');
  }
  traces.push({scenario,steps});
 }
 fs.writeFileSync(path.join(output,'consequence-parity.json'),JSON.stringify({cases,cleaning,tiers,traces}));
}
