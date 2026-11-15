using System;

namespace Ares.Domain
{
    public enum OxygenMode { Economy, Balanced, Boost }

    [Serializable]
    public sealed class OxygenSettings
    {
        public float capacityLitres = 1000;
        public float cycleSeconds = 30;
        public float crewLitres = 4.8f;
        public float habitatLitres = 2.1f;
        public float laboratoryLitres = 1.1f;
        public float economyProduction = 6, balancedProduction = 9, boostProduction = 13;
        public float economyEnergy = 1.2f, balancedEnergy = 2.4f, boostEnergy = 4.8f;
        public float criticalPercent = 25;
    }

    /// <summary>Game-cycle oxygen accounting. Litres and kWh are explicit gameplay units, not a physiological model.</summary>
    public sealed class OxygenPlant
    {
        readonly OxygenSettings settings;
        public OxygenMode Mode { get; private set; } = OxygenMode.Balanced;
        public bool LaboratoryEnabled { get; private set; } = true;
        public float ReserveLitres { get; private set; }
        public float TotalProduced { get; private set; }
        public float TotalConsumed { get; private set; }
        public float TotalVented { get; private set; }
        public float TotalUnserved { get; private set; }
        public float TotalEnergyKwh { get; private set; }
        public float PowerFraction { get; private set; } = 1;
        public float CapacityLitres => settings.capacityLitres;
        public float CycleSeconds => settings.cycleSeconds;
        public float Percent => ReserveLitres / CapacityLitres * 100;
        public bool Critical => Percent < settings.criticalPercent;
        public float CrewConsumption => settings.crewLitres;
        public float HabitatConsumption => settings.habitatLitres;
        public float LaboratoryConsumption => LaboratoryEnabled ? settings.laboratoryLitres : 0;
        public float Consumption => CrewConsumption + HabitatConsumption + LaboratoryConsumption;
        public float NominalProduction => Mode == OxygenMode.Economy ? settings.economyProduction : Mode == OxygenMode.Boost ? settings.boostProduction : settings.balancedProduction;
        public float EnergyPerCycle => Mode == OxygenMode.Economy ? settings.economyEnergy : Mode == OxygenMode.Boost ? settings.boostEnergy : settings.balancedEnergy;
        public float Production => NominalProduction * PowerFraction;
        public float Balance => Production - Consumption;
        public float RemainingSeconds => Balance < -.001f ? ReserveLitres / -Balance * CycleSeconds : float.PositiveInfinity;
        public OxygenPlant(OxygenSettings settings, float initialPercent)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            foreach (float value in new[] {settings.capacityLitres, settings.cycleSeconds, settings.crewLitres, settings.habitatLitres,
                settings.laboratoryLitres, settings.economyProduction, settings.balancedProduction, settings.boostProduction,
                settings.economyEnergy, settings.balancedEnergy, settings.boostEnergy})
                if (!Finite(value) || value <= 0) throw new ArgumentException("Oxygen rates and capacity must be finite and positive.");
            SetReservePercent(initialPercent);
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public bool SetMode(OxygenMode value)
        { if (!Enum.IsDefined(typeof(OxygenMode), value)) return false; Mode = value; return true; }
        public void SetLaboratory(bool enabled) => LaboratoryEnabled = enabled;
        // Campaign alone synchronizes discrete mission rewards/penalties into the physical reserve.
        internal void SetReservePercent(float value)
        { if (Finite(value)) ReserveLitres = Math.Max(0, Math.Min(100, value)) * CapacityLitres / 100; }
        internal void ApplyPercentDelta(float delta)
        { if (Finite(delta)) ReserveLitres = Math.Max(0, Math.Min(CapacityLitres, ReserveLitres + delta * CapacityLitres / 100)); }
        public void Tick(float seconds, float suppliedEnergyKwh)
        {
            if (!Finite(seconds) || seconds <= 0 || !Finite(suppliedEnergyKwh) || suppliedEnergyKwh < 0) return;
            float cycles = seconds / CycleSeconds;
            float requested = EnergyPerCycle * cycles;
            float usedEnergy = Math.Min(requested, suppliedEnergyKwh);
            PowerFraction = requested > 0 ? usedEnergy / requested : 0;
            float generated = NominalProduction * cycles * PowerFraction;
            float demand = Consumption * cycles;
            float delivered = Math.Min(demand, ReserveLitres + generated);
            float remaining = ReserveLitres + generated - delivered;
            TotalProduced += generated; TotalConsumed += delivered; TotalUnserved += demand - delivered;
            TotalVented += Math.Max(0, remaining - CapacityLitres); TotalEnergyKwh += usedEnergy;
            ReserveLitres = Math.Min(CapacityLitres, remaining);
        }
    }
}
