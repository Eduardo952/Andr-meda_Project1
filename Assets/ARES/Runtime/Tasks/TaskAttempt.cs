using System;
using System.Linq;
using Ares.Data;
using Ares.Domain;

namespace Ares.Tasks
{
    /// <summary>Pure attempt state. Only accepted results are forwarded to Campaign by the presenter.</summary>
    public sealed class TaskAttempt
    {
        public readonly string Id;
        public readonly TaskReadings Readings;
        public readonly int[] Values = new int[6];
        public readonly bool[] Done = new bool[4];
        public readonly float[] Dust;
        public readonly int[] Passes;
        public int Stage { get; private set; }
        public int Selected = -1;
        public int Mistakes { get; private set; }
        public bool Begun { get; private set; }
        public bool Solved { get; private set; }
        public bool Expired { get; private set; }
        public float Seconds { get; private set; }
        public string Priority { get; }
        public Allocation Demand { get; }
        public int IntegrityBefore { get; }
        public int OxygenBefore { get; }
        public bool Sample { get; }
        public readonly CampaignRules Rules;
        public TaskAttempt(string id, GameDefinition definition, Campaign campaign)
        {
            Id = id; Rules = definition.rules; Demand = campaign.Current.demand;
            IntegrityBefore = campaign.Integrity; OxygenBefore = campaign.Oxygen;
            Sample = campaign.Current.sample;
            Readings = new TaskReadings(definition, campaign.Current);
            Priority = campaign.Oxygen < Rules.initialOxygen ? "air" : Demand.life >= Demand.greenhouse ? "heat" : "water";
            var consequences = new Consequences(Rules); var facts = campaign.Current.facts;
            Dust = consequences.PanelDustAfterStorm(campaign.PanelDust, consequences.StormForSol(campaign.SolIndex, facts.hasWindSpeed ? (float?)facts.windSpeed : null));
            Passes = Dust.Select(consequences.CleaningPasses).ToArray();
            if (Id == "CmeCountdown") Values[0] = 400;
        }
        public bool Active => Begun && !Solved && !Expired;
        public void Begin() { if (!Begun) Begun = true; }
        public void Tick(float seconds)
        {
            if (!Active || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) return;
            Seconds = Math.Min(Rules.taskSeconds, Seconds + seconds);
            if (Seconds >= Rules.taskSeconds) Expired = true;
        }
        void Check(bool correct, bool final = true) { if (!Active) return; if (!correct) Mistakes++; else if (final) Solved = true; else Stage++; }
        public int ActiveArray => Enumerable.Range(0, 4).OrderByDescending(i => Dust[i]).Where(i => Values[i] < Passes[i]).DefaultIfEmpty(-1).First();
        public void Choose(string choice)
        {
            if (!Active) return;
            switch (Id)
            {
                case "SortFlares": Check(Stage < 5 && choice == "ABC MX".Replace(" ", "")[Stage].ToString(), false); if (Stage == 5) Solved = true; break;
                case "ResourcePriority": Check(choice == Priority); break;
                case "WindReading": Check(choice == Readings.WindDirection); break;
                case "PowerBalance": Check(choice == (Demand.Total <= Rules.energyPool ? "matches" : "shortfall"), false); break;
                case "PhotoDetective": Check(Stage == 0 ? choice == "1" : choice == "no", Stage != 0); break;
                case "CmeCountdown": Check(choice == TaskReadings.TravelHours(Readings.Speed).ToString()); break;
            }
        }
        public void Tap(int index)
        {
            if (!Active || index < 0 || index >= Values.Length) return;
            switch (Id)
            {
                case "PipeFlow": Values[index] = (Values[index] + Rules.miniGames.dialSteps / 4) % Rules.miniGames.dialSteps; break;
                case "WireShield":
                    if (index >= 4 || Selected < 0 || Done[index]) return;
                    if (Selected != index) Mistakes++; else { Done[index] = true; Selected = -1; Solved = Done.All(v => v); } break;
                case "CleanPanels": if (index == ActiveArray) { Values[index]++; if (ActiveArray == -1) Solved = true; } break;
                case "SmartFarmCare":
                    if (index >= Rules.miniGames.beds) return;
                    if (Stage == 0) { if (Selected != 0 || Done[index]) return; if (index == 1) { Mistakes++; return; } Done[index] = true; Selected = -1; if (Done[0] && Done[2]) { Stage = 1; Array.Clear(Done, 0, Done.Length); } }
                    else if (Stage == 2) { Done[index] = true; if (Done.Take(Rules.miniGames.apples).All(v => v)) Solved = true; } break;
            }
        }
        public void Set(int index, int value)
        {
            if (!Active || index < 0 || index >= Values.Length) return;
            if (Id == "IrrigationPlan" && (value < 0 || value + Values.Take(3).Sum() - Values[index] > 3)) return;
            Values[index] = value;
            if (Id == "SmartFarmCare" && Stage == 1 && value == Rules.miniGames.lightTarget) Stage = 2;
        }
        public void Confirm()
        {
            if (!Active) return;
            switch (Id)
            {
                case "AlignCoils": Check(Values.Take(3).SequenceEqual(Rules.miniGames.dialTargets)); break;
                case "PipeFlow": Check(Values.SequenceEqual(Rules.miniGames.dialTargets.Concat(Rules.miniGames.dialTargets))); break;
                case "TiltSun": Check(Values[0] == Readings.Tilt); break;
                case "ThermostatDial": Check(Values[0] == Readings.Heating); break;
                case "IrrigationPlan": Check(Values.Take(3).SequenceEqual(new[] { 2, 0, 1 })); break;
                case "PowerBalance": if (Stage == 1) Check(Values.Take(3).SequenceEqual(new[] { Demand.shield, Demand.life, Demand.greenhouse })); break;
                case "CmeCountdown": if (Stage == 0) Check(Math.Abs(Values[0] - Readings.Speed) <= Rules.miniGames.speedTolerance, false); break;
                case "CleanPanels": Check(ActiveArray == -1); break;
            }
        }
    }
}
