using System.Collections.Generic;
using Ares.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ares.World
{
    /// <summary>Presentation from BaseLandmarks.tsx. Reads campaign progress; never awards resources.</summary>
    public sealed class ReferenceLandmarks : MonoBehaviour
    {
        sealed class Part
        {
            public ReferenceWorldPart source;
            public Renderer renderer;
            public Color color;
            public Vector3 center;
        }
        readonly List<Part> parts = new List<Part>();
        ReferenceExplorer explorer;
        GameFrontend frontend;
        MaterialPropertyBlock properties;
        Light treeLight;
        float time, health = 1;
        public int AnimatedParts => parts.Count;

        void Start()
        {
            explorer = GetComponent<ReferenceExplorer>(); frontend = GetComponent<GameFrontend>();
            properties = new MaterialPropertyBlock();
            foreach (var source in GetComponentsInChildren<ReferenceWorldPart>(true))
            {
                if (!source.role.StartsWith("hub_") && source.role != "magnetic_shield") continue;
                var renderer = source.GetComponent<Renderer>();
                parts.Add(new Part { source=source, renderer=renderer, color=renderer.sharedMaterial.GetColor("_BaseColor"), center=renderer.bounds.center });
                if(source.role!="hub_canopy") { renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false; }
            }
            treeLight = new GameObject("Hub tree / warm light").AddComponent<Light>();
            treeLight.transform.SetParent(transform,false); treeLight.transform.localPosition=new Vector3(0,1.2f,0);
            treeLight.type=LightType.Point; treeLight.range=7; treeLight.intensity=2.2f;
            ColorUtility.TryParseHtmlString("#ffe1aa",out var warm); treeLight.color=warm; treeLight.shadows=LightShadows.None;
        }
        void LateUpdate()
        {
            bool reduced=explorer.reducedMotion;
            if(!frontend.BlocksWorld&&!reduced)time+=Mathf.Min(Time.deltaTime,.05f);
            float target=frontend.Campaign!=null&&frontend.Campaign.AllTasksDone?1:.55f;
            health=Mathf.Lerp(health,target,1-Mathf.Exp(-2*Mathf.Min(Time.deltaTime,.05f)));
            ColorUtility.TryParseHtmlString("#c1b66d",out var tired);
            foreach(var part in parts)
            {
                int i=part.source.instance; var t=part.source.transform;
                switch(part.source.role)
                {
                    case "hub_canopy":
                        t.localScale=Vector3.one*(.9f+health*.1f);
                        var color=Color.Lerp(part.color,tired,(1-health)*.7f);
                        float pulse=reduced?0:Mathf.Sin(time*1.6f)*.5f+.5f;
                        properties.Clear();properties.SetColor("_BaseColor",color);
                        properties.SetColor("_EmissionColor",color*(.06f+health*(.1f+pulse*.12f)));
                        part.renderer.SetPropertyBlock(properties); break;
                    case "hub_hologram":
                        float initial=i*Mathf.PI/2+Mathf.PI/4,a=initial+(reduced?0:time*.15f);
                        var rotation=ReferenceSpace.Euler(0,a-initial,0);
                        var origin=new Vector3(Mathf.Sin(initial)*3.1f,3,-Mathf.Cos(initial)*3.1f);
                        var position=new Vector3(Mathf.Sin(a)*3.1f,3+(reduced?0:Mathf.Sin(time*1.2f+i)*.18f),-Mathf.Cos(a)*3.1f);
                        t.SetPositionAndRotation(position-rotation*origin,rotation); break;
                    case "hub_firefly":
                        float angle=i*2.39f+(reduced?0:time*(.18f+(i%4)*.05f)),radius=1.4f+(i%5)*.45f;
                        float initialScale=.6f+(Mathf.Sin(i*1.7f)*.5f+.5f);
                        float scale=(.6f+(reduced?.4f:Mathf.Sin(time*3+i*1.7f)*.5f+.5f)*health)/initialScale;
                        var p=new Vector3(Mathf.Sin(angle)*radius,1.6f+(i%6)*.55f+(reduced?0:Mathf.Sin(time*1.3f+i)*.3f),-Mathf.Cos(angle)*radius);
                        t.localScale=Vector3.one*scale;t.localPosition=p-part.center*scale; break;
                    case "magnetic_shield":
                        properties.Clear();properties.SetFloat("_Phase",time);part.renderer.SetPropertyBlock(properties);break;
                }
            }
        }
        void OnDestroy() { if(treeLight!=null)Destroy(treeLight.gameObject); }
    }
}
