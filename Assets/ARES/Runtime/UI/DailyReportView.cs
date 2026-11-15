using Ares.Domain.Communications;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Renders a frozen report; never queries live systems while viewing history.</summary>
    internal static class DailyReportView
    {
        public static void Build(VisualElement parent, DailyBaseSnapshot report)
        {
            var grid = Box(parent, "comms-report-grid");
            var crops = Card(grid, "Estufa", $"Água no reservatório: {GreenhouseNarrator.Litres(report.ReservoirMl)} L");
            Text(crops, $"No regador: {report.WateringCanMl} mL · Usada hoje: {GreenhouseNarrator.Litres(report.WaterUsedMl)} L", "comms-small");
            int healthy = 0, atRisk = 0;
            foreach (var shelf in report.Shelves) { if (shelf.Health == Ares.Domain.ShelfHealth.Healthy) healthy++; else if (shelf.Health == Ares.Domain.ShelfHealth.AtRisk) atRisk++; }
            Text(crops, $"{healthy}/4 saudáveis · {report.Shelves.Count - healthy - atRisk} precisam de cuidado · {atRisk} em risco", "comms-small");
            var details = new Foldout { text = "Ver cada prateleira", value = false }; crops.Add(details);
            for (int i = 0; i < report.Shelves.Count; i++) Text(details, $"{i + 1} · {report.Shelves[i].Crop}: {GreenhouseNarrator.Health(report.Shelves[i].Health)}", "comms-small");
            Card(grid, "Painéis solares", $"Energia na bateria: {report.AvailableEnergyKwh:0.0} kWh\n{report.DirtyPanels} de 32 painéis precisam de limpeza.");
            var shield = Card(grid, "Escudo magnético", report.ShieldAllocated >= report.ShieldNeeded ? "Energia de proteção suficiente." : "Proteção precisa de atenção: faltou energia.");
            Text(shield, $"Energia: {report.ShieldAllocated} / {report.ShieldNeeded} kWh\nExposição acumulada: {report.RadiationIndex} (índice do jogo)", "comms-small");
            var rest = Card(grid, "Dormitório", report.RestEnvironmentStable ? "Ambiente para descansar: estável." : "Ambiente para descansar: precisa de atenção.");
            Text(rest, $"Oxigênio: {report.OxygenPercent}% · Integridade da base: {report.Integrity}%", "comms-small");
            Text(rest, "Avaliado pelo ar, energia e integridade. Horas de sono ainda não são medidas pelo jogo.", "comms-small");
            var events = Card(grid, "Demandas conflitantes", report.DemandEvent == Ares.Domain.DemandEvent.None ? "Nenhum evento atingiu a base neste dia."
                : report.DemandEvent == Ares.Domain.DemandEvent.DustStorm ? "Tempestade de poeira" : "Erupção solar");
            if (report.Day == 2 || report.Day == 5) Text(events, report.ShieldRegulated ? "Escudo regulado: sim · proteção válida neste dia" : "Escudo regulado: não", "comms-small");
            Text(events, $"Painéis ainda soterrados: {report.BuriedPanels}/32", "comms-small");
            Text(events, report.AntennaConnected ? "Antena conectada" : "Antena danificada · reparo necessário", "comms-small");
        }
        static VisualElement Card(VisualElement parent, string title, string content)
        { var card = Box(parent, "comms-report-card"); Text(card, title, "heading"); Text(card, content); return card; }
    }
}
