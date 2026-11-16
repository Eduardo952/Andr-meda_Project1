using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ares.Infrastructure.Nasa
{
    public sealed class SolImagery {public SolApod apod;public SolPhoto photo;}
    public sealed partial class NasaSolService
    {
        readonly Dictionary<string,Task<SolImagery>> imagery=new Dictionary<string,Task<SolImagery>>();readonly object imageryGate=new object();
        public Task<SolImagery> GetImageryAsync(string mission,int sol,CancellationToken token=default,string[] excludeUrls=null,bool retry=false)
        {
            mission=MissionPlanner.MissionId(mission,today());sol=Math.Max(1,Math.Min(5,sol));string key=mission+"|"+sol;
            lock(imageryGate){
                imagery.TryGetValue(key,out var task);
                if(task==null || retry && task.IsCompleted && (task.IsCanceled||task.IsFaulted||task.Result.apod==null&&task.Result.photo==null)){
                    task=LoadFreshImagery(mission,sol,excludeUrls);imagery[key]=task;
                }
                return HttpJsonClient.Wait(task,token);
            }
        }
        async Task<SolImagery> LoadFreshImagery(string mission,int sol,string[] excludeUrls)
        {
            var excluded=new HashSet<string>(excludeUrls??Array.Empty<string>(),StringComparer.Ordinal);
            // Do not reroll merely because a request failed. Only a positively identified repeat needs another draw.
            for(int attempt=0;attempt<4;attempt++){
                string draw=attempt==0?mission:SecretRedactor.Hash(mission).Substring(0,32)+"-image-"+attempt;
                var result=await LoadImagery(draw,sol);bool repeat=false;
                if(result.apod!=null && excluded.Contains(result.apod.url)){result.apod=null;repeat=true;}
                if(result.photo!=null && excluded.Contains(result.photo.url)){result.photo=null;repeat=true;}
                if(result.apod!=null||result.photo!=null||!repeat)return result;
            }
            return new SolImagery();
        }
        async Task<SolImagery> LoadImagery(string mission,int sol)
        {
            // Images have their own mission/Sol seed. Climate catalog snapshots must never pin the telescope's image deck.
            var plan=MissionPlanner.Plan(mission,sol,today());
            var rows=await Task.WhenAll(Nasa("/planetary/apod","start_date="+plan.apod_start+"&end_date="+plan.apod_end,config.HttpTimeoutSeconds),
                Json(config.ImageLibraryBaseUrl.TrimEnd('/')+"/search?q=Mars%20surface%20Curiosity&media_type=image&page_size=1&page="+plan.photo_page,config.HttpTimeoutSeconds));
            var window=MissionPlanner.Select(plan,Array.Empty<DonkiFlare>());
            var response=SolDataAssembler.Assemble(window,rows[0],null,null,null,rows[1]);
            return new SolImagery {apod=response.apod,photo=response.photo};
        }
    }
}
