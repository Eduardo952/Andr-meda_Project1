using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ares.Infrastructure.Nasa
{
    public static class DeterministicRng
    {
        public static uint Xmur3(string value)
        {
            unchecked {
                uint h = 1779033703u ^ (uint)value.Length;
                foreach (char ch in value) { h = (h ^ ch) * 3432918353u; h = (h << 13) | (h >> 19); }
                h = (h ^ (h >> 16)) * 2246822507u; h = (h ^ (h >> 13)) * 3266489909u;
                return h ^ (h >> 16);
            }
        }
        public static Func<double> Mulberry32(uint seed)
        {
            return () => { unchecked {
                seed += 0x6D2B79F5u;
                uint t = (seed ^ (seed >> 15)) * (1u | seed);
                t = (t + (t ^ (t >> 7)) * (61u | t)) ^ t;
                return (t ^ (t >> 14)) / 4294967296.0;
            }};
        }
        public static double Value(string mission, string tag) => Mulberry32(Xmur3(mission + "|" + tag))();
    }
    public static class MarsCalendar
    {
        public const double Day = 86400000, SolMs = 88775244;
        public static readonly double Landing = new DateTimeOffset(2012, 8, 6, 5, 17, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        public static double Round(double x) => Math.Floor(x + .5);
        public static double DayMs(string date) => new DateTimeOffset(DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)).ToUnixTimeMilliseconds();
        public static string Iso(double ms) => DateTimeOffset.FromUnixTimeMilliseconds((long)ms).UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static string SolDate(int s0, int k) => Iso(Landing + (s0 + k) * SolMs + SolMs / 2);
        public static double LsOf(double ms)
        {
            double dT = ms / Day + 2440587.5 - 2451545.0, m = (19.3871 + .52402073 * dT) * Math.PI / 180;
            double a = 270.3863 + .5240384 * dT;
            double e = (10.691 + 3e-7 * dT) * Math.Sin(m) + .623 * Math.Sin(2*m) + .05 * Math.Sin(3*m) + .005 * Math.Sin(4*m) + .0005 * Math.Sin(5*m);
            return ((a + e) % 360 + 360) % 360;
        }
    }
    public static class MissionPlanner
    {
        public const int MissionSols = 5, BlockDays = 120;
        public static string MissionId(string value, DateTime today) => value != null && Regex.IsMatch(value, "\\A[A-Za-z0-9_-]{8,64}\\z") ? value : "anon-" + today.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        public static int Sol(string value)
        {
            var match = Regex.Match(value ?? "", @"^\s*([+-]?\d+)");
            if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) || n == 0) n = 1;
            return (int)Math.Min(MissionSols, Math.Max(1, n));
        }
        public static MissionPlan Plan(string mission, int sol, DateTime today)
        {
            mission = MissionId(mission, today); sol = Math.Min(5, Math.Max(1, sol));
            double midnight = MarsCalendar.DayMs(today.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            double bMin = MarsCalendar.DayMs("2012-08-07"), bMax = midnight - 45 * MarsCalendar.Day - (BlockDays - 1) * MarsCalendar.Day;
            double Pick(double min, double max, string tag) => min + Math.Floor(DeterministicRng.Value(mission, tag) * Math.Max(1, MarsCalendar.Round((max-min)/MarsCalendar.Day)+1)) * MarsCalendar.Day;
            double block = Pick(bMin, Math.Max(bMin,bMax), "block"), apod = Pick(MarsCalendar.DayMs("1995-06-16"), midnight - 12 * MarsCalendar.Day, "apod|"+sol);
            return new MissionPlan { mission=mission, sol=sol, block_start=MarsCalendar.Iso(block), block_end=MarsCalendar.Iso(block+119*MarsCalendar.Day),
                apod_start=MarsCalendar.Iso(apod), apod_end=MarsCalendar.Iso(apod+9*MarsCalendar.Day), photo_page=1+(int)Math.Floor(DeterministicRng.Value(mission,"photo|"+sol)*40) };
        }
        static string Date(DonkiFlare f) { var s=string.IsNullOrEmpty(f.peakTime)?f.beginTime:f.peakTime; return s==null?"":s.Substring(0,Math.Min(10,s.Length)); }
        sealed class Candidate { public int start, variety; public string[] dates; public bool ok; }
        public static SelectedWindow Select(MissionPlan plan, IEnumerable<DonkiFlare> source)
        {
            var flares=(source ?? Array.Empty<DonkiFlare>()).Where(f=>f!=null && Regex.IsMatch(f.classType ?? "", "^[ABCMX]")).ToArray();
            var byDay=new Dictionary<string,int>();
            foreach(var f in flares) { string date=Date(f); if(date.Length==0)continue; int level=f.classType[0]=='X'?3:f.classType[0]=='M'?2:1;
                byDay[date]=Math.Max(byDay.TryGetValue(date,out var previous)?previous:0,level); }
            var candidates=new List<Candidate>();double block=MarsCalendar.DayMs(plan.block_start);
            for(int off=0;off<=BlockDays-7;off++) {
                int start=(int)Math.Floor((block+off*MarsCalendar.Day-MarsCalendar.Landing)/MarsCalendar.SolMs);
                var dates=Enumerable.Range(0,5).Select(k=>MarsCalendar.SolDate(start,k)).ToArray();
                var levels=dates.Select(d=>byDay.TryGetValue(d,out var level)?level:0).ToArray();
                candidates.Add(new Candidate {start=start,dates=dates,ok=levels.Max()>=2 && levels.Min()<=1,variety=levels.Distinct().Count()});
            }
            string reason="eventful";var pool=candidates.Where(c=>c.ok).ToList();
            if(pool.Count>0) {int best=pool.Max(c=>c.variety);pool=pool.Where(c=>c.variety==best).ToList();}
            else {pool=candidates;reason=flares.Length>0?"calm_period":"no_flare_data";}
            var chosen=pool[(int)Math.Floor(DeterministicRng.Value(plan.mission,"window")*pool.Count)];
            string earth=chosen.dates[plan.sol-1];double day=MarsCalendar.DayMs(earth),ls=MarsCalendar.LsOf(day+MarsCalendar.Day/2);
            bool season=ls>=180 && ls<330;double roll=DeterministicRng.Value(plan.mission,"dust|"+plan.sol);
            return new SelectedWindow { sol=plan.sol,mission=plan.mission,earth_date=earth,d_prev=MarsCalendar.Iso(day-MarsCalendar.Day),d_next=MarsCalendar.Iso(day+MarsCalendar.Day),
                curiosity_sol=chosen.start+plan.sol-1,mission_window=new MissionWindow {earth_dates=chosen.dates,curiosity_sols=Enumerable.Range(chosen.start,5).ToArray(),reason=reason},
                ls=MarsCalendar.Round(ls*10)/10,dust_season=season,dust_storm_risk=season?(roll<.45?"high":"medium"):(roll<.08?"medium":"low"),flares_day=flares.Where(f=>Date(f)==earth).ToArray() };
        }
    }
}
