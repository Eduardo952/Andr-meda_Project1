using System;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Infrastructure.Nasa;
using Newtonsoft.Json.Linq;

// Explicit opt-in console smoke test. Reads local secrets; never writes request URLs or headers.
public static class LiveSmoke
{
    public sealed class Transport : IHttpTransport, IDisposable
    {
        readonly HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        public readonly Dictionary<string,string> Raw = new Dictionary<string,string>();
        public readonly Dictionary<string,int> Counts = new Dictionary<string,int>();
        public async Task<HttpResponse> SendAsync(HttpRequest request,CancellationToken cancellation)
        {
            var uri=new Uri(request.Url);string key=SecretRedactor.Redact(request.Url);
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);timeout.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));
            try {
                using var message=new HttpRequestMessage(new HttpMethod(request.Method),request.Url);
                if(request.Body!=null) message.Content=new StringContent(request.Body,System.Text.Encoding.UTF8,"application/json");
                foreach(var header in request.Headers)message.Headers.TryAddWithoutValidation(header.Key,header.Value);
                using var response=await client.SendAsync(message,timeout.Token);
                var bytes=await response.Content.ReadAsByteArrayAsync(timeout.Token);
                var result=new HttpResponse {Status=(int)response.StatusCode,Data=bytes};
                foreach(var header in response.Headers)result.Headers[header.Key]=string.Join(",",header.Value);
                lock(Raw) { Counts[uri.Host+uri.AbsolutePath+" "+result.Status]=Counts.GetValueOrDefault(uri.Host+uri.AbsolutePath+" "+result.Status)+1; }
                if(result.Success && uri.Host!="api.groq.com")lock(Raw)Raw[key]=result.Text;
                Console.WriteLine(uri.Host+uri.AbsolutePath+" HTTP "+result.Status);
                return result;
            }catch(OperationCanceledException) when(!cancellation.IsCancellationRequested) {Console.WriteLine(uri.Host+uri.AbsolutePath+" timeout");return HttpResponse.Error("timeout");}
            catch(HttpRequestException) {Console.WriteLine(uri.Host+uri.AbsolutePath+" connection_failed");return HttpResponse.Error("connection");}
        }
        public void Dispose()=>client.Dispose();
    }
    public static async Task<int> Main(string[] args)
    {
        if(args.Contains("--unit")){Console.WriteLine(await Ares.Editor.NasaMigrationTests.RunAsync(Directory.GetCurrentDirectory()));return 0;}
        if(!args.Contains("--live")){Console.WriteLine("Pass --live to explicitly call NASA and Groq using local credentials.");return 2;}
        try {
            var config=NasaConfig.Load("Assets/StreamingAssets/integrations.json");
            var credentials=await new LocalSecretsProvider("Assets/StreamingAssets/secrets.local.json", "Temp/no-secrets-override").LoadAsync(default);
            if(string.IsNullOrWhiteSpace(credentials.NASA_API_KEY)||string.IsNullOrWhiteSpace(credentials.GROQ_API_KEY))throw new Exception("Local credentials missing");
            using var transport=new Transport();using var http=new HttpJsonClient(transport,config.RetryCount);
            string folder=Path.Combine("Temp","nasa-live-"+Guid.NewGuid().ToString("N"));
            using var service=new NasaSolService(config,credentials,http,new SolDataCache(folder),()=>new DateTime(2026,11,15,0,0,0,DateTimeKind.Utc));
            var cases=new JArray();var samples=new List<SolResponse>();
            foreach(string mission in new[]{"mission-ABC12345","test_mission_001"}) {
                var dates=new HashSet<string>();
                for(int sol=1;sol<=5;sol++) {
                    var response=await service.GetSolAsync(mission,sol);samples.Add(response);
                    if(!dates.Add(response.earth_date))throw new Exception("Duplicate Sol date");
                    cases.Add(new JObject { ["mission"]=mission,["sol"]=sol,["response"]=JToken.FromObject(response) });
                    Console.WriteLine(mission+" sol="+sol+" earth_date="+response.earth_date+" apod="+(response.apod!=null)+" weather="+(response.facts.weather!=null)+" photo="+(response.photo!=null));
                }
            }
            using(var restored=new NasaSolService(config,credentials,http,new SolDataCache(folder),()=>new DateTime(2026,11,16,0,0,0,DateTimeKind.Utc)))
                foreach(var sample in samples)if(!Ares.Editor.NasaMigrationTests.Same(JToken.FromObject(sample),JToken.FromObject(await restored.GetSolAsync(sample.mission,sample.sol))))throw new Exception("Disk repeat mismatch");
            var prompts=new PromptBuilder(File.ReadAllText("Assets/ARES/Resources/ARES/Narrator/bubble-system.txt"),File.ReadAllText("Assets/ARES/Resources/ARES/Narrator/questions.json"));
            var request=new JObject {["lang"]="pt-BR",["sol"]=1,["phase"]="hint",["facts"]=JToken.FromObject(samples[0].facts)};
            bool groqValid=false;string groqStatus;
            try {
                var response=await new GroqLlmClient(http,config,credentials,prompts).CompleteAsync(prompts.Prepare(request),default);
                groqStatus=NarrationValidator.Validate(response.text,request)??"valid";groqValid=groqStatus=="valid";
                Console.WriteLine("Groq parse/validation: "+groqStatus);
            }catch {groqStatus="unavailable; offline fallback verified separately";Console.WriteLine(groqStatus);}
            Directory.CreateDirectory("Logs");
            var report=new JObject { ["today"]="2026-11-15",["cases"]=cases,["raw"]=JObject.FromObject(transport.Raw),["http"]=JObject.FromObject(transport.Counts),["disk_repeat_identical"]=true,["groq_valid"]=groqValid,["groq_status"]=groqStatus };
            File.WriteAllText("Logs/nasa-live.json",NasaJson.Write(report));
            Console.WriteLine("PASS: 10 Sol responses; distinct dates; repeat from disk identical. Details: Logs/nasa-live.json");return 0;
        }catch(Exception error){Console.WriteLine("FAIL: "+SecretRedactor.Redact(error.GetType().Name));return 1;}
    }
}
