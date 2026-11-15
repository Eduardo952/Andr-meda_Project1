using Ares.World;
using Ares.Domain;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    internal sealed class GreenhouseCareHud
    {
        readonly GreenhouseCare care;
        readonly VisualElement card;
        readonly Label water, climate, instruction;
        readonly Label[] shelves = new Label[4];
        readonly ProgressBar can, progress;
        public GreenhouseCareHud(VisualElement root, GreenhouseCare care)
        {
            this.care = care; card = Box(root, "panel greenhouse-care-hud"); card.name = "greenhouse-care-hud";
            card.Add(new CartoonText("ARES BASE", 29));
            water = Text(card, "", "small");
            can = new ProgressBar { lowValue = 0, highValue = GreenhouseWatering.CapacityMl }; card.Add(can);
            climate = Text(card, "", "small");
            for (int i = 0; i < 4; i++) shelves[i] = Text(card, "", "greenhouse-care-shelf");
            instruction = Text(card, "", "greenhouse-care-instruction");
            progress = new ProgressBar { lowValue = 0, highValue = 1 }; card.Add(progress); Ignore(card);
        }
        static void Ignore(VisualElement element) { element.pickingMode = PickingMode.Ignore; foreach (var child in element.Children()) Ignore(child); }
        public void Update()
        {
            card.style.display = care.Inside && care.Watering != null ? DisplayStyle.Flex : DisplayStyle.None;
            var w = care.Watering; var g = care.Simulation; if (w == null) return;
            water.text = $"Reservatório: {GreenhouseNarrator.Litres(w.ReservoirMl)} / 11 L · Dia {g.Day} de 5";
            can.value = w.CanMl; can.title = $"Regador: {w.CanMl} / 510 mL" + (w.HasCan ? "" : " · no suporte");
            climate.text = "Luz: " + GreenhouseNarrator.Light(g.Light) + " · " + GreenhouseNarrator.LightHint(g.Light);
            for (int i = 0; i < 4; i++)
            {
                var shelf = g.Shelves[i];
                shelves[i].text = $"{i + 1} · {GreenhouseNarrator.Soil(shelf.Soil)} · {shelf.DeliveredDoses}/{shelf.RequiredDoses} regas" + (shelf.CaredFor ? " · pronto" : "");
            }
            instruction.text = care.InteractionPrompt ?? (g.DayEnded ? "Cuidados deste dia encerrados." : g.CareReady ? "Cuidados de hoje prontos! [E] Abra o painel para registrar." : w.ActiveShelf >= 0 ? "Regando… fique perto até terminar." : care.NearbyShelf >= 0 ? GreenhouseNarrator.WaterHint(g.Shelves[care.NearbyShelf]) + "\n" + (g.Shelves[care.NearbyShelf].RequiredDoses == 0 ? "Clique na prateleira para observar." : !w.HasCan ? "Pegue o regador na entrada." : w.CanMl < 510 ? "Reabasteça o regador no reservatório." : "Clique na prateleira para regar.") : !w.HasCan ? "Clique no regador, na entrada, para pegar." : w.AvailableMl < 510 ? "Sem água para outra rega completa. Cuide também da luz." : w.CanMl < 510 ? "Clique no reservatório para reabastecer." : "Observe o solo de cada prateleira antes de regar.");
            progress.style.display = w.ActiveShelf >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
            progress.value = w.PourProgress; progress.title = "Uma rega: 510 mL";
        }
    }
}
