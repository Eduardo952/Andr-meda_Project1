using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Domain.Hazards;
using Ares.Infrastructure.Nasa;

namespace Ares.Infrastructure.Hazards
{
    public sealed class MissionScenarioService
    {
        readonly NasaSolService nasa;readonly HazardCatalog catalog;readonly ScenarioResult[] snapshots;readonly string folder;
        readonly HazardBalance balance;readonly ScenarioRules rules;
        readonly Dictionary<string,Task<ScenarioResult>> pending=new Dictionary<string,Task<ScenarioResult>>();readonly object gate=new object();
        public MissionScenarioService(NasaSolService nasa,HazardCatalog catalog,ScenarioResult[] snapshots,string folder,HazardBalance balance=null,ScenarioRules rules=null){this.nasa=nasa;this.catalog=catalog;this.snapshots=snapshots??Array.Empty<ScenarioResult>();this.folder=folder;this.balance=balance??new HazardBalance();this.rules=rules??new ScenarioRules();}
        public Task<ScenarioResult> GetAsync(string mission,CancellationToken token=default)
        {lock(gate){if(!pending.TryGetValue(mission,out var task)){task=BoundedLoad(mission,token);pending[mission]=task;}return task;}}
        async Task<ScenarioResult> BoundedLoad(string mission,CancellationToken token)
        {
            using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(token)){
                timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(5,balance.scenarioWaitSeconds)));
                try{return await HttpJsonClient.Wait(Load(mission,timeout.Token),timeout.Token);}
                catch(OperationCanceledException){token.ThrowIfCancellationRequested();return Snapshot(mission);}
            }
        }
        public ScenarioResult Snapshot(string mission)
        {
            if(snapshots.Length==0)return new ScenarioResult {mission=mission,mode="Consecutive",provenance="sample",sols=Enumerable.Range(1,5).Select(s=>NasaSolService.Offline(mission,s)).ToArray()};
            var result=NasaJson.Read<ScenarioResult>(NasaJson.Write(snapshots[(int)(DeterministicRng.Value(mission,"snapshot")*snapshots.Length)]));
            result.mission=mission;result.provenance="snapshot";
            for(int i=0;i<result.sols.Length;i++){result.sols[i].mission=mission;result.sols[i].sol=result.sols[i].data_dates.sol=i+1;}return result;
        }
        async Task<ScenarioResult> Load(string mission,CancellationToken token)
        {
            string path=Path.Combine(folder,SecretRedactor.Hash(mission)+".json");
            try{if(File.Exists(path)){var saved=NasaJson.Read<ScenarioResult>(await Task.Run(()=>File.ReadAllText(path),token));if(saved?.mission==mission && saved.sols?.Length==5)return saved;}}catch(IOException){}catch(Newtonsoft.Json.JsonException){}
            async Task<ScenarioResult> Fallback() {
                var first=await nasa.GetSolAsync(mission,1,token);if(first.is_fallback)return Snapshot(mission);
                var days=new SolResponse[5];days[0]=first;for(int sol=2;sol<=5;sol++){days[sol-1]=await nasa.GetSolAsync(mission,sol,token);if(days[sol-1].is_fallback)return Snapshot(mission);}
                if(!ScenarioSolvability.Accepts(days,balance))return Snapshot(mission);
                return new ScenarioResult {mission=mission,mode="Consecutive",provenance="live",sols=days};
            }
            var result=await new ScenarioDirector(balance,rules).SelectAsync(mission,catalog,(day,sol,c)=>HttpJsonClient.Wait(nasa.GetArchiveSolAsync(mission,sol,day.date,c),c),days=>ScenarioSolvability.Accepts(days,balance),Fallback,token);
            if(result.sols!=null && result.provenance!="sample")try{Directory.CreateDirectory(folder);await Task.Run(()=>File.WriteAllText(path,NasaJson.Write(result)),token);}catch(IOException){}
            return result;
        }
    }
}
