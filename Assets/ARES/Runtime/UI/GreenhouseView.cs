using System;
using Ares.Domain;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Daily observations and light controls. Water is delivered only by the world watering can.</summary>
    internal sealed class GreenhouseView
    {
        readonly GreenhouseSimulation game;
        readonly GreenhouseWatering watering;
        readonly Action accept, close;
        readonly VisualElement body, footer;
        bool information;
        public GreenhouseView(VisualElement root, GreenhouseSimulation game, GreenhouseWatering watering, Action accept, Action close)
        {
            this.game = game; this.watering = watering; this.accept = accept; this.close = close;
            var panel = new ChallengePanel(root, new Localization(PresentationLocalizer.Language), "Estufa · Ares Base", "greenhouse", null, false,
                () => { information = !information; Render(); }, close);
            root.AddToClassList("greenhouse-view"); root.name = "greenhouse-view";
            body = panel.Body; footer = panel.Footer; Render();
        }
        public void Render()
        {
            body.Clear(); footer.Clear();
            Text(body, GreenhouseNarrator.Welcome, "greenhouse-welcome");
            var content = Box(body, "greenhouse-daily");
            var reserve = Box(content, "greenhouse-water-card");
            Text(reserve, $"Dia {game.Day} de 5 · Reservatório: {GreenhouseNarrator.Litres(watering.ReservoirMl)} L", "heading").name = "greenhouse-water";
            var gauge = new ProgressBar { lowValue = 0, highValue = GreenhouseRules.InitialWaterMl, value = watering.ReservoirMl, title = "Reserva para toda a missão" }; reserve.Add(gauge);
            Text(reserve, $"No regador: {watering.CanMl} / 510 mL · Usados hoje: {GreenhouseNarrator.Litres(game.TodayDeliveredMl)} L", "small");
            if (watering.AvailableMl < GreenhouseRules.DoseMl)
                Text(reserve, "Não há água suficiente para outra rega de 510 mL. Você ainda pode observar as plantas e ajustar a luz.", "greenhouse-error");
            var shelves = Box(content, "row greenhouse-daily-shelves");
            for (int i = 0; i < game.Shelves.Count; i++)
            {
                int index = i; var shelf = game.Shelves[i];
                var card = Box(shelves, "greenhouse-daily-shelf shelf-" + shelf.Health.ToString().ToLowerInvariant());
                Text(card, $"{i + 1} · {shelf.Crop}", "heading");
                Text(card, "10 recipientes", "small");
                Text(card, "Solo: " + GreenhouseNarrator.Soil(shelf.Soil), "greenhouse-soil");
                Text(card, GreenhouseNarrator.WaterHint(shelf));
                Text(card, $"Regas: {shelf.DeliveredDoses} / {shelf.RequiredDoses}", "small");
                var observe = Button(card, shelf.Inspected ? "Observado" : "Observar solo", () => { game.Inspect(index); Render(); }, "ghost");
                observe.name = "greenhouse-inspect-" + i; observe.SetEnabled(!shelf.Inspected && game.CanCare);
            }
            var light = Box(content, "greenhouse-light-card");
            Text(light, "Luz: " + GreenhouseNarrator.Light(game.Light), "heading").name = "greenhouse-light";
            Text(light, GreenhouseNarrator.LightHint(game.Light));
            var controls = Box(light, "row");
            var less = Button(controls, "− Diminuir", () => { game.AdjustLight(-1); Render(); }, "ghost");
            less.name = "greenhouse-light-less"; less.SetEnabled(game.CanCare && game.Light != GrowLight.Low);
            var more = Button(controls, "+ Aumentar", () => { game.AdjustLight(1); Render(); }, "ghost");
            more.name = "greenhouse-light-more"; more.SetEnabled(game.CanCare && game.Light != GrowLight.Excessive);
            Text(light, game.Environment, "small");
            var steps = Box(content, "row mini-data-grid");
            Step(steps, "1 · Pegar regador", "drop");
            Step(steps, "2 · Encher: 510 mL", "drop");
            Step(steps, "3 · Clicar na prateleira", "sprout");
            if (game.LastResult != null) Text(content, GreenhouseNarrator.Result(game.LastResult), "greenhouse-result");
            if (information)
            {
                var science = Box(content, "greenhouse-cycle");
                Text(science, GreenhouseNarrator.Inspiration); Text(science, GreenhouseNarrator.WaterNote); Text(science, GreenhouseNarrator.EnvironmentNote);
                Text(science, "Nas 4 prateleiras: todas moderadas = 2,04 L/dia; todas secas = 4,08 L/dia; todas muito úmidas = 0 L/dia.");
                Text(science, "Solo seco pode desidratar as plantas. Água demais pode prejudicar as raízes. Pouca luz prejudica o crescimento; luz demais pode causar estresse.");
                Button(science, "Pesquisa VEG-04 · NASA", () => Application.OpenURL("https://ntrs.nasa.gov/citations/20240000823"), "ghost");
            }
            Button(footer, "Voltar à estufa", close, "ghost").name = "greenhouse-return";
            var done = Button(footer, "Registrar cuidados de hoje", accept);
            done.name = "greenhouse-accept"; done.SetEnabled(game.CareReady);
        }
        static void Step(VisualElement parent, string text, string icon)
        {
            var step = Box(parent, "row greenhouse-water-step");
            step.Add(new ChallengeIcon(icon, ChallengePalette.Life, 30));
            Text(step, text, "small");
        }
    }
}
