using System;

namespace Ares.Domain
{
    public enum DemandEvent { None, DustStorm, SolarFlare }
    public enum DemandStage { Dormant, Warning, Impact, Recovery, Resolved }

    /// <summary>Seeded day schedule and event lifecycle. Resource effects belong to Campaign.</summary>
    public sealed class ConflictingDemands
    {
        public const string ShieldTaskId = "RegulateElectromagneticShield";
        public const float DustWarningSeconds = 20;
        public const float FlareWarningSeconds = 60;
        public const float ImpactSeconds = 30;
        readonly DemandEvent[] schedule;
        public int Seed { get; }
        public int Day { get; private set; }
        public DemandEvent Event { get; private set; }
        public DemandStage Stage { get; private set; }
        public float RemainingSeconds { get; private set; }
        public bool ShieldRegulated { get; private set; }
        public bool ImpactedToday { get; private set; }
        public bool ShieldTaskAvailable => Day == 2 || Day == 5;
        public bool Warning => Stage == DemandStage.Warning;
        public bool DustInAir => Event == DemandEvent.DustStorm && (Warning || Stage == DemandStage.Impact);
        public bool SolarActivity => Event == DemandEvent.SolarFlare && (Warning || Stage == DemandStage.Impact);

        public ConflictingDemands(int days, int? seed = null)
        {
            Seed = seed ?? Guid.NewGuid().GetHashCode();
            var random = new Random(Seed);
            schedule = new DemandEvent[days];
            // Pick at least one flare from the only days on which prevention is available.
            int flareDay = days >= 5 && random.Next(2) == 0 ? 5 : 2;
            if (flareDay <= days) schedule[flareDay - 1] = DemandEvent.SolarFlare;
            int other = flareDay == 2 ? 5 : 2;
            if (other <= days && random.NextDouble() < .35) schedule[other - 1] = DemandEvent.SolarFlare;
            // Two distinct dust days, chosen from all remaining days; no simultaneous new events.
            var candidates = new System.Collections.Generic.List<int>();
            for (int i = 0; i < days; i++) if (schedule[i] == DemandEvent.None) candidates.Add(i);
            for (int n = 0; n < 2 && candidates.Count > 0; n++)
            { int pick = random.Next(candidates.Count); schedule[candidates[pick]] = DemandEvent.DustStorm; candidates.RemoveAt(pick); }
            BeginDay(1);
        }
        internal void BeginDay(int day)
        {
            Day = day; Event = DemandEvent.None; Stage = DemandStage.Dormant;
            RemainingSeconds = 0; ShieldRegulated = false; ImpactedToday = false;
        }
        internal void Arm(int mandatoryCompleted)
        {
            if (mandatoryCompleted < 2 || Stage != DemandStage.Dormant || Day < 1 || Day > schedule.Length) return;
            Event = schedule[Day - 1];
            Stage = Event == DemandEvent.None ? DemandStage.Resolved : DemandStage.Warning;
            RemainingSeconds = Event == DemandEvent.SolarFlare ? FlareWarningSeconds : Event == DemandEvent.DustStorm ? DustWarningSeconds : 0;
        }
        internal DemandEvent Tick(float seconds, bool panelsRecovered, bool communicationsRepaired)
        {
            if (Stage == DemandStage.Warning)
            {
                RemainingSeconds = Math.Max(0, RemainingSeconds - seconds);
                if (RemainingSeconds <= 0) return Impact();
            }
            else if (Stage == DemandStage.Impact)
            {
                RemainingSeconds = Math.Max(0, RemainingSeconds - seconds);
                if (RemainingSeconds <= 0) Stage = DemandStage.Recovery;
            }
            if (Stage == DemandStage.Recovery && (Event == DemandEvent.DustStorm ? panelsRecovered : communicationsRepaired)) Stage = DemandStage.Resolved;
            return DemandEvent.None;
        }
        internal DemandEvent Impact()
        {
            if (!Warning) return DemandEvent.None;
            ImpactedToday = true; Stage = DemandStage.Impact; RemainingSeconds = ImpactSeconds; return Event;
        }
        internal DemandEvent ForceDustStorm()
        {
            Event = DemandEvent.DustStorm; Stage = DemandStage.Warning;
            return Impact();
        }
        internal bool RegulateShield(ShieldRegulation attempt)
        {
            if (!ShieldTaskAvailable || ShieldRegulated || attempt == null || !attempt.Aligned) return false;
            ShieldRegulated = true; return true;
        }
    }

    /// <summary>Optional daily calibration; independent of mandatory task results and rewards.</summary>
    public sealed class ShieldRegulation
    {
        static readonly int[] Targets = { 40, 70, 50 };
        readonly int[] values = { 20, 30, 80 };
        public int Value(int index) => values[index];
        public int Target(int index) => Targets[index];
        public bool Aligned => values[0] == Targets[0] && values[1] == Targets[1] && values[2] == Targets[2];
        public bool Adjust(int index, int value)
        {
            if (index < 0 || index >= values.Length || value < 0 || value > 100 || value % 10 != 0) return false;
            values[index] = value; return true;
        }
    }
}
