using System;
using System.Collections.Generic;

namespace Ares.Domain.Communications
{
    public sealed class ReportShelf
    {
        public string Crop { get; }
        public ShelfHealth Health { get; }
        public SoilMoisture Soil { get; }
        internal ReportShelf(GreenhouseShelf shelf) { Crop = shelf.Crop; Health = shelf.Health; Soil = shelf.Soil; }
    }
    /// <summary>End-of-day snapshot. Copies values so later gameplay cannot rewrite an earlier report.</summary>
    public sealed class DailyBaseSnapshot
    {
        public int Day { get; }
        public bool ScenarioUsesSampleData { get; }
        public IReadOnlyList<ReportShelf> Shelves { get; }
        public int ReservoirMl { get; }
        public int WateringCanMl { get; }
        public int WaterUsedMl { get; }
        public float AvailableEnergyKwh { get; }
        public int DirtyPanels { get; }
        public string ShieldStatus { get; }
        public int ShieldAllocated { get; }
        public int ShieldNeeded { get; }
        public int RadiationIndex { get; }
        public int Integrity { get; }
        public int OxygenPercent { get; }
        public bool RestEnvironmentStable { get; }
        public DemandEvent DemandEvent { get; }
        public bool ShieldRegulated { get; }
        public int BuriedPanels { get; }
        public bool AntennaConnected { get; }
        internal DailyBaseSnapshot(Campaign campaign)
        {
            Day = campaign.SolIndex + 1;
            ScenarioUsesSampleData = campaign.Current.sample;
            var crops = new List<ReportShelf>(); foreach (var shelf in campaign.Greenhouse.Shelves) crops.Add(new ReportShelf(shelf)); Shelves = crops.AsReadOnly();
            ReservoirMl = campaign.Greenhouse.ReservoirMl; WateringCanMl = campaign.Watering.CanMl; WaterUsedMl = campaign.Greenhouse.TodayDeliveredMl;
            AvailableEnergyKwh = campaign.Solar.ReserveKwh; DirtyPanels = campaign.Solar.DirtyPanels;
            ShieldStatus = campaign.Outcome.shield.status; ShieldAllocated = campaign.Outcome.shield.allocated; ShieldNeeded = campaign.Outcome.shield.demand;
            RadiationIndex = campaign.Dose; Integrity = campaign.Integrity; OxygenPercent = campaign.Oxygen;
            DemandEvent = campaign.Demands.ImpactedToday ? campaign.Demands.Event : DemandEvent.None;
            ShieldRegulated = campaign.Demands.ShieldRegulated; BuriedPanels = campaign.Solar.BuriedPanels; AntennaConnected = campaign.Communications.Connected;
            // A game assessment from existing telemetry, not a fabricated sleep or medical measurement.
            RestEnvironmentStable = OxygenPercent >= 20 && Integrity >= 50 && AvailableEnergyKwh >= 1;
        }
    }
}
