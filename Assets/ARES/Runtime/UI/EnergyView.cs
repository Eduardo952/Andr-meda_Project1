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
            parent = new ChallengeGrid(); parent.AddToClassList("energy-board"); panel.Body.Add(parent);
            var pool=Text(parent, l.T("energy.pool", "n", campaign.EnergyPool), "badge");
            var battery=Text(parent,"","small");
            if (campaign.EnergyPool < rules.energyPool) Text(parent, l.T("energy.dustNote", "loss", rules.energyPool - campaign.EnergyPool), "warning");
            var sliders = new SliderInt[3]; var valueLabels = new Label[3]; var meters=new VisualElement[3];var notices=new Label[3];
            string[] systems = { "shield", "life", "greenhouse" };
            int[] demands = { campaign.Current.demand.shield, campaign.Current.demand.life, campaign.Current.demand.greenhouse };
            var free = new Label(); var start = new Button();
            Action update = () => { var a = campaign.Allocation; int[] v = { a.shield, a.life, a.greenhouse }; for (int i = 0; i < 3; i++) { sliders[i]?.SetValueWithoutNotify(v[i]); if (valueLabels[i] != null) valueLabels[i].text = l.T("sys." + systems[i]) + " · " + v[i] + " / " + demands[i]+" kWh";if(meters[i]!=null)meters[i].style.width=Length.Percent(demands[i]>0?UnityEngine.Mathf.Clamp01((float)v[i]/demands[i])*100:100);if(notices[i]!=null){notices[i].text=v[i]<demands[i]?"Faltam "+(demands[i]-v[i])+" kWh para atender este sistema.":"Energia necessária reservada!";notices[i].EnableInClassList("energy-warning",v[i]<demands[i]);}} free.text = l.T("energy.free") + ": " + (campaign.EnergyPool - a.Total); start.SetEnabled(a.Total == campaign.EnergyPool); };
            var cards=Box(parent,"energy-cards");
            for (int i = 0; i < 3; i++)
            {
                int index = i; var card = Box(cards, "mini-energy-row");
                var heading=Box(card,"energy-card-heading");heading.Add(MissionVisualStyle.Icon(i==1?"lifeSupport":systems[i],54));
                valueLabels[i] = Text(heading, "", "mini-instruction");
                var track=Box(card,"mission-comparison-track");meters[i]=Box(track,"mission-comparison-fill");meters[i].style.backgroundColor=MissionVisualStyle.Sector(i==1?"lifeSupport":systems[i]);
                sliders[i] = new SliderInt(0, campaign.EnergyPool) { name = "energy-" + systems[i], tooltip = l.T("sys." + systems[i]) }; card.Add(sliders[i]);
                notices[i]=Text(card,"","small");
                sliders[i].RegisterValueChangedCallback(e => { var a = campaign.Allocation; int old = index == 0 ? a.shield : index == 1 ? a.life : a.greenhouse; int value = Math.Min(campaign.EnergyPool - a.Total + old, UnityEngine.Mathf.RoundToInt((float)e.newValue / rules.sliderStep) * rules.sliderStep); if (index == 0) a.shield = value; else if (index == 1) a.life = value; else a.greenhouse = value; campaign.SetAllocation(a); update(); });
            }
            parent.Add(free); free.AddToClassList("badge"); Text(parent, l.T("energy.budgetNote"), "small");
            var row = panel.Footer; Button(row, l.T("energy.close"), close, "ghost");
            start.name = "energy-confirm";
            Action refresh=()=>{demands[0]=campaign.Current.demand.shield;demands[1]=campaign.Current.demand.life;demands[2]=campaign.Current.demand.greenhouse;update();pool.text=l.T("energy.pool","n",campaign.EnergyPool);foreach(var slider in sliders)slider.highValue=campaign.EnergyPool;if(campaign.NativeHazards){battery.text=campaign.NativeDataReady?l.T("hazard.battery","n",campaign.Solar.ReserveKwh.ToString("0"),"max",campaign.Solar.CapacityKwh):l.T("hazard.loading");start.SetEnabled(campaign.NativeDataReady && campaign.Allocation.Total==campaign.EnergyPool);}};
            campaign.Changed+=refresh;parent.RegisterCallback<DetachFromPanelEvent>(_=>campaign.Changed-=refresh);
            start.text = l.T(campaign.Phase == Phase.Plan ? "energy.start" : "energy.save"); start.clicked += () => { if (campaign.Phase == Phase.Tasks || campaign.StartSol()) close(); }; row.Add(start); refresh();
        }
    }
}
