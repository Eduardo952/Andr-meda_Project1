using System;
using System.Collections.Generic;

namespace Ares.Domain
{
    public enum SoilMoisture { Dry, Moderate, Wet }
    public enum GrowLight { Low, Moderate, Excessive }
    public enum IrrigationLevel { Low, Medium, High }
    public enum GreenhouseAdjustment { None, MoreWater, LessWater, MoreLight, LessLight }
    public enum ShelfHealth { Healthy, Problem, AtRisk }
    public enum GreenhouseWeather { Stable, DustStorm, StrongSun, ClearSky }
    public enum GreenhouseDecisionError { None, Finished, InvalidChoice, InvalidAdjustment, InsufficientWater }

    public sealed class GreenhouseShelf
    {
        public string Crop { get; }
        public ShelfHealth Health { get; internal set; }
        public int ProblemCycles { get; internal set; }
        internal GreenhouseShelf(string crop) { Crop = crop; }
    }

    public sealed class GreenhouseResult
    {
        public int Cycle { get; internal set; }
        public int BeforeMl { get; internal set; }
        public int UsedMl { get; internal set; }
        public int RemainingMl { get; internal set; }
        public SoilMoisture Soil { get; internal set; }
        public GrowLight Light { get; internal set; }
        public bool Waterlogged { get; internal set; }
        public ShelfHealth Health { get; internal set; }
        public IrrigationLevel FinalLevel { get; internal set; }
    }

