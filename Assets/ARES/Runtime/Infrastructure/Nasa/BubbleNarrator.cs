using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Ares.Infrastructure.Nasa
{
    [Serializable] public sealed class NarratorResponse { public string text, mood="happy";public bool off_topic; }
    public sealed class PromptBuilder
    {
        public static readonly string[] Phases={"sol_start","alert","hint","sol_summary","task_complete","answer","free_question"};
        readonly string system;readonly Dictionary<string,string[]> questions;
        public PromptBuilder(string system,string questionsJson){this.system=system;questions=NasaJson.Read<Dictionary<string,string[]>>(questionsJson);}
        public JObject Prepare(JObject input)
        {
            var b=(JObject)input.DeepClone();string language=(string)b["lang"],phase=(string)b["phase"];
            if(!(language=="en" || language=="pt-BR") || !(b["sol"]?.Type==JTokenType.Integer || b["sol"]?.Type==JTokenType.Float) || !Js.Truth(b["facts"]) || !Phases.Contains(phase))throw new ArgumentException("bad input");
            if(phase=="answer") {
                if(!questions.TryGetValue((string)b["question_id"]??"",out var pair))throw new ArgumentException("unknown question_id");
                b["question_text"]=pair[language=="pt-BR"?1:0];
            }
            if(phase=="free_question") {
                string text=Sanitize(b["question_text"]==null || b["question_text"].Type==JTokenType.Null?"":Js.String(b["question_text"]));
                if(text.Length<3)throw new ArgumentException("empty question");b["question_text"]=text;
            }
            return b;
        }
        public static string Sanitize(string text)
        {
            text=Regex.Replace(text??"",@"https?://\S+"," ",RegexOptions.IgnoreCase);
            text=Regex.Replace(text,@"\S+@\S+"," ");text=Regex.Replace(text,@"[0-9]{6,}"," ");
            text=Regex.Replace(text,@"[<>{}`]"," ");return Js.Slice(Regex.Replace(text,@"\s+"," ").Trim(),140);
        }
        public string Completion(JObject prepared,NasaConfig config)=>NasaJson.Write(new {
            model=config.GroqModel,temperature=config.GroqTemperature,max_tokens=config.GroqMaxTokens,
            response_format=new {type="json_object"},messages=new[]{new {role="system",content=system},new {role="user",content=NasaJson.Write(prepared)}}
        });
    }
    public static class NarratorResponseParser
    {
        public const string Fallback="Estou com um pouco de interferencia no radio agora. Pode repetir, astronauta?";
        public static NarratorResponse Parse(JObject envelope)
        {
            // The direct chat transport wraps choices[0].message.content in output before this exact A5 port.
            JToken rawValue=null;
            foreach(var candidate in new[]{envelope?["output"],envelope?["text"],envelope?["response"],(envelope?["message"] as JObject)?["content"]})
                if(candidate!=null && candidate.Type!=JTokenType.Null){rawValue=candidate;break;}
            JToken parsed=null;
            if(Js.Truth(rawValue) && (rawValue is JObject || rawValue is JArray))parsed=rawValue;
            string raw=parsed!=null?NasaJson.Write(rawValue):Js.Truth(rawValue)?Js.String(rawValue):"";
            raw=Regex.Replace(raw,@"<think>[\s\S]*?</think>","",RegexOptions.IgnoreCase);
            raw=Regex.Replace(raw,"```(?:json)?","",RegexOptions.IgnoreCase).Trim();
            if(parsed==null) {
                int start=raw.IndexOf('{');if(start>=0) {
                    int depth=0,end=-1;
                    // Deliberately match A5's brace counter, including its handling of braces in strings.
                    for(int i=start;i<raw.Length;i++){if(raw[i]=='{')depth++;else if(raw[i]=='}'){depth--;if(depth==0){end=i;break;}}}
                    if(end>=0)try{parsed=NasaJson.Parse(raw.Substring(start,end-start+1));}catch(Newtonsoft.Json.JsonException){}
                }
            }
            var obj=parsed as JObject;
            string text=Js.Truth(obj?["text"])?Js.String(obj["text"]).Trim():raw.Length>0 && !raw.Contains("{")?raw.Trim():"";
            if(text.Length==0)text=Fallback;
            var sentences=Regex.Matches(text,@"[^.!?]+[.!?]+(\s|$)|[^.!?]+$");
            if(sentences.Count>0)text=string.Concat(sentences.Cast<Match>().Take(3).Select(m=>m.Value)).Trim();
            string mood=(string)obj?["mood"];if(!new[]{"happy","worried","excited","proud"}.Contains(mood))mood="happy";
            return new NarratorResponse {text=Js.Slice(text,600),mood=mood,off_topic=obj?["off_topic"]?.Type==JTokenType.Boolean && (bool)obj["off_topic"]};
        }
    }
    public interface ILlmClient { Task<NarratorResponse> CompleteAsync(JObject prepared,CancellationToken cancellation); }
    public sealed class GroqLlmClient : ILlmClient
    {
        readonly HttpJsonClient http;readonly NasaConfig config;readonly ApiCredentials credentials;readonly PromptBuilder prompts;
        public GroqLlmClient(HttpJsonClient http,NasaConfig config,ApiCredentials credentials,PromptBuilder prompts){this.http=http;this.config=config;this.credentials=credentials;this.prompts=prompts;}
        public async Task<NarratorResponse> CompleteAsync(JObject prepared,CancellationToken cancellation)
        {
            bool proxy=!string.IsNullOrWhiteSpace(config.GroqProxyBaseUrl);string endpoint=proxy?config.GroqProxyBaseUrl:config.GroqBaseUrl;
            bool directAllowed=config.PrivateTrustedBuild;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            directAllowed=true;
#endif
            if(!NasaConfig.Https(endpoint) || !proxy && (!directAllowed || string.IsNullOrWhiteSpace(credentials.GROQ_API_KEY)))throw new InvalidOperationException("Groq credentials/proxy not configured");
            if(!proxy && new Uri(endpoint).Host!="api.groq.com")throw new InvalidOperationException("Direct Groq host must be api.groq.com");
            var request=new HttpRequest {Url=endpoint,Method="POST",Body=prompts.Completion(prepared,config),TimeoutSeconds=config.HttpTimeoutSeconds,MaxBytes=128*1024};
            if(!proxy)request.Headers["Authorization"]="Bearer "+credentials.GROQ_API_KEY;
            var response=await http.SendAsync(request,cancellation).ConfigureAwait(false);if(!response.Success)throw new InvalidOperationException("Groq unavailable");
            var json=NasaJson.Parse(response.Text) as JObject;
            if(json?["choices"] is JArray choices && choices.Count>0)return NarratorResponseParser.Parse(new JObject {["output"]=choices[0]?["message"]?["content"]});
            return NarratorResponseParser.Parse(json??new JObject());
        }
    }
    public sealed class BubbleNarrator
    {
        readonly PromptBuilder prompts;readonly ILlmClient client;readonly Func<JObject,NarratorResponse> fallback;
        public BubbleNarrator(PromptBuilder prompts,ILlmClient client,Func<JObject,NarratorResponse> fallback=null){this.prompts=prompts;this.client=client;this.fallback=fallback??LocalFallback;}
        public async Task<NarratorResponse> SayAsync(JObject request,CancellationToken cancellation=default)
        {
            var prepared=prompts.Prepare(request); // Invalid questions fail locally before transport/fallback.
            try {
                var response=await client.CompleteAsync(prepared,cancellation).ConfigureAwait(false);
                return NarrationValidator.Validate(response.text,prepared)==null?response:fallback(prepared);
            }catch(OperationCanceledException){throw;}catch{return fallback(prepared);}
        }
        public static NarratorResponse LocalFallback(JObject request)=>new NarratorResponse {
            text=(string)request["lang"]=="en"?"I have a little radio interference right now. Could you try again, astronaut?":NarratorResponseParser.Fallback,mood="happy"
        };
    }
}
