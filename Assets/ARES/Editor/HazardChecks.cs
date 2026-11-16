using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ares.Data;
using Ares.Domain;
using Ares.Domain.Hazards;
using Ares.Infrastructure.Nasa;
using Newtonsoft.Json.Linq;

namespace Ares.Editor
{
    /// <summary>Pure domain tests, also executable outside Unity. Synthetic fixtures never enter the shipped catalog.</summary>
    public static class HazardChecks
    {
        static int checks;static void Check(bool value,string reason){checks++;if(!value)throw new InvalidOperationException(reason);}
        public static SolResponse Fixture(string mission,int sol,string flare=null,double magnitude=1,double? speed=null,string impact="none",double? cold=null,string dust="low")
        {
            var f=new JObject { ["severity"]="calm",["flare"]=flare==null?JValue.CreateNull():JObject.FromObject(new {@class=flare,magnitude=magnitude}),["cme"]=speed==null?JValue.CreateNull():JObject.FromObject(new {speed_kms=speed.Value}),["mars_impact"]=JObject.FromObject(new {level=impact}),["weather"]=cold==null?JValue.CreateNull():JObject.FromObject(new {temp_avg_c=cold.Value})};
            return NasaJson.Read<SolResponse>(new JObject {["mission"]=mission,["sol"]=sol,["earth_date"]="2020-01-"+sol.ToString("00"),["facts"]=f,["effects"]=JObject.FromObject(new {dust_storm_risk=dust,panel_factor=dust=="high"?.8:dust=="medium"?.9:1,mars_season_ls=220}),["data_dates"]=JObject.FromObject(new {sol=sol,curiosity_sol=2600+sol})}.ToString());
        }
        public static async Task<string> RunAsync(string root)
        {
            checks=0;var b=new HazardBalance();var empty=Fixture("test",1);Check(HazardPlanner.Build(empty).events.Length==0,"Null observations activated a hazard");
            var high=Fixture("test",1,"X",2,1400,"direct",-100,"high");var p=HazardPlanner.Build(high);
            Check(p.Has(HazardKind.Flare)&&p.Has(HazardKind.Cme)&&p.Has(HazardKind.Radiation)&&p.Has(HazardKind.Dust)&&p.Has(HazardKind.Cold),"Source activation");
            Check(p.shieldDemand==Rules.Shield('X',2,1400),"Shield formula changed");Check(p.lifeDemand==Rules.Thermal(-100),"Thermal formula changed");
            Check(NasaJson.Write(p)==NasaJson.Write(HazardPlanner.Build(high)),"Non-deterministic hazard plan");
            var low=HazardPlanner.Build(Fixture("test",1,"C",9,900));Check(!low.Has(HazardKind.Flare)&&!low.Has(HazardKind.Cme)&&!low.Has(HazardKind.Radiation),"Advisory promoted to damaging event");
            Check(!HazardPlanner.Build(Fixture("test",1,cold:-64.9)).Has(HazardKind.Cold),"Cold threshold");
            var noEffects=Fixture("test",1,dust:"high");noEffects.effects=null;Check(!HazardPlanner.Build(noEffects).Has(HazardKind.Dust),"Null effects activated dust");
            for(int mask=0;mask<32;mask++){
                var v=Fixture("null-combinations",1,"X",2,1400,"direct",-100,"high");if((mask&1)==0)v.facts.flare=null;if((mask&2)==0)v.facts.cme=null;if((mask&4)==0)v.facts.mars_impact=null;if((mask&8)==0)v.facts.weather=null;if((mask&16)==0)v.effects=null;var plan=HazardPlanner.Build(v);
                Check(plan.Has(HazardKind.Flare)==((mask&1)!=0)&&plan.Has(HazardKind.Cme)==((mask&6)==6)&&plan.Has(HazardKind.Radiation)==((mask&5)!=0)&&plan.Has(HazardKind.Cold)==((mask&8)!=0)&&plan.Has(HazardKind.Dust)==((mask&16)!=0),"Null combination "+mask);
                Check(plan.events.All(e=>!string.IsNullOrEmpty(e.field)&&!string.IsNullOrEmpty(e.api)),"Missing provenance "+mask);
            }
            var locales=new Dictionary<string,Dictionary<string,string>>();foreach(string language in new[]{"en","pt-BR"})locales[language]=((JArray)JObject.Parse(File.ReadAllText(Path.Combine(root,"Assets/ARES/Resources/ARES/UI/"+language+".hazards.json")))["entries"]).ToDictionary(e=>(string)e["key"],e=>(string)e["value"]);
            Check(new HashSet<string>(locales["en"].Keys).SetEquals(locales["pt-BR"].Keys),"Hazard localization keys differ");
            var templates=new LocalNarratorTemplates((lang,key)=>locales[lang].TryGetValue(key,out var value)?value:key);
            foreach(string lang in locales.Keys)foreach(string cause in new[]{"Radiation","Hypoxia","Cold","Power","Structure"}){
                var request=new JObject {["lang"]=lang,["phase"]="alert",["sol"]=1,["facts"]=new JObject {["game"]=new JObject {["condition"]="Warning",["condition_cause"]=cause}}};var line=templates.For(request);Check(NarrationValidator.Validate(line.text,request)==null,"Narrator hazard line "+lang+" "+cause+": "+NarrationValidator.Validate(line.text,request));
            }
            var condition=new PlayerConditionModel(b);condition.Tick(30,100,100,100,0,false);Check(condition.Stage==CrewCondition.Warning,"Invisible warning collapsed crew");
            condition.Tick(14,100,100,100,0,true);Check(condition.Stage==CrewCondition.Warning,"Warning grace too short");condition.Tick(1,100,100,100,0,true);Check(condition.Stage==CrewCondition.Incapacitated&&!condition.Abort,"First medical evacuation");
            condition.Recover();condition.Tick(15,100,100,100,0,true);condition.Recover();condition.Tick(15,100,100,100,0,true);Check(condition.Abort&&condition.CollapseCount==3,"Third evacuation must abort");
            var allocation=new Allocation{shield=20,life=15,greenhouse=10};var demand=new Allocation{shield=50,life=30,greenhouse=25};var total=ExposureModel.AllocationPenalty(allocation,demand,true);var ledger=new ExposureModel();ResourcePenalty charged=default;
            for(int i=1;i<=600;i++)charged+=ledger.Advance(ExposureModel.AllocationPenalty(allocation,demand,false),i/600f,35);
            charged+=ledger.Settle(total);Check(Math.Abs(charged.dose-59)<.001&&Math.Abs(charged.integrity-30)<.001&&Math.Abs(charged.oxygen-22.5)<.001,"Double-charged live + seal ledger");
            var extras=new ExposureModel();for(int i=0;i<1000;i++)extras.Extra(1,1,1,b);Check(extras.Additional.oxygen==12&&extras.Additional.dose==20&&extras.Additional.integrity==14,"New-source caps");
            var battery=new SolarPlant(new SolarMaintenanceSettings());battery.ConfigureGrid(30,0);var grid=new EnergyGrid(battery,b);grid.Shock(p.events.First(e=>e.kind==HazardKind.Cme),default,demand);Check(grid.OfflineSystem=="life","No brownout after unabsorbed shock");grid.Tick(200);Check(grid.OfflineSystem==null,"Brownout did not expire");
            var catalog=NasaJson.Read<HazardCatalog>(File.ReadAllText(Path.Combine(root,"Assets/ARES/Resources/ARES/Hazards/catalog.json")));
            Check(catalog.days.Length>0 && catalog.days.All(d=>!d.observation.is_fallback),"Catalog must contain actual observations");
            var snapshots=new List<ScenarioResult>();for(int i=0;i<12;i++){
                string mission="offline-verified-"+i;
                var result=await new ScenarioDirector().SelectAsync(mission,catalog,(day,sol,token)=>Task.FromResult(Clone(day.observation,mission,sol)),days=>ScenarioSolvability.Accepts(days),()=>Task.FromResult<ScenarioResult>(null));
                if(result==null)continue;
                Check(result.sols.Select(s=>s.earth_date).Distinct().Count()==5,"Repeated dates");Check(result.sols.Select(s=>s.data_dates.curiosity_sol).Distinct().Count()==5,"Repeated Curiosity Sols");
                for(int s=1;s<5;s++)Check(HazardPlanner.Build(result.sols[s]).Dominant!=HazardPlanner.Build(result.sols[s-1]).Dominant,"Repeated dominant event family");snapshots.Add(result);
            }
            Check(snapshots.Count>0,"No solvable curated snapshots");File.WriteAllText(Path.Combine(root,"Assets/ARES/Resources/ARES/Hazards/snapshots.json"),NasaJson.Write(snapshots));
            var rules=NasaJson.Read<CampaignRules>(File.ReadAllText(Path.Combine(root,"Assets/ARES/Import/balance.json")));
            var imageryTransport=new ImageryTransport();using(var imageHttp=new HttpJsonClient(imageryTransport,0))using(var imageService=new NasaSolService(new NasaConfig(),new ApiCredentials {NASA_API_KEY="mock-only"},imageHttp,new SolDataCache(Path.Combine(root,"Temp/ImageryChecks")))){
                var first=await imageService.GetImageryAsync("image-mission-alpha",1);var again=await imageService.GetImageryAsync("image-mission-alpha",1);var second=await imageService.GetImageryAsync("image-mission-beta",1);var next=await imageService.GetImageryAsync("image-mission-alpha",2);
                Check(first.apod!=null && first.apod.url==again.apod.url,"Same mission/Sol must reuse its image");Check(first.apod.url!=second.apod.url && first.apod.url!=next.apod.url,"Image choice pinned to climate snapshot");Check(imageryTransport.calls==6,"Imagery must use only APOD + image library and cache same Sol");
            }
            int optimalFinished=0,optimalAborted=0,optimalEvacs=0,carelessCollapsed=0,carelessAborted=0;
            for(int i=0;i<200;i++){
                var days=new SolResponse[5];var rng=new Random(i+1901);
                for(int s=0;s<5;s++)days[s]=Fixture("simulation-"+i,s+1,s%2==1?(rng.Next(2)==0?"M":"X"):null,1+rng.NextDouble()*4,s%2==1?(double?)(500+rng.Next(1200)):null,s==3?"direct":"none",-55-rng.Next(42),s==2||s==4?(rng.Next(2)==0?"high":"medium"):"low");
                var optimal=Simulate(days,rules,true);var careless=Simulate(days,rules,false);
                if(optimal.Phase==Phase.Victory)optimalFinished++;if(optimal.Condition.Abort)optimalAborted++;optimalEvacs+=optimal.Condition.CollapseCount;
                if(careless.Condition.CollapseCount>0)carelessCollapsed++;if(careless.Condition.Abort)carelessAborted++;
            }
            var report=new {missions=200,optimalFinished,optimalAborted,optimalEvacuations=optimalEvacs,carelessCollapsed,carelessAborted,assertions=checks,snapshots=snapshots.Count,scope="Pure Campaign simulation: actual five-Sol clocks, allocations, greenhouse water, tasks and reports; physical travel/UI not simulated."};
            Directory.CreateDirectory(Path.Combine(root,"Docs/Hazards"));File.WriteAllText(Path.Combine(root,"Docs/Hazards/simulation-results.json"),NasaJson.Write(report));
            Check(optimalFinished>=198&&optimalAborted==0,"Optimal campaign acceptance: "+NasaJson.Write(report));Check(carelessCollapsed>=60,"Careless policy did not expose enough risk");
            return "PASS: "+checks+" hazard assertions; "+optimalFinished+"/200 optimal missions completed, "+optimalAborted+" aborted; careless "+carelessCollapsed+" collapsed, "+carelessAborted+" aborted. "+snapshots.Count+" verified offline scenarios.";
        }
        public static SolResponse Clone(SolResponse original,string mission,int sol){var data=NasaJson.Read<SolResponse>(NasaJson.Write(original));data.mission=mission;data.sol=data.data_dates.sol=sol;return data;}
        sealed class ImageryTransport:IHttpTransport
        {
            public int calls;
            public Task<HttpResponse> SendAsync(HttpRequest request,System.Threading.CancellationToken cancellation){calls++;string date=System.Text.RegularExpressions.Regex.Match(request.Url,@"start_date=([0-9-]+)").Groups[1].Value;string json=date.Length>0?NasaJson.Write(new[]{new {date=date,title="Synthetic image test",media_type="image",url="https://apod.nasa.gov/apod/image/"+date+".jpg"}}):"{}";return Task.FromResult(new HttpResponse {Status=200,Data=System.Text.Encoding.UTF8.GetBytes(json)});}
        }
        public static Campaign Simulate(SolResponse[] days,CampaignRules rules,bool optimal)
        {
            var data=new CampaignData {sols=days.Select((s,i)=>new SolData {sol=i+1,tasks=Array.Empty<Assignment>()}).ToArray()};var campaign=new Campaign(data,rules);campaign.EnableNativeHazards();
            foreach(var day in days){
                if(campaign.Phase==Phase.GameOver||campaign.Phase==Phase.Victory)break;
                campaign.ApplyNativeSol(day,"synthetic-test","Curated");campaign.CloseReport();
                campaign.SetAllocation(optimal?MissionDirector.Recommend(campaign.EnergyPool,campaign.Current.demand,campaign.Solar.ReserveKwh):new Allocation {greenhouse=campaign.EnergyPool});campaign.StartSol();
                bool done=false;for(int time=0;time<1200 && campaign.Phase==Phase.Tasks;time++){
                    bool shelter=optimal||time>=600;bool rest=optimal && !campaign.RestUsed && (campaign.Dose>=40||campaign.Oxygen<65);
                    campaign.SetExposureContext(optimal,shelter,true,rest);
                    if(optimal){
                        if(campaign.Dose>=65)campaign.UseMedKit();if(campaign.Oxygen<=40)campaign.UseOxygenCanister();
                        if(campaign.Solar.DirtyPanels>0)for(int n=0;n<SolarPlant.PanelCount;n++)typeof(SolarPlant).GetMethod("Clean",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(campaign.Solar,new object[]{n});
                        var a=MissionDirector.Recommend(campaign.EnergyPool,campaign.Current.demand,campaign.Solar.ReserveKwh);if(!a.Equals(campaign.Allocation))campaign.SetAllocation(a);
                    }
                    campaign.Tick(1,false);
                    if(time>=160 && !done && campaign.Condition.Stage!=CrewCondition.Incapacitated){CompleteTasks(campaign);done=true;}
                    if(time>=600 && campaign.Condition.Stage!=CrewCondition.Incapacitated){if(!done){CompleteTasks(campaign);done=true;}campaign.SetAllocation(optimal?MissionDirector.Recommend(campaign.EnergyPool,campaign.Current.demand,campaign.Solar.ReserveKwh):new Allocation {greenhouse=campaign.EnergyPool});campaign.Seal("control",campaign.Allocation);}
                }
                if(campaign.Phase!=Phase.Summary)break;
                campaign.SendDailyReport();campaign.NextSol();
            }return campaign;
        }
        public static void CompleteTasks(Campaign campaign)
        {
            campaign.Watering.PickUp();for(int shelf=0;shelf<campaign.Greenhouse.Shelves.Count;shelf++){
                campaign.Greenhouse.Inspect(shelf);int guard=0;while(!campaign.Greenhouse.Shelves[shelf].CaredFor && guard++<4){campaign.Watering.Fill();campaign.Watering.StartPour(shelf);campaign.Watering.Tick(shelf,false,3);}
            }
            while(campaign.Greenhouse.Light!=GrowLight.Moderate)campaign.Greenhouse.AdjustLight(campaign.Greenhouse.Light<GrowLight.Moderate?1:-1);
            foreach(var task in campaign.Current.tasks){if(campaign.Completed(task.id))continue;campaign.BeginTask(task.id,task.room);if(GreenhouseSimulation.IsTask(task.id))campaign.FinishGreenhouseTask();else campaign.FinishTask(task.id,true,20,0);}
        }
    }
}
