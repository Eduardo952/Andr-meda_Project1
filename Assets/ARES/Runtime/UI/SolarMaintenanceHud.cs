using Ares.World;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Stored energy, production and contextual tool instructions share one compact HUD card.</summary>
    internal sealed class SolarMaintenanceHud
    {
        readonly SolarMaintenance solar;
        readonly Localization l;
        readonly VisualElement card;
        readonly Label generation, instruction, warning;
        readonly ProgressBar reserve, cleaning;
        public SolarMaintenanceHud(VisualElement root, SolarMaintenance solar, Localization l)
        {
            this.solar = solar; this.l = l;
            card = Box(root, "panel solar-maintenance"); card.name = "solar-maintenance";
            Text(card, l.T("solar.title"), "heading");
            reserve = new ProgressBar(); card.Add(reserve);
            generation = Text(card, "", "small");
            warning = Text(card, l.T("solar.critical"), "solar-critical"); warning.name = "solar-critical";
            instruction = Text(card, "", "small");
            cleaning = new ProgressBar { lowValue = 0, highValue = 1 }; card.Add(cleaning);
            IgnorePicking(card);
        }
        static void IgnorePicking(VisualElement element)
        { element.pickingMode = PickingMode.Ignore; foreach (var child in element.Children()) IgnorePicking(child); }
        public void Update()
        {
            var plant = solar.Plant; if (plant == null) return;
            reserve.highValue = plant.CapacityKwh; reserve.value = plant.ReserveKwh;
            reserve.title = l.T("solar.reserve", "n", plant.ReserveKwh.ToString("F1"));
            generation.text = l.T("solar.generation", "n", plant.GeneratedKwh.ToString("F1"), "seconds", plant.CycleSeconds, "dirty", plant.DirtyPanels);
            warning.text = l.T("solar.critical", "threshold", plant.CriticalReserveKwh);
            warning.style.display = plant.IsCritical ? DisplayStyle.Flex : DisplayStyle.None;
            card.EnableInClassList("solar-danger", plant.IsCritical);
            string key = solar.CanReturn ? "solar.return" : solar.CanPickUp ? "solar.pickup" : !solar.Cleaning.HasBroom ? "solar.findBroom"
                : solar.NearbyPanel < 0 ? "solar.enterZone" : !plant.NeedsCleaning(solar.NearbyPanel) ? "solar.clean" : "solar.hold";
            instruction.text = l.T(key, "n", solar.NearbyPanel + 1, "seconds", plant.CleaningSeconds);
            cleaning.style.display = solar.Cleaning.ActivePanel >= 0 ? DisplayStyle.Flex : DisplayStyle.None;
            cleaning.value = solar.Cleaning.Progress; cleaning.title = l.T("solar.cleaning", "n", (solar.Cleaning.Progress * 100).ToString("F0"));
        }
        public void SetVisible(bool visible) { card.style.display=visible?DisplayStyle.Flex:DisplayStyle.None; }
    }
}
