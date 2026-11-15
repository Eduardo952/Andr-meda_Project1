using UnityEngine;

namespace Ares.World
{
    /// <summary>Small pooled cartoon puffs. Presentation consumes actual movement, never drives locomotion.</summary>
    public sealed class SprintDust : MonoBehaviour
    {
        ParticleSystem particles;
        Material material;
        Vector3 previousPosition;
        float distance;
        bool hasPosition, leftFoot;
        public int EmittedPuffs { get; private set; }
        public int LiveParticles => particles != null ? particles.particleCount : 0;

        void Awake()
        {
            var effect = new GameObject("Sprint dust (pooled)");
            effect.transform.SetParent(transform, false);
            particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false; main.playOnAwake = false; main.maxParticles = 32;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.38f, .6f);
            main.startSize = new ParticleSystem.MinMaxCurve(.22f, .36f);
            main.startSpeed = 0; main.gravityModifier = 0;
            main.startRotation = new ParticleSystem.MinMaxCurve(-.5f, .5f);
            main.startColor = new Color(1, .94f, .81f, .7f);
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape; shape.enabled = false;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = .38f;
            var size = particles.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1,
                new AnimationCurve(new Keyframe(0, .45f), new Keyframe(.4f, 1), new Keyframe(1, 1.35f)));
            var color = particles.colorOverLifetime; color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .1f), new GradientAlphaKey(0, 1) });
            color.color = fade;
            material = new Material(Resources.Load<Shader>("ARES/Effects/CartoonDust"));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard; renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public void Tick(Vector3 position, float heading, bool running, bool reducedMotion)
        {
            float moved = hasPosition ? Vector3.Distance(position, previousPosition) : 0;
            previousPosition = position; hasPosition = true;
            if (!running || moved > 2) { distance = 0; return; }
            distance += moved;
            float spacing = reducedMotion ? 1.6f : .8f;
            if (distance < spacing) return;
            distance %= spacing; leftFoot = !leftFoot;
            var forward = new Vector3(Mathf.Sin(heading), 0, -Mathf.Cos(heading));
            var side = new Vector3(forward.z, 0, -forward.x);
            var puff = new ParticleSystem.EmitParams {
                position = position - forward * .18f + side * (leftFoot ? .12f : -.12f) + Vector3.up * .12f
            };
            if (reducedMotion) puff.startSize = .2f;
            particles.Emit(puff, 1); EmittedPuffs++;
            // Emit() does not start simulation on an initially stopped particle system.
            if (!particles.isPlaying) particles.Play();
        }
        void OnDisable()
        {
            hasPosition = false; distance = 0;
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (particles != null) Destroy(particles.gameObject);
        }
    }
}
