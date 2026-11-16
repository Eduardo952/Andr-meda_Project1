using System;
using System.Collections.Generic;
using Ares.Data;
using Ares.Domain.Communications;
using Ares.Domain.Hazards;

namespace Ares.Domain
{
    public enum Phase { Report, Plan, Tasks, Summary, GameOver, Victory }
    [Serializable] public sealed class SuccessfulTaskResult { public string id; public float seconds; public int mistakes, stars; }
    /// <summary>Owns mission results. UI and presentation cannot award rewards or skip sealing.</summary>
    public sealed partial class Campaign
    {
        readonly CampaignData data;
        readonly CampaignRules rules;
        readonly Consequences consequences;
        readonly Dictionary<string, int> results = new Dictionary<string, int>();
        readonly List<SuccessfulTaskResult> measuredResults = new List<SuccessfulTaskResult>();
        public IReadOnlyList<SuccessfulTaskResult> SuccessfulTaskResults => measuredResults.AsReadOnly();
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
        public int EnergyPool => NativeHazards?EnergyGrid.Pool(NativePlan,Solar.AverageDust):consequences.EffectiveEnergyPool(panelDust);
        public SolarPlant Solar { get; }
        public OxygenPlant OxygenSystem { get; }
        public ConflictingDemands Demands { get; }
        public ShieldRegulation ShieldCalibration { get; private set; }
        public int MandatoryCompleted { get { int count = 0; foreach (var task in Current.tasks) if (results.ContainsKey(task.id)) count++; return count; } }
        public string HazardTarget => Phase != Phase.Tasks ? null
            : Demands.Event == DemandEvent.SolarFlare && Demands.Warning && !Demands.ShieldRegulated ? "shield"
            : Solar.BuriedPanels > 0 || Demands.DustInAir ? "solar"
            : !Communications.Connected ? "control" : null;
        public DiscoveryJournal Discoveries { get; } = new DiscoveryJournal();
        public CommunicationsSystem Communications { get; } = new CommunicationsSystem();
        public bool CanAdvanceDay => Phase == Phase.Summary && Communications.ReportForDay(SolIndex + 1)?.Sent == true;
        public bool SendDailyReport() => Phase == Phase.Summary && Communications.SendReport(SolIndex + 1);
        public bool ReplyToEarth(int messageId, ReplyKind kind)
        {
            if (Phase == Phase.GameOver || Phase == Phase.Victory || !Enum.IsDefined(typeof(ReplyKind), kind)) return false;
            float cost = CommunicationsSystem.ReplyCost(kind);
            if (Solar.ReserveKwh < cost || !Communications.QueueReply(messageId, kind)) return false;
            Solar.DrawEnergy(cost); return true;
        }
        public GreenhouseSimulation Greenhouse { get; private set; } = new GreenhouseSimulation();
        public GreenhouseWatering Watering { get; private set; }
        public string SuccessTier => consequences.SuccessTier(Integrity, Oxygen, Dose);
        public int Stars(int solIndex, string id) => solIndex == SolIndex ? (results.TryGetValue(id, out int stars) ? stars : 0) : history.TryGetValue(solIndex, out var previous) && previous.TryGetValue(id, out int saved) ? saved : 0;
        public bool AllTasksDone { get { foreach (var task in Current.tasks) if (!results.ContainsKey(task.id)) return false; return true; } }
        public Campaign(CampaignData data, CampaignRules rules, SolarMaintenanceSettings solarSettings = null, OxygenSettings oxygenSettings = null, int? eventSeed = null)
        {
            if (data?.sols == null || data.sols.Length == 0) throw new ArgumentException("Campaign is empty");
            this.data = CloneCampaign(data); this.rules = rules ?? throw new ArgumentNullException(nameof(rules)); consequences = new Consequences(rules);
            Integrity = rules.initialIntegrity; Oxygen = rules.initialOxygen; SolFraction = rules.startSolFraction;
            Solar = new SolarPlant(solarSettings ?? new SolarMaintenanceSettings());
            OxygenSystem = new OxygenPlant(oxygenSettings ?? new OxygenSettings(), Oxygen);
            Demands = new ConflictingDemands(data.sols.Length, eventSeed);
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
            if (Phase != Phase.Plan || Allocation.Total != EnergyPool || NativeHazards && !NativeDataReady) return false;
            Phase = Phase.Tasks; Alarm = null; AlarmSeconds = 0; Changed?.Invoke(); return true;
        }
        public bool BeginShieldRegulation(string nearbyRoom)
        {
            if (Phase != Phase.Tasks || nearbyRoom != "shield" || OpenTask != null || !Demands.ShieldTaskAvailable || Demands.ShieldRegulated) return false;
            ShieldCalibration = new ShieldRegulation(); OpenTask = ConflictingDemands.ShieldTaskId; Changed?.Invoke(); return true;
        }
        public bool FinishShieldRegulation()
        {
            if (Phase != Phase.Tasks || OpenTask != ConflictingDemands.ShieldTaskId || !Demands.RegulateShield(ShieldCalibration)) return false;
            OpenTask = null; ShieldCalibration = null; Changed?.Invoke(); return true;
        }
        public bool BeginTask(string id, string nearbyRoom)
        {
            if (Phase != Phase.Tasks || OpenTask != null || results.ContainsKey(id)) return false;
            foreach (var task in Current.tasks) if (task.id == id && task.room == nearbyRoom) { OpenTask = id; Changed?.Invoke(); return true; }
            return false;
        }
        public void CancelTask() { OpenTask = null; ShieldCalibration = null; Changed?.Invoke(); }
        public bool FinishTask(string id, bool puzzleSolved, float seconds, int mistakes)
        {
            if (GreenhouseSimulation.IsTask(id) || id == ConflictingDemands.ShieldTaskId) return false;
            int stars = Score(seconds, mistakes);
            if (Phase != Phase.Tasks || OpenTask != id || !puzzleSolved || stars == 0 || results.ContainsKey(id)) return false;
            measuredResults.Add(new SuccessfulTaskResult { id = id, seconds = seconds, mistakes = mistakes, stars = stars });
            return RecordTask(id, stars);
        }
        public bool FinishGreenhouseTask()
        {
            if (Phase != Phase.Tasks || !GreenhouseSimulation.IsTask(OpenTask) || !Greenhouse.CareReady || results.ContainsKey(OpenTask)) return false;
            // This activity rewards completing the learning process, not speed or an answer revealed by the UI.
            return RecordTask(OpenTask, 3);
        }
        bool RecordTask(string id, int stars)
        {
            results.Add(id, stars); OpenTask = null; Integrity = Rules.ApplyDelta(Integrity, rules.taskReward.integrity);
            OxygenSystem.ApplyPercentDelta(rules.taskReward.oxygen); UpdateOxygenDisplay();
            if (id == "CleanPanels" && Solar.DirtyPanels == 0) CleanedThisSol = true;
            if(NativeHazards && (id=="WireShield" || id=="PipeFlow" || id=="PowerBalance"))Grid.Repair();
            Changed?.Invoke(); return true;
        }
        public void Tick(float seconds, bool overlayOpen, bool paused = false)
        {
            if (Phase == Phase.GameOver || Phase == Phase.Victory) return;
            if (!paused) Communications.Tick(seconds);
            if (Phase != Phase.Tasks || paused || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return;
            if(NativeHazards){TickNative(seconds);return;}
            if(!NativeHazards) Demands.Arm(MandatoryCompleted);
            // A late completion still grants the entire warning, even at the end of the clock.
            seconds = Math.Min(seconds, Math.Max(SolRemainingSeconds, Demands.Warning ? Demands.RemainingSeconds : 0));
            // A task/computer is part of the day. Only explicit pause stops time and life support.
            float remaining = seconds;
            while (remaining > 0)
            {
                float step = Math.Min(remaining, 1); remaining -= step;
                SolElapsedSeconds = Math.Min(MissionTime.RealSecondsPerSol, SolElapsedSeconds + step);
                if(NativeHazards) Demands.SyncNative(NativePlan,SolElapsedSeconds); else ApplyDemand(Demands.Tick(step, Solar.BuriedPanels == 0, Communications.Connected));
                if (Phase == Phase.GameOver) return;
                Solar.Tick(step); TickOxygen(step);
                if (Phase == Phase.GameOver) return;
            }
            SolFraction = (rules.startSolFraction + SolElapsedSeconds / MissionTime.RealSecondsPerSol) % 1;
            Alarm = Demands.SolarActivity ? "solar-flare" : Demands.DustInAir ? "dust-storm" : null;
            AlarmSeconds = Demands.Warning ? (Demands.Event == DemandEvent.SolarFlare ? ConflictingDemands.FlareWarningSeconds : ConflictingDemands.DustWarningSeconds) - Demands.RemainingSeconds : 0;
            if (SolRemainingSeconds <= 0 && !Demands.Warning) { EndedByClock = true; CompleteSol(); }
        }
        // Entering the shield room is not calibration. Retained for callers of the former shelter alarm.
        public void ReachShelter() { if(NativeHazards)SetExposureContext(true,true,remedyVisible,resting); }
        /// <summary>Explicit console override; uses the real event effects without awarding/completing missions.</summary>
        public bool TryStartWindStorm()
        {
            if (NativeHazards || Phase != Phase.Tasks || Demands.DustInAir || Demands.SolarActivity) return false;
            ApplyDemand(Demands.ForceDustStorm()); Alarm = "dust-storm"; AlarmSeconds = 0; Changed?.Invoke(); return true;
        }
        void ApplyDemand(DemandEvent demand)
        {
            if (demand == DemandEvent.None) return;
            if (demand == DemandEvent.DustStorm) Solar.BuryPanels();
            else
            {
                Communications.DamageAntenna();
                Dose = Demands.ShieldRegulated ? Rules.ApplyDelta(Dose, 12) : 100;
                if (Dose >= 100) { Die("radiation"); return; }
            }
            Changed?.Invoke();
        }
        void Die(string cause)
        {
            if(NativeHazards){Condition.Tick(0,Dose,Oxygen,Integrity,ColdExposure,false);return;}
            if (Phase == Phase.GameOver) return;
            FailureCause = cause; Phase = Phase.GameOver; OpenTask = null; ShieldCalibration = null;
            Watering.Stop(); Alarm = null; Changed?.Invoke();
        }
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
                if (OxygenSystem.ReserveKg <= 0) { UpdateOxygenDisplay(); Die("suffocation"); return; }
            }
            int previous = Oxygen;
            UpdateOxygenDisplay();
            if (previous != Oxygen) Changed?.Invoke();
        }
        void UpdateOxygenDisplay() => Oxygen = Math.Max(0, Math.Min(100, (int)Math.Floor(OxygenSystem.Percent + .5f)));
        public bool Seal(string nearbyRoom, Allocation confirmation)
        {
            if(NativeHazards && SolRemainingSeconds>0)return false;
            if(NativeHazards && (Condition.Stage==CrewCondition.Incapacitated || Dose>=100 || Oxygen<=0 || Integrity<=0 || NativePlan.Has(HazardKind.Radiation) && !ShelterReached))return false;
            if (Phase != Phase.Tasks || nearbyRoom != "control" || OpenTask != null || !AllTasksDone || Allocation.Total != EnergyPool || !confirmation.Equals(Allocation)) return false;
            Demands.Arm(MandatoryCompleted);
            if (Demands.Warning) { Changed?.Invoke(); return false; }
            CompleteSol(); return Phase == Phase.Summary;
        }
        void CompleteSol()
        {
            Watering.Stop(); Greenhouse.EndDay();
            OpenTask = null;
            Outcome = NativeHazards?SettleNative():consequences.EvaluateSol(Allocation, Current.demand);
            var facts = Current.facts;
            // Weather now happens live. Do not generate a second scripted storm in the report.
            var storm = new DustStorm { active = false, source = "gameplay" };
            Events = Array.AsReadOnly(consequences.Evaluate(Outcome, facts, ShelterMissed, Dose, panelDust, storm, CleanedThisSol));
            var totals = Consequences.Totals(Events);
            Integrity = Rules.ApplyDelta(Integrity, (NativeHazards?0:Outcome.integrityDelta) + totals.integrity);
            OxygenSystem.ApplyPercentDelta((NativeHazards?0:Outcome.oxygenDelta) + totals.oxygen); UpdateOxygenDisplay();
            Dose = Rules.ApplyDelta(Dose, NativeHazards?0:totals.dose); BonusStars += totals.bonusStars;
            if(NativeHazards && totals.bonusStars>0)MedKits++;
            for (int bank = 0; bank < panelDust.Length; bank++)
            {
                float dust = 0; for (int i = bank * 8; i < (bank + 1) * 8; i++) dust += Solar.Dust(i) * 100;
                panelDust[bank] = dust / 8;
            }
            if (!NativeHazards && OxygenSystem.ReserveKg <= 0) { Die("suffocation"); return; }
            if (!NativeHazards && Dose >= 100) { Die("radiation"); return; }
            Communications.PrepareReport(new DailyBaseSnapshot(this));
            Phase = Phase.Summary; Alarm = null; Changed?.Invoke();
        }
        public void NextSol()
        {
            if (!CanAdvanceDay) return;
            measuredResults.Clear();
            SolElapsedSeconds = 0; EndedByClock = false;
            if (!NativeHazards && (Dose >= 100 || Integrity <= 0 || OxygenSystem.ReserveKg <= 0)) { Phase = Phase.GameOver; FailureCause = Dose >= 100 ? "radiation" : OxygenSystem.ReserveKg <= 0 ? "suffocation" : "powerCollapse"; }
            else if (SolIndex + 1 >= data.sols.Length) Phase = Phase.Victory;
            else { history[SolIndex] = new Dictionary<string, int>(results); SolIndex++; results.Clear(); Allocation = default; OpenTask = null; SolFraction = rules.startSolFraction; AlarmSeconds = 0; CleanedThisSol = false; ShelterMissed = false; Events = Array.Empty<ConsequenceEvent>(); Outcome = null; Greenhouse.BeginDay(SolIndex + 1); Phase = Phase.Report; }
            if (Phase == Phase.Report) { Communications.BeginDay(SolIndex + 1); Demands.BeginDay(SolIndex + 1); ShieldCalibration = null; }
            if(NativeHazards && Phase==Phase.Report)NextNativeSol();
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
