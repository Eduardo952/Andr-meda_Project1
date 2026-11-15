using System;
using System.Collections.Generic;

namespace Ares.Domain
{
    public enum SoilMoisture { Dry, Moderate, Wet }
    public enum GrowLight { Low, Moderate, Excessive }
    public enum ShelfHealth { Healthy, Problem, AtRisk }

    public sealed class GreenhouseShelf
    {
        public string Crop { get; }
        public int Containers => GreenhouseRules.ContainersPerShelf;
        public SoilMoisture Soil { get; internal set; }
        public int RequiredDoses { get; internal set; }
        public int DeliveredDoses { get; internal set; }
        public bool Inspected { get; internal set; }
        public bool CaredFor => Inspected && DeliveredDoses >= RequiredDoses;
        public ShelfHealth Health { get; internal set; }
        public int ProblemDays { get; internal set; }
        internal GreenhouseShelf(string crop) { Crop = crop; }
    }

    public sealed class GreenhouseResult
    {
        public int Day { get; internal set; }
        public int UsedMl { get; internal set; }
        public int HealthyShelves { get; internal set; }
    }

    /// <summary>Five-day crop state and reservoir ledger. No rendering, input, clock or NASA API dependency.</summary>
    public sealed class GreenhouseSimulation
    {
        public const int InitialWaterMl = GreenhouseRules.InitialWaterMl;
        readonly GreenhouseShelf[] shelves = { new GreenhouseShelf("Frutas"), new GreenhouseShelf("Legumes"), new GreenhouseShelf("Verduras"), new GreenhouseShelf("Verduras") };
        public IReadOnlyList<GreenhouseShelf> Shelves { get; }
        public int Day { get; private set; } = 1;
        public int ReservoirMl { get; private set; } = InitialWaterMl;
        public int TotalDeliveredMl { get; private set; }
        public int TodayDeliveredMl { get; private set; }
        public GrowLight Light { get; private set; } = GrowLight.Moderate;
        public string Environment { get; private set; }
        public bool DayEnded { get; private set; }
        public bool Finished => DayEnded && Day == GreenhouseRules.MissionDays;
        public bool CanCare => !DayEnded;
        public bool CareReady { get { foreach (var shelf in shelves) if (!shelf.CaredFor) return false; return Light == GrowLight.Moderate; } }
        public int HealthyDays { get; private set; }
        public GreenhouseResult LastResult { get; private set; }
        public GreenhouseSimulation(GreenhouseDayConditions initial = null)
        { Shelves = Array.AsReadOnly(shelves); ApplyConditions(initial ?? GreenhouseDayConditions.ForDay(1)); }
        public static bool IsTask(string id) => id == "SmartFarmCare" || id == "IrrigationPlan";
        public void BeginDay(int day)
        {
            if (!DayEnded || day != Day + 1 || day > GreenhouseRules.MissionDays) return;
            Day = day; DayEnded = false; TodayDeliveredMl = 0;
            ApplyConditions(GreenhouseDayConditions.ForDay(day));
        }
        void ApplyConditions(GreenhouseDayConditions conditions)
        {
            Environment = conditions.Description; Light = conditions.Light;
            for (int i = 0; i < shelves.Length; i++)
            {
                var shelf = shelves[i]; shelf.Soil = conditions.Soil(i);
                shelf.RequiredDoses = GreenhouseRules.Doses(shelf.Soil); shelf.DeliveredDoses = 0; shelf.Inspected = false;
                UpdateHealth(shelf);
            }
        }
        public bool Inspect(int index)
        {
            if (!CanCare || index < 0 || index >= shelves.Length) return false;
            shelves[index].Inspected = true; return true;
        }
        public bool AdjustLight(int direction)
        {
            if (!CanCare || (direction != -1 && direction != 1)) return false;
            int value = Math.Max(0, Math.Min(2, (int)Light + direction));
            if (value == (int)Light) return false;
            Light = (GrowLight)value; foreach (var shelf in shelves) UpdateHealth(shelf); return true;
        }
        internal int Withdraw(int requestedMl)
        {
            if (!CanCare || requestedMl <= 0) return 0;
            int amount = Math.Min(requestedMl, ReservoirMl); ReservoirMl -= amount; return amount;
        }
        internal bool DeliverDose(int index)
        {
            if (!CanCare || index < 0 || index >= shelves.Length) return false;
            var shelf = shelves[index];
            if (!shelf.Inspected || shelf.DeliveredDoses >= shelf.RequiredDoses) return false;
            shelf.DeliveredDoses++; TodayDeliveredMl += GreenhouseRules.DoseMl; TotalDeliveredMl += GreenhouseRules.DoseMl;
            if (shelf.DeliveredDoses >= shelf.RequiredDoses) shelf.Soil = SoilMoisture.Moderate;
            UpdateHealth(shelf); return true;
        }
        void UpdateHealth(GreenhouseShelf shelf)
        {
            int problems = (shelf.Soil == SoilMoisture.Moderate ? 0 : 1) + (Light == GrowLight.Moderate ? 0 : 1);
            shelf.Health = problems == 0 ? ShelfHealth.Healthy : problems > 1 || shelf.ProblemDays >= 2 ? ShelfHealth.AtRisk : ShelfHealth.Problem;
        }
        public void EndDay()
        {
            if (DayEnded) return;
            int healthy = 0;
            foreach (var shelf in shelves)
            {
                if (shelf.DeliveredDoses < shelf.RequiredDoses) shelf.Soil = SoilMoisture.Dry;
                // Leaving wet soil alone lets it drain during the day; extra watering is blocked.
                else if (shelf.RequiredDoses == 0) shelf.Soil = SoilMoisture.Moderate;
                bool good = shelf.Soil == SoilMoisture.Moderate && Light == GrowLight.Moderate;
                shelf.ProblemDays = good ? 0 : shelf.ProblemDays + 1;
                UpdateHealth(shelf); if (good) healthy++;
            }
            if (healthy == shelves.Length) HealthyDays++;
            LastResult = new GreenhouseResult { Day = Day, UsedMl = TodayDeliveredMl, HealthyShelves = healthy };
            DayEnded = true;
        }
    }
}
