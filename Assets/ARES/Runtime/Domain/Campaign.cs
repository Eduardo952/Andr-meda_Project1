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
        readonly CampaignRules rules;
        readonly Consequences consequences;
        readonly Dictionary<string, int> results = new Dictionary<string, int>();
        readonly Dictionary<int, Dictionary<string, int>> history = new Dictionary<int, Dictionary<string, int>>();
        float[] panelDust = new float[4];
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
        public float SolElapsedSeconds { get; private set; }
        public float SolRemainingSeconds => Math.Max(0, MissionTime.RealSecondsPerSol - SolElapsedSeconds);
        public bool EndedByClock { get; private set; }
        public int Dose { get; private set; }
        public int BonusStars { get; private set; }
        public bool CleanedThisSol { get; private set; }
        public bool ShelterMissed { get; private set; }
        public string FailureCause { get; private set; }
        public IReadOnlyList<float> PanelDust => Array.AsReadOnly(panelDust);
        public IReadOnlyList<ConsequenceEvent> Events { get; private set; } = Array.Empty<ConsequenceEvent>();
        public SolOutcome Outcome { get; private set; }
        public int EnergyPool => consequences.EffectiveEnergyPool(panelDust);
        public SolarPlant Solar { get; }
        public OxygenPlant OxygenSystem { get; }
        public DiscoveryJournal Discoveries { get; } = new DiscoveryJournal();
        public GreenhouseSimulation Greenhouse { get; private set; } = new GreenhouseSimulation();
        public GreenhouseWatering Watering { get; private set; }
        public string SuccessTier => consequences.SuccessTier(Integrity, Oxygen, Dose);
        public int Stars(int solIndex, string id) => solIndex == SolIndex ? (results.TryGetValue(id, out int stars) ? stars : 0) : history.TryGetValue(solIndex, out var previous) && previous.TryGetValue(id, out int saved) ? saved : 0;
        public bool AllTasksDone { get { foreach (var task in Current.tasks) if (!results.ContainsKey(task.id)) return false; return true; } }
        public Campaign(CampaignData data, CampaignRules rules, SolarMaintenanceSettings solarSettings = null, OxygenSettings oxygenSettings = null)
        {
            if (data?.sols == null || data.sols.Length == 0) throw new ArgumentException("Campaign is empty");
            this.data = data; this.rules = rules ?? throw new ArgumentNullException(nameof(rules)); consequences = new Consequences(rules);
            Integrity = rules.initialIntegrity; Oxygen = rules.initialOxygen; SolFraction = rules.startSolFraction;
            Solar = new SolarPlant(solarSettings ?? new SolarMaintenanceSettings());
            OxygenSystem = new OxygenPlant(oxygenSettings ?? new OxygenSettings(), Oxygen);
            Watering = new GreenhouseWatering(Greenhouse);
        }
        public bool Completed(string id) => results.ContainsKey(id);
        public void CloseReport() { if (Phase != Phase.Report) return; Phase = Phase.Plan; Changed?.Invoke(); }
        public bool SetAllocation(Allocation value)
        {
            if (Phase != Phase.Plan && Phase != Phase.Tasks) return false;
            if (value.shield < 0 || value.life < 0 || value.greenhouse < 0 || value.Total > EnergyPool || value.shield % rules.sliderStep != 0 || value.life % rules.sliderStep != 0 || value.greenhouse % rules.sliderStep != 0) return false;
            Allocation = value; Changed?.Invoke(); return true;
        }
        public bool StartSol()
        {
            if (Phase != Phase.Plan || Allocation.Total != EnergyPool) return false;
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
            if (GreenhouseSimulation.IsTask(id)) return false;
            int stars = Score(seconds, mistakes);
            if (Phase != Phase.Tasks || OpenTask != id || !puzzleSolved || stars == 0 || results.ContainsKey(id)) return false;
            return RecordTask(id, stars);
        }
        public bool FinishGreenhouseTask()
        {
            if (Phase != Phase.Tasks || !GreenhouseSimulation.IsTask(OpenTask) || !Greenhouse.Finished || Watering.CompletedManualCycles != Greenhouse.CompletedCycles || results.ContainsKey(OpenTask)) return false;
            // This activity rewards completing the learning process, not speed or an answer revealed by the UI.
            return RecordTask(OpenTask, 3);
        }
        bool RecordTask(string id, int stars)
        {
            results.Add(id, stars); OpenTask = null; Integrity = Rules.ApplyDelta(Integrity, rules.taskReward.integrity);
            OxygenSystem.ApplyPercentDelta(rules.taskReward.oxygen); UpdateOxygenDisplay();
            if (id == "CleanPanels") CleanedThisSol = true;
            Changed?.Invoke(); return true;
        }
        public void Tick(float seconds, bool overlayOpen, bool paused = false)
        {
            if (Phase != Phase.Tasks || paused || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return;
            seconds = Math.Min(seconds, SolRemainingSeconds);
            // A task/computer is part of the day. Only explicit pause stops time and life support.
            float remaining = seconds;
            while (remaining > 0)
            { float step = Math.Min(remaining, 1); remaining -= step; Solar.Tick(step); TickOxygen(step); }
            SolElapsedSeconds = Math.Min(MissionTime.RealSecondsPerSol, SolElapsedSeconds + seconds);
            SolFraction = (rules.startSolFraction + SolElapsedSeconds / MissionTime.RealSecondsPerSol) % 1;
            if (!string.IsNullOrEmpty(Alarm)) { AlarmSeconds = Math.Min(rules.shelterSeconds, AlarmSeconds + seconds); if (AlarmSeconds >= rules.shelterSeconds) { Alarm = null; ShelterMissed = true; Changed?.Invoke(); } }
            if (SolRemainingSeconds <= 0) { EndedByClock = true; CompleteSol(); }
        }
        public void ReachShelter() { if (Phase == Phase.Tasks) { Alarm = null; Changed?.Invoke(); } }
        /// <summary>Called by the campaign clock, including time spent in tasks and at the terminal.</summary>
        public void TickOxygen(float seconds)
        {
            if (Phase != Phase.Tasks || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return;
            // Bound the step for exact battery exhaustion and stable resource accounting.
            while (seconds > 0)
            {
                float step = Math.Min(seconds, 1); seconds -= step;
                float energy = Solar.DrawEnergy(OxygenSystem.EnergyPerCycle * step / OxygenSystem.CycleSeconds);
                OxygenSystem.Tick(step, energy);
            }
            int previous = Oxygen;
            UpdateOxygenDisplay();
            if (previous != Oxygen) Changed?.Invoke();
        }
        void UpdateOxygenDisplay() => Oxygen = Math.Max(0, Math.Min(100, (int)Math.Floor(OxygenSystem.Percent + .5f)));
        public bool Seal(string nearbyRoom, Allocation confirmation)
        {
            if (Phase != Phase.Tasks || nearbyRoom != "control" || OpenTask != null || !AllTasksDone || Allocation.Total != EnergyPool || !confirmation.Equals(Allocation)) return false;
            CompleteSol(); return true;
        }
        void CompleteSol()
        {
            OpenTask = null;
            Outcome = consequences.EvaluateSol(Allocation, Current.demand);
            var facts = Current.facts;
            var storm = consequences.StormForSol(SolIndex, facts.hasWindSpeed ? (float?)facts.windSpeed : null);
            Events = Array.AsReadOnly(consequences.Evaluate(Outcome, facts, ShelterMissed, Dose, panelDust, storm, CleanedThisSol));
            var totals = Consequences.Totals(Events);
            Integrity = Rules.ApplyDelta(Integrity, Outcome.integrityDelta + totals.integrity);
            OxygenSystem.ApplyPercentDelta(Outcome.oxygenDelta + totals.oxygen); UpdateOxygenDisplay();
            Dose = Rules.ApplyDelta(Dose, totals.dose); BonusStars += totals.bonusStars;
            panelDust = consequences.PanelDustAfterCleaning(panelDust, storm, CleanedThisSol);
            Phase = Phase.Summary; Alarm = null; Changed?.Invoke();
        }
        public void NextSol()
        {
            if (Phase != Phase.Summary) return;
            SolElapsedSeconds = 0; EndedByClock = false;
            if (Dose >= 100 || Integrity <= 0 || Oxygen <= 0) { Phase = Phase.GameOver; FailureCause = Dose >= 100 ? "radiation" : Oxygen <= 0 ? "suffocation" : "powerCollapse"; }
            else if (SolIndex + 1 >= data.sols.Length) Phase = Phase.Victory;
            else { history[SolIndex] = new Dictionary<string, int>(results); SolIndex++; results.Clear(); Allocation = default; OpenTask = null; SolFraction = rules.startSolFraction; AlarmSeconds = 0; CleanedThisSol = false; ShelterMissed = false; Events = Array.Empty<ConsequenceEvent>(); Outcome = null; Greenhouse = new GreenhouseSimulation(); Watering = new GreenhouseWatering(Greenhouse); Phase = Phase.Report; }
            Changed?.Invoke();
        }
        int Score(float seconds, int mistakes) => float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0 || seconds > rules.taskSeconds || mistakes < 0 ? 0 : seconds <= rules.miniGames.threeStarSeconds && mistakes == 0 ? 3 : seconds <= rules.miniGames.twoStarSeconds && mistakes <= rules.miniGames.twoStarMistakes ? 2 : 1;
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
