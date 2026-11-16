using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ares.Infrastructure.Nasa;
using Newtonsoft.Json.Linq;

namespace Ares.Editor
{
    /// <summary>Runs unchanged in the Unity editor and in the standalone verification harness.</summary>
    public static class NasaMigrationTests
    {
        static int assertions;
        static void Check(bool ok,string label){assertions++;if(!ok)throw new Exception(label);}
        static void Equal(object actual,JToken expected,string label)
        {
            var token=NasaJson.Parse(NasaJson.Write(actual));
            Check(Same(token,expected),label+"\nactual: "+token+"\nexpected: "+expected);
        }
        public static bool Same(JToken a,JToken b)
        {
            if(a==null || b==null)return a==b;
            bool Number(JToken t)=>t.Type==JTokenType.Float || t.Type==JTokenType.Integer;
            if(Number(a)&&Number(b))return Math.Abs((double)a-(double)b)<1e-10;
            if(a is JObject x && b is JObject y)return x.Count==y.Count && x.Properties().All(p=>y.ContainsKey(p.Name)&&Same(p.Value,y[p.Name]));
            if(a is JArray aa && b is JArray bb)return aa.Count==bb.Count && aa.Select((v,i)=>Same(v,bb[i])).All(v=>v);
            return JToken.DeepEquals(a,b);
        }
        sealed class FakeHttp : IHttpTransport
        {
            public readonly List<HttpRequest> Calls=new List<HttpRequest>();public Func<HttpRequest,CancellationToken,Task<HttpResponse>> Handler;
            public Task<HttpResponse> SendAsync(HttpRequest r,CancellationToken c){lock(Calls)Calls.Add(r);return Handler(r,c);}
        }
        sealed class FakeLlm : ILlmClient {public int Calls;public Task<NarratorResponse> CompleteAsync(JObject request,CancellationToken c){Calls++;throw new IOException("offline");}}
        static HttpResponse Json(string body,int status=200)=>new HttpResponse{Status=status,Data=Encoding.UTF8.GetBytes(body)};
        public static async Task<string> RunAsync(string root)
        {
            assertions=0;var fixture=(JObject)NasaJson.Parse(File.ReadAllText(Path.Combine(root,"Tools/NasaMigration/Fixtures/reference.json")));
            DateTime today=new DateTime(2026,11,15,0,0,0,DateTimeKind.Utc);
            string[] tags={"block","apod|1","apod|5","photo|1","window","dust|3"};
            double[][] vectors={new[]{.09449368389323354,.2091667999047786,.0454355925321579,.9575499268248677,.3619374935515225,.08820752729661763},new[]{.10588326212018728,.525161201832816,.7675962659996003,.09747042786329985,.18408808624371886,.20674833725206554}};
            var missions=new[]{"mission-ABC12345","test_mission_001"};
            for(int m=0;m<2;m++)for(int i=0;i<tags.Length;i++)Check(Math.Abs(DeterministicRng.Value(missions[m],tags[i])-vectors[m][i])<1e-15,"RNG vector "+m+" "+tags[i]);
            Check(MarsCalendar.SolDate(1000,0)=="2015-05-31","solDate 1000");Check(MarsCalendar.SolDate(2000,4)=="2018-03-27","solDate 2000");Check(MarsCalendar.SolDate(3000,2)=="2021-01-16","solDate 3000");
            Check(Math.Abs(MarsCalendar.LsOf(MarsCalendar.DayMs("2015-03-10")+MarsCalendar.Day/2)-305.59074570684015)<1e-10,"Ls 2015");
            Check(Math.Abs(MarsCalendar.LsOf(MarsCalendar.DayMs("2020-11-02")+MarsCalendar.Day/2)-307.40161488906597)<1e-10,"Ls 2020");
            Check(MarsCalendar.Round(-1.5)==-1 && MarsCalendar.Round(2.5)==3 && MarsCalendar.Round(-.5)==0,"JS rounding");
            Check(MissionPlanner.MissionId("bad",today)=="anon-2026-11-15" && MissionPlanner.MissionId("valid-123\n",today)=="anon-2026-11-15","mission validation");
            Check(MissionPlanner.MissionId(new string('x',64),today)==new string('x',64) && MissionPlanner.MissionId(new string('x',65),today)=="anon-2026-11-15","mission lengths");
            Check(MissionPlanner.Sol("2junk")==2 && MissionPlanner.Sol("0")==1 && MissionPlanner.Sol("99")==5 && MissionPlanner.Sol("-4")==1,"parseInt/clamp");
            var reasons=new HashSet<string>();
            foreach(JObject item in fixture["cases"]) {
                var plan=MissionPlanner.Plan((string)item["mission"],(int)item["sol"],today);var expectedPlan=(JObject)item["plan"];
                foreach(string key in new[]{"mission","sol","block_start","block_end","apod_start","apod_end","photo_page"})Check(JToken.DeepEquals(JObject.Parse(NasaJson.Write(plan))[key],expectedPlan[key]),"CONFIG "+key);
                var sources=(JObject)item["sources"];string Source(string name)=>sources[name]?.ToString();
                var window=MissionPlanner.Select(plan,ApiModels.Many<DonkiFlare>(Source("flr")));Equal(window,item["window"],"A2 window");reasons.Add(window.mission_window.reason);
                Equal(SolDataAssembler.Assemble(window,Source("apod"),Source("cme"),Source("enlil"),Source("weather"),Source("images")),item["response"],"A3 response");
            }
            Check(reasons.SetEquals(new[]{"eventful","calm_period","no_flare_data"}),"all window branches");
            string system=File.ReadAllText(Path.Combine(root,"Assets/ARES/Resources/ARES/Narrator/bubble-system.txt")),questions=File.ReadAllText(Path.Combine(root,"Assets/ARES/Resources/ARES/Narrator/questions.json"));
            var prompts=new PromptBuilder(system,questions);Check(JObject.Parse(questions).Count==12,"question table");
            foreach(JObject item in fixture["prepared"]) {
                var prepared=prompts.Prepare((JObject)item["input"]);Equal(prepared,item["expected"],"A4 phase "+(string)prepared["phase"]);
                var body=JObject.Parse(prompts.Completion(prepared,new NasaConfig()));
                Check((string)body["model"]=="openai/gpt-oss-120b" && (double)body["temperature"]==.7 && (int)body["max_tokens"]==1500,"executed Groq parameters");
                Check((string)body["messages"][0]["content"]==system,"verbatim system prompt");
                var llm=new FakeLlm();var narrator=new BubbleNarrator(prompts,llm);var result=await narrator.SayAsync((JObject)item["input"]);
                Check(result.text==NarratorResponseParser.Fallback && llm.Calls==1,"offline phase "+(string)prepared["phase"]);
            }
            foreach(JObject item in fixture["narrator"])Equal(NarratorResponseParser.Parse((JObject)item["input"]),item["expected"],"A5 parser");
            var catalogs=new Dictionary<string,Dictionary<string,string>>();
            foreach(string lang in new[]{"en","pt-BR"}) catalogs[lang]=((JArray)JObject.Parse(File.ReadAllText(Path.Combine(root,"Assets/ARES/Resources/ARES/UI/"+lang+".json")))["entries"]).OfType<JObject>().ToDictionary(e=>(string)e["key"],e=>(string)e["value"]);
            var templates=new LocalNarratorTemplates((lang,key)=>catalogs[lang].TryGetValue(key,out var value)?value:key);
            foreach(string lang in new[]{"en","pt-BR"})foreach(string phase in PromptBuilder.Phases) {
                var request=new JObject {["lang"]=lang,["sol"]=1,["phase"]=phase,["facts"]=new JObject {["severity"]="calm"},["demand_kwh"]=new JObject {["shield"]=20,["life"]=30,["greenhouse"]=25},["question_id"]="what_is_a_flare"};
                var local=templates.For(request);
                Check(!string.IsNullOrEmpty(local.text) && !local.text.Contains("narrator.") && !local.text.Contains("{{"),"existing local template "+lang+" "+phase);
            }
            var malformed=SolDataAssembler.Assemble(MissionPlanner.Select(MissionPlanner.Plan("test_mission_001",1,today),Array.Empty<DonkiFlare>()),"[{\"url\":{}}]","not-json","[{\"impactList\":false}]","{\"sol\":1,\"atmo_opacity\":{}}","{\"collection\":false}");
            Check(malformed.apod==null && malformed.facts.cme==null && malformed.facts.weather==null && malformed.photo==null,"malformed independent sources stay null");
            var invalid=new JObject { ["lang"]="pt-BR",["sol"]=1,["facts"]=new JObject(),["phase"]="free_question",["question_text"]="https://example.org 123456"};
            var noCall=new FakeLlm();try{await new BubbleNarrator(prompts,noCall).SayAsync(invalid);Check(false,"invalid question accepted");}catch(ArgumentException){Check(noCall.Calls==0,"sanitization before network");}
            var validation=new JObject {["lang"]="en",["sol"]=1,["facts"]=JObject.Parse("{\"flare\":null,\"cme\":null,\"weather\":null,\"wind\":null,\"mars_impact\":null}"),["task_results"]=new JArray()};
            Check(NarrationValidator.Validate("The flare is X9.9.",validation)!=null,"absent flare rejected");Check(NarrationValidator.Validate("There are 987 stars.",validation)!=null,"unknown metrics rejected");
            Check(NarrationValidator.Validate("Vamos explorar a base hoje.",validation)=="wrong_language","language guard");
            Check(SecretRedactor.Redact("https://api.nasa.gov?api_key=private-value&x=1\nAuthorization: Bearer private-value").IndexOf("private-value",StringComparison.Ordinal)<0,"secret redaction");
            await TransportChecks();await ServiceChecks(root,fixture,today);
            return "PASS: "+assertions+" assertions; bit-exact RNG/calendar/planner, A1-A5 JS parity, seven narrator phases, validation/sanitization, partial failures, retry/cancellation/deduplication, disk cache and offline fallback.";
        }
        static async Task TransportChecks()
        {
            var fake=new FakeHttp();int call=0;var delays=new List<double>();
            fake.Handler=(r,c)=>{var response=Json("{}",++call==1?429:call==2?503:200);if(call==1)response.Headers["Retry-After"]="3";return Task.FromResult(response);};
            using(var http=new HttpJsonClient(fake,2,(delay,c)=>{delays.Add(delay.TotalSeconds);return Task.CompletedTask;})) {
                Check((await http.SendAsync(new HttpRequest {Url="https://example.org"})).Success && call==3,"429/503 retry");Check(delays.SequenceEqual(new[]{3.0,2.0}),"Retry-After and backoff");
            }
            var completion=new TaskCompletionSource<HttpResponse>(TaskCreationOptions.RunContinuationsAsynchronously);fake=new FakeHttp {Handler=(r,c)=>completion.Task};
            using(var http=new HttpJsonClient(fake))using(var cancel=new CancellationTokenSource()) {
                var request=new HttpRequest {Url="https://example.org/dedup"};var first=http.SendAsync(request,cancel.Token);var second=http.SendAsync(request);cancel.Cancel();
                try{await first;Check(false,"cancel ignored");}catch(OperationCanceledException){Check(true,"cancelled waiter");}
                completion.SetResult(Json("{}"));Check((await second).Success && fake.Calls.Count==1,"shared request survives one cancellation");
            }
            fake=new FakeHttp {Handler=(r,c)=>Task.FromResult(Json("{}",403))};using(var http=new HttpJsonClient(fake))Check(!(await http.SendAsync(new HttpRequest {Url="https://example.org/403"})).Success && fake.Calls.Count==1,"403 not retried");
            var timed=new FakeHttp {Handler=(r,c)=>Task.FromResult(HttpResponse.Error("timeout"))};using(var http=new HttpJsonClient(timed))Check(!(await http.SendAsync(new HttpRequest {Url="https://example.org/timeout"})).Success,"timeout result");
        }
        static async Task ServiceChecks(string root,JObject fixture,DateTime today)
        {
            var source=(JObject)fixture["cases"][0]["sources"];
            string Which(string url)=>url.Contains("/FLR?")?"flr":url.Contains("/apod?")?"apod":url.Contains("/CMEAnalysis?")?"cme":url.Contains("/WSAEnlilSimulations?")?"enlil":url.Contains("/search?")?"images":"weather";
            foreach(string failure in new[]{"none","flr","apod","cme","enlil","weather","images","all"}) {
                var fake=new FakeHttp {Handler=(r,c)=>Task.FromResult(failure=="all" || Which(r.Url)==failure?HttpResponse.Error("offline"):Json(source[Which(r.Url)]?.ToString()??"[]"))};
                string folder=Path.Combine(root,"Temp/NasaTests",Guid.NewGuid().ToString("N"));var config=new NasaConfig();
                using(var http=new HttpJsonClient(fake,0))using(var service=new NasaSolService(config,new ApiCredentials {NASA_API_KEY="mock-only"},http,new SolDataCache(folder),()=>today)) {
                    var response=await service.GetSolAsync("test_mission_001",1);Check(response.sol==1 && response.mission=="test_mission_001","cache identity "+failure);
                    if(failure=="apod")Check(response.apod==null && response.photo!=null,"APOD independent failure");
                    if(failure=="cme")Check(response.facts.cme==null && response.facts.weather!=null,"CME independent failure");
                    if(failure=="weather")Check(response.facts.weather==null && response.apod!=null,"weather independent failure");
                    if(failure=="images")Check(response.photo==null && response.apod!=null,"photo independent failure");
                    if(failure=="enlil")Check(response.facts.mars_impact.level=="none","A3 CME-only impact rule");
                    if(failure=="all")Check(response.is_fallback && response.facts.wind==null,"static offline sample");
                    if(failure=="none") {
                        var responses=new List<SolResponse>{response};for(int sol=2;sol<=5;sol++)responses.Add(await service.GetSolAsync("test_mission_001",sol));
                        Check(fake.Calls.Count(r=>Which(r.Url)=="flr")==1,"one FLR per mission");Check(fake.Calls.Count<=26,"five-Sol request budget");
                        Check(responses.Select(r=>r.earth_date).Distinct().Count()==5,"Sol Earth date variation");
                        var blocked=new FakeHttp {Handler=(r,c)=>Task.FromResult(HttpResponse.Error("offline"))};
                        using(var offlineHttp=new HttpJsonClient(blocked))using(var cachedService=new NasaSolService(config,new ApiCredentials(),offlineHttp,new SolDataCache(folder),()=>today)) {
                            var restored=await cachedService.GetSolAsync("test_mission_001",1);Check(NasaJson.Write(restored)==NasaJson.Write(response),"disk round trip");
                        }
                    }
                }
            }
        }
    }
}
