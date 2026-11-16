using System;

namespace Ares.Domain
{
    [Serializable]
    public sealed class SolarMaintenanceSettings
    {
        public float cleanPanelKwh = .4f;
        public float energyCycleSeconds = 30;
        public float consumptionKwhPerCycle = 9.6f;
        public float reserveCapacityKwh = 160;
        public float initialReserveKwh = 160;
        public float criticalReserveKwh = 100;
        public float dustDelaySeconds = 180;
        public float dustStaggerSeconds = 4;
        public float dustBuildSeconds = 120;
        public float cleaningSeconds = 4;
    }

    /// <summary>Independent panel wear and stored energy. Units are kWh per game cycle, not instantaneous power.</summary>
    public sealed class SolarPlant
    {
        public const int PanelCount = 32;
        readonly SolarMaintenanceSettings settings;
        readonly float[] ages = new float[PanelCount];
        readonly float[] dust = new float[PanelCount];
        readonly bool[] buried = new bool[PanelCount];
        public int BuriedPanels { get; private set; }
        public float ReserveKwh { get; private set; }
        bool gridMode;float gridCapacity;
        public float CapacityKwh => gridMode?gridCapacity:settings.reserveCapacityKwh;
        public void ConfigureGrid(float capacity,float start){gridMode=true;gridCapacity=capacity;ReserveKwh=Math.Max(0,Math.Min(capacity,start));}
        public void StoreEnergy(float amount){if(Nonnegative(amount))ReserveKwh=Math.Min(CapacityKwh,ReserveKwh+amount);}
        public float AverageDust {get {float total=0;for(int i=0;i<PanelCount;i++)total+=dust[i];return total/PanelCount*100;}}
        public void Deposit(float amount){if(!Nonnegative(amount))return;for(int i=0;i<PanelCount;i++){dust[i]=Math.Min(1,dust[i]+amount);ages[i]=settings.dustDelaySeconds+((i*13)%PanelCount)*settings.dustStaggerSeconds+dust[i]*settings.dustBuildSeconds;}}
        public float CycleSeconds => settings.energyCycleSeconds;
        public float CriticalReserveKwh => gridMode?gridCapacity*.2f:settings.criticalReserveKwh;
        public float CleaningSeconds => settings.cleaningSeconds;
        public bool IsCritical => ReserveKwh < CriticalReserveKwh;
        public int CleanedPanels { get; private set; }
        public float GeneratedKwh { get { float sum = 0; for (int i = 0; i < PanelCount; i++) sum += Generation(i); return sum; } }
        public int DirtyPanels { get { int count = 0; for (int i = 0; i < PanelCount; i++) if (NeedsCleaning(i)) count++; return count; } }

        public SolarPlant(SolarMaintenanceSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (!Positive(settings.energyCycleSeconds) || !Positive(settings.dustBuildSeconds) || !Positive(settings.cleaningSeconds)
                || !Positive(settings.reserveCapacityKwh) || !Positive(settings.cleanPanelKwh)
                || !Nonnegative(settings.consumptionKwhPerCycle) || !Nonnegative(settings.initialReserveKwh)
                || !Nonnegative(settings.criticalReserveKwh) || settings.criticalReserveKwh > settings.reserveCapacityKwh
                || !Nonnegative(settings.dustDelaySeconds) || !Nonnegative(settings.dustStaggerSeconds))
                throw new ArgumentException("Invalid solar maintenance settings");
            ReserveKwh = Math.Min(settings.initialReserveKwh, settings.reserveCapacityKwh);
        }
        static bool Nonnegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
        static bool Positive(float value) => Nonnegative(value) && value > 0;
        public float Dust(int panel) => dust[panel];
        public float Generation(int panel) => settings.cleanPanelKwh * (1 - dust[panel]);
        public bool NeedsCleaning(int panel) => panel >= 0 && panel < PanelCount && dust[panel] >= .05f;
        /// <summary>Burial persists through simulation and days until the real cleaning tool removes it.</summary>
        internal void BuryPanels()
        {
            BuriedPanels = PanelCount;
            for (int i = 0; i < PanelCount; i++)
            {
                buried[i] = true; dust[i] = 1;
                ages[i] = settings.dustDelaySeconds + ((i * 13) % PanelCount) * settings.dustStaggerSeconds + settings.dustBuildSeconds;
            }
        }
        /// <summary>Returns the energy actually supplied; consumers cannot drive the battery below zero.</summary>
        public float DrawEnergy(float requestedKwh)
        {
            if (!Nonnegative(requestedKwh)) return 0;
            float supplied = Math.Min(requestedKwh, ReserveKwh); ReserveKwh -= supplied; return supplied;
        }
        public void Tick(float seconds)
        {
            if (!Positive(seconds)) return;
            if(gridMode)return; // Native grid budgets the Sol; deposition occurs only from a verified dust event.
            // Small simulation steps also make accelerated checks match ordinary frame updates.
            while (seconds > 0)
            {
                float step = Math.Min(seconds, 1); seconds -= step;
                float before = GeneratedKwh;
                for (int i = 0; i < PanelCount; i++)
                {
                    ages[i] += step;
                    // A stable permutation spreads the first dusty panels throughout the park.
                    float delay = settings.dustDelaySeconds + ((i * 13) % PanelCount) * settings.dustStaggerSeconds;
                    dust[i] = Math.Max(0, Math.Min(1, (ages[i] - delay) / settings.dustBuildSeconds));
                }
                float generated = (before + GeneratedKwh) * .5f;
                ReserveKwh = Math.Max(0, Math.Min(CapacityKwh, ReserveKwh + (generated - settings.consumptionKwhPerCycle) * step / CycleSeconds));
            }
        }
        internal bool Clean(int panel)
        {
            if (!NeedsCleaning(panel)) return false;
            if (buried[panel]) { buried[panel] = false; BuriedPanels--; }
            ages[panel] = dust[panel] = 0; CleanedPanels++; return true;
        }
    }

    /// <summary>Tool ownership and uninterrupted hold interaction; contains no input or rendering dependencies.</summary>
    public sealed class SolarCleaningSession
    {
        readonly SolarPlant plant;
        float heldSeconds;
        public bool HasBroom { get; private set; }
        public int ActivePanel { get; private set; } = -1;
        public float Progress => heldSeconds / plant.CleaningSeconds;
        public SolarCleaningSession(SolarPlant plant) { this.plant = plant; }
        public void PickUpBroom() { HasBroom = true; }
        public void ReturnBroom() { HasBroom = false; Cancel(); }
        public void Cancel() { heldSeconds = 0; ActivePanel = -1; }
        public bool Tick(int panel, bool held, bool blocked, float seconds)
        {
            if (blocked || !HasBroom || !held || !plant.NeedsCleaning(panel)) { Cancel(); return false; }
            if (panel != ActivePanel) { Cancel(); ActivePanel = panel; }
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) return false;
            heldSeconds += seconds;
            if (heldSeconds < plant.CleaningSeconds) return false;
            bool cleaned = plant.Clean(panel); Cancel(); return cleaned;
        }
    }
}
