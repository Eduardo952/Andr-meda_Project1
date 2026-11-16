import fs from 'node:fs';
import assert from 'node:assert/strict';
import {reference} from './reference.mjs';
const live=JSON.parse(fs.readFileSync('Logs/nasa-live.json','utf8'));
function raw(path, query){
 const hit=Object.entries(live.raw).find(([url])=>{const u=new URL(url);return u.pathname===path && Object.entries(query).every(([k,v])=>u.searchParams.get(k)===String(v));});
 if(!hit)return [];
 const data=JSON.parse(hit[1]);if(data?.error)return [];return Array.isArray(data)?data:[data];
}
for(const row of live.cases){
 const {mission,sol}=row,plan=reference(mission,sol).plan;
 const flr=raw('/DONKI/FLR',{startDate:plan.block_start,endDate:plan.block_end});
 const window=reference(mission,sol,{flr}).window;
 const sources={flr,apod:raw('/planetary/apod',{start_date:plan.apod_start,end_date:plan.apod_end}),
 cme:raw('/DONKI/CMEAnalysis',{startDate:window.earth_date}),enlil:raw('/DONKI/WSAEnlilSimulations',{startDate:window.d_prev}),
 weather:raw('/'+window.curiosity_sol,{}),images:raw('/search',{page:plan.photo_page})};
 assert.deepStrictEqual(row.response,JSON.parse(JSON.stringify(reference(mission,sol,sources).response)),`${mission} Sol ${sol}`);
}
console.log('PASS: 10 live C# SolResponse objects equal Appendix A JavaScript, with identical NASA inputs and UTC date.');
