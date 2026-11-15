using System;
using Ares.Data;
using Ares.Domain;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    internal static class EnergyView
    {
        public static void Build(VisualElement parent, Campaign campaign, CampaignRules rules, Localization l, Action close)
        {
            VisualElement information = null;
            var panel = new ChallengePanel(parent, l, l.T("energy.title"), "control", null, false,
                () => { if (information != null) information.style.display = information.resolvedStyle.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None; }, close);
            information = Box(panel.Body, "mini-hint"); Text(information, l.T("energy.budgetNote")); information.style.display = DisplayStyle.None;
            Text(panel.Body, l.T("energy.hint"), "mini-context mini-instruction");
            parent = new ChallengeGrid(); panel.Body.Add(parent);
            Text(parent, l.T("energy.pool", "n", campaign.EnergyPool), "badge");
            if (campaign.EnergyPool < rules.energyPool) Text(parent, l.T("energy.dustNote", "loss", rules.energyPool - campaign.EnergyPool), "warning");
            var sliders = new SliderInt[3]; var valueLabels = new Label[3];
            string[] systems = { "shield", "life", "greenhouse" };
            int[] demands = { campaign.Current.demand.shield, campaign.Current.demand.life, campaign.Current.demand.greenhouse };
            var free = new Label(); var start = new Button();
            Action update = () => { var a = campaign.Allocation; int[] v = { a.shield, a.life, a.greenhouse }; for (int i = 0; i < 3; i++) { sliders[i]?.SetValueWithoutNotify(v[i]); if (valueLabels[i] != null) valueLabels[i].text = l.T("sys." + systems[i]) + " · " + v[i] + " kWh · " + l.T("energy.need", "n", demands[i]); } free.text = l.T("energy.free") + ": " + (campaign.EnergyPool - a.Total); start.SetEnabled(a.Total == campaign.EnergyPool); };
            for (int i = 0; i < 3; i++)
            {
                int index = i; var card = Box(parent, "mini-energy-row");
                valueLabels[i] = Text(card, "", "mini-instruction");
                sliders[i] = new SliderInt(0, campaign.EnergyPool) { tooltip = l.T("sys." + systems[i]) }; card.Add(sliders[i]);
                sliders[i].RegisterValueChangedCallback(e => { var a = campaign.Allocation; int old = index == 0 ? a.shield : index == 1 ? a.life : a.greenhouse; int value = Math.Min(campaign.EnergyPool - a.Total + old, UnityEngine.Mathf.RoundToInt((float)e.newValue / rules.sliderStep) * rules.sliderStep); if (index == 0) a.shield = value; else if (index == 1) a.life = value; else a.greenhouse = value; campaign.SetAllocation(a); update(); });
            }
            parent.Add(free); free.AddToClassList("badge"); Text(parent, l.T("energy.budgetNote"), "small");
            var row = panel.Footer; Button(row, l.T("energy.close"), close, "ghost");
            start.text = l.T(campaign.Phase == Phase.Plan ? "energy.start" : "energy.save"); start.clicked += () => { if (campaign.Phase == Phase.Tasks || campaign.StartSol()) close(); }; row.Add(start); update();
        }
    }
}
