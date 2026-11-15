using Ares.Domain;
using UnityEngine.UIElements;
using Ares.UI;
using System.Reflection;

namespace Ares.Editor
{
    /// <summary>Bounded daily care fixture. Never advances days or manufactures water.</summary>
    internal static class GreenhouseCheckFixture
    {
        public static bool Complete(Campaign campaign, string id)
        {
            if (!GreenhouseSimulation.IsTask(id)) return campaign.FinishTask(id, true, 10, 0);
            CareForToday(campaign.Greenhouse, campaign.Watering);
            return campaign.FinishGreenhouseTask();
        }
        public static void CareForToday(GreenhouseSimulation game, GreenhouseWatering water)
        {
            water.PickUp();
            if (game.Light != GrowLight.Moderate) game.AdjustLight(game.Light == GrowLight.Low ? 1 : -1);
            for (int i = 0; i < GreenhouseRules.ShelfCount; i++)
            {
                game.Inspect(i);
                for (int dose = game.Shelves[i].DeliveredDoses; dose < game.Shelves[i].RequiredDoses; dose++)
                {
                    water.Fill();
                    if (!water.StartPour(i) || !water.Tick(i, false, 3)) throw new System.InvalidOperationException("Cannot complete daily care: insufficient water or inactive day.");
                }
            }
        }
        public static void Press(VisualElement root, string name)
        {
            var button = root.Q<Button>(name);
            if (button == null || !button.enabledInHierarchy) throw new System.InvalidOperationException("Missing/disabled greenhouse control: " + name);
            using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
        }
        public static void CompleteThroughControls(VisualElement root, GameFrontend ui)
        { CareForToday(ui.Campaign.Greenhouse, ui.Campaign.Watering); Reopen(ui); }
        public static void Reopen(GameFrontend ui)
        {
            foreach (var task in ui.Campaign.Current.tasks) if (task.room == "greenhouse")
            {
                if (ui.Campaign.OpenTask == null) ui.Campaign.BeginTask(task.id, task.room);
                typeof(GameFrontend).GetMethod("OpenTask", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ui, new object[] { task.id }); break;
            }
        }
    }
}
