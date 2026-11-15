using UnityEngine;

namespace Ares.World
{
    /// <summary>One bounded dust pool for grains and turbulent storm clouds; never mutates weather or resources.</summary>
    public sealed class MartianWind : MonoBehaviour
    {
        public const int Capacity = 960;
        readonly ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[Capacity];
        ReferenceExplorer explorer;
        Ares.UI.GameFrontend frontend;
        ParticleSystem particles;
        Material material;
        MeshCollider terrain;
        float budget, gustTime;
        Vector3 previous;
        bool positioned;
        public int LiveParticles => particles != null ? particles.particleCount : 0;
        public float StormStrength { get; private set; }
        void Awake()
        {
            explorer = GetComponent<ReferenceExplorer>(); frontend = GetComponent<Ares.UI.GameFrontend>();
            foreach (var collider in GetComponentsInChildren<MeshCollider>())
                if (collider.name.StartsWith("landscape_") && collider.bounds.size.x > 300) { terrain = collider; break; }
            var effect = new GameObject("Martian wind / airborne dust"); effect.transform.SetParent(transform, false);
            particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.maxParticles = Capacity; main.loop = true; main.playOnAwake = false;
            main.startSize3D = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.startSpeed = 0;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3, 5);
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape; shape.enabled = false;
            var color = particles.colorOverLifetime; color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] {new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[] {new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(.6f,.7f),new GradientAlphaKey(0,1)});
            color.color = fade;
            material = new Material(Resources.Load<Shader>("ARES/Effects/WindDust"));
            var renderer = particles.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
        }
        bool Sheltered(Vector3 position)
            => HabitatShelter.Contains(explorer.definition.layout, position);
        float Ground(Vector3 point)
        { return terrain != null && terrain.Raycast(new Ray(point + Vector3.up * 30,Vector3.down),out var hit,100) ? hit.point.y : 0; }
        void LateUpdate()
        {
            var settings = explorer.windSettings;
            if (!settings.enabled || (frontend != null && frontend.BlocksWorld)) { Clear(); return; }
            var weather = new Ares.Domain.DustStormWeather(frontend?.Campaign?.Demands);
            StormStrength = Mathf.MoveTowards(StormStrength, weather.Strength, Time.deltaTime * .7f);
            Vector3 center = explorer.ControlledPosition;
            if (positioned && Vector3.Distance(previous,center)>settings.radius) Clear();
            previous = center; positioned = true;
            float dt = Mathf.Min(Time.deltaTime,.05f); gustTime += dt;
            float gust = .7f + Mathf.PerlinNoise(gustTime*.09f, .37f)*.65f;
            bool storm = StormStrength > .02f;
            Vector3 direction = settings.direction.sqrMagnitude>.001f ? settings.direction.normalized : Vector3.right;
            direction = Quaternion.Euler(0, Mathf.Sin(gustTime*.17f)*12, 0)*direction;
            float speed = Mathf.Lerp(settings.speed * gust, Mathf.Max(settings.speed, weather.SpeedMetersPerSecond), StormStrength) * (explorer.reducedMotion ? .45f : 1);
            float radius = Mathf.Lerp(settings.radius, Mathf.Max(32, settings.radius), StormStrength);
            int count = particles.GetParticles(buffer);
            for (int i=0;i<count;i++)
            {
                var p = buffer[i];
                if (Sheltered(p.position) || Vector3.Distance(p.position,center)>radius*1.6f) p.remainingLifetime=0;
                else
                {
                    float swirl = Mathf.Sin(gustTime * 2.5f + p.randomSeed % 17) * StormStrength;
                    var crosswind = new Vector3(-direction.z, 0, direction.x);
                    p.velocity = direction * speed + crosswind * swirl * 2.6f + Vector3.up * swirl * .8f;
                }
                buffer[i]=p;
            }
            particles.SetParticles(buffer,count);
            budget += dt * Mathf.Lerp(settings.particlesPerSecond * gust, 260, StormStrength) * (explorer.reducedMotion ? .4f : 1);
            int emit = Mathf.Min(18, Mathf.FloorToInt(budget)); budget = Mathf.Min(18, budget - emit);
            for (int i=0;i<emit;i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                Vector3 point = center + new Vector3(offset.x,0,offset.y) - direction * radius * StormStrength * .35f;
                if (Sheltered(point)) continue;
                bool grain = Random.value < (storm ? .65f : .8f);
                point.y = Ground(point) + Random.Range(.25f, Mathf.Lerp(settings.height, 13, StormStrength));
                var color = Color.Lerp(new Color(.74f,.43f,.26f),new Color(.94f,.69f,.42f),Random.value);
                color.a = Mathf.Lerp(settings.opacity * (grain ? 1 : .24f), grain ? .82f : .48f, StormStrength);
                float size = grain ? Random.Range(.07f, .18f) : Mathf.Lerp(Random.Range(.7f,1.6f), Random.Range(5,11), StormStrength);
                particles.Emit(new ParticleSystem.EmitParams {position=point, velocity=direction*speed,
                    startColor=color, startSize3D=new Vector3(size * (grain ? 1 : 1.6f), size, size),
                    rotation = Random.Range(-25f,25f), startLifetime = storm ? Random.Range(2.5f,4.5f) : Random.Range(3,5)},1);
            }
            if (!particles.isPlaying) particles.Play();
        }
        void Clear() { budget=0; positioned=false; if(particles!=null)particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
        void OnDisable() => Clear();
        void OnDestroy() { if(material!=null)Destroy(material);if(particles!=null)Destroy(particles.gameObject); }
    }
}
