using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Infrastructure.Nasa;

namespace Ares.Domain.Hazards
{
    [Serializable] public sealed class HazardCatalog { public string generatedUtc,sourceVersion="NASA-DONKI/Curiosity; hazards-v1";public int blocksScanned,requests,weatherShortlisted;public CatalogDay[] days=Array.Empty<CatalogDay>(); }
    [Serializable] public sealed class CatalogDay {public string date;public int curiositySol;public SolResponse observation;}
    [Serializable] public sealed class ScenarioSlot {public string name;public string[] acceptable;}
    [Serializable] public sealed class ScenarioRules
    {
        public ScenarioSlot[] slots={
            new ScenarioSlot{name="Orientation",acceptable=new[]{"calm","mild-atmosphere"}},
            new ScenarioSlot{name="Radiation A",acceptable=new[]{"flare-m","radiation"}},
            new ScenarioSlot{name="Atmosphere / breather",acceptable=new[]{"breather","calm"}},
            new ScenarioSlot{name="Radiation B",acceptable=new[]{"impact","radiation"}},
            new ScenarioSlot{name="Climax",acceptable=new[]{"climax-atmosphere","climax","atmosphere"}}
        };
    }
    [Serializable] public sealed class ScenarioResult {public string mission,mode,provenance;public SolResponse[] sols;public int repicks;}
    public sealed class ScenarioDirector
    {
        readonly HazardBalance balance;readonly ScenarioRules profile;
        public ScenarioDirector(HazardBalance balance=null,ScenarioRules profile=null){this.balance=balance??new HazardBalance();this.profile=profile??new ScenarioRules();}
        public static bool Matches(string requirement,HazardPlan p,SolResponse response) {
            switch(requirement) {
                case "calm":return p.Dominant=="calm";
                case "mild-atmosphere":return p.Dominant=="atmosphere" && p.events.All(e=>e.tier==HazardTier.Low);
                case "flare-m":return response.facts?.flare?.@class=="M" && !p.Has(HazardKind.Cme) && !p.Has(HazardKind.Radiation);
                case "radiation":return p.Dominant=="radiation";
                case "impact":return p.Has(HazardKind.Cme);
                case "atmosphere":return p.Dominant=="atmosphere";
                case "breather":return p.Dominant=="atmosphere" && p.events.All(e=>e.tier<=HazardTier.Medium);
                case "climax-atmosphere":return p.Dominant=="atmosphere" && p.Has(HazardKind.Dust) && p.Has(HazardKind.Cold);
                case "climax":return p.events.Any(e=>e.damaging && e.tier==HazardTier.High);
                default:return false;
            }
        }
        public async Task<ScenarioResult> SelectAsync(string mission,HazardCatalog catalog,Func<CatalogDay,int,CancellationToken,Task<SolResponse>> verify,
            Func<IReadOnlyList<SolResponse>,bool> solvable,Func<Task<ScenarioResult>> fallback,CancellationToken token=default)
        {
            if(catalog?.days==null || catalog.days.Length<5)return await fallback();
            var chosen=new List<SolResponse>();var dates=new HashSet<string>();var curiosity=new HashSet<int>();int repicks=0;
            for(int slot=0;slot<5;slot++) {
                bool found=false;string previous=chosen.Count==0?null:HazardPlanner.Build(chosen.Last(),balance).Dominant;
                foreach(string requirement in profile.slots[slot].acceptable) {
                    var candidates=catalog.days.Where(d=>d?.observation!=null && !dates.Contains(d.date) && !curiosity.Contains(d.curiositySol))
                        .Where(d=>Matches(requirement,HazardPlanner.Build(d.observation,balance),d.observation))
                        .OrderBy(d=>DeterministicRng.Value(mission,"slot|"+slot+"|"+d.date)).ThenBy(d=>d.date,StringComparer.Ordinal).Take(balance.maxRepicksPerSlot);
                    foreach(var candidate in candidates) {
                        token.ThrowIfCancellationRequested();var real=await verify(candidate,slot+1,token);if(real==null){repicks++;continue;}
                        var plan=HazardPlanner.Build(real,balance);
                        if(real.earth_date!=candidate.date || real.data_dates?.curiosity_sol!=candidate.curiositySol || !Matches(requirement,plan,real) || plan.Dominant==previous){repicks++;continue;}
                        var trial=new List<SolResponse>(chosen){real};if(solvable!=null && !solvable(trial)){repicks++;continue;}
                        chosen.Add(real);dates.Add(real.earth_date);curiosity.Add(real.data_dates.curiosity_sol);found=true;break;
                    }
                    if(found)break;
                }
                if(!found)return await fallback();
            }
            return new ScenarioResult {mission=mission,mode="Curated",provenance="catalog-verified",sols=chosen.ToArray(),repicks=repicks};
        }
    }
}
