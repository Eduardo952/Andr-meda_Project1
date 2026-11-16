using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Ares.Infrastructure.Nasa
{
    /// <summary>Preserves the game's post-LLM checks: facts, task results, numbers, classes and language.</summary>
    public static class NarrationValidator
    {
        public static IReadOnlyDictionary<string,string[]> TaskNames {private get;set;} = new Dictionary<string,string[]>();
        static JToken At(JToken value,string key)=>(value as JObject)?[key];
        static bool Has(string text,string pattern)=>Regex.IsMatch(text,pattern,RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
        static double Round1(double n)=>MarsCalendar.Round(n*10)/10;
        public static string DetectLanguage(string text)
        {
            int en=Regex.Matches(text,@"\b(the|and|is|are|was|you|we|our|to|of|at|today|energy|shield|needs?|let's)\b",RegexOptions.IgnoreCase).Count;
            int pt=Regex.Matches(text,@"(?<![\p{L}])(é|são|está|foi|uma|um|você|nós|nosso|nossa|para|de|do|da|hoje|energia|escudo|não|mas|vamos|precisa)(?![\p{L}])",RegexOptions.IgnoreCase).Count;
            return en==pt?"unknown":en>pt?"en":"pt-BR";
        }
        public static string Validate(string text,JObject request)
        {
            if(string.IsNullOrWhiteSpace(text))return "empty";string t=text.Trim();if(t.Length>400)return "too_long";
            if(Regex.Split(t,@"[.!?…]+(?=\s|$)").Count(s=>s.Trim().Length>1)>3)return "too_many_sentences";
            string lang=DetectLanguage(t);if(lang!="unknown" && lang!=(string)request["lang"])return "wrong_language";
            var facts=request["facts"] as JObject??new JObject();var results=(request["task_results"] as JArray??new JArray()).OfType<JObject>().ToArray();
            var ids=new HashSet<string>(TaskNames.Keys);foreach(var id in request["assigned_tasks"] as JArray??new JArray())ids.Add(id.Type==JTokenType.String?(string)id:(string)id["id"]??"");
            bool Mentions(string id)=>new[]{id}.Concat(TaskNames.TryGetValue(id,out var names)?names:Array.Empty<string>()).Any(n=>!string.IsNullOrEmpty(n) && t.IndexOf(n,StringComparison.OrdinalIgnoreCase)>=0);
            foreach(string id in ids)if(Mentions(id) && !results.Any(r=>(string)r["id"]==id))return "mentions_incomplete_task";
            if(results.Length==0 && Has(t,@"\b(completed|finished|mistakes?|seconds?|stars?)\b|conclu[ií]|terminou|\berros?\b|segundos|estrelas"))return "mentions_absent_task_result";
            var mentioned=results.Where(r=>Mentions((string)r["id"]??"")).ToArray();
            foreach(var pair in new[]{new[]{@"(-?\d+(?:[.,]\d+)?)\s*(?:mistakes?|erros?)\b","mistakes"},new[]{@"(-?\d+(?:[.,]\d+)?)\s*(?:seconds?|segundos)\b","seconds"},new[]{@"(-?\d+(?:[.,]\d+)?)\s*(?:stars?|estrelas)\b","stars"}})
                foreach(Match match in Regex.Matches(t,pair[0],RegexOptions.IgnoreCase)) {
                    double value=double.Parse(match.Groups[1].Value.Replace(',','.'),CultureInfo.InvariantCulture);
                    if(!(mentioned.Length>0?mentioned:results).Any(r=>Js.Number(r[pair[1]]) is double n && Round1(n)==Round1(value)))return "unknown_task_metric";
                }
            if(!Js.Truth(facts["flare"]) && Has(t,@"\bflares?\b|erup[cç][aã]o|erup[cç][oõ]es"))return "mentions_absent_flare";
            if(!Js.Truth(facts["cme"]) && Has(t,@"\bCMEs?\b|coronal|eje[cç][aã]o"))return "mentions_absent_cme";
            if(!Js.Truth(facts["weather"]) && Has(t,@"temperatur|°\s?C|graus|degrees"))return "mentions_absent_weather";
            if(!Js.Truth(facts["wind"]) && Has(t,@"\bwind\b|\bvento\b"))return "mentions_absent_wind";
            if(!Js.Truth(At(facts["game"],"simulated_storm")) && Has(t,@"dust storms?|tempestades? de poeira"))return "mentions_absent_dust_storm";
            if((!Js.Truth(facts["mars_impact"]) || (string)At(facts["mars_impact"],"level")=="none") && Has(t,@"\b(hit|hits|reach(es)?|atinge|atingir|chega(r)?) (mars|marte)\b") && !Has(t,@"\b(not|won't|n[aã]o)\b"))return "mentions_absent_impact";
            const string classes=@"\b[ABCMX]\d+(?:[.,]\d+)?\b";
            foreach(Match match in Regex.Matches(t,classes)) {
                string c=match.Value.Replace(',','.');if(!Js.Truth(facts["flare"]) || c[0].ToString()!=(string)At(facts["flare"],"class") || double.Parse(c.Substring(1),CultureInfo.InvariantCulture)!=Js.Number(At(facts["flare"],"magnitude")))return "unknown_flare_class";
            }
            var allowed=new HashSet<double>();
            void Add(JToken value,bool abs=false){var n=Js.Number(value);if(!n.HasValue)return;allowed.Add(Round1(n.Value));if(abs){allowed.Add(MarsCalendar.Round(n.Value));allowed.Add(Math.Abs(MarsCalendar.Round(n.Value)));allowed.Add(Round1(Math.Abs(n.Value)));}}
            Add(request["sol"]);foreach(var r in results){Add(r["seconds"]);Add(r["mistakes"]);Add(r["stars"]);}
            Add(At(facts["flare"],"magnitude"));Add(At(facts["cme"],"speed_kms"));Add(At(facts["weather"],"temp_avg_c"),true);Add(At(facts["wind"],"speed_avg_ms"));
            foreach(string section in new[]{"demand_kwh","player_kwh"})foreach(string key in new[]{"shield","life","greenhouse"})Add(At(request[section],key));
            foreach(string key in new[]{"dose","energy_pool_kwh","panel_dust_pct"})Add(At(facts["game"],key));
            foreach(string key in new[]{"battery_kwh","oxygen_percent","cold_exposure_seconds","medical_evacuations","med_kits","oxygen_canisters"})Add(At(facts["game"],key));
            foreach(var e in At(facts["game"],"consequences") as JArray??new JArray())foreach(string key in new[]{"allocated","demand","missing","dose","energy_loss_pct"})if(e[key]?.Type!=JTokenType.Null)Add(e[key]);
            foreach(Match token in Regex.Matches(Regex.Replace(t,classes," "),@"-?\d[\d.,]*")) {
                string s=token.Value.TrimEnd('.',',');var variants=new[]{s.Replace(',','.'),Regex.Replace(s,@"[.,](?=\d{3}\b)","").Replace(',','.'),s.Replace(".","").Replace(",","")};
                bool known=variants.Any(v=>double.TryParse(v,NumberStyles.Float,CultureInfo.InvariantCulture,out var n) && (allowed.Contains(Round1(n)) || allowed.Contains(Round1(Math.Abs(n)))));
                if(!known)return "unknown_number";
            }
            return null;
        }
    }
}
