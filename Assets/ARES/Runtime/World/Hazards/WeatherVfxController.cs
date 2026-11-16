using Ares.Domain;
using Ares.Domain.Hazards;
using Ares.UI;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Fixed Shuriken pools. NASA plan is input only; visuals never create a gameplay event.</summary>
    public sealed class WeatherVfxController:MonoBehaviour
    {
        const int Count=7,Capacity=192;
        readonly ParticleSystem[] pools=new ParticleSystem[Count];readonly Material[] materials=new Material[Count];
        readonly float[] targets=new float[Count],strengths=new float[Count];GameFrontend frontend;ReferenceExplorer explorer;
        public int ParticleCount {get {int count=0;foreach(var p in pools)if(p!=null)count+=p.particleCount;return count;}}
        public float Strength(HazardKind kind)=>strengths[(int)kind];
        void Start()
        {
            frontend=GetComponent<GameFrontend>();explorer=GetComponent<ReferenceExplorer>();
            for(int i=0;i<Count;i++) {
                if(i==(int)HazardKind.Dust)continue; // existing renderer owns all airborne Martian dust.
                var child=new GameObject("Hazard / "+(HazardKind)i);child.transform.SetParent(transform,false);
                var p=child.AddComponent<ParticleSystem>();p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);pools[i]=p;
                var main=p.main;main.playOnAwake=false;main.maxParticles=Capacity;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=2.2f;main.startSpeed=.1f;main.startSize=i==(int)HazardKind.Cold?.045f:.12f;
                main.startColor=Palette((HazardKind)i);p.useAutoRandomSeed=false;p.randomSeed=(uint)(319+i*83);
                var emission=p.emission;emission.rateOverTime=0;
                var shape=p.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=i==(int)HazardKind.Cold?7:3;
                var velocity=p.velocityOverLifetime;velocity.enabled=true;velocity.y=i==(int)HazardKind.Cold?-.18f:.22f;
                var color=p.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.6f,.25f),new GradientAlphaKey(0,1)});color.color=gradient;
                materials[i]=new Material(Resources.Load<Shader>("ARES/Effects/WindDust"));var renderer=p.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=materials[i];renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        public static Color Palette(HazardKind kind)=>kind==HazardKind.Cold?new Color(.6f,.85f,1,.35f):kind==HazardKind.Radiation?new Color(.65f,.5f,.95f,.25f):kind==HazardKind.Cme?new Color(.35f,.8f,.9f,.35f):kind==HazardKind.Flare?new Color(1,.65f,.25f,.35f):kind==HazardKind.LowOxygen?new Color(.6f,.8f,.9f,.2f):new Color(.9f,.75f,.3f,.35f);
        void Update()
        {
            var c=frontend?.Campaign;for(int i=0;i<Count;i++)targets[i]=0;
            bool running=c?.NativeHazards==true && c.Phase==Phase.Tasks && !frontend.HazardPresentationPaused;
            if(running && c.NativePlan!=null) {
                foreach(var e in c.NativePlan.events)if(c.SolElapsedSeconds>=e.startsAt && c.SolElapsedSeconds<=e.startsAt+e.duration)targets[(int)e.kind]=(int)e.tier/3f;
                if(c.Oxygen<=c.HazardSettings.cautionOxygen)targets[(int)HazardKind.LowOxygen]=c.Oxygen<=c.HazardSettings.warningOxygen?1:.5f;
                if(c.Grid.OfflineSystem!=null)targets[(int)HazardKind.Power]=1;
            }
            bool indoor=HabitatShelter.Contains(explorer.definition.layout,explorer.PlayerPosition);
            for(int i=0;i<Count;i++) {
                strengths[i]=Mathf.MoveTowards(strengths[i],targets[i],Time.deltaTime*.7f);var p=pools[i];if(p==null)continue;
                p.transform.position=explorer.PlayerPosition+Vector3.up*1.2f;
                if(!running){if(p.isPlaying)p.Pause();continue;}
                var emission=p.emission;emission.rateOverTime=strengths[i]*55*(explorer.reducedMotion?.2f:1)*(indoor && i==(int)HazardKind.Cold?.25f:1);
                if(!p.isPlaying)p.Play();
            }
        }
        void OnDestroy(){foreach(var m in materials)if(m!=null)Destroy(m);foreach(var p in pools)if(p!=null)Destroy(p.gameObject);}
    }
}
