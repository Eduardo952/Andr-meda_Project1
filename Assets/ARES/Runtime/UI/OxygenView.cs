using System;
using Ares.Domain;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Composes the glanceable reserve, one next-step hint, controls and optional learning.</summary>
    internal sealed class OxygenView
    {
        readonly Campaign campaign;
        readonly OxygenPlant plant;
        readonly OxygenTankArt tank;
        readonly OxygenControls controls;
        readonly OxygenDetails details;
        readonly VisualElement statusBox, productionFill, consumptionFill;
        readonly Label reserve, trend, heading, advice, water, power, filter, comparison;
        public OxygenView(VisualElement parent, Campaign campaign, Func<bool> canInteract)
        {
            this.campaign = campaign; plant = campaign.OxygenSystem;
            parent.AddToClassList("oxygen-view");
            parent.RegisterCallback<GeometryChangedEvent>(e => parent.EnableInClassList("ox-compact", e.newRect.width < 900));
            var intro = Box(parent, "ox-intro");
            Text(intro, "Oxigênio da base", "ox-title");
            Text(intro, "Cuide da reserva para a equipe respirar.", "ox-subtitle");
            var dashboard = Box(parent, "ox-dashboard");
            var storage = Box(dashboard, "ox-storage");
            Text(storage, "NOSSA RESERVA", "ox-eyebrow");
            tank = new OxygenTankArt(); storage.Add(tank);
            reserve = Text(storage, "", "ox-percent"); reserve.name = "oxygen-reserve";
            Text(storage, "do tanque", "ox-muted");
            trend = Text(storage, "", "ox-trend");
            var decisions = Box(dashboard, "ox-decisions");
            statusBox = Box(decisions, "ox-status"); statusBox.name = "oxygen-status";
            heading = Text(statusBox, "", "ox-status-title"); heading.name = "oxygen-status-title";
            advice = Text(statusBox, "", "ox-advice"); advice.name = "oxygen-advice";
            var flow = Box(decisions, "ox-flow");
            productionFill = FlowRow(flow, "Produção", "ox-production");
            consumptionFill = FlowRow(flow, "Uso da base", "ox-consumption");
            comparison = Text(flow, "", "ox-comparison");
            controls = new OxygenControls(decisions, plant, canInteract, Refresh);
            var support = Box(parent, "ox-support");
            water = Text(support, "", "ox-chip"); water.name = "oxygen-water";
            power = Text(support, "", "ox-chip"); power.name = "oxygen-power";
            filter = Text(support, "", "ox-chip"); filter.name = "oxygen-filter";
            details = new OxygenDetails(parent, campaign, canInteract);
            Refresh();
        }
        static VisualElement FlowRow(VisualElement parent, string title, string style)
        {
            var row = Box(parent, "ox-flow-row"); Text(row, title, "ox-flow-label");
            return Box(Box(row, "ox-flow-track"), "ox-flow-fill " + style);
        }
        public void Refresh()
        {
            var state = OxygenGuidance.Read(plant, campaign.Phase);
            heading.text = state.Title; advice.text = state.Advice;
            statusBox.EnableInClassList("ox-warning", state.Warning);
            reserve.text = plant.Percent.ToString("0") + "%";
            trend.text = plant.Percent <= .01f ? "Tanque vazio" : campaign.Phase == Phase.Plan ? "Pronto para o dia" : plant.Balance < -.00001f ? "↓ Diminuindo"
                : plant.Percent >= 99.9f ? "Tanque cheio" : plant.Balance > .00001f ? "↑ Aumentando" : "Reserva estável";
            float currentProduction = plant.WaterKg <= 0 ? 0 : plant.Production;
            float scale = Mathf.Max(currentProduction, plant.Consumption, .001f);
            productionFill.style.width = Length.Percent(currentProduction / scale * 100);
            consumptionFill.style.width = Length.Percent(plant.Consumption / scale * 100);
            productionFill.tooltip = $"{currentProduction * 1000 * 60 / plant.CycleSeconds:0} g por minuto de jogo";
            consumptionFill.tooltip = $"{plant.Consumption * 1000 * 60 / plant.CycleSeconds:0} g por minuto de jogo";
            float balance = currentProduction - plant.Consumption;
            comparison.text = balance < -.00001f ? "Usamos mais do que produzimos." : balance > .00001f ? "Produzimos mais do que usamos." : "Produção e uso estão equilibrados.";
            bool lowWater = plant.WaterKg <= 0 || plant.WaterKg < (plant.WaterKg + plant.TotalWaterUsed) * .2f;
            water.text = plant.WaterKg <= 0 ? "! Água esgotada" : lowWater ? "! Água acabando" : "Água disponível";
            power.text = plant.PowerFraction < .99f ? "! Pouca energia" : "Energia suficiente";
            filter.text = plant.PowerFraction < .99f ? "! Filtro com pouca energia" : plant.CarbonDioxideKg > .001f ? "Filtro limpando o ar" : "Filtro ativo";
            water.EnableInClassList("ox-chip-warning", lowWater);
            power.EnableInClassList("ox-chip-warning", plant.PowerFraction < .99f);
            filter.EnableInClassList("ox-chip-warning", plant.PowerFraction < .99f);
            controls.Refresh(); details.Refresh();
            tank.Fill = plant.Percent / 100; tank.Critical = plant.Critical; tank.MarkDirtyRepaint();
        }
    }
}
