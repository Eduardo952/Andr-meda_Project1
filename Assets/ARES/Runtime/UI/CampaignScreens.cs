using System;
using Ares.Domain;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    internal static class CampaignScreens
    {
        public static void Report(VisualElement root, Campaign c, Localization l, Action close)
        {
            root.AddToClassList("report");
            Text(root, l.T("report.title", "n", c.Current.sol), "title");
            if (c.Current.sample) Text(root, l.T("report.sample"), "warning");
            var f = c.Current.facts;
            var readings = new Ares.Data.SnapshotReadings(c.Current);
            Text(root, l.T("report.realData"), "badge");
            var row = Box(root, "row report-grid");
            Reading(row, l.T("report.flare"), string.IsNullOrEmpty(f.flareClass) ? l.T("report.noFlare") : f.flareClass);
            Reading(row, l.T("report.cme"), f.hasCmeSpeed ? f.cmeSpeed + " km/s" : l.T("report.noCme"));
            Reading(row, l.T("report.sep"), l.T(readings.SepDetected?"report.sepYes":"report.noSep"));
            Reading(row, l.T("report.marsImpact"), l.T(!readings.ImpactKnown?"report.noSim":!readings.MarsImpact?"report.impactNo":readings.Glancing?"report.impactGlancing":"report.impactYes"));
            Reading(row, l.T("report.temp"), f.hasAirTempMean ? f.airTempMean + " °C" : l.T("report.noReading"));
            Reading(row, l.T("report.wind"), (f.hasWindSpeed ? f.windSpeed + " m/s" : l.T("report.noReading")) + (readings.WindDirection==null?"":" · "+l.T("compass."+readings.WindDirection)));
            Reading(row, l.T("report.pressure"), readings.Pressure.HasValue?readings.Pressure.Value+" Pa":l.T("report.noReading"));
            Text(root, l.T("report.weatherWhere"), "small"); Text(root, l.T("report.gameRule"), "badge");
            var demand = Box(root, "row");
            Reading(demand, l.T("sys.shield"), c.Current.demand.shield + " kWh");
            Reading(demand, l.T("sys.life"), c.Current.demand.life + " kWh");
            Reading(demand, l.T("sys.greenhouse"), c.Current.demand.greenhouse + " kWh");
            if (c.Current.thermalMissing) Text(root, l.T("report.fallback"), "warning");
            Text(root, l.T("report.dateNote", "n", c.Current.sol, "date", c.Current.date), "small");
            Button(root, l.T(c.Phase == Phase.Report ? "report.continue" : "report.close"), close);
        }
        static void Reading(VisualElement root, string title, string value) { var card = Box(root, "card"); Text(card, title, "small"); Text(card, value, "heading"); }
        public static void Summary(VisualElement root, Campaign c, Localization l, Action next)
        {
            Text(root, l.T("summary.title", "n", c.Current.sol), "title");
            Text(root, l.T("hud.integrity") + " " + c.Integrity + " · " + l.T("hud.oxygen") + " " + c.Oxygen + " · " + l.T("hud.dose") + " " + c.Dose, "heading");
            string[] systems = { "shield", "life", "greenhouse" }; var results = new[] { c.Outcome.shield, c.Outcome.life, c.Outcome.greenhouse };
            for (int i = 0; i < 3; i++) Reading(root, l.T("sys." + systems[i]), results[i].allocated + " / " + results[i].demand + " kWh · " + l.T("summary." + results[i].status));
            foreach (var item in c.Events) Text(root, l.T("consequences.events." + item.id + ".title") + " · Δ O₂ " + item.effects.oxygen + " · Δ " + l.T("hud.dose") + " " + item.effects.dose, "small");
            Button(root, l.T("tasks.continue"), next);
        }
    }
}
