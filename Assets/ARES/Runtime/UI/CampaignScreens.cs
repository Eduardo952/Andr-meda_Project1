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
            var f = c.Current.facts;
            var readings = new Ares.Data.SnapshotReadings(c.Current);
            Text(root, "Prepare a casa para mais um dia em Marte.", "heading");
            Text(root, "Cada equipamento precisa de uma parte da energia.", "small");
            var demand = Box(root, "row");
            Demand(demand, l.T("sys.shield"), c.Current.demand.shield, "shield");
            Demand(demand, l.T("sys.life"), c.Current.demand.life, "oxygen");
            Demand(demand, l.T("sys.greenhouse"), c.Current.demand.greenhouse, "sprout");
            if (c.Current.thermalMissing) Text(root, l.T("report.fallback"), "warning");
            var science = new Foldout { text = "Céu e clima · ver registros", value = false }; root.Add(science);
            Text(science, c.Current.sample ? "Valores de treino para esta história, não medições da NASA." : l.T("report.realData"), "badge");
            var row = Box(science, "row report-grid");
            Reading(row, l.T("report.flare"), string.IsNullOrEmpty(f.flareClass) ? l.T("report.noFlare") : f.flareClass);
            Reading(row, l.T("report.cme"), f.hasCmeSpeed ? f.cmeSpeed + " km/s" : l.T("report.noCme"));
            Reading(row, l.T("report.sep"), l.T(readings.SepDetected?"report.sepYes":"report.noSep"));
            Reading(row, l.T("report.marsImpact"), l.T(!readings.ImpactKnown?"report.noSim":!readings.MarsImpact?"report.impactNo":readings.Glancing?"report.impactGlancing":"report.impactYes"));
            Reading(row, l.T("report.temp"), f.hasAirTempMean ? f.airTempMean + " °C" : l.T("report.noReading"));
            Reading(row, l.T("report.wind"), (f.hasWindSpeed ? f.windSpeed + " m/s" : l.T("report.noReading")) + (readings.WindDirection==null?"":" · "+l.T("compass."+readings.WindDirection)));
            Reading(row, l.T("report.pressure"), readings.Pressure.HasValue?readings.Pressure.Value+" Pa":l.T("report.noReading"));
            if (!c.Current.sample)
            { Text(science, l.T("report.weatherWhere"), "small"); Text(science, l.T("report.dateNote", "n", c.Current.sol, "date", c.Current.date), "small"); }
            Text(science, "A divisão de energia é uma regra do jogo. Os desafios solares usam outros registros reais, identificados na fonte.", "small");
            Button(root, l.T(c.Phase == Phase.Report ? "report.continue" : "report.close"), close);
        }
        static void Demand(VisualElement parent, string title, int amount, string icon)
        {
            var card = Box(parent, "card");
            if (icon == "sprout") card.Add(new ChallengeIcon(icon, ChallengePalette.Green, 52));
            else
            {
                var picture = new Image { image = UnityEngine.Resources.Load<UnityEngine.Texture2D>("ARES/UI/HudIcons/" + (icon == "shield" ? "integrity" : icon) + "-v1"), scaleMode = UnityEngine.ScaleMode.ScaleToFit };
                picture.style.width = 64; picture.style.height = 64; card.Add(picture);
            }
            Text(card, title, "heading"); Text(card, amount + " kWh", "title");
        }
        static void Reading(VisualElement root, string title, string value) { var card = Box(root, "card"); Text(card, title, "small"); Text(card, value, "heading"); }
        public static void Summary(VisualElement root, Campaign c, Localization l, Action next)
        {
            Text(root, l.T("summary.title", "n", c.Current.sol), "title");
            Text(root, l.T("hud.integrity") + " " + c.Integrity + " · " + l.T("hud.oxygen") + " " + c.Oxygen + " · " + l.T("hud.dose") + " " + c.Dose, "heading");
            string[] systems = { "shield", "life", "greenhouse" }; var results = new[] { c.Outcome.shield, c.Outcome.life, c.Outcome.greenhouse };
            for (int i = 0; i < 3; i++) Reading(root, l.T("sys." + systems[i]), results[i].allocated + " / " + results[i].demand + " kWh · " + l.T("summary." + results[i].status));
            foreach (var item in c.Events) Text(root, l.T("consequences.events." + item.id + ".title") + " · Δ O₂ " + item.effects.oxygen + " · Δ " + l.T("hud.dose") + " " + item.effects.dose, "small");
            Text(root, "Antes de seguir, revise e envie o relatório da base para a Terra.", "small");
            Button(root, "Revisar relatório na central", next).name = "summary-communications";
        }
    }
}