    /// <summary>Child-paced teaching simulation. Integer millilitres avoid rounding or negative balances.</summary>
    public sealed class GreenhouseSimulation
    {
        public const int InitialWaterMl = 11000;
        static readonly GreenhouseWeather[] WeatherSequence = {
            GreenhouseWeather.Stable, GreenhouseWeather.DustStorm, GreenhouseWeather.StrongSun,
            GreenhouseWeather.ClearSky, GreenhouseWeather.Stable, GreenhouseWeather.DustStorm,
            GreenhouseWeather.ClearSky, GreenhouseWeather.StrongSun
        };
        readonly GreenhouseShelf[] shelves = { new GreenhouseShelf("Fruta"), new GreenhouseShelf("Legume"), new GreenhouseShelf("Verdura"), new GreenhouseShelf("Verdura") };
        public IReadOnlyList<GreenhouseShelf> Shelves { get; }
        public int Cycle { get; private set; } = 1;
        public int WaterMl { get; private set; } = InitialWaterMl;
        public SoilMoisture Soil { get; private set; } = SoilMoisture.Moderate;
        public GrowLight Light { get; private set; } = GrowLight.Moderate;
        public GreenhouseWeather Weather { get; private set; } = GreenhouseWeather.Stable;
        public bool Waterlogged { get; private set; }
        public bool Finished => WaterMl < ConsumptionMl(IrrigationLevel.Low);
        public int CompletedCycles { get; private set; }
        public int HealthyCycles { get; private set; }
        public GreenhouseResult LastResult { get; private set; }
        public string Reflection { get; private set; } = "";
        public GreenhouseSimulation() { Shelves = Array.AsReadOnly(shelves); }
        public static bool IsTask(string id) => id == "SmartFarmCare" || id == "IrrigationPlan";
        public static int ConsumptionMl(IrrigationLevel level)
        {
            switch(level) { case IrrigationLevel.Low: return 1360; case IrrigationLevel.Medium: return 2040; case IrrigationLevel.High: return 2320; default: throw new ArgumentOutOfRangeException(nameof(level)); }
        }
        public bool CanAdjust(IrrigationLevel level, GreenhouseAdjustment adjustment)
        {
            switch(adjustment)
            {
                case GreenhouseAdjustment.None: return true;
                case GreenhouseAdjustment.MoreWater: return Soil == SoilMoisture.Dry && level < IrrigationLevel.High;
                case GreenhouseAdjustment.LessWater: return Soil == SoilMoisture.Wet && level > IrrigationLevel.Low;
                case GreenhouseAdjustment.MoreLight: return Light == GrowLight.Low;
                case GreenhouseAdjustment.LessLight: return Light == GrowLight.Excessive;
                default: return false;
            }
        }
        public static IrrigationLevel FinalLevel(IrrigationLevel chosen, GreenhouseAdjustment adjustment)
            => (IrrigationLevel)((int)chosen + (adjustment == GreenhouseAdjustment.MoreWater ? 1 : adjustment == GreenhouseAdjustment.LessWater ? -1 : 0));
        public GreenhouseDecisionError Decide(IrrigationLevel chosen, GreenhouseAdjustment adjustment)
        {
            if (Finished) return GreenhouseDecisionError.Finished;
            if (!Enum.IsDefined(typeof(IrrigationLevel), chosen)) return GreenhouseDecisionError.InvalidChoice;
            if (!CanAdjust(chosen, adjustment)) return GreenhouseDecisionError.InvalidAdjustment;
            var final = FinalLevel(chosen, adjustment); int used = ConsumptionMl(final);
            if (WaterMl < used) return GreenhouseDecisionError.InsufficientWater;
            int before = WaterMl;
            // Rules are explicit: high irrigation on dry soil resolves to Moderate (the allowed deterministic option).
            Waterlogged = Soil == SoilMoisture.Wet && final == IrrigationLevel.High;
            Soil = SoilAfter(Soil, final);
            if (adjustment == GreenhouseAdjustment.MoreLight || adjustment == GreenhouseAdjustment.LessLight) Light = GrowLight.Moderate;
            WaterMl -= used; CompletedCycles++;
            if (Soil == SoilMoisture.Moderate && Light == GrowLight.Moderate) HealthyCycles++;
            UpdateShelves(true);
            LastResult = new GreenhouseResult { Cycle = Cycle, BeforeMl = before, UsedMl = used, RemainingMl = WaterMl,
                Soil = Soil, Light = Light, Waterlogged = Waterlogged, Health = shelves[0].Health, FinalLevel = final };
            if (!Finished) { Cycle++; ApplyWeather(WeatherSequence[(Cycle - 1) % WeatherSequence.Length]); UpdateShelves(false); }
            return GreenhouseDecisionError.None;
        }
        public static SoilMoisture SoilAfter(SoilMoisture soil, IrrigationLevel level)
        {
            switch(soil)
            {
                case SoilMoisture.Dry: return level == IrrigationLevel.Low ? SoilMoisture.Dry : SoilMoisture.Moderate;
                case SoilMoisture.Moderate: return level == IrrigationLevel.Low ? SoilMoisture.Dry : level == IrrigationLevel.Medium ? SoilMoisture.Moderate : SoilMoisture.Wet;
                case SoilMoisture.Wet: return level == IrrigationLevel.Low ? SoilMoisture.Moderate : SoilMoisture.Wet;
                default: throw new ArgumentOutOfRangeException(nameof(soil));
            }
        }
        void ApplyWeather(GreenhouseWeather weather)
        {
            Weather = weather;
            if (weather == GreenhouseWeather.DustStorm) Light = GrowLight.Low;
            else if (weather == GreenhouseWeather.StrongSun)
            { Light = GrowLight.Excessive; Soil = Soil == SoilMoisture.Wet ? SoilMoisture.Moderate : SoilMoisture.Dry; Waterlogged = false; }
            else if (weather == GreenhouseWeather.ClearSky) Light = GrowLight.Moderate;
        }
        void UpdateShelves(bool countCycle)
        {
            int factors = (Soil == SoilMoisture.Moderate ? 0 : 1) + (Light == GrowLight.Moderate ? 0 : 1);
            foreach(var shelf in shelves)
            {
                if (countCycle) shelf.ProblemCycles = factors > 0 ? shelf.ProblemCycles + 1 : 0;
                shelf.Health = factors == 0 ? ShelfHealth.Healthy : factors == 2 || shelf.ProblemCycles >= 2 ? ShelfHealth.AtRisk : ShelfHealth.Problem;
            }
        }
        public void SaveReflection(string value)
        { if (Finished) { value = (value ?? "").Trim(); Reflection = value.Substring(0, Math.Min(value.Length, 240)); } }
    }
}
