using Ares.Domain;
using Ares.Domain.Hazards;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    internal sealed class HazardHud
    {
        readonly VisualElement panel;readonly Label state,reading,remedy,source;readonly Button kit,oxygen;
        readonly Campaign campaign;readonly Localization l;
        public bool WarningVisible {get;private set;}
        float nextRefresh;
        public HazardHud(VisualElement root,Campaign campaign,Localization l)
        {
            this.campaign=campaign;this.l=l;panel=Box(root,"hazard-status");panel.name="hazard-status";
            state=Text(panel,"","heading");reading=Text(panel,"","small");remedy=Text(panel,"","small");source=Text(panel,"","small");
            var row=Box(panel,"hazard-actions");kit=Button(row,"",()=>campaign.UseMedKit(),"ghost");oxygen=Button(row,"",()=>campaign.UseOxygenCanister(),"ghost");
        }
        public void Tick(bool visible)
        {
            visible&=campaign.NativeHazards && campaign.Phase==Phase.Tasks;
            panel.style.display=visible?DisplayStyle.Flex:DisplayStyle.None;WarningVisible=false;if(!visible)return;
            var c=campaign.Condition;WarningVisible=c.Stage==CrewCondition.Warning;
            if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+.15f;
            HazardEvent active=null;foreach(var e in campaign.NativePlan.events)if(e.damaging && campaign.SolElapsedSeconds>=e.startsAt-60 && campaign.SolElapsedSeconds<=e.startsAt+e.duration){active=e;break;}
            state.text=l.T("hazard.state."+c.Stage)+(active==null?"":" · "+l.T("hazard.kind."+active.kind));
            reading.text=l.T("hazard.reading","dose",campaign.Dose,"rate",campaign.DoseRate.ToString("0.0"),"o2",campaign.Oxygen,"battery",campaign.Solar.ReserveKwh.ToString("0"));
            if(c.Stage==CrewCondition.Incapacitated)remedy.text=l.T("hazard.rescue");
            else if(c.Stage==CrewCondition.Warning)remedy.text=l.T("hazard.remedy."+c.Cause);
            else if(active!=null && campaign.SolElapsedSeconds<active.startsAt)remedy.text=l.T("hazard.shelter","n",Mathf.CeilToInt(active.startsAt-campaign.SolElapsedSeconds));
            else remedy.text=campaign.RadioBlackout?l.T("hazard.radio"):campaign.SolRemainingSeconds<=0?l.T("hazard.seal"):l.T("hazard.rest");
            if(c.Stage<CrewCondition.Warning && campaign.NeedsPanelCleaning)remedy.text=l.T("hazard.clean");
            source.text=l.T("hazard.source."+campaign.NativePlan.dataProvenance)+" · "+campaign.NativePlan.earthDate+" · "+l.T("hazard.mode."+campaign.NativePlan.mode)+(active==null?"":" · "+active.api+" / "+active.field);
            kit.text=l.T("hazard.kit","n",campaign.MedKits);oxygen.text=l.T("hazard.oxygen","n",campaign.OxygenCanisters);
            kit.SetEnabled(campaign.MedKits>0 && campaign.Dose>0 && c.Stage!=CrewCondition.Incapacitated);oxygen.SetEnabled(campaign.OxygenCanisters>0 && campaign.Oxygen<100 && c.Stage!=CrewCondition.Incapacitated);
            panel.EnableInClassList("hazard-warning",c.Stage==CrewCondition.Warning);panel.EnableInClassList("hazard-collapse",c.Stage==CrewCondition.Incapacitated);
        }
    }
}
