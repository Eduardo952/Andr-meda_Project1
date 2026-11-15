using System;

namespace Ares.Domain
{
    public enum OxygenMode { Economy, Balanced, Boost }

    [Serializable]
    public sealed class OxygenSettings
    {
        public int crewCount = 4;
        public float capacityKg = 8, initialWaterKg = 30;
        public float cycleSeconds = 30;
        // NASA HIDH reference loads per person / Earth day, including exercise.
        public float crewOxygenKgPerEarthDay = .82f, crewCo2KgPerEarthDay = 1.04f;
        // Fictional hardware and losses; see OXYGEN_SCIENCE.md.
        public float leakageKgPerSol = .03f, laboratoryKgPerSol = .15f;
        public float economyKgPerSol = 2.8f, balancedKgPerSol = 3.8f, boostKgPerSol = 5.2f;
        public float scrubberKgPerSol = 5;
        public float economyEnergy = 1.2f, balancedEnergy = 2.4f, boostEnergy = 4.8f, scrubberEnergy = .3f;
        public float criticalPercent = 25;
    }

    /// <summary>Mass accounting in kg: metabolic loads, water electrolysis and separate CO2 removal.
    /// Hardware capacity/power are game assumptions. One Sol takes 600 active real seconds.</summary>
    public sealed class OxygenPlant
    {
        public const float WaterPerOxygenKg = 1.125f; // 2 H2O -> 2 H2 + O2; rounded molar masses.
        readonly OxygenSettings settings;
        public OxygenMode Mode { get; private set; } = OxygenMode.Balanced;
        public bool LaboratoryEnabled { get; private set; } = true;
        public float ReserveKg { get; private set; }
        public float WaterKg { get; private set; }
        public float CarbonDioxideKg { get; private set; }
        public float TotalProduced { get; private set; }
        public float TotalConsumed { get; private set; }
        public float TotalVented { get; private set; }
        public float TotalUnserved { get; private set; }
        public float TotalWaterUsed { get; private set; }
        public float TotalHydrogenProduced => TotalWaterUsed - TotalProduced;
        public float TotalCo2Produced { get; private set; }
        public float TotalCo2Removed { get; private set; }
        public float TotalEnergyKwh { get; private set; }
        public float PowerFraction { get; private set; } = 1;
        public float Production { get; private set; }
        public int CrewCount => settings.crewCount;
        public float CapacityKg => settings.capacityKg;
        public float CycleSeconds => settings.cycleSeconds;
        public float Percent => ReserveKg / CapacityKg * 100;
        public bool Critical => Percent < settings.criticalPercent;
        float PerCycle(float kgPerSol) => kgPerSol * CycleSeconds / MissionTime.RealSecondsPerSol;
        public float CrewConsumption => PerCycle(settings.crewCount * settings.crewOxygenKgPerEarthDay * MissionTime.EarthDaysPerSol);
        public float HabitatConsumption => PerCycle(settings.leakageKgPerSol);
        public float LaboratoryConsumption => LaboratoryEnabled ? PerCycle(settings.laboratoryKgPerSol) : 0;
        public float Consumption => CrewConsumption + HabitatConsumption + LaboratoryConsumption;
        public float NominalProduction => PerCycle(Mode == OxygenMode.Economy ? settings.economyKgPerSol : Mode == OxygenMode.Boost ? settings.boostKgPerSol : settings.balancedKgPerSol);
        public float EnergyPerCycle => settings.scrubberEnergy + (WaterKg <= 0 ? 0 : Mode == OxygenMode.Economy ? settings.economyEnergy : Mode == OxygenMode.Boost ? settings.boostEnergy : settings.balancedEnergy);
        public float Balance => Production - Consumption;
        public float RemainingSeconds => Balance < -.000001f ? ReserveKg / -Balance * CycleSeconds : float.PositiveInfinity;
        public OxygenPlant(OxygenSettings settings, float initialPercent)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (settings.crewCount < 1 || !Finite(settings.criticalPercent) || settings.criticalPercent < 0 || settings.criticalPercent > 100)
                throw new ArgumentException("Invalid crew count or reserve threshold.");
            foreach (float value in new[] {settings.capacityKg, settings.cycleSeconds, settings.crewOxygenKgPerEarthDay,
                settings.crewCo2KgPerEarthDay, settings.economyKgPerSol, settings.balancedKgPerSol, settings.boostKgPerSol,
                settings.scrubberKgPerSol, settings.economyEnergy, settings.balancedEnergy, settings.boostEnergy, settings.scrubberEnergy})
                if (!Finite(value) || value <= 0) throw new ArgumentException("Oxygen capacity and rates must be finite and positive.");
            foreach (float value in new[] {settings.initialWaterKg, settings.leakageKgPerSol, settings.laboratoryKgPerSol})
                if (!Finite(value) || value < 0) throw new ArgumentException("Water and losses must be finite and non-negative.");
            WaterKg = settings.initialWaterKg; SetReservePercent(initialPercent); Production = WaterKg > 0 ? NominalProduction : 0;
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public bool SetMode(OxygenMode value)
        { if (!Enum.IsDefined(typeof(OxygenMode), value)) return false; Mode = value; return true; }
        public void SetLaboratory(bool enabled) => LaboratoryEnabled = enabled;
        internal void SetReservePercent(float value)
        { if (Finite(value)) ReserveKg = Math.Max(0, Math.Min(100, value)) * CapacityKg / 100; }
        internal void ApplyPercentDelta(float delta)
        { if (Finite(delta)) ReserveKg = Math.Max(0, Math.Min(CapacityKg, ReserveKg + delta * CapacityKg / 100)); }
        public void Tick(float seconds, float suppliedEnergyKwh)
        {
            if (!Finite(seconds) || seconds <= 0 || !Finite(suppliedEnergyKwh) || suppliedEnergyKwh < 0) return;
            float cycles = seconds / CycleSeconds;
            float requested = EnergyPerCycle * cycles;
            float usedEnergy = Math.Min(requested, suppliedEnergyKwh);
            PowerFraction = requested > 0 ? usedEnergy / requested : 0;
            float generated = Math.Min(NominalProduction * cycles * PowerFraction, WaterKg / WaterPerOxygenKg);
            float usedWater = generated * WaterPerOxygenKg;
            WaterKg = Math.Max(0, WaterKg - usedWater); TotalWaterUsed += usedWater;
            Production = generated / cycles;
            float demand = Consumption * cycles;
            float delivered = Math.Min(demand, ReserveKg + generated);
            float remaining = ReserveKg + generated - delivered;
            TotalProduced += generated; TotalConsumed += delivered; TotalUnserved += demand - delivered;
            TotalVented += Math.Max(0, remaining - CapacityKg); TotalEnergyKwh += usedEnergy;
            ReserveKg = Math.Min(CapacityKg, remaining);
            // Respiration first; leaks/laboratory do not produce metabolic CO2.
            float breathing = Math.Min(CrewConsumption * cycles, delivered);
            float co2 = breathing * settings.crewCo2KgPerEarthDay / settings.crewOxygenKgPerEarthDay;
            float removed = Math.Min(CarbonDioxideKg + co2, PerCycle(settings.scrubberKgPerSol) * cycles * PowerFraction);
            TotalCo2Produced += co2; TotalCo2Removed += removed; CarbonDioxideKg = Math.Max(0, CarbonDioxideKg + co2 - removed);
        }
    }
}
