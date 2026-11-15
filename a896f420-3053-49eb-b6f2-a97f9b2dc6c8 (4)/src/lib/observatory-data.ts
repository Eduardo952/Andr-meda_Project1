import { z } from "zod";
export const ApodSchema=z.object({date:z.string(),title:z.string(),explanation:z.string().default(""),media_type:z.enum(["image","video"]),url:z.string().url().refine(v=>v.startsWith("https://")),thumbnail_url:z.string().url().optional(),copyright:z.string().optional()});
export type Apod=z.infer<typeof ApodSchema>;
export interface LibraryPicture {title:string;description:string;credit:string;url:string;id:string}
export interface ObservatoryData {date:string;apod:Apod|null;photo:LibraryPicture|null;source:"live"|"snapshot"|"unavailable";sample:boolean;photoSource?:"live"|"snapshot"|"unavailable"}
export function validEarthDate(value:string){return /^\d{4}-\d{2}-\d{2}$/.test(value)&&Number.isFinite(new Date(value+"T00:00:00Z").getTime())&&new Date(value+"T00:00:00Z").toISOString().slice(0,10)===value;}
export class DateCache<T>{
 private values=new Map<string,{value:T;at:number}>();private pending=new Map<string,Promise<T>>();
 async get(date:string,load:()=>Promise<T>,now=Date.now()):Promise<T>{const saved=this.values.get(date);if(saved&&now-saved.at<86400000)return saved.value;const active=this.pending.get(date);if(active)return active;const request=load().then(value=>{this.values.set(date,{value,at:now});if(this.values.size>32){const oldest=this.values.keys().next().value;if(oldest)this.values.delete(oldest);}return value;}).finally(()=>this.pending.delete(date));this.pending.set(date,request);return request;}
}
export async function loadApod(date:string,key:string|undefined,snapshot:Apod|null,fetcher:typeof fetch=fetch):Promise<{apod:Apod|null;source:ObservatoryData["source"]}>{
 const fallback={apod:snapshot,source:snapshot?"snapshot" as const:"unavailable" as const};
 if(!validEarthDate(date)||!key)return fallback;
 try{const response=await fetcher(`https://api.nasa.gov/planetary/apod?date=${encodeURIComponent(date)}&thumbs=true&api_key=${encodeURIComponent(key)}`,{signal:AbortSignal.timeout(6000)});if(!response.ok)return fallback;const parsed=ApodSchema.safeParse(await response.json());if(!parsed.success||parsed.data.date!==date)return fallback;return{apod:parsed.data,source:"live"};}catch{return fallback;}
}

/** Library pictures are independent of the campaign's provisional Earth dates. */
export async function loadLibraryPicture(id:string|undefined,snapshot:LibraryPicture|null,fetcher:typeof fetch=fetch):Promise<{photo:LibraryPicture|null;photoSource:"live"|"snapshot"|"unavailable"}>{
 const fallback={photo:snapshot,photoSource:snapshot?"snapshot" as const:"unavailable" as const};
 const params=new URLSearchParams({media_type:"image",page_size:"6"});
 if(id&&!id.startsWith("SAMPLE-"))params.set("nasa_id",id);
 else params.set("q","Mars surface Curiosity");
 try{
  const response=await fetcher(`https://images-api.nasa.gov/search?${params}`,{signal:AbortSignal.timeout(6000)});
  if(!response.ok)return fallback;
  const body=await response.json() as {collection?:{items?:{data?:{nasa_id?:string;title?:string;description?:string;photographer?:string;center?:string}[];links?:{href?:string;rel?:string}[]}[]}};
  for(const item of body.collection?.items??[]){
   const info=item.data?.[0],url=item.links?.find(link=>link.rel==="preview"&&link.href?.startsWith("https://"))?.href;
   if(info?.nasa_id&&info.title&&url)return{photo:{id:info.nasa_id,title:info.title,description:info.description??"",credit:info.photographer??info.center??"NASA",url},photoSource:"live"};
  }
 }catch{/* Preserve an authentic snapshot when the library cannot be reached. */}
 return fallback;
}