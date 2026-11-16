using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ares.Infrastructure.Nasa;
using Ares.Infrastructure.Hazards;
using Ares.Domain.Hazards;
using Newtonsoft.Json.Linq;

public static class HazardsTool
{
 public static async Task<int> Main(string[] args) {
  try {
   if(args.Contains("--telescope-unit")){Console.WriteLine(await TelescopeTests.Run());return 0;}
   if(args.Contains("--unit")){Console.WriteLine(await Ares.Editor.NasaMigrationTests.RunAsync(Directory.GetCurrentDirectory()));Console.WriteLine(await Ares.Editor.HazardChecks.RunAsync(Directory.GetCurrentDirectory()));return 0;}
   if(!args.Contains("--catalog")&&!args.Contains("--live-scenario")&&!args.Contains("--imagery")){Console.WriteLine("Use --unit, --catalog, --live-scenario or --imagery.");return 2;}
   var config=NasaConfig.Load("Assets/StreamingAssets/integrations.json");
   var credentials=await new LocalSecretsProvider("Assets/StreamingAssets/secrets.local.json","Temp/no-override").LoadAsync(default);
   using var transport=new LiveSmoke.Transport();using var http=new HttpJsonClient(transport,config.RetryCount);
   using var nasa=new NasaSolService(config,credentials,http,new SolDataCache("Temp/HazardCatalogData"));
   if(args.Contains("--imagery")){
    var first=nasa.GetImageryAsync("telescope-new-mission-alpha",1);var second=nasa.GetImageryAsync("telescope-new-mission-beta",1);var values=await Task.WhenAll(first,second);
    var report=values.Select((v,i)=>new{mission=i,apodDate=v.apod?.date,apodTitle=v.apod?.title,photoId=v.photo?.id}).ToArray();File.WriteAllText("Docs/Hazards/telescope-live-images.json",NasaJson.Write(report));Console.WriteLine(NasaJson.Write(report));return 0;
   }
   if(args.Contains("--live-scenario")){
    var index=NasaJson.Read<HazardCatalog>(File.ReadAllText("Assets/ARES/Resources/ARES/Hazards/catalog.json"));
    var saved=NasaJson.Read<ScenarioResult[]>(File.ReadAllText("Assets/ARES/Resources/ARES/Hazards/snapshots.json"));
    var service=new MissionScenarioService(nasa,index,saved,"Temp/HazardLiveScenarios");
    var result=await service.GetAsync("hazard-live-network-validation-v2");
    var rules=NasaJson.Read<Ares.Data.CampaignRules>(File.ReadAllText("Assets/ARES/Import/balance.json"));
    var campaign=Ares.Editor.HazardChecks.Simulate(result.sols,rules,true);
    var report=new {result.mode,result.provenance,result.repicks,phase=campaign.Phase.ToString(),evacuations=campaign.Condition.CollapseCount,dates=result.sols.Select(s=>new{s.earth_date,s.data_dates.curiosity_sol,dominant=HazardPlanner.Build(s).Dominant})};
    File.WriteAllText("Docs/Hazards/live-scenario-results.json",NasaJson.Write(report));Console.WriteLine(NasaJson.Write(report));return campaign.Phase==Ares.Domain.Phase.Victory?0:1;
   }
   string wind=await nasa.ProbeHistoricalWindAsync();var windJson=wind==null?null:NasaJson.Parse(wind) as JObject;
   var sols=(windJson?["sol_keys"] as JArray)?.Select(x=>(string)x).ToArray()??Array.Empty<string>();
   var windReport=new {reachable=wind!=null,sols=sols.Length,hasHws=sols.Any(s=>windJson[s]?["HWS"]!=null),hasWd=sols.Any(s=>windJson[s]?["WD"]!=null)};
   Directory.CreateDirectory("Docs/Hazards");File.WriteAllText("Docs/Hazards/wind-probe.json",NasaJson.Write(windReport));
   var catalog=await new HazardCatalogBuilder(nasa,"Temp/HazardCatalogScan").BuildAsync(DateTime.UtcNow,Console.WriteLine);
   Directory.CreateDirectory("Assets/ARES/Resources/ARES/Hazards");
   File.WriteAllText("Assets/ARES/Resources/ARES/Hazards/catalog.json",NasaJson.Write(catalog));
   Console.WriteLine("Catalog verified days: "+catalog.days.Length);
   Console.WriteLine("Kinds: "+string.Join(", ",catalog.days.GroupBy(d=>HazardPlanner.Build(d.observation).Dominant).Select(g=>g.Key+"="+g.Count())));
   return 0;
  }catch(Exception e){Console.WriteLine("Hazards check failed: "+e.GetType().Name+": "+e.Message);return 1;}
 }
}
