using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Ares.Infrastructure.Nasa
{
    public static class Js
    {
        public static bool Truth(JToken t) => t!=null && t.Type!=JTokenType.Null && t.Type!=JTokenType.Undefined &&
            (t.Type!=JTokenType.Boolean || (bool)t) && (t.Type!=JTokenType.String || ((string)t).Length>0) &&
            ((t.Type!=JTokenType.Float && t.Type!=JTokenType.Integer) || ((double)t!=0 && !double.IsNaN((double)t)));
        public static string String(JToken t) => t==null || t.Type==JTokenType.Undefined ? "undefined" : t.Type==JTokenType.Null ? "null" :
            t.Type==JTokenType.String ? (string)t : t.Type==JTokenType.Object ? "[object Object]" : t.Type==JTokenType.Array ? string.Join(",",t.Select(String)) :
            t.Type==JTokenType.Boolean ? ((bool)t?"true":"false") : Convert.ToString(((JValue)t).Value,CultureInfo.InvariantCulture);
        public static double? ParseFloat(JToken token)
        {
            var match=Regex.Match(String(token),@"^\s*([+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?)");
            return match.Success && double.TryParse(match.Groups[1].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out var n) && !double.IsInfinity(n) && !double.IsNaN(n) ? n : (double?)null;
        }
        public static double? Number(JToken token)
        {
            if(token==null)return null;if(token.Type==JTokenType.Null)return 0;if(token.Type==JTokenType.Boolean)return (bool)token?1:0;
            var s=String(token).Trim();if(s.Length==0)return 0;
            return double.TryParse(s,NumberStyles.Float,CultureInfo.InvariantCulture,out var n) && !double.IsNaN(n) && !double.IsInfinity(n)?n:(double?)null;
        }
        public static string Slice(string s,int count) => (s??"").Substring(0,Math.Min(count,(s??"").Length));
        public static string Or(string a,string b) => string.IsNullOrEmpty(a)?b:a;
    }
    public static class ApiModels
    {
        public static JObject[] Rows(string json)
        {
            if(string.IsNullOrWhiteSpace(json))return Array.Empty<JObject>();
            try {
                var token=NasaJson.Parse(json);var all=token is JArray a?a.ToArray():new[]{token};
                return all.OfType<JObject>().Where(j=>!Js.Truth(j["error"])).ToArray();
            }catch(Newtonsoft.Json.JsonException){return Array.Empty<JObject>();}
        }
        public static T[] Many<T>(string json)
        {
            var values = new List<T>();
            foreach (var row in Rows(json)) try { values.Add(NasaJson.Read<T>(row.ToString())); }
                catch (Newtonsoft.Json.JsonException) { /* A malformed source must not discard the other sources. */ }
            return values.ToArray();
        }
        public static T First<T>(string json) where T:class => Many<T>(json).FirstOrDefault();
    }
    public static class EffectsCalculator
    {
        public static SolEffects Calculate(SelectedWindow w,SolFacts f)
        {
            string impact=f.mars_impact?.level??"none";
            return new SolEffects { radiation_alert=f.severity=="alert",comm_disruption=f.flare?.@class=="X"?"high":f.flare?.@class=="M"?"medium":"none",
                shield_load_factor=impact=="direct"?1:impact=="glancing_blow"?.6:.2,
                heating_factor=f.weather==null?1:MarsCalendar.Round(Math.Min(1.3,Math.Max(.8,1+(-55-f.weather.temp_avg_c)/200))*100)/100,
                dust_storm_risk=w.dust_storm_risk,panel_factor=w.dust_storm_risk=="high"?.5:w.dust_storm_risk=="medium"?.8:1,
                mars_season_ls=w.ls,dust_season=w.dust_season };
        }
    }
    public static class SolDataAssembler
    {
        public static SolResponse Assemble(SelectedWindow w,string apodJson,string cmeJson,string enlilJson,string weatherJson,string imagesJson)
        {
            SolApod apod=null;FlareFact flare=null;CmeFact cme=null;ImpactFact impact=null;WeatherFact weather=null;SolPhoto photo=null;
            var apods=ApiModels.Many<NasaApodDto>(apodJson);
            var image=apods.FirstOrDefault(x=>x!=null && x.media_type!="video" && (Js.Or(x.hdurl,x.url)??"").StartsWith("https://",StringComparison.Ordinal));
            JToken Rendered(JToken t) => t is JObject o && o.ContainsKey("rendered")?o["rendered"]:t;
            if(image!=null) {
                var title=Rendered(image.title);var explanation=Rendered(image.explanation);
                apod=new SolApod { date=image.date,title=Js.Truth(title)?Js.String(title):"Astronomy Picture of the Day",
                    explanation=Js.Slice(Regex.Replace(Js.Truth(explanation)?Js.String(explanation):"","<[^>]+>",""),600),
                    url=Js.Or(image.hdurl,image.url),copyright=string.IsNullOrEmpty(image.copyright)?null:image.copyright };
            }
            foreach(var f in w.flares_day??Array.Empty<DonkiFlare>()) {
                var m=Regex.Match(f.classType??"",@"^([ABCMX])(\d+(?:\.\d+)?)");if(!m.Success)continue;
                var candidate=new FlareFact {@class=m.Groups[1].Value,magnitude=double.Parse(m.Groups[2].Value,CultureInfo.InvariantCulture),peak_time=Js.Or(f.peakTime,Js.Or(f.beginTime,null)),date=w.earth_date};
                if(flare==null || "ABCMX".IndexOf(candidate.@class)>"ABCMX".IndexOf(flare.@class) || candidate.@class==flare.@class && candidate.magnitude>flare.magnitude)flare=candidate;
            }
            var speeds=ApiModels.Rows(cmeJson).Select(c=>Js.Number(c["speed"])).Where(n=>n.HasValue).Select(n=>n.Value).ToArray();
            if(speeds.Length>0)cme=new CmeFact {speed_kms=speeds.Max(),date=w.earth_date};
            var sims=ApiModels.Many<DonkiEnlil>(enlilJson);
            if(sims.Length>0 || cme!=null) {
                string level="none",arrival=null;
                foreach(var s in sims)foreach(var imp in s.impactList??Array.Empty<DonkiImpact>())if(string.Equals(imp.location,"mars",StringComparison.OrdinalIgnoreCase)) {
                    if(Js.Truth(imp.isGlancingBlow)) {if(level=="none"){level="glancing_blow";arrival=Js.Or(imp.arrivalTime,null);}}
                    else {level="direct";arrival=Js.Or(imp.arrivalTime,arrival);}
                }
                impact=new ImpactFact {level=level,arrival_time=arrival,window=new[]{w.d_prev,w.d_next}};
            }
            var rawWeather=ApiModels.Rows(weatherJson).FirstOrDefault();
            var source=ApiModels.First<MarsWeatherDto>(weatherJson);
            if(rawWeather!=null && rawWeather.ContainsKey("sol") && source!=null) {
                double? min=Js.ParseFloat(source.min_temp),max=Js.ParseFloat(source.max_temp);
                if(min.HasValue && max.HasValue)weather=new WeatherFact { temp_min_c=min.Value,temp_max_c=max.Value,temp_avg_c=MarsCalendar.Round((min.Value+max.Value)/2*10)/10,
                    pressure_pa=Js.ParseFloat(source.pressure),opacity=Js.Or(source.atmo_opacity,null),curiosity_sol=w.curiosity_sol,terrestrial_date=Js.Or(source.terrestrial_date,null),is_historical=false };
            }
            var item=ApiModels.First<ImageLibraryDto>(imagesJson)?.collection?.items?.FirstOrDefault();var info=item?.data?.FirstOrDefault();
            var preview=item?.links?.FirstOrDefault(l=>l.rel=="preview" && (l.href??"").StartsWith("https://",StringComparison.Ordinal));
            if(info!=null && preview!=null)photo=new SolPhoto {id=info.nasa_id,title=info.title,description=Js.Slice(info.description,400),credit=Js.Or(info.photographer,Js.Or(info.center,"NASA")),url=preview.href};
            var facts=new SolFacts {flare=flare,cme=cme,mars_impact=impact,weather=weather,wind=null,
                severity=flare?.@class=="X" || impact?.level=="direct"?"alert":flare?.@class=="M" || impact?.level=="glancing_blow"?"attention":"calm"};
            return new SolResponse { sol=w.sol,mission=w.mission,earth_date=w.earth_date,mission_window=w.mission_window,facts=facts,apod=apod,photo=photo,effects=EffectsCalculator.Calculate(w,facts),
                data_dates=new SolDataDates {sol=w.sol,earth_date=w.earth_date,curiosity_sol=w.curiosity_sol,flare=flare==null?null:w.earth_date,cme=cme==null?null:w.earth_date,mars_impact_window=impact?.window,
                    weather=weather==null?null:Js.Or(weather.terrestrial_date,w.earth_date),apod=apod?.date,
                    note="Flare, CME, Mars impact and Mars weather belong to the same real period: Earth date above (Curiosity sol "+w.curiosity_sol+"). The Observatory picture is from a random day in NASA's picture archive." } };
        }
    }
}
