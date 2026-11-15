using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ares.World
{
    /// <summary>Fades only opaque greenhouse meshes that hide the caregiver from the camera.</summary>
    public sealed class GreenhouseOcclusion : IDisposable
    {
        sealed class Candidate
        {
            public MeshRenderer renderer;
            public Material original;
            public MeshSightline sightline;
            public float alpha=1, blockedSeconds, clearSeconds;
            public bool faded, transparent;
        }
        readonly List<Candidate> candidates=new List<Candidate>();
        readonly Dictionary<Material,Material> transparent=new Dictionary<Material,Material>();
        readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        public GreenhouseOcclusion(Transform root)
        {
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!renderer.enabled || !renderer.TryGetComponent<MeshFilter>(out var filter))continue;
                var mesh=filter.sharedMesh;
                if(mesh==null || renderer.GetComponentInParent<AstronautAnimator>()!=null)continue;
                // Renderer bounds may not be initialized during Awake; imported mesh bounds are stable.
                var p=renderer.transform.TransformPoint(mesh.bounds.center);var material=renderer.sharedMaterial;
                if(p.x<32 || p.x>72 || Mathf.Abs(p.z)>16 || p.y<.5f || p.y>7 || material==null || !material.HasProperty("_Surface") || material.GetFloat("_Surface")!=0)continue;
                candidates.Add(new Candidate {renderer=renderer,original=material,sightline=new MeshSightline(mesh)});
            }
        }
        Material Transparent(Material original)
        {
            if(transparent.TryGetValue(original,out var found))return found;
            var copy=new Material(original){name=original.name+" (care visibility)",renderQueue=3000};
            copy.SetFloat("_Surface",1);copy.SetFloat("_ZWrite",0);copy.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);copy.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            copy.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");copy.SetShaderPassEnabled("ShadowCaster",false);
            transparent.Add(original,copy);return copy;
        }
        public void Update(Camera camera,Vector3 player,bool enabled)
        {
            Vector3 target=player+Vector3.up*1.25f;
            Vector3 origin=camera!=null?camera.transform.position:target;
            Vector3 direction=target-origin;
            float distance=direction.magnitude;
            var ray=new Ray(origin,direction.normalized);
            var viewport=camera!=null?camera.WorldToViewportPoint(target):Vector3.zero;
            bool visible=enabled && camera!=null && distance>.3f && viewport.z>0 && viewport.x>=0 && viewport.x<=1 && viewport.y>=0 && viewport.y<=1;
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            foreach(var item in candidates)
            {
                var renderer=item.renderer;
                if(renderer==null)continue;
                // Bounds are only a cheap rejection test: empty space between leaves must not hide a plant.
                bool blocked=visible && renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.bounds.IntersectRay(ray,out float hit)
                    && hit<distance-.3f && item.sightline.Intersects(renderer.transform,origin,target-direction.normalized*.3f);
                item.blockedSeconds=blocked?item.blockedSeconds+dt:0;
                item.clearSeconds=blocked?0:item.clearSeconds+dt;
                if(!enabled || camera==null) item.faded=false;
                else if(item.blockedSeconds>=.08f) item.faded=true;
                else if(item.clearSeconds>=.15f) item.faded=false;
                item.alpha=Mathf.MoveTowards(item.alpha,item.faded?.35f:1,dt*4);
                if(item.alpha<1 && !item.transparent){renderer.sharedMaterial=Transparent(item.original);item.transparent=true;}
                if(!item.transparent)continue;
                renderer.GetPropertyBlock(properties);
                Color color=properties.HasColor("_BaseColor")?properties.GetColor("_BaseColor"):item.original.GetColor("_BaseColor");
                color.a=item.alpha*item.original.GetColor("_BaseColor").a;properties.SetColor("_BaseColor",color);renderer.SetPropertyBlock(properties);
                if(item.alpha>=1){renderer.sharedMaterial=item.original;item.transparent=false;}
            }
        }
        public void Dispose()
        {
            foreach(var item in candidates)if(item.renderer!=null && item.transparent)
            {item.renderer.sharedMaterial=item.original;item.renderer.GetPropertyBlock(properties);var color=properties.HasColor("_BaseColor")?properties.GetColor("_BaseColor"):item.original.GetColor("_BaseColor");color.a=item.original.GetColor("_BaseColor").a;properties.SetColor("_BaseColor",color);item.renderer.SetPropertyBlock(properties);}
            foreach(var material in transparent.Values)UnityEngine.Object.Destroy(material);
        }
    }
}
