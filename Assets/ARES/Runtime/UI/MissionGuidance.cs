using Ares.Data;
using Ares.Domain;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Tour and room speech are presentation only, deliberately separate from Campaign.</summary>
    internal sealed class TourProgress { public int Step; public bool Dismissed; }
    internal sealed class MissionGuidance
    {
        static readonly string[] Tour = { "control", "observatory", "shield", "lifeSupport", "greenhouse", "solar" };
        readonly GameDefinition definition;
        readonly Localization locale;
        readonly VisualElement panel, speech;
        readonly Label tourText, line, direction;
        readonly TourProgress progress;
        string lastRoom;
        float remaining;
        public MissionGuidance(VisualElement root, GameDefinition definition, Localization locale, TourProgress progress)
        {
            this.definition=definition; this.locale=locale; this.progress=progress;
            panel=Box(root,"walk-panel panel"); tourText=Text(panel,""); Button(panel,"×",()=>{progress.Dismissed=true; panel.style.display=DisplayStyle.None;},"ghost");
            speech=Box(root,"speech-panel panel"); Text(speech,locale.T("bubble.name"),"heading"); line=Text(speech,"");
            direction=Text(root,"","direction badge"); direction.pickingMode=PickingMode.Ignore;
        }
        public void Update(Campaign campaign,Vector3 player,bool blocked,float seconds)
        {
            RoomData entering=null;
            foreach(var room in definition.layout.rooms) if(WorldNavigation.Inside(player.x,-player.z,room,-.6f)) {entering=room;break;}
            string roomId=entering?.id;
            if(!blocked && roomId!=null && roomId!=lastRoom)
            {
                lastRoom=roomId; line.text=locale.T("bubble.room."+roomId); remaining=4.5f;
                if(progress.Step<Tour.Length && Tour[progress.Step]==roomId) progress.Step++;
            }
            if(!blocked) remaining=Mathf.Max(0,remaining-seconds);
            speech.style.display=!blocked && remaining>0?DisplayStyle.Flex:DisplayStyle.None;
            string target=campaign.Phase==Phase.Plan?"control":null;
            if(campaign.Phase==Phase.Tasks) { target="control"; foreach(var task in campaign.Current.tasks) if(!campaign.Completed(task.id)) { target=task.room;break; } }
            if(campaign.HazardTarget != null) target=campaign.HazardTarget;
            panel.style.display=!blocked && !progress.Dismissed && progress.Step<Tour.Length && string.IsNullOrEmpty(campaign.Alarm) && (target==null || target==Tour[progress.Step])?DisplayStyle.Flex:DisplayStyle.None;
            if(progress.Step<Tour.Length) tourText.text=locale.T("refine.walk."+Tour[progress.Step]);
            direction.style.display=!blocked && target!=null?DisplayStyle.Flex:DisplayStyle.None;
            if(target!=null) foreach(var room in definition.layout.rooms) if(room.id==target)
            {
                var delta=room.Station-player; var local=Camera.main.transform.InverseTransformDirection(delta);
                string arrow=Mathf.Abs(local.x)>Mathf.Abs(local.z)?local.x>0?"→":"←":local.z>0?"↑":"↓";
                direction.text=arrow+" "+locale.T("rooms."+target)+" · "+Mathf.RoundToInt(delta.magnitude)+" m"; break;
            }
        }
    }
}
