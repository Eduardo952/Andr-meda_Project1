import { describe,it,expect,vi } from "vitest";
import { DateCache,loadApod,loadLibraryPicture,validEarthDate,type Apod } from "@/lib/observatory-data";
const apod:Apod={date:"2026-11-14",title:"Solar view",explanation:"The Sun",media_type:"image",url:"https://example.org/sun.jpg",copyright:"Photographer"};
describe("APOD server data rules",()=>{
 it("loads attributed library photos for sample dates without requesting a sample NASA ID",async()=>{
  const fetcher=vi.fn().mockResolvedValue(Response.json({collection:{items:[{data:[{nasa_id:"PIA123",title:"Mars surface",center:"JPL"}],links:[{rel:"preview",href:"https://images-assets.nasa.gov/mars.jpg"}]}]}}));
  const result=await loadLibraryPicture("SAMPLE-IMG-1",null,fetcher);
  expect(fetcher.mock.calls[0]?.[0]).not.toContain("SAMPLE-IMG-1");
  expect(result).toEqual({photo:{id:"PIA123",title:"Mars surface",description:"",credit:"JPL",url:"https://images-assets.nasa.gov/mars.jpg"},photoSource:"live"});
 });
 it("keeps an authentic library snapshot on failure",async()=>{
  const photo={id:"PIA123",title:"Mars",description:"",credit:"NASA",url:"https://images-assets.nasa.gov/mars.jpg"};
  expect(await loadLibraryPicture(photo.id,photo,vi.fn().mockRejectedValue(new Error("offline")))).toEqual({photo,photoSource:"snapshot"});
 });
 it("rejects sample dates",()=>expect(validEarthDate("SAMPLE-DATE-1")).toBe(false));
 it("does not call NASA without an allowed key",async()=>{const fetcher=vi.fn();expect(await loadApod(apod.date,undefined,apod,fetcher)).toEqual({apod,source:"snapshot"});expect(fetcher).not.toHaveBeenCalled();});
 it("falls back on NASA rate limit",async()=>{const fetcher=vi.fn().mockResolvedValue(new Response("",{status:429}));expect(await loadApod(apod.date,"test-key",apod,fetcher)).toEqual({apod,source:"snapshot"});});
 it("requests the exact Earth date and preserves video credit",async()=>{const video={...apod,media_type:"video",thumbnail_url:"https://example.org/thumb.jpg"};const fetcher=vi.fn().mockResolvedValue(Response.json(video));const result=await loadApod(apod.date,"test-key",null,fetcher);expect(fetcher.mock.calls[0]?.[0]).toContain("date=2026-11-14&thumbs=true");expect(result.apod?.copyright).toBe("Photographer");expect(result.apod?.media_type).toBe("video");});
 it("caches and deduplicates requests per date",async()=>{const cache=new DateCache<number>(),load=vi.fn().mockResolvedValue(5);expect(await Promise.all([cache.get("2026-11-14",load),cache.get("2026-11-14",load)])).toEqual([5,5]);expect(load).toHaveBeenCalledTimes(1);await cache.get("2026-11-15",load);expect(load).toHaveBeenCalledTimes(2);});
});