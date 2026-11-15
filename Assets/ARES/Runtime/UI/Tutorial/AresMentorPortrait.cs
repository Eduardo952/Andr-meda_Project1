using System;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Foreground presentation of the existing pet. Owns its render target and restores the world pet.</summary>
    internal sealed class AresMentorPortrait : IDisposable
    {
        readonly Image image;
        readonly BubbleAnimator original, actor;
        readonly GameObject stage;
        readonly Camera camera;
        readonly RenderTexture texture;
        readonly Renderer[] worldRenderers;
        readonly bool[] previousVisibility;
        Vector2 origin;
        bool visible, disposed;
        float entrance, time;

        public AresMentorPortrait(Image image, BubbleAnimator original)
        {
            this.image = image; this.original = original;
            worldRenderers = original.GetComponentsInChildren<Renderer>();
            previousVisibility = new bool[worldRenderers.Length];
            stage = new GameObject("ARES / tutorial mentor portrait");
            stage.transform.position = new Vector3(0, -1500, 0);
            actor = UnityEngine.Object.Instantiate(original, stage.transform);
            actor.transform.localPosition = Vector3.zero;
            actor.transform.localRotation = Quaternion.Euler(0, -15, 0);
            SetLayer(actor.transform);
            var bounds = new Bounds(actor.transform.position, Vector3.zero);
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>()) { renderer.forceRenderingOff = false; bounds.Encapsulate(renderer.bounds); }
            camera = new GameObject("ARES / mentor camera").AddComponent<Camera>();
            camera.transform.SetParent(stage.transform, false);
            camera.transform.position = bounds.center + Vector3.back * 5;
            camera.transform.LookAt(bounds.center);
            camera.orthographic = true; camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.15f;
            camera.nearClipPlane = .1f; camera.farClipPlane = 12; camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear;
            camera.allowHDR = false; camera.allowMSAA = false; camera.enabled = false;
            texture = new RenderTexture(640, 640, 24, RenderTextureFormat.ARGB32) { name = "ARES mentor portrait", hideFlags = HideFlags.DontSave };
            texture.Create(); camera.targetTexture = texture; image.image = texture;
            var light = new GameObject("ARES / mentor key light").AddComponent<Light>();
            light.transform.SetParent(stage.transform, false); light.type = LightType.Directional;
            light.color = new Color32(255, 235, 204, 255); light.intensity = 1.4f; light.cullingMask = 1 << 31;
            light.transform.rotation = Quaternion.Euler(35, -30, 0);
        }
        static void SetLayer(Transform node) { node.gameObject.layer = 31; foreach (Transform child in node) SetLayer(child); }
        public void Show(bool show)
        {
            if (show == visible || disposed) return;
            visible = show; if(camera!=null)camera.enabled = show;
            if (show)
            {
                entrance = 0;
                if (Camera.main != null && original!=null && image.panel != null)
                {
                    Vector3 point = Camera.main.WorldToScreenPoint(original.transform.position);
                    origin = RuntimePanelUtils.ScreenToPanel(image.panel, new Vector2(point.x, Screen.height - point.y));
                }
            }
            for (int i = 0; i < worldRenderers.Length; i++)
                if (worldRenderers[i] != null)
                {
                    if (show) previousVisibility[i] = worldRenderers[i].forceRenderingOff;
                    worldRenderers[i].forceRenderingOff = show || previousVisibility[i];
                }
        }
        public void Tick(float seconds, bool reducedMotion)
        {
            if (!visible || disposed || actor==null) return;
            entrance = reducedMotion ? 1 : Mathf.Min(1, entrance + seconds / .75f);
            if (!reducedMotion) time += seconds;
            float eased = 1 - Mathf.Pow(1 - entrance, 3);
            Vector2 target = image.parent.LocalToWorld(image.layout.center);
            Vector2 offset = (origin - target) * (1 - eased);
            image.style.translate = new Translate(offset.x, offset.y + (reducedMotion ? 0 : Mathf.Sin(time * 1.8f) * 9), 0);
            image.style.scale = new Scale(Vector3.one * Mathf.Lerp(.15f, 1, eased));
            actor.Apply(reducedMotion ? 0 : time, "happy", !reducedMotion, !reducedMotion);
        }
        public void Dispose()
        {
            if(disposed)return;
            Show(false);disposed=true;image.image = null;if(camera!=null)camera.targetTexture = null;
            if(texture!=null){texture.Release();UnityEngine.Object.Destroy(texture);}if(stage!=null)UnityEngine.Object.Destroy(stage);
        }
    }
}
