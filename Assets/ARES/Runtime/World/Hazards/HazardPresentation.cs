using Ares.Domain;
using Ares.Domain.Hazards;
using Ares.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.World
{
    /// <summary>Presentation only. Cannot activate hazards, award resources or decide collapse.</summary>
    [DefaultExecutionOrder(80)]
    public sealed class HazardPresentation : MonoBehaviour
    {
        GameFrontend frontend;ReferenceExplorer explorer;AstronautAnimator actor;Campaign previous;
        int rescueVersion;bool moved;VisualElement tint;
        void Start(){frontend=GetComponent<GameFrontend>();explorer=GetComponent<ReferenceExplorer>();actor=GetComponentInChildren<AstronautAnimator>();}
        void LateUpdate()
        {
            var c=frontend?.Campaign;
            if(c==null || !c.NativeHazards){tint?.RemoveFromHierarchy();return;}
            if(c!=previous){previous=c;rescueVersion=0;moved=false;}
            var root=GetComponent<UIDocument>().rootVisualElement;
            if(tint==null || tint.parent!=root){tint=new VisualElement {name="hazard-vignette",pickingMode=PickingMode.Ignore};tint.style.position=Position.Absolute;tint.style.left=tint.style.right=tint.style.top=tint.style.bottom=0;root.Insert(0,tint);}
            bool fallen=c.Condition.Stage==CrewCondition.Incapacitated;
            bool warning=c.Condition.Stage==CrewCondition.Warning;
            tint.style.backgroundColor=fallen?new Color(.02f,.03f,.06f,.4f):warning?new Color(.38f,.22f,.03f,.1f):Color.clear;
            if(fallen && actor!=null){
                float t=c.Condition.CollapseSeconds;
                if(t>=c.HazardSettings.collapseAnimationSeconds){if(!moved){explorer.RecoverAtBed();moved=true;}actor.transform.position=explorer.Dorm.Beds[0]+new Vector3(0,1.4f,-1.1f);actor.transform.rotation=Quaternion.Euler(90,0,0);actor.Apply(default);}
                else {
                    float kneel=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-1)/1.5f));float lie=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-2.5f)/1.5f));
                    actor.transform.position=explorer.PlayerPosition+Vector3.up*(.08f*lie);
                    actor.transform.rotation=Quaternion.Euler(75*lie,actor.transform.eulerAngles.y,0);
                    actor.Apply(new AstronautPose {bodyHeight=-.45f*kneel,bodyPitch=.35f*kneel,leftArm=-.8f*kneel,rightArm=-.8f*kneel,leftLeg=-.9f*kneel,rightLeg=-.9f*kneel});
                }
            }
            if(c.RescueVersion!=rescueVersion){rescueVersion=c.RescueVersion;explorer.RecoverAtBed();moved=false;}
        }
        void OnDestroy(){tint?.RemoveFromHierarchy();}
    }
}
