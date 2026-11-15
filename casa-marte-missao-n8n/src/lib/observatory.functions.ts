import { createServerFn } from "@tanstack/react-start";
import { z } from "zod";
import { RAW_SNAPSHOTS } from "@/data/snapshots";
import { MISSION_ID_RE, mergeN8nSol, n8nSolCache, resolveN8n } from "./n8n";
import { ApodSchema, DateCache, loadApod, loadLibraryPicture, type ObservatoryData, type LibraryPicture } from "./observatory-data";
const cache=new DateCache<ObservatoryData>();
/** Order: n8n webhook (GET /nasa-sol) -> direct NASA with NASA_API_KEY -> frozen local snapshot. */
export const getObservatory=createServerFn({method:"GET"}).inputValidator((input:unknown)=>z.object({sol:z.number().int().min(1).max(5),mission:z.string().regex(MISSION_ID_RE).optional()}).parse(input)).handler(async({data}):Promise<ObservatoryData>=>{
 const snapshot=RAW_SNAPSHOTS[data.sol-1];if(!snapshot)throw new Error("Unknown Sol");
 const live=await n8nSolCache.get(resolveN8n(process.env),data.sol,{mission:data.mission});
 if(live?.apod&&live.photo)return mergeN8nSol(live,null);
 const key=process.env["NASA_API_KEY"]??(process.env["NASA_ALLOW_DEMO_KEY"]==="true"?"DEMO_KEY":undefined);
 const date=snapshot.earth_date;const fallback=ApodSchema.safeParse(snapshot.apod);
 return cache.get(date,async()=>{
  const photo:LibraryPicture|null=snapshot.image_library.url&&snapshot.image_library.title?{id:snapshot.image_library.nasa_id,url:snapshot.image_library.url,title:snapshot.image_library.title,description:snapshot.image_library.description??"",credit:snapshot.image_library.credit}:null;
  const [result,library]=await Promise.all([loadApod(date,key,fallback.success?fallback.data:null),loadLibraryPicture(snapshot.image_library.nasa_id,photo)]);
  return{date,...result,...library,sample:snapshot.sample};
 }).then(base=>live?mergeN8nSol(live,base):base);
});