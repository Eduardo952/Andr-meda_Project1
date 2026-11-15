using System;
using Ares.Data;
using Ares.Domain;
using UnityEditor;

namespace Ares.Editor
{
    /// <summary>Fast model checks scoped to day duration, oxygen science and randomized discoveries.</summary>
    public static class LifeSupportChecks
    {
        public static void VerifyModel()
        {
            var settings = new OxygenSettings();
            var plant = new OxygenPlant(settings, 50);
            float demand = plant.Consumption;
            plant.Tick(30, plant.EnergyPerCycle);
            Near(plant.TotalProduced, .19f, "Production per 30 seconds");
            Near(plant.TotalConsumed, demand, "Consumption");
            Near(plant.ReserveKg, 4 + .19f - demand, "Reserve balance");
            Near(plant.TotalWaterUsed, .19f * 1.125f, "Electrolysis water mass");
            Near(plant.TotalWaterUsed, plant.TotalProduced + plant.TotalHydrogenProduced, "Water split conservation");
            Near(plant.TotalCo2Produced, 4 * 1.04f * MissionTime.EarthDaysPerSol / 20, "NASA CO2 metabolic load");
            Near(plant.CarbonDioxideKg, 0, "Powered scrubber");
            plant.SetMode(OxygenMode.Economy); plant.Tick(30, plant.EnergyPerCycle);
            Require(plant.Balance < 0, "Economy uses reserve");
            float previousDemand = plant.Consumption; plant.SetLaboratory(false);
            Require(plant.Consumption < previousDemand && plant.CrewConsumption > 0, "Optional laboratory, essential crew");
            Require(!plant.SetMode((OxygenMode)99), "Invalid mode");
            var unpowered = new OxygenPlant(settings, 50); unpowered.Tick(30, 0);
            Near(unpowered.TotalProduced, 0, "No energy, no oxygen");
            Require(unpowered.CarbonDioxideKg > 0, "CO2 accumulates without powered removal");
            var dry = new OxygenPlant(new OxygenSettings { initialWaterKg = 0 }, 50); dry.Tick(30, dry.EnergyPerCycle);
            Near(dry.TotalProduced, 0, "No water, no oxygen"); Near(dry.CarbonDioxideKg, 0, "Scrubber independent of electrolysis water");
            var limited = new OxygenPlant(new OxygenSettings { initialWaterKg = .01f }, 50); limited.Tick(30, limited.EnergyPerCycle);
            Near(limited.TotalProduced, .01f / 1.125f, "Water-limited output"); Near(limited.WaterKg, 0, "No negative water");
            var half = new OxygenPlant(settings, 50); half.Tick(30, half.EnergyPerCycle / 2);
            Near(half.TotalProduced, .095f, "Partial power");
            var full = new OxygenPlant(settings, 100); full.Tick(30, full.EnergyPerCycle);
            Near(full.ReserveKg, 8, "Capacity limit"); Require(full.TotalVented > 0, "Excess accounted for");
            var empty = new OxygenPlant(settings, 0); empty.Tick(30, 0);
            Near(empty.ReserveKg, 0, "Underflow"); Near(empty.TotalUnserved, empty.Consumption, "Unserved oxygen tracked");
            float reserve = half.ReserveKg; half.Tick(float.NaN, 1); half.Tick(-1, 1); half.Tick(1, -1);
            Near(half.ReserveKg, reserve, "Invalid deltas ignored");
            foreach (int fps in new[] { 30, 120 })
            {
                var run = new OxygenPlant(settings, 50);
                for (int i = 0; i < fps * 30; i++) run.Tick(1f / fps, run.EnergyPerCycle / 30 / fps);
                Near(run.ReserveKg, 4 + .19f - demand, "Frame-rate reserve", .002f);
                Near(4 + run.TotalProduced - run.TotalConsumed - run.TotalVented, run.ReserveKg, "Oxygen conservation", .002f);
                Near(run.WaterKg + run.TotalWaterUsed, settings.initialWaterKg, "Water conservation", .003f);
                Near(run.TotalCo2Produced - run.TotalCo2Removed, run.CarbonDioxideKg, "CO2 conservation", .002f);
            }
            var definition = AssetDatabase.LoadAssetAtPath<GameDefinition>("Assets/ARES/Generated/GameDefinition.asset");
            var campaign = new Campaign(definition.campaign, definition.rules);
            campaign.CloseReport(); campaign.SetAllocation(new Allocation { shield = 30, life = 40, greenhouse = 30 });
            Require(campaign.StartSol(), "Start day");
            campaign.Tick(100, true, true); Near(campaign.SolElapsedSeconds, 0, "Pause freezes day");
            Near(campaign.OxygenSystem.TotalConsumed, 0, "Pause freezes life support");
            campaign.Tick(300, true); Near(campaign.SolElapsedSeconds, 300, "Computer time counts");
            var task = campaign.Current.tasks[0]; Require(campaign.BeginTask(task.id, task.room), "Task fixture");
            campaign.Tick(299, true); Require(campaign.Phase == Phase.Tasks, "Day must not finish early");
            campaign.Tick(1, true); Require(campaign.Phase == Phase.Summary && campaign.EndedByClock && campaign.OpenTask == null, "Exactly 600 seconds ends unfinished day safely");
            Near(campaign.SolRemainingSeconds, 0, "Countdown complete");
            Require(!campaign.Completed(task.id), "Clock never awards unfinished task");
            CommunicationCheckFixture.SendRequiredReport(campaign); campaign.NextSol(); Require(campaign.SolIndex == 1 && campaign.Phase == Phase.Report, "Next day report");
            Near(campaign.SolRemainingSeconds, 600, "New day duration");
            var first = DiscoveryDeck.Shuffle(5, 10); var repeat = DiscoveryDeck.Shuffle(5, 10); var different = DiscoveryDeck.Shuffle(5, 20);
            var used = new bool[5]; bool changed = false;
            for (int i = 0; i < 5; i++) { Require(!used[first[i]], "No repeated photo"); used[first[i]] = true; Require(first[i] == repeat[i], "Seed reproducibility"); changed |= first[i] != different[i]; }
            Require(changed, "Independent draws vary");
        }
        static void Near(float a, float b, string message, float tolerance = .0002f) => Require(Math.Abs(a - b) < tolerance, message + ": " + a + " != " + b);
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
