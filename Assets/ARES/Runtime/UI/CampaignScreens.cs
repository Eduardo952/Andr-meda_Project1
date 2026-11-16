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
            root.AddToClassList("report"); root.AddToClassList("technical-report");
            string Copy(string pt, string en) => l.Language == "en" ? en : pt;
            var header = Box(root, "technical-report-header");
            Text(header, Copy("BASE ARES / RELATÓRIO TÉCNICO", "ARES BASE / TECHNICAL REPORT"), "technical-report-eyebrow");
            Text(header, l.T("report.title", "n", c.Current.sol), "title");
            Text(header, Copy("Data de referência: ", "Reference date: ") + c.Current.date, "technical-report-muted");
            var f = c.Current.facts;
            var readings = new Ares.Data.SnapshotReadings(c.Current);
            var nativeFacts = c.NativeResponse?.facts;
            var budget = Box(root, "technical-report-budget");
            Text(budget, Copy("ENERGIA DISPONÍVEL", "AVAILABLE ENERGY"), "technical-report-eyebrow");
            Text(budget, c.EnergyPool + " kWh", "technical-report-value");
            Text(budget, Copy("Demanda total: ", "Total demand: ") + c.Current.demand.Total + " kWh", "technical-report-budget-total");
            Text(root, Copy("Necessidade por sistema", "Demand by system"), "technical-report-section");
            var demand = Box(root, "technical-report-demands");
            Demand(demand, l.T("sys.shield"), c.Current.demand.shield, "shield");
            Demand(demand, l.T("sys.life"), c.Current.demand.life, "oxygen");
            Demand(demand, l.T("sys.greenhouse"), c.Current.demand.greenhouse, "sprout");
            if (c.Current.thermalMissing) Text(root, l.T("report.fallback"), "warning");
            Text(root, c.Current.sample ? Copy("SIMULAÇÃO · valores de treino", "SIMULATION · training values")
                : Copy("ARQUIVO CIENTÍFICO · observações históricas", "SCIENTIFIC ARCHIVE · historical observations"), "technical-report-source");
            var columns = Box(root, "technical-report-columns");
            var solar = Box(columns, "technical-report-group");
            Text(solar, Copy("Atividade solar", "Solar activity"), "technical-report-section");
            ReportReading(solar, l.T("report.flare"), string.IsNullOrEmpty(f.flareClass) ? l.T("report.noFlare") : f.flareClass);
            ReportReading(solar, l.T("report.cme"), f.hasCmeSpeed ? f.cmeSpeed + " km/s" : l.T("report.noCme"));
            string impact = c.Current.native ? nativeFacts?.mars_impact?.level : null;
            ReportReading(solar, l.T("report.marsImpact"), c.Current.native
                ? l.T(impact == null ? "report.noSim" : impact == "direct" ? "report.impactYes" : impact == "glancing_blow" ? "report.impactGlancing" : "report.impactNo")
                : l.T(!readings.ImpactKnown ? "report.noSim" : !readings.MarsImpact ? "report.impactNo" : readings.Glancing ? "report.impactGlancing" : "report.impactYes"));
            var weather = Box(columns, "technical-report-group");
            Text(weather, Copy("Condições em Marte", "Mars conditions"), "technical-report-section");
            ReportReading(weather, l.T("report.temp"), f.hasAirTempMean ? f.airTempMean + " °C" : l.T("report.noReading"));
            ReportReading(weather, l.T("report.wind"), (f.hasWindSpeed ? f.windSpeed + " m/s" : l.T("report.noReading")) + (c.Current.native || readings.WindDirection == null ? "" : " · " + l.T("compass." + readings.WindDirection)));
            double? pressure = c.Current.native ? nativeFacts?.weather?.pressure_pa : readings.Pressure;
            ReportReading(weather, l.T("report.pressure"), pressure.HasValue ? pressure.Value.ToString("0.#") + " Pa" : l.T("report.noReading"));
            var science = new Foldout { text = Copy("Fontes e notas técnicas", "Sources and technical notes"), value = false }; root.Add(science);
            science.AddToClassList("technical-report-notes");
            ReportReading(science, l.T("report.sep"), c.Current.native ? l.T("report.noReading") : l.T(readings.SepDetected ? "report.sepYes" : "report.noSep"));
            if (!c.Current.sample)
            { Text(science, c.Current.native ? "NASA/DONKI · Curiosity/REMS (MAAS2)" : l.T("report.weatherWhere"), "small"); Text(science, l.T("report.dateNote", "n", c.Current.sol, "date", c.Current.date), "small"); }
            Text(science, Copy("A distribuição de energia e seus efeitos são regras do jogo. Leituras ausentes não são substituídas por medições inventadas.", "Energy allocation and its effects are game rules. Missing readings are not replaced with invented measurements."), "small");
            var footer = Box(root, "technical-report-footer");
            Text(footer, c.Phase == Phase.Report || c.Phase == Phase.Plan
                ? Copy("Confirme a distribuição de energia no terminal para liberar as missões.", "Confirm energy allocation at the terminal to unlock tasks.")
                : Copy("Consulte o terminal para acompanhar a distribuição de energia.", "Use the terminal to review energy allocation."), "technical-report-muted");
            Button(footer, l.T(c.Phase == Phase.Report ? "report.continue" : "report.close"), close).name = "technical-report-close";
        }
        static void Demand(VisualElement parent, string title, int amount, string icon)
        {
            var card = Box(parent, "technical-report-demand technical-report-" + icon);
            Text(card, title, "technical-report-muted"); Text(card, amount + " kWh", "technical-report-value");
        }
        static void ReportReading(VisualElement root, string title, string value)
        { var row = Box(root, "technical-report-reading"); Text(row, title, "technical-report-label"); Text(row, value, "technical-report-reading-value"); }
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
