using UnityEngine;

namespace Ares.World
{
    /// <summary>Bounded ambient dust around the controlled actor. No gameplay/weather rules or wheel dust.</summary>
    public sealed class MartianWind : MonoBehaviour
    {
        const int Capacity = 260;
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
        void Awake()
        {
            explorer = GetComponent<ReferenceExplorer>(); frontend = GetComponent<Ares.UI.GameFrontend>();
            foreach (var collider in GetComponentsInChildren<MeshCollider>())
                if (collider.name.StartsWith("landscape_") && collider.bounds.size.x > 300) { terrain = collider; break; }
            var effect = new GameObject("Martian wind / airborne dust"); effect.transform.SetParent(transform, false);
            particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main; main.maxParticles = Capacity; main.loop = true; main.playOnAwake = false;
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
        {
            foreach (var room in explorer.definition.layout.rooms)
                if (room.shape != "park" && WorldNavigation.Inside(position.x, -position.z, room, .8f)) return true;
            return false;
        }
        float Ground(Vector3 point)
        { return terrain != null && terrain.Raycast(new Ray(point + Vector3.up * 30,Vector3.down),out var hit,100) ? hit.point.y : 0; }
        void LateUpdate()
        {
            var settings = explorer.windSettings;
            if (!settings.enabled || (frontend != null && frontend.BlocksWorld)) { Clear(); return; }
            Vector3 center = explorer.ControlledPosition;
            if (positioned && Vector3.Distance(previous,center)>settings.radius) Clear();
            previous = center; positioned = true;
            float dt = Mathf.Min(Time.deltaTime,.05f); gustTime += dt;
            float gust = .7f + Mathf.PerlinNoise(gustTime*.09f, .37f)*.65f;
            Vector3 direction = settings.direction.sqrMagnitude>.001f ? settings.direction.normalized : Vector3.right;
            direction = Quaternion.Euler(0, Mathf.Sin(gustTime*.17f)*12, 0)*direction;
            float speed = settings.speed * gust * (explorer.reducedMotion ? .45f : 1);
            int count = particles.GetParticles(buffer);
            for (int i=0;i<count;i++)
            {
                var p = buffer[i];
                if (Sheltered(p.position) || Vector3.Distance(p.position,center)>settings.radius*1.6f) p.remainingLifetime=0;
                else p.velocity = direction*speed + Vector3.up*Mathf.Sin(gustTime*1.3f+p.randomSeed%17)*.13f;
                buffer[i]=p;
            }
            particles.SetParticles(buffer,count);
            budget += dt*settings.particlesPerSecond*gust*(explorer.reducedMotion?.2f:1);
            int emit = Mathf.Min(6,Mathf.FloorToInt(budget)); budget -= emit;
            for (int i=0;i<emit;i++)
            {
                Vector2 offset = Random.insideUnitCircle*settings.radius;
                Vector3 point = center + new Vector3(offset.x,0,offset.y);
                if (Sheltered(point)) continue;
                bool grain = Random.value < .8f;
                point.y = Ground(point) + Random.Range(.25f,settings.height);
                var color = Color.Lerp(new Color(.74f,.43f,.26f),new Color(.94f,.69f,.42f),Random.value);
                color.a = settings.opacity*(grain?1:.24f);
                particles.Emit(new ParticleSystem.EmitParams {position=point, velocity=direction*speed,
                    startColor=color, startSize=grain?Random.Range(.07f,.16f):Random.Range(.7f,1.6f)},1);
            }
            if (!particles.isPlaying) particles.Play();
        }
        void Clear() { budget=0; positioned=false; if(particles!=null)particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); }
        void OnDisable() => Clear();
        void OnDestroy() { if(material!=null)Destroy(material);if(particles!=null)Destroy(particles.gameObject); }
    }
}
