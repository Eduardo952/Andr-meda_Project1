using System;
using System.Collections.Generic;
using Ares.Data;

namespace Ares.Domain
{
    public enum Phase { Report, Plan, Tasks, Summary, GameOver, Victory }
    /// <summary>Owns mission results. UI and presentation cannot award rewards or skip sealing.</summary>
    public sealed class Campaign
    {
        readonly CampaignData data;
        readonly Dictionary<string, int> results = new Dictionary<string, int>();
        public event Action Changed;
        public Phase Phase { get; private set; } = Phase.Report;
        public int SolIndex { get; private set; }
        public int Integrity { get; private set; } = 100;
        public int Oxygen { get; private set; } = 100;
        public Allocation Allocation { get; private set; }
        public SolData Current => data.sols[SolIndex];
        public string OpenTask { get; private set; }
        public string Alarm { get; private set; }
        public float AlarmSeconds { get; private set; }
        public float SolFraction { get; private set; } = .3f;
        public bool AllTasksDone { get { foreach (var task in Current.tasks) if (!results.ContainsKey(task.id)) return false; return true; } }
        public Campaign(CampaignData data) { if (data?.sols == null || data.sols.Length == 0) throw new ArgumentException("Campaign is empty"); this.data = data; }
        public bool Completed(string id) => results.ContainsKey(id);
        public void CloseReport() { if (Phase != Phase.Report) return; Phase = Phase.Plan; Changed?.Invoke(); }
        public bool SetAllocation(Allocation value)
        {
            if (Phase != Phase.Plan && Phase != Phase.Tasks) return false;
            if (value.shield < 0 || value.life < 0 || value.greenhouse < 0 || value.Total > 100 || value.shield % 5 != 0 || value.life % 5 != 0 || value.greenhouse % 5 != 0) return false;
            Allocation = value; Changed?.Invoke(); return true;
        }
        public bool StartSol()
        {
            if (Phase != Phase.Plan || Allocation.Total != 100) return false;
            Phase = Phase.Tasks; Alarm = Current.alarm; AlarmSeconds = 0; Changed?.Invoke(); return true;
        }
        public bool BeginTask(string id, string nearbyRoom)
        {
            if (Phase != Phase.Tasks || OpenTask != null || results.ContainsKey(id)) return false;
            foreach (var task in Current.tasks) if (task.id == id && task.room == nearbyRoom) { OpenTask = id; Changed?.Invoke(); return true; }
            return false;
        }
        public void CancelTask() { OpenTask = null; Changed?.Invoke(); }
        public bool FinishTask(string id, bool puzzleSolved, float seconds, int mistakes)
        {
            int stars = Rules.Score(seconds, mistakes);
            if (Phase != Phase.Tasks || OpenTask != id || !puzzleSolved || stars == 0 || results.ContainsKey(id)) return false;
            results.Add(id, stars); OpenTask = null; Integrity = Math.Min(100, Integrity + 2); Oxygen = Math.Min(100, Oxygen + 2); Changed?.Invoke(); return true;
        }
        public void Tick(float seconds, bool overlayOpen)
        {
            if (Phase != Phase.Tasks || overlayOpen || OpenTask != null || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) return;
            SolFraction = Math.Min(.999f, SolFraction + seconds / 480f);
            if (!string.IsNullOrEmpty(Alarm)) { AlarmSeconds = Math.Min(60, AlarmSeconds + seconds); if (AlarmSeconds >= 60) Alarm = null; }
        }
        public void ReachShelter() { if (Phase == Phase.Tasks) { Alarm = null; Changed?.Invoke(); } }
        public bool Seal(string nearbyRoom, Allocation confirmation)
        {
            if (Phase != Phase.Tasks || nearbyRoom != "control" || OpenTask != null || !AllTasksDone || Allocation.Total != 100 || !confirmation.Equals(Allocation)) return false;
            var demand = Current.demand;
            Integrity = Rules.ApplyDelta(Integrity, -Rules.Missing(Allocation.shield, demand.shield));
            Oxygen = Rules.ApplyDelta(Oxygen, -Rules.Missing(Allocation.life, demand.life) - Rules.Missing(Allocation.greenhouse, demand.greenhouse) * .5f);
            Phase = Phase.Summary; Alarm = null; Changed?.Invoke(); return true;
        }
        public void NextSol()
        {
            if (Phase != Phase.Summary) return;
            if (Integrity <= 0 || Oxygen <= 0) Phase = Phase.GameOver;
            else if (SolIndex + 1 >= data.sols.Length) Phase = Phase.Victory;
            else { SolIndex++; results.Clear(); Allocation = default; OpenTask = null; SolFraction = .3f; Phase = Phase.Report; }
            Changed?.Invoke();
        }
    }
    public static class Rules
    {
        public static int RoundTo5(double value) => (int)Math.Floor(value / 5 + .5) * 5;
        public static int Shield(char? flare, double magnitude, double? speed)
        {
            double basis = flare == 'X' ? 40 : flare == 'M' ? 22 : flare == 'C' ? 10 : 5;
            double value = flare.HasValue ? basis + 2.5 * magnitude : 0;
            if (speed > 0) value += Math.Pow(speed.Value / 400, 2) * 4;
            return RoundTo5(Math.Max(10, Math.Min(75, value)));
        }
        public static int? Thermal(double? temperature) => !temperature.HasValue || double.IsNaN(temperature.Value) || double.IsInfinity(temperature.Value) ? (int?)null : RoundTo5(10 + .15 * Math.Max(0, 20 - temperature.Value));
        public static int Missing(int allocated, int demand) => demand - allocated > 5 ? demand - allocated : 0;
        public static int ApplyDelta(int value, float delta) => Math.Max(0, Math.Min(100, (int)Math.Floor(value + delta + .5)));
        public static int Score(float seconds, int mistakes) => float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0 || seconds > 60 || mistakes < 0 ? 0 : seconds <= 30 && mistakes == 0 ? 3 : seconds <= 45 && mistakes <= 2 ? 2 : 1;
    }
}
