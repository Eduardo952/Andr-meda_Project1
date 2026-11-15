using System.Collections.Generic;
using Ares.Data;
using Ares.Domain;
using Ares.UI;
using UnityEngine;

namespace Ares.World
{
    /// <summary>The original station rings: pending tasks only, within 22 m; no mission mutations.</summary>
    public sealed class ReferenceStationMarkers : MonoBehaviour
    {
        readonly List<(RoomData room,Transform root)> markers=new List<(RoomData,Transform)>();
        ReferenceExplorer explorer;
        GameFrontend frontend;
        void Start()
        {
            explorer=GetComponent<ReferenceExplorer>();frontend=GetComponent<GameFrontend>();
            foreach(var room in explorer.definition.layout.rooms)
            {
                var root=transform.Find(room.id+"_marker");if(root==null)continue;
                Vector3 pivot=room.Station;root.position=pivot;
                foreach(Transform child in root)child.localPosition=-pivot;
                markers.Add((room,root));
            }
        }
        void LateUpdate()
        {
            var campaign=frontend?.Campaign;
            foreach(var marker in markers)
            {
                bool pending=false;
                if(campaign!=null)
                {
                    if(marker.room.id=="control")pending=campaign.Phase==Phase.Plan;
                    bool any=false;
                    foreach(var task in campaign.Current.tasks)if(!campaign.Completed(task.id)){any=true;if(task.room==marker.room.id)pending=true;}
                    if(marker.room.id=="control"&&campaign.Phase==Phase.Tasks&&!any)pending=true;
                    if (campaign.Phase == Phase.Tasks && marker.room.id == "shield" && campaign.Demands.ShieldTaskAvailable && !campaign.Demands.ShieldRegulated) pending = true;
                    if (marker.room.id == campaign.HazardTarget) pending = true;
                }
                marker.root.gameObject.SetActive(!frontend.TutorialActive&&pending&&Vector3.Distance(explorer.PlayerPosition,marker.room.Station)<22);
                marker.root.localScale=Vector3.one*(explorer.reducedMotion?1:1+Mathf.Sin(Time.time*2)*.06f);
            }
        }
    }
}
