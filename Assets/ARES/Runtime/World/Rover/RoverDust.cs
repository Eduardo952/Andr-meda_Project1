using UnityEngine;

namespace Ares.World.Rover
{
    /// <summary>One bounded world-space pool, emitted from the six contact patches using actual travel.</summary>
    public sealed class RoverDust : MonoBehaviour
    {
        ParticleSystem particles;
        Material material;
        float buildup, budget;
        public float Intensity { get; private set; }
        public int Emitted { get; private set; }
        public void Initialize()
        {
            particles = gameObject.AddComponent<ParticleSystem>();
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main; main.loop=true; main.playOnAwake=false; main.maxParticles=420;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.8f,1.7f);
            main.startSize=new ParticleSystem.MinMaxCurve(.18f,.44f); main.startSpeed=0; main.gravityModifier=0;
            main.startColor=new Color(.76f,.49f,.31f,.46f);
            var emission=particles.emission; emission.enabled=false;
            var shape=particles.shape; shape.enabled=false;
            var size=particles.sizeOverLifetime; size.enabled=true;
            size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.45f,1.5f),new Keyframe(1,2.4f)));
            var color=particles.colorOverLifetime; color.enabled=true;
            var fade=new Gradient(); fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)}); color.color=fade;
            material=new Material(Resources.Load<Shader>("ARES/Effects/CartoonDust"));
            var renderer=particles.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows=false;
        }
        public void Tick(RoverMotion motion, RoverModel model, RoverSettings settings, float dt, bool enabled, bool reducedMotion)
        {
            float speed=Mathf.Abs(motion.Speed);
            bool moving=enabled && speed>.15f && !motion.Obstructed;
            buildup=Mathf.MoveTowards(buildup,moving?1:0,dt/Mathf.Max(.1f,moving?settings.dustBuildSeconds:1));
            Intensity=moving?Mathf.Clamp01(speed/settings.maxSpeed)*Mathf.Lerp(.18f,1,buildup):0;
            if(!moving) { budget=0; return; }
            budget+=dt*settings.dustRatePerWheel*Intensity*(reducedMotion?.2f:1);
            int count=Mathf.FloorToInt(budget); if(count==0)return; budget-=count;
            for(int i=0;i<model.Wheels.Length;i++)
            {
                var p=model.Wheels[i].position-Vector3.up*(settings.wheelRadius-.08f);
                particles.Emit(new ParticleSystem.EmitParams {
                    position=p, velocity=-model.transform.forward*motion.Speed*.1f+Vector3.up*.35f,
                    startSize=Random.Range(.2f,.43f)*Mathf.Lerp(.6f,1.3f,Intensity)
                },count); Emitted+=count;
            }
            if(!particles.isPlaying)particles.Play();
        }
        public void Clear() { buildup=budget=Intensity=0; if(particles!=null)particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
        void OnDestroy() { if(material!=null)Destroy(material); }
    }
}
