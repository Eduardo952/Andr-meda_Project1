using System;
using System.IO;
using System.Linq;
using Ares.Data;
using Ares.Domain;
using Ares.Tasks;
using UnityEditor;

namespace Ares.Editor
{
    public static class TaskInteractionChecks
    {
        static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        [MenuItem("ARES/Validate task interactions")]
        public static void Run()
        {
            var d = AssetDatabase.LoadAssetAtPath<GameDefinition>("Assets/ARES/Generated/GameDefinition.asset");
            if (d == null) { var ids = AssetDatabase.FindAssets("t:GameDefinition"); Require(ids.Length == 1, "Expected one definition"); d = AssetDatabase.LoadAssetAtPath<GameDefinition>(AssetDatabase.GUIDToAssetPath(ids[0])); }
            string[] idsToTest = { "SortFlares", "WireShield", "PipeFlow", "AlignCoils", "TiltSun", "ThermostatDial", "ResourcePriority", "IrrigationPlan", "SmartFarmCare", "PowerBalance", "CmeCountdown", "WindReading", "CleanPanels", "PhotoDetective" };
            int count = 0;
            for (int sol = 0; sol < 5; sol++)
            {
                var data = new CampaignData { sols = new[] { d.campaign.sols[sol] } }; var campaign = new Campaign(data, d.rules);
                foreach (var id in idsToTest)
                {
                    var a = new TaskAttempt(id, d, campaign); a.Tick(100); Require(!a.Expired && a.Seconds == 0, "Briefing consumed clock");
                    a.Confirm(); Require(!a.Solved, "Briefing accepted input"); a.Begin(); a.Tick(1);
                    Solve(a); Require(a.Solved, "Unsolvable puzzle: " + id + " Sol " + sol); Require(campaign.Integrity == d.rules.initialIntegrity && campaign.Oxygen == d.rules.initialOxygen, "Attempt changed campaign resources");
                    int mistakes = a.Mistakes; a.Choose("wrong"); a.Tick(100); Require(a.Mistakes == mistakes && !a.Expired, "Finished attempt still mutable");
                    var expired = new TaskAttempt(id, d, campaign); expired.Begin(); expired.Tick(d.rules.taskSeconds); Solve(expired); Require(expired.Expired && !expired.Solved, "Expired attempt accepted input"); count++;
                }
            }
            File.WriteAllText("Logs/ares-tasks.txt", "PASS: " + count + " puzzle/sol combinations; briefing, timeout, completion freeze and resource isolation.");
        }
        internal static void Solve(TaskAttempt a)
        {
            switch (a.Id)
            {
                case "SortFlares": foreach (char c in "ABCMX") a.Choose(c.ToString()); break;
                case "WireShield": for (int i = 0; i < 4; i++) { a.Selected = i; a.Tap(i); } break;
                case "PipeFlow": for (int i = 0; i < 6; i++) for (int n = 0; n < a.Rules.miniGames.dialTargets[i % 3] / 3; n++) a.Tap(i); a.Confirm(); break;
                case "AlignCoils": for (int i = 0; i < 3; i++) a.Set(i, a.Rules.miniGames.dialTargets[i]); a.Confirm(); break;
                case "TiltSun": a.Set(0, a.Readings.Tilt); a.Confirm(); break;
                case "ThermostatDial": a.Set(0, a.Readings.Heating); a.Confirm(); break;
                case "ResourcePriority": a.Choose(a.Priority); break;
                case "IrrigationPlan": a.Set(0, 2); a.Set(2, 1); a.Confirm(); break;
                case "SmartFarmCare": a.Selected = 0; a.Tap(0); a.Selected = 0; a.Tap(2); a.Set(0, a.Rules.miniGames.lightTarget); for (int i = 0; i < 3; i++) a.Tap(i); break;
                case "PowerBalance": a.Choose(a.Demand.Total <= a.Rules.energyPool ? "matches" : "shortfall"); a.Set(0,a.Demand.shield); a.Set(1,a.Demand.life); a.Set(2,a.Demand.greenhouse); a.Confirm(); break;
                case "CmeCountdown": a.Set(0, (int)a.Readings.Speed); a.Confirm(); a.Choose(TaskReadings.TravelHours(a.Readings.Speed).ToString()); break;
                case "WindReading": a.Choose(a.Readings.WindDirection); break;
                case "CleanPanels": for (int i = 0; i < 20 && a.Active && a.ActiveArray >= 0; i++) a.Tap(a.ActiveArray); a.Confirm(); break;
                case "PhotoDetective": a.Choose("1"); a.Choose("no"); break;
            }
        }
    }
}
