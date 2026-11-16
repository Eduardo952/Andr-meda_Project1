using Ares.UI;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Envelope follows the real alarm. One shield draw, no per-frame material instantiation.</summary>
    [DefaultExecutionOrder(220)]
    public sealed class MissionStormPresentation : MonoBehaviour
    {
        Renderer shield;MaterialPropertyBlock properties;GameFrontend frontend;ReferenceExplorer explorer;
        public float Strength {get;private set;} float phase;
        void Start()
        {
            frontend=GetComponent<GameFrontend>();explorer=GetComponent<ReferenceExplorer>();properties=new MaterialPropertyBlock();
            foreach(var part in GetComponentsInChildren<ReferenceWorldPart>())if(part.role=="magnetic_shield")shield=part.GetComponent<Renderer>();
        }
        void LateUpdate()
        {
            if(shield==null)return;
            if(properties==null)properties=new MaterialPropertyBlock();
            bool alarm=frontend.Campaign?.Demands.SolarActivity == true;
            Strength=Mathf.MoveTowards(Strength,alarm?1:0,Time.deltaTime/(alarm?2.5f:4));
            if(!explorer.reducedMotion)phase+=Time.deltaTime;
            shield.GetPropertyBlock(properties);properties.SetFloat("_Storm",Strength);properties.SetFloat("_StormTime",phase);
            properties.SetFloat("_ShieldIntegrity",(frontend.Campaign?.Integrity??100)/100f);shield.SetPropertyBlock(properties);
        }
    }
}
