using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ares.Infrastructure.Nasa
{
    /// <summary>The caller supplies mission and Sol only. All dates come from the original planner.</summary>
    public sealed partial class NasaSolService : IDisposable
    {
        readonly NasaConfig config;readonly ApiCredentials credentials;readonly HttpJsonClient http;readonly SolDataCache cache;
        readonly Func<DateTime> today;readonly string legacyFolder;readonly CancellationTokenSource lifetime=new CancellationTokenSource();
        readonly object gate=new object();readonly Dictionary<string,Task<MissionBlock>> blocks=new Dictionary<string,Task<MissionBlock>>();
        readonly Dictionary<string,Task<SolResponse>> inFlight=new Dictionary<string,Task<SolResponse>>();
        public NasaSolService(NasaConfig config,ApiCredentials credentials,HttpJsonClient http,SolDataCache cache,Func<DateTime> today=null,string legacyFolder=null)
        {this.config=config;this.credentials=credentials;this.http=http;this.cache=cache;this.today=today??(()=>DateTime.UtcNow);this.legacyFolder=legacyFolder;}
        public async Task<SolResponse> GetSolAsync(string mission,int sol,CancellationToken cancellation=default)
        {
            mission=MissionPlanner.MissionId(mission,today());sol=Math.Min(5,Math.Max(1,sol));
            var response=await HttpJsonClient.Wait(GetOrCreate(mission,sol),cancellation).ConfigureAwait(false);
            if(sol<5 && !lifetime.IsCancellationRequested)_=ObservePrefetch(GetOrCreate(mission,sol+1));
            return response;
        }
        static async Task ObservePrefetch(Task<SolResponse> request){try{await request.ConfigureAwait(false);}catch(OperationCanceledException){}catch{}}
        Task<SolResponse> GetOrCreate(string mission,int sol)
        {
            string key=mission+"|"+sol;
            lock(gate) {
                if(inFlight.TryGetValue(key,out var task))return task;
                var completion=new TaskCompletionSource<SolResponse>(TaskCreationOptions.RunContinuationsAsynchronously);inFlight.Add(key,completion.Task);
                _=Complete(key,mission,sol,completion);return completion.Task;
            }
        }
        async Task Complete(string key,string mission,int sol,TaskCompletionSource<SolResponse> completion)
        {
            try{completion.TrySetResult(await Load(mission,sol).ConfigureAwait(false));}catch(OperationCanceledException){completion.TrySetCanceled();}catch{completion.TrySetResult(Offline(mission,sol));}
            finally{lock(gate)inFlight.Remove(key);}
        }
        async Task<MissionBlock> Block(string mission)
        {
            var block=await cache.ReadBlockAsync(mission).ConfigureAwait(false)??new MissionBlock {mission=mission,todayUtc=today().ToUniversalTime().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)};
            if(block.flares!=null)return block;
            var plan=MissionPlanner.Plan(mission,1,DateTime.SpecifyKind(DateTime.ParseExact(block.todayUtc,"yyyy-MM-dd",CultureInfo.InvariantCulture),DateTimeKind.Utc));
            var text=await Nasa("/DONKI/FLR","startDate="+plan.block_start+"&endDate="+plan.block_end,config.FlareTimeoutSeconds).ConfigureAwait(false);
            if(text!=null)block.flares=ApiModels.Many<DonkiFlare>(text);
            await cache.WriteBlockAsync(block).ConfigureAwait(false);return block;
        }
        Task<MissionBlock> Mission(string id){lock(gate){if(!blocks.TryGetValue(id,out var task)){task=Block(id);blocks.Add(id,task);}return task;}}
        async Task<string> Json(string url,int timeout)
        {
            if(!NasaConfig.Https(url))return null;
            var response=await http.SendAsync(new HttpRequest {Url=url,TimeoutSeconds=timeout},lifetime.Token).ConfigureAwait(false);
            if(!response.Success)return null;
            try{var parsed=NasaJson.Parse(response.Text);if(parsed==null || parsed.Type==Newtonsoft.Json.Linq.JTokenType.Null || (parsed is Newtonsoft.Json.Linq.JObject o && Js.Truth(o["error"])))return null;return response.Text;}
            catch(Newtonsoft.Json.JsonException){return null;}
        }
        Task<string> Nasa(string path,string query,int timeout)
        {
            bool proxy=!string.IsNullOrWhiteSpace(config.NasaProxyBaseUrl);
            if(!proxy && string.IsNullOrWhiteSpace(credentials.NASA_API_KEY))return Task.FromResult<string>(null);
            string baseUrl=proxy?config.NasaProxyBaseUrl:config.NasaBaseUrl;
            // Personal key is never attached to arbitrary configured hosts.
            if(!proxy && (!Uri.TryCreate(baseUrl,UriKind.Absolute,out var uri) || uri.Host!="api.nasa.gov"))return Task.FromResult<string>(null);
            return Json(baseUrl.TrimEnd('/')+path+"?"+query+(proxy?"":"&api_key="+Uri.EscapeDataString(credentials.NASA_API_KEY)),timeout);
        }
        async Task<SolResponse> Load(string mission,int sol)
        {
            lifetime.Token.ThrowIfCancellationRequested();
            if(config.UseLegacyN8n) {
                // One-release comparison uses exported responses, never the retired n8n server/secret.
                var legacy=await Task.Run(()=> {
                    string path=Path.Combine(legacyFolder??"",SecretRedactor.Hash(mission+"|"+sol)+".json");
                    try{return File.Exists(path)?NasaJson.Read<SolResponse>(File.ReadAllText(path)):null;}catch{return null;}
                }).ConfigureAwait(false);
                if(legacy?.mission==mission && legacy.sol==sol)return legacy;
                return Offline(mission,sol);
            }
            var cached=await cache.ReadAsync(mission,sol,config.CacheFreshSeconds).ConfigureAwait(false);if(cached!=null)return cached;
            var block=await Mission(mission).ConfigureAwait(false);
            var anchor=DateTime.SpecifyKind(DateTime.ParseExact(block.todayUtc,"yyyy-MM-dd",CultureInfo.InvariantCulture),DateTimeKind.Utc);
            var plan=MissionPlanner.Plan(mission,sol,anchor);var win=MissionPlanner.Select(plan,block.flares);
            var requests=new[] {
                Nasa("/planetary/apod","start_date="+plan.apod_start+"&end_date="+plan.apod_end,config.HttpTimeoutSeconds),
                Nasa("/DONKI/CMEAnalysis","startDate="+win.earth_date+"&endDate="+win.earth_date+"&mostAccurateOnly=true&catalog=ALL",config.HttpTimeoutSeconds),
                Nasa("/DONKI/WSAEnlilSimulations","startDate="+win.d_prev+"&endDate="+win.d_next,config.HttpTimeoutSeconds),
                Json(config.MarsWeatherBaseUrl.TrimEnd('/')+"/"+win.curiosity_sol,config.HttpTimeoutSeconds),
                Json(config.ImageLibraryBaseUrl.TrimEnd('/')+"/search?q=Mars%20surface%20Curiosity&media_type=image&page_size=1&page="+plan.photo_page,config.HttpTimeoutSeconds)
            };
            var values=await Task.WhenAll(requests).ConfigureAwait(false);bool any=false;foreach(var value in values)any|=value!=null;
            if(!any) {
                // Never substitute a different Sol's facts/photo. Last cached means this identity.
                cached=await cache.ReadAsync(mission,sol,config.CacheFreshSeconds,true).ConfigureAwait(false);
                if(cached!=null)return cached;if(block.flares==null)return Offline(mission,sol);
            }
            var result=SolDataAssembler.Assemble(win,values[0],values[1],values[2],values[3],values[4]);
            if(!any)result.is_fallback=true;else await cache.WriteAsync(result).ConfigureAwait(false);
            return result;
        }
        public static SolResponse Offline(string mission,int sol)
        {
            var window=MissionPlanner.Select(MissionPlanner.Plan("offline-sample",1,new DateTime(2020,1,1,0,0,0,DateTimeKind.Utc)),Array.Empty<DonkiFlare>());
            var result=SolDataAssembler.Assemble(window,null,null,null,null,null);
            result.mission=mission;result.sol=result.data_dates.sol=sol;result.is_fallback=true;return result;
        }
        public void Dispose(){lifetime.Cancel();}
    }
}
