using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Domain.Hazards;
using Ares.Infrastructure.Nasa;
using Newtonsoft.Json.Linq;

namespace Ares.Infrastructure.Hazards
{
    /// <summary>Resumable archive scan using the existing NASA facade. Output contains public data only.</summary>
    public sealed class HazardCatalogBuilder
    {
        readonly NasaSolService nasa;readonly string folder;readonly HazardBalance balance;
        public HazardCatalogBuilder(NasaSolService nasa,string folder,HazardBalance balance=null){this.nasa=nasa;this.folder=folder;this.balance=balance??new HazardBalance();}
        async Task<string> Block(string kind,string start,string end,CancellationToken token)
        {
            string file=Path.Combine(folder,kind+"-"+start+".json");
            if(File.Exists(file))return await Task.Run(()=>File.ReadAllText(file),token);
            string json=await nasa.ReadArchiveBlockAsync(kind,start,end,token);
            // Failed/timeout requests are not marked complete and can be retried on resume.
            if(json!=null)await Task.Run(()=>File.WriteAllText(file,json),token);return json;
        }
        public async Task<HazardCatalog> BuildAsync(DateTime today,Action<string> progress,CancellationToken token=default)
        {
            Directory.CreateDirectory(folder);var dates=new SortedSet<string>(StringComparer.Ordinal);int blocks=0;
            DateTime start=new DateTime(2012,8,7),last=today.Date.AddDays(-45);
            for(var day=start;day<=last;day=day.AddDays(120)) {
                token.ThrowIfCancellationRequested();string a=day.ToString("yyyy-MM-dd"),z=(day.AddDays(119)<last?day.AddDays(119):last).ToString("yyyy-MM-dd");
                var rows=await Task.WhenAll(Block("FLR",a,z,token),Block("CMEAnalysis",a,z,token),Block("WSAEnlilSimulations",a,z,token));blocks++;
                foreach(var row in ApiModels.Rows(rows[0])) {string date=(string)(row["peakTime"]??row["beginTime"]);if(date?.Length>=10)dates.Add(date.Substring(0,10));}
                foreach(var row in ApiModels.Rows(rows[2])) {
                    bool mars=(row["impactList"] as JArray)?.OfType<JObject>().Any(i=>string.Equals((string)i["location"],"Mars",StringComparison.OrdinalIgnoreCase))==true;
                    string date=(string)row["modelCompletionTime"];if(mars && date?.Length>=10)dates.Add(date.Substring(0,10));
                }
                // These are date candidates only, never event claims. They enter the catalog only after real requests.
                dates.Add(a);dates.Add(day.AddDays(Math.Min(60,(last-day).Days)).ToString("yyyy-MM-dd"));
                progress?.Invoke("Archive blocks: "+blocks+"; candidate dates: "+dates.Count);
            }
            var shortlist=dates.Where(d=>string.CompareOrdinal(d,"2012-08-07")>=0 && string.CompareOrdinal(d,last.ToString("yyyy-MM-dd"))<=0)
                .GroupBy(d=>d.Substring(0,4)).SelectMany(g=>g.OrderBy(d=>DeterministicRng.Value("catalog-v1",d)).Take(balance.catalogShortlistPerYear)).ToArray();
            var observations=new List<CatalogDay>();
            // Bounded batches; REMS is queried only for shortlisted candidates, never all archive Sols.
            for(int index=0;index<shortlist.Length;index+=balance.catalogRequestConcurrency) {
                var batch=shortlist.Skip(index).Take(balance.catalogRequestConcurrency).Select(async date=> {
                    string path=Path.Combine(folder,"sol-"+date+".json");SolResponse response;
                    if(File.Exists(path))response=NasaJson.Read<SolResponse>(await Task.Run(()=>File.ReadAllText(path),token));
                    else {response=await nasa.GetArchiveSolAsync("catalog-v1",1,date,token);if(response!=null)await Task.Run(()=>File.WriteAllText(path,NasaJson.Write(response)),token);}
                    if(response==null || response.is_fallback || response.facts?.weather==null && response.facts?.flare==null && response.facts?.cme==null && response.facts?.mars_impact==null)return null;
                    return new CatalogDay {date=date,curiositySol=response.data_dates.curiosity_sol,observation=response};
                });
                observations.AddRange((await Task.WhenAll(batch)).Where(o=>o!=null));progress?.Invoke("Verified dates: "+observations.Count+" / "+shortlist.Length);
            }
            return new HazardCatalog {generatedUtc=today.ToUniversalTime().ToString("O"),blocksScanned=blocks,weatherShortlisted=shortlist.Length,days=observations.OrderBy(d=>d.date,StringComparer.Ordinal).ToArray()};
        }
    }
}
