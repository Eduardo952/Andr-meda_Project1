using System;
using System.Linq;
using UnityEngine;

namespace Ares.Data
{
    [Serializable] public sealed class ScienceRecord
    {
        public string kind, id, date, label, url;
        public float value;
        public bool preliminary;
        public bool Valid => OfficialSources.IsOfficial(url) && !string.IsNullOrEmpty(id) && DateTime.TryParse(date, out _)
            && (kind == "flare" ? System.Text.RegularExpressions.Regex.IsMatch(label ?? "", @"^[ABCMX]\d+(\.\d+)?$")
                : kind == "cme" && value > 0 && value < 10000 && !float.IsNaN(value));
    }
    [Serializable] public sealed class ScienceStudy { public string id, title, url; }
    [Serializable] public sealed class ScienceArchive
    {
        public string provider, mode, retrievedUtc;
        public ScienceRecord[] records;
        public ScienceStudy[] studies;
        static ScienceArchive cached;
        public static ScienceArchive Load()
        {
            if(cached != null)return cached;
            var asset=Resources.Load<TextAsset>("ARES/Science/archive");
            if(asset==null)return null;
            try { cached=JsonUtility.FromJson<ScienceArchive>(asset.text); }
            catch(ArgumentException){return null;}
            return cached?.mode=="archive"?cached:null;
        }
        public ScienceRecord ForDay(string kind,int day)
        {
            var valid=(records??Array.Empty<ScienceRecord>()).Where(x=>x!=null&&x.kind==kind&&x.Valid).ToArray();
            return valid.Length==0?null:valid[Math.Max(0,day-1)%valid.Length];
        }
    }
    public static class OfficialSources
    {
        public static bool IsOfficial(string url)
        {
            if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme!="https")return false;
            return uri.Host=="nasa.gov"||uri.Host.EndsWith(".nasa.gov",StringComparison.OrdinalIgnoreCase)
                ||uri.Host=="asc-csa.gc.ca"||uri.Host.EndsWith(".asc-csa.gc.ca",StringComparison.OrdinalIgnoreCase);
        }
    }
}
