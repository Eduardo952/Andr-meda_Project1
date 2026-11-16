using Ares.UI;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Ares.World
{
    /// <summary>Reference Sky.tsx/solClock.ts palette and sun path, driven by the existing mission clock.</summary>
    [DefaultExecutionOrder(110)]
    public sealed class ReferenceEnvironment : MonoBehaviour
    {
        Material sky, previousSky;
        Light sun, fill;
        GameFrontend frontend;
        ReferenceExplorer explorer;
        float stormStrength;
        public float StormStrength => stormStrength;
        readonly Stack<(bool fog,float near,float far)> fogStack=new Stack<(bool,float,float)>();
        public float Daylight { get; private set; }
        void Start()
        {
            frontend = GetComponent<GameFrontend>();
            explorer = GetComponent<ReferenceExplorer>();
            var shader = Resources.Load<Shader>("ARES/Effects/ReferenceSky");
            sky = new Material(shader) { name = "ARES reference Martian sky" };
            previousSky = RenderSettings.skybox; RenderSettings.skybox = sky;
            if (Camera.main != null) Camera.main.clearFlags = CameraClearFlags.Skybox;
            foreach (var light in FindObjectsByType<Light>())
                if (light.type == LightType.Directional && light.name == "Sun") { sun = light; break; }
            fill = new GameObject("Reference cool fill").AddComponent<Light>();
            fill.transform.SetParent(transform, false); fill.type = LightType.Directional;
            fill.transform.rotation = Quaternion.LookRotation(-new Vector3(12,8,18));
            fill.color = Hex("#a3c6ff"); fill.intensity = .32f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 65; RenderSettings.fogEndDistance = 230;
            Update();
            RenderPipelineManager.beginCameraRendering+=BeginCamera;
            RenderPipelineManager.endCameraRendering+=EndCamera;
        }
        void Update()
        {
            if (sky == null) return;
            float fraction = frontend?.Campaign?.SolFraction ?? .35f;
            float a = 2 * Mathf.PI * (fraction - .25f);
            Vector3 direction = new Vector3(-Mathf.Cos(a), Mathf.Sin(a), -.35f).normalized;
            float t = Mathf.Clamp01((direction.y + .12f) / .4f);
            Daylight = t*t*(3-2*t);
            bool emergency=!string.IsNullOrEmpty(frontend?.Campaign?.Alarm);
            var weather = Ares.Domain.DustStormWeather.ForCampaign(frontend?.Campaign);
            stormStrength = Mathf.MoveTowards(stormStrength, weather.Strength, Time.deltaTime * .7f);
            if(emergency)Daylight*=.78f;
            sky.SetVector("_SunDirection", direction); sky.SetFloat("_Day", Daylight);
            sky.SetFloat("_Dust", stormStrength);
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.LookRotation(-direction);
                sun.color = Color.Lerp(Hex("#ffe1b6"), Hex("#dfb57d"), stormStrength);
                sun.intensity = (.18f + Daylight*1.65f) * Mathf.Lerp(1, .42f, stormStrength);
                sun.shadowNormalBias = .035f; sun.shadowBias = .05f;
            }
            RenderSettings.ambientSkyColor = Hex("#b5c7f4") * (.28f+Daylight*.44f);
            RenderSettings.ambientEquatorColor = Hex("#dcd6ed") * (.30f+Daylight*.30f);
            RenderSettings.ambientGroundColor = Hex("#d98771") * .32f;
            RenderSettings.fogColor = Color.Lerp(Hex("#292a4c"), Hex("#e3a18a"), Daylight);
            if(emergency)RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,Hex("#ca7950"),.45f);
            RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,Hex("#ae8157"),stormStrength*.9f);
            bool sheltered = explorer != null && HabitatShelter.Contains(explorer.definition.layout, explorer.ControlledPosition);
            float localStorm = stormStrength * (sheltered ? .45f : 1);
            RenderSettings.fogStartDistance = Mathf.Lerp(65, 9, localStorm);
            RenderSettings.fogEndDistance = Mathf.Lerp(230, 48, localStorm);
            if (fill != null) fill.intensity = Mathf.Lerp(.32f, .19f, stormStrength);
        }
        static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var c); return c; }
        void BeginCamera(ScriptableRenderContext context,Camera camera)
        {
            fogStack.Push((RenderSettings.fog,RenderSettings.fogStartDistance,RenderSettings.fogEndDistance));
            // Panoramas/maps are inspection views. Preserve normal ground-level reference fog.
            if(camera.orthographic)RenderSettings.fog=false;
            else if(camera.transform.position.y>60)
            {float offset=camera.transform.position.y-30;RenderSettings.fogStartDistance+=offset;RenderSettings.fogEndDistance+=offset;}
        }
        void EndCamera(ScriptableRenderContext context,Camera camera)
        {
            if(fogStack.Count==0)return;
            var saved=fogStack.Pop();RenderSettings.fog=saved.fog;RenderSettings.fogStartDistance=saved.near;RenderSettings.fogEndDistance=saved.far;
        }
        void OnDestroy()
        {
            RenderPipelineManager.beginCameraRendering-=BeginCamera;
            RenderPipelineManager.endCameraRendering-=EndCamera;
            if (RenderSettings.skybox == sky) RenderSettings.skybox = previousSky;
            if (sky != null) Destroy(sky);
            if (fill != null) Destroy(fill.gameObject);
        }
    }
}
