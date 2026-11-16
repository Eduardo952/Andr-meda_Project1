using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Ares.Infrastructure.Nasa;

namespace Ares.Domain.Hazards
{
    public enum HazardKind { Flare, Cme, Radiation, Dust, Cold, LowOxygen, Power }
    public enum HazardTier { Low=1, Medium=2, High=3 }
    public enum HazardProvenance { Real, Derived, Simulated }
    [Serializable] public sealed class HazardEvent
    {
        public string id,field,api,earthDate,description;
        public HazardKind kind; public HazardTier tier; public HazardProvenance provenance;
        public float startsAt,duration,intensity,windHeading; public bool damaging;
    }
    [Serializable] public sealed class HazardPlan
    {
        public string mission,earthDate,dataProvenance,mode;public int sol;
        public HazardEvent[] events=Array.Empty<HazardEvent>();
        public int shieldDemand,lifeDemand,greenhouseDemand;public float solarFactor=1;
        public bool Has(HazardKind kind)=>Array.Exists(events,e=>e.kind==kind && e.damaging);
        public string Dominant=>Has(HazardKind.Radiation)||Has(HazardKind.Flare)||Has(HazardKind.Cme)?"radiation":Has(HazardKind.Dust)||Has(HazardKind.Cold)?"atmosphere":"calm";
    }
    public static class HazardPlanner
    {
        public static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        public static HazardPlan Build(SolResponse data,HazardBalance balance=null,string provenance=null,string mode="Consecutive")
        {
            if(data==null)throw new ArgumentNullException(nameof(data));var b=balance??new HazardBalance();var f=data.facts;
            var plan=new HazardPlan {mission=data.mission,sol=data.sol,earthDate=data.earth_date,dataProvenance=provenance??(data.is_fallback?"sample":"live"),mode=mode};
            var events=new List<HazardEvent>();
            float Schedule(string timestamp,string id) {
                double roll=DeterministicRng.Value(data.mission+"|"+data.sol+"|"+id,"time");
                if(DateTimeOffset.TryParse(timestamp,CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date) && date.UtcDateTime.ToString("yyyy-MM-dd")==data.earth_date)
                    roll=(date.UtcDateTime.TimeOfDay.TotalDays-.3+1)%1;
                return (float)Math.Max(b.minEventSeconds,Math.Min(b.latestEventSeconds,roll*MissionTime.RealSecondsPerSol));
            }
            void Add(HazardKind kind,HazardTier tier,string api,string field,string timestamp,HazardProvenance origin,bool damage,float intensity,string description) {
                string id=kind.ToString().ToLowerInvariant();double r=DeterministicRng.Value(data.mission+"|"+data.sol+"|"+id,"duration");
                events.Add(new HazardEvent {id=id,kind=kind,tier=tier,api=api,field=field,earthDate=data.earth_date,provenance=origin,damaging=damage,
                    startsAt=Schedule(timestamp,id),duration=b.eventDuration*(float)(1+b.tierDurationJitter*(r*2-1)),intensity=intensity,description=description,
                    windHeading=(float)(DeterministicRng.Value(data.mission+"|"+data.sol+"|"+id,"direction")*360)});
            }
            var flare=f?.flare;bool validFlare=flare!=null && flare.@class!=null && flare.@class.Length==1 && "ABCMX".Contains(flare.@class) && Finite(flare.magnitude) && flare.magnitude>=0;
            var cme=f?.cme;bool validCme=cme!=null && Finite(cme.speed_kms) && cme.speed_kms>0;
            string impact=f?.mars_impact?.level;bool hit=impact=="direct"||impact=="glancing_blow";
            double? temperature=f?.weather!=null && Finite(f.weather.temp_avg_c)?f.weather.temp_avg_c:(double?)null;
            plan.shieldDemand=Rules.Shield(validFlare?(char?)flare.@class[0]:null,validFlare?flare.magnitude:0,validCme?(double?)cme.speed_kms:null);
            plan.lifeDemand=Rules.Thermal(temperature)??25;plan.greenhouseDemand=20;
            if(validFlare) {
                var tier=flare.@class=="X"?HazardTier.High:flare.@class=="M"?HazardTier.Medium:HazardTier.Low;
                float flux=(float)Math.Max(.2,Math.Min(3,1+Math.Log10(Math.Max(.1,flare.magnitude))*.35));
                Add(HazardKind.Flare,tier,"DONKI/FLR","facts.flare",flare.peak_time,HazardProvenance.Real,tier!=HazardTier.Low,flux,flare.@class+flare.magnitude.ToString("0.##",CultureInfo.InvariantCulture));
            }
            if(validCme)Add(HazardKind.Cme,cme.speed_kms<b.lowCmeSpeed?HazardTier.Low:cme.speed_kms<=b.highCmeSpeed?HazardTier.Medium:HazardTier.High,
                "DONKI/CMEAnalysis + WSA-Enlil","facts.cme + facts.mars_impact",f.mars_impact?.arrival_time,HazardProvenance.Real,hit,
                hit?(impact=="direct"?1:.6f)*(float)Math.Min(2,cme.speed_kms/1000):.2f,impact??"no reading");
            if(validFlare&&flare.@class=="X" || impact=="direct")Add(HazardKind.Radiation,HazardTier.High,"DONKI/FLR + WSA-Enlil",
                impact=="direct"?"facts.mars_impact.level":"facts.flare.class",impact=="direct"?f.mars_impact.arrival_time:flare.peak_time,HazardProvenance.Derived,true,impact=="direct"?1:.6f,"derived; no SEP observation");
            var effects=data.effects;string risk=effects?.dust_storm_risk;
            if(!data.is_fallback && effects!=null && (risk=="medium"||risk=="high") && Finite(effects.panel_factor) && effects.panel_factor>0 && effects.panel_factor<1 && Finite(effects.mars_season_ls)) {
                var tier=risk=="high"?HazardTier.High:HazardTier.Medium;
                Add(HazardKind.Dust,tier,"Mars24 / mission seed","effects.dust_storm_risk + effects.panel_factor",null,HazardProvenance.Derived,true,tier==HazardTier.High?1.25f:1,"season-derived risk; simulated wind");
                plan.solarFactor=(float)Math.Max(b.solarFloor,Math.Min(1,effects.panel_factor));
                plan.greenhouseDemand+=tier==HazardTier.High?b.greenhouseDustHigh:b.greenhouseDustMedium;
            }
            if(temperature.HasValue && temperature<=b.coldCaution)Add(HazardKind.Cold,temperature<=b.coldEmergency?HazardTier.High:temperature<=b.coldWarning?HazardTier.Medium:HazardTier.Low,
                "Curiosity/REMS","facts.weather.temp_avg_c",null,HazardProvenance.Real,true,(float)Math.Min(2,Math.Abs(temperature.Value)/80),temperature.Value.ToString("0.0",CultureInfo.InvariantCulture)+" C");
            plan.events=events.OrderBy(e=>e.startsAt).ThenBy(e=>e.id,StringComparer.Ordinal).ToArray();return plan;
        }
    }
}
