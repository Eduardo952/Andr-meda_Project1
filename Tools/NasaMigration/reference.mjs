import fs from 'node:fs';
import vm from 'node:vm';
const markdown=fs.readFileSync('Anexo/prompt_migracao_nasa_groq_unity.md','utf8').replace(/\r\n/g,'\n');
const sections=[...markdown.matchAll(/## A([1-5])\.[\s\S]*?```js\n([\s\S]*?)\n```/g)];
if(sections.length!==5)throw new Error('Expected all five Appendix A nodes');
const code=Object.fromEntries(sections.map(m=>[m[1],m[2]]));
const today='2026-11-15';
class FixedDate extends Date { static now(){return Date.parse(today+'T00:00:00Z');} }
function run(n,nodes={},input={}) {
 const context={Date:FixedDate,$input:{first:()=>({json:input})},$:name=>({first:()=>({json:nodes[name]?.[0]}),all:()=>{if(!nodes[name])throw new Error('missing node');return nodes[name].map(json=>({json}));}})};
 return vm.runInNewContext('(function(){'+code[n]+'})()',context)[0].json;
}
export function reference(mission,sol,fixtures={}) {
 const config=run(1,{}, {query:{mission,sol}});
 const window=run(2,{'CONFIG':[config],'DONKI FLR (bloco)':fixtures.flr??[]});
 const response=run(3,{'CONFIG':[config],'Escolher janela':[window],'APOD':fixtures.apod??[],'DONKI CME':fixtures.cme??[],'DONKI WSA-Enlil':fixtures.enlil??[],'Clima Marte (Curiosity)':fixtures.weather??[],'NASA Images':fixtures.images??[]});
 return {plan:config,window,response};
}
const output={today,cases:[],narrator:[],prepared:[]};
for(const mission of ['mission-ABC12345','test_mission_001'])for(let sol=1;sol<=5;sol++) {
 const empty=reference(mission,sol);const dates=empty.window.mission_window.earth_dates;
 const sources={flr:[{classType:'X2.7',peakTime:dates[0]+'T12:00:00Z'},{classType:'M3.4',beginTime:dates[2]+'T08:00:00Z'},{classType:'C4.1',peakTime:dates[4]+'T10:00:00Z'},{classType:'invalid'}],
 apod:[{media_type:'video',url:'https://example.org/video'},{date:'2001-01-02',media_type:'image',title:{rendered:'A nebula'},explanation:{rendered:'<b>Real</b> light.'},hdurl:'https://example.org/image.jpg'}],
 cme:[{speed:'600'},{speed:1250},{speed:null},{speed:'invalid'}],
 enlil:[{impactList:[{location:'Mars',isGlancingBlow:true,arrivalTime:'2001-02-01T00:00Z'},{location:'mars',isGlancingBlow:false,arrivalTime:'2001-02-02T00:00Z'}]}],
 weather:[{sol:1000,min_temp:'-80.5 C',max_temp:'-20.2',pressure:'720',atmo_opacity:'Sunny',terrestrial_date:'2001-01-03'}],
 images:[{collection:{items:[{data:[{nasa_id:'mars-1',title:'Mars',description:'Terrain',center:'JPL'}],links:[{rel:'preview',href:'https://example.org/mars.jpg'}]}]}}]};
 output.cases.push({mission,sol,sources,...reference(mission,sol,sources)});
 if(sol===1)for(const reason of ['no_flare_data','calm_period']) {
 const source={flr:reason==='calm_period'?[{classType:'C2.0',peakTime:dates[0]+'T00:00Z'}]:[]};
 output.cases.push({mission,sol,sources:source,...reference(mission,sol,source)});
 }
}
for(const value of [
 {output:'<think>private reasoning</think>```json\n{"text":"First. Second! Third? Fourth.","mood":"proud","off_topic":true}\n```'},
 {output:{text:'Hello astronaut.',mood:'worried',off_topic:false}},
 {text:'Plain text. With a second sentence.'},
 {response:'before {"text":"Nested {brace} here.","mood":"excited"} after'},
 {output:'{not json}'},{output:''},{message:{content:'{"text":"Four. Five. Six. Seven.","mood":"invalid","off_topic":"true"}'}}
])output.narrator.push({input:value,expected:run(5,{},value)});
for(const phase of ['sol_start','alert','hint','sol_summary','task_complete','answer','free_question']) {
 const input={lang:'pt-BR',sol:1,facts:{severity:'calm'},phase,question_id:'why_real_data',question_text:'Olá https://example.com teste@demo.org 123456 <Marte> {} ` ciência?'};
 const built=run(4,{'CONFIG':[{cfg:{GROQ_MODEL:'llama-3.3-70b-versatile'},body:input}]});
 output.prepared.push({input,expected:JSON.parse(built.body.messages[1].content)});
}
fs.mkdirSync('Tools/NasaMigration/Fixtures',{recursive:true});
fs.writeFileSync('Tools/NasaMigration/Fixtures/reference.json',JSON.stringify(output,null,2));
console.log('Original Appendix A executed: '+output.cases.length+' Sol cases, '+output.prepared.length+' phases, '+output.narrator.length+' parser cases.');
