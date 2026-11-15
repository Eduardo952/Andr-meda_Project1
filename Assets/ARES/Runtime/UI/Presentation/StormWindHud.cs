using Ares.Domain;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Outdoor weather instrument. Reads the same simulated gust that moves the dust.</summary>
    internal sealed class StormWindHud
    {
        readonly VisualElement card;
        readonly Label speed;
        readonly ReferenceExplorer explorer;
        public StormWindHud(VisualElement root, ReferenceExplorer explorer)
        {
            this.explorer = explorer;
            card = UiElements.Box(root, "storm-wind-card"); card.name = "storm-wind-hud";
            card.pickingMode = PickingMode.Ignore; card.style.display = DisplayStyle.None;
            UiElements.Text(card, "VENTO EXTERNO", "storm-wind-title");
            speed = UiElements.Text(card, "", "storm-wind-speed"); speed.name = "storm-wind-speed";
            UiElements.Text(card, "Tempestade de areia · rajadas", "storm-wind-caption");
            foreach (var child in card.Children()) child.pickingMode = PickingMode.Ignore;
        }
        public void Update(Campaign campaign, bool visible)
        {
            bool show = visible && campaign?.Phase == Phase.Tasks && campaign.Demands.DustInAir
                && !HabitatShelter.Contains(explorer.definition.layout, explorer.ControlledPosition, .1f);
            card.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            string reading = Mathf.RoundToInt(new DustStormWeather(campaign.Demands).SpeedKmh) + " km/h";
            if (speed.text != reading) speed.text = reading;
        }
    }
}
