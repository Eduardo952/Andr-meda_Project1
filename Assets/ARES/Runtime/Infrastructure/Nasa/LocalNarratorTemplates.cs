using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Ares.Infrastructure.Nasa
{
    /// <summary>Existing client templates, using the existing locale catalog. No network or invented observations.</summary>
    public sealed class LocalNarratorTemplates
    {
        readonly Func<string,string,string> translate;
        public LocalNarratorTemplates(Func<string,string,string> translate) { this.translate=translate; }
        public NarratorResponse For(JObject request)
        {
            string lang=(string)request["lang"],phase=(string)request["phase"];
            string T(string key,params object[] values) {
                string text=translate(lang,key);
                for(int i=0;i+1<values.Length;i+=2)text=text.Replace("{{"+values[i]+"}}",Convert.ToString(values[i+1],CultureInfo.InvariantCulture));
                return text;
            }
            string N(double n)=>n.ToString("0.#",CultureInfo.GetCultureInfo(lang=="en"?"en-US":"pt-BR"));
            var f=request["facts"] as JObject??new JObject();
            var parts=new List<string>();string mood="happy";
            var keys=new[]{"shield","life","greenhouse"};
            NarratorResponse Line(string text,string emotion)=>new NarratorResponse {text=text,mood=emotion};
            string condition=(string)(f["game"] as JObject)?["condition"];
            if((phase=="alert" || phase=="hint") && (condition=="Warning" || condition=="Incapacitated"))return Line(T(condition=="Incapacitated"?"hazard.rescue":"hazard.remedy."+(string)(f["game"] as JObject)?["condition_cause"]),"worried");
            switch(phase) {
                case "task_complete":
                    var task=(request["task_results"] as JArray)?.LastOrDefault() as JObject;
                    return task==null?Line(T("bubble.taskTry"),"happy"):Line(T("mini.taskNarration","task",T("mini."+(string)task["id"]+".name"),"seconds",N((double)task["seconds"]),"mistakes",(int)task["mistakes"]),"proud");
                case "answer":
                    string answerKey="narrator.answers."+((string)request["question_id"]??"what_is_a_flare");
                    string answer=T(answerKey);
                    // The existing client has three local answers; the remaining nine require the LLM.
                    return answer==answerKey?BubbleNarrator.LocalFallback(request):Line(answer,"excited");
                case "hint":
                    var demand=request["demand_kwh"] as JObject;
                    if(demand==null)return BubbleNarrator.LocalFallback(request);
                    string big=keys.Aggregate((a,b)=>(double)demand[a]>=(double)demand[b]?a:b);
                    return Line(T("narrator.hint","system",T("sys."+big)),"excited");
                case "sol_summary":
                    var game=f["game"] as JObject;
                    var primary=(game?["consequences"] as JArray)?.OfType<JObject>().FirstOrDefault(e=>(string)e["id"]!="perfect-sol" && (string)e["id"]!="energy-surplus");
                    if(primary!=null)parts.Add(T("narrator.consequences."+(string)primary["id"],"a",primary["allocated"]??0,"d",primary["demand"]??0,"m",primary["missing"]??0,"dose",primary["dose"],"loss",primary["energy_loss_pct"]));
                    var result=request["result"] as JObject;
                    var bad=keys.Where(k=>(string)result?[k]=="deficit").ToArray();
                    parts.Add(bad.Length==0?T("narrator.summary.good"):T("narrator.summary.bad","systems",string.Join(", ",bad.Select(k=>T("sys."+k)))));
                    // Original BALANCE.REVEAL_ANSWER_AT_SUMMARY is true. These are actual game demands.
                    if(request["demand_kwh"] is JObject d)parts.Add(T("narrator.summary.reveal","s",d["shield"],"l",d["life"],"g",d["greenhouse"]));
                    return Line(string.Join(" ",parts),bad.Length==0?"proud":"worried");
                default:
                    string severity=(string)f["severity"]??"calm";
                    parts.Add(T("narrator.start."+severity,"sol",request["sol"]));
                    if(f["flare"] is JObject flare)parts.Add(T("narrator.flare","c",(string)flare["class"]+N((double)flare["magnitude"])));
                    if(f["cme"] is JObject cme)parts.Add(T((string)(f["mars_impact"] as JObject)?["level"]=="none"?"narrator.cmeNoImpact":"narrator.cme","v",N((double)cme["speed_kms"])));
                    else if(f["weather"] is JObject weather)parts.Add(T("narrator.weather","t",N(MarsCalendar.Round((double)weather["temp_avg_c"]))));
                    if((bool?)(f["game"] as JObject)?["simulated_storm"]==true)parts.Add(T("narrator.dustStorm","v",f["wind"] is JObject wind?N((double)wind["speed_avg_ms"]):T("narrator.trainingWind")));
                    if(phase=="alert"){parts.RemoveRange(1,parts.Count-1);parts.Add(T("narrator.alert"));}
                    mood=severity=="alert"?"worried":severity=="attention"?"excited":"happy";
                    return Line(string.Join(" ",parts.Take(3)),mood);
            }
        }
    }
}
