using Ares.Domain;
using UnityEngine.UIElements;
using Ares.UI;
using System.Reflection;

namespace Ares.Editor
{
    /// <summary>Only test fixtures may supply decisions on behalf of the player.</summary>
    internal static class GreenhouseCheckFixture
    {
        public static bool Complete(Campaign campaign, string id)
        {
            if (!GreenhouseSimulation.IsTask(id)) return campaign.FinishTask(id,true,10,0);
            while(!campaign.Greenhouse.Finished) ManualCycle(campaign,IrrigationLevel.Low,GreenhouseAdjustment.None);
            return campaign.FinishGreenhouseTask();
        }
        public static void ManualCycle(Campaign campaign,IrrigationLevel level,GreenhouseAdjustment adjustment)
        {
            var water=campaign.Watering;water.PickUp();
            if(!water.HasPlan && water.Plan(level,adjustment)!=GreenhouseDecisionError.None)throw new System.InvalidOperationException("Cannot prepare fixture cycle");
            water.Fill();for(int i=0;i<4;i++)water.Tick(i,true,false,3);
        }
        public static void Press(VisualElement root,string name)
        {
            var button=root.Q<Button>(name);
            if(button==null || !button.enabledInHierarchy) throw new System.InvalidOperationException("Missing/disabled greenhouse control: "+name);
            using(var evt=NavigationSubmitEvent.GetPooled()) { evt.target=button; button.SendEvent(evt); }
        }
        public static void CompleteThroughControls(VisualElement root, GameFrontend ui)
        {
            while(!ui.Campaign.Greenhouse.Finished)
            {
                if(!ui.Campaign.Watering.HasPlan){Press(root,"greenhouse-level-0");Press(root,"greenhouse-confirm");}
                ManualCycle(ui.Campaign,IrrigationLevel.Low,GreenhouseAdjustment.None);
                Reopen(ui);
            }
        }
        public static void Reopen(GameFrontend ui)
        {
            foreach(var task in ui.Campaign.Current.tasks)if(task.room=="greenhouse")
            {
                if(ui.Campaign.OpenTask==null)ui.Campaign.BeginTask(task.id,task.room);
                typeof(GameFrontend).GetMethod("OpenTask",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(ui,new object[]{task.id});break;
            }
        }
    }
}
