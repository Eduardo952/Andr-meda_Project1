using Ares.Domain;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Above task and terminal overlays, without intercepting their input.</summary>
    internal sealed class MissionEventBanner
    {
        readonly Label label;
        public MissionEventBanner(VisualElement root)
        {
            label = UiElements.Text(root, "", "mission-event-banner");
            label.name = "mission-event-banner"; label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Absolute; label.style.display = DisplayStyle.None;
        }
        public void Update(Campaign campaign, bool visible, bool modal)
        {
            string message = visible && campaign.Phase == Phase.Tasks ? PresentationLocalizer.Text(MissionEventCopy.Status(campaign)) : "";
            if (label.text != message) label.text = message;
            label.style.display = message.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            label.style.top = modal ? 8 : 110;
            if (message.Length > 0) label.BringToFront();
        }
        public void Hide() => label.style.display = DisplayStyle.None;
    }
}
