using Ares.Tasks;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Read-only cards and provenance matching ChallengeReadings.tsx.</summary>
    internal static class ChallengeReadings
    {
        public static string Sector(string id)
        {
            switch (id)
            {
                case "SortFlares": case "PhotoDetective": return "observatory";
                case "AlignCoils": case "WireShield": case "CmeCountdown": return "shield";
                case "PipeFlow": case "ThermostatDial": case "ResourcePriority": return "lifeSupport";
                case "SmartFarmCare": case "IrrigationPlan": return "greenhouse";
                default: return "solar";
            }
        }
        public static string Source(string id, Localization locale)
        {
            switch (id)
            {
                case "SortFlares": return "Explosões no Sol · NASA/DONKI";
                case "CmeCountdown": return "Nuvens solares · NASA/DONKI";
                case "ThermostatDial": return "Temperatura de Marte · InSight";
                case "WindReading": return "Vento em Marte · InSight";
                case "CleanPanels": return locale.T("consequences.simulated");
                default: return null;
            }
        }
        public static void Build(VisualElement parent, TaskAttempt a, Localization l)
        {
            var grid = Box(parent, "row mini-data-grid");
            void Card(string key, object value, string icon = "compass", string note = "mini.rulesNote")
            {
                var card = Box(grid, "row mini-data-card");
                card.Add(new ChallengeIcon(icon, ChallengePalette.Sector(Sector(a.Id)), 36));
                var content = Box(card, "mini-data-content");
                Text(content, l.T(key), "small"); Text(content, value.ToString(), "heading"); card.tooltip = l.T(note);
            }
            switch (a.Id)
            {
                case "TiltSun": Card("mini.targetAngle", a.Readings.Tilt + "°", "sun"); break;
                case "CmeCountdown": Card("mini.speed", a.Readings.Speed, "sun", "mini.constantSpeed"); break;
                case "AlignCoils": Card("challenge.marks", a.Readings.CoilMarks); break;
                case "WireShield": Card("challenge.connections", "◆ ● ▲ ✚"); break;
                case "ThermostatDial": Card("mini.outside", a.Readings.Temperature + " °C", "thermometer", "challenge.dataNotes.temperature"); Card("mini.heating", a.Readings.Heating, "sun"); break;
                case "ResourcePriority": Card("challenge.oxygen", a.OxygenBefore); Card("sys.life", a.Demand.life, "thermometer"); Card("sys.greenhouse", a.Demand.greenhouse, "sprout"); break;
                case "IrrigationPlan": Card("challenge.waterBudget", 3, "drop"); Card("challenge.moistureTarget", 3, "sprout"); break;
                case "SmartFarmCare": Card("mini.lightTarget", a.Rules.miniGames.lightTarget, "sun"); break;
                case "CleanPanels": Card("challenge.dust", a.Readings.DustPatches, "sun"); break;
                case "PowerBalance": Card("challenge.available", a.Rules.energyPool, "sun"); Card("challenge.demand", a.Demand.Total); break;
                case "WindReading": Card("challenge.wind", l.T(a.Readings.WindPractice ? "report.noReading" : "compass." + a.Readings.WindDirection), "compass", "challenge.dataNotes.wind"); break;
            }
            if (grid.childCount == 0) grid.RemoveFromHierarchy();
        }
    }
}
