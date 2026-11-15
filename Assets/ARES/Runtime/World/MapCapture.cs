using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ares.World
{
    /// <summary>Owns a separate orthographic camera and its texture, only while the map is open.</summary>
    public sealed class MapCapture : IDisposable
    {
        readonly Camera camera;
        readonly Rect bounds;
        readonly Rect baseBounds;
        readonly List<Renderer> roofs = new List<Renderer>();
        readonly List<bool> roofVisibility = new List<bool>();
        bool rendering, framed;
        readonly int maxResolution;
        public RenderTexture Texture { get; private set; }
        readonly MapNavigation navigation;
        public MapProjection Projection => navigation.Projection;
        public float Zoom => navigation.Zoom;
        public float MaxZoom => navigation.MaxZoom;

        public MapCapture(ReferenceExplorer explorer)
        {
            var settings = explorer.mapSettings;
            float radius = Mathf.Max(20, explorer.roverSettings.worldLimit) + Mathf.Max(0, settings.padding);
            bounds = new Rect(-radius, -radius, radius * 2, radius * 2);
            var baseArea = new Rect();
            bool firstRoom = true;
            // Include authored rooms even if a replacement world is larger than the rover area.
            foreach (var room in explorer.definition.layout.rooms)
            {
                var area = new Rect(room.x - room.width * .5f - settings.padding,
                    -room.z - room.depth * .5f - settings.padding,
                    room.width + settings.padding * 2, room.depth + settings.padding * 2);
                if (firstRoom) { baseArea = area; firstRoom = false; }
                else
                {
                    baseArea.xMin = Mathf.Min(baseArea.xMin, area.xMin); baseArea.xMax = Mathf.Max(baseArea.xMax, area.xMax);
                    baseArea.yMin = Mathf.Min(baseArea.yMin, area.yMin); baseArea.yMax = Mathf.Max(baseArea.yMax, area.yMax);
                }
                bounds.xMin = Mathf.Min(bounds.xMin, room.x - room.width * .5f - settings.padding);
                bounds.xMax = Mathf.Max(bounds.xMax, room.x + room.width * .5f + settings.padding);
                bounds.yMin = Mathf.Min(bounds.yMin, -room.z - room.depth * .5f - settings.padding);
                bounds.yMax = Mathf.Max(bounds.yMax, -room.z + room.depth * .5f + settings.padding);
            }
            if(explorer.definition.layout.landmarks!=null)foreach(var landmark in explorer.definition.layout.landmarks)
            {
                float margin=landmark.radius+settings.padding;
                baseArea.xMin=Mathf.Min(baseArea.xMin,landmark.x-margin);baseArea.xMax=Mathf.Max(baseArea.xMax,landmark.x+margin);
                baseArea.yMin=Mathf.Min(baseArea.yMin,-landmark.z-margin);baseArea.yMax=Mathf.Max(baseArea.yMax,-landmark.z+margin);
                bounds.xMin=Mathf.Min(bounds.xMin,baseArea.xMin);bounds.xMax=Mathf.Max(bounds.xMax,baseArea.xMax);
                bounds.yMin=Mathf.Min(bounds.yMin,baseArea.yMin);bounds.yMax=Mathf.Max(bounds.yMax,baseArea.yMax);
            }
            baseBounds = firstRoom ? bounds : baseArea;
            foreach (Transform group in explorer.transform)
                if (group.name.EndsWith("_roof")) roofs.AddRange(group.GetComponentsInChildren<Renderer>(true));
            maxResolution = Mathf.Clamp(settings.maxTextureSize, 512, 4096);
            navigation = new MapNavigation(bounds, settings.maxZoom);
            var go = new GameObject("ARES / map camera") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(explorer.transform, false);
            camera = go.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(190, 111, 78, 255);
            camera.nearClipPlane = .1f;
            camera.farClipPlane = Mathf.Max(100, settings.altitude) + 100;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.cullingMask = (Camera.main != null ? Camera.main.cullingMask : ~0) & ~ReferenceCelestials.MapExcludedLayers;
            camera.depth = -20;
            camera.transform.SetPositionAndRotation(new Vector3(bounds.center.x,
                Mathf.Max(100, settings.altitude), bounds.center.y), Quaternion.Euler(90, 0, 0));
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }

        public void Resize(float width, float height)
        {
            if (width < 1 || height < 1 || float.IsNaN(width) || float.IsNaN(height)) return;
            float scale = Mathf.Min(1, maxResolution / Mathf.Max(width, height));
            int w = Mathf.Max(16, Mathf.RoundToInt(width * scale));
            int h = Mathf.Max(16, Mathf.RoundToInt(height * scale));
            if (Texture != null && Texture.width == w && Texture.height == h) return;
            ReleaseTexture();
            Texture = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
            { name = "ARES map panorama", filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            Texture.Create();
            navigation.Resize((float)w / h);
            if (!framed) { navigation.Frame(baseBounds); framed = true; }
            camera.aspect = (float)w / h;
            ApplyFrame();
            camera.targetTexture = Texture;
            camera.enabled = true;
        }

        public void ZoomAt(float factor, Vector2 anchor) { navigation.ZoomAt(factor, anchor); ApplyFrame(); }
        public void Pan(Vector2 normalizedDrag) { navigation.Pan(normalizedDrag); ApplyFrame(); }
        public void Locate(Vector3 position) { navigation.Locate(position); ApplyFrame(); }
        public void ResetView() { navigation.Reset(); ApplyFrame(); }
        public void FocusBase() { navigation.Frame(baseBounds); ApplyFrame(); }
        void BeginCamera(ScriptableRenderContext context, Camera current)
        {
            if (current != camera || rendering) return;
            rendering = true; roofVisibility.Clear();
            // A map cutaway is camera-local: collisions and the gameplay cutaway stay intact.
            foreach (var roof in roofs)
            {
                roofVisibility.Add(roof != null && roof.forceRenderingOff);
                if (roof != null) roof.forceRenderingOff = true;
            }
        }
        void EndCamera(ScriptableRenderContext context, Camera current)
        {
            if (current == camera) RestoreRoofs();
        }
        void RestoreRoofs()
        {
            if (!rendering) return;
            for (int i = 0; i < roofs.Count; i++) if (roofs[i] != null) roofs[i].forceRenderingOff = roofVisibility[i];
            rendering = false;
        }
        void ApplyFrame()
        {
            var rect = Projection.WorldRect;
            camera.orthographicSize = rect.height * .5f;
            camera.transform.position = new Vector3(rect.center.x, camera.transform.position.y, rect.center.y);
        }

        void ReleaseTexture()
        {
            camera.enabled = false;
            camera.targetTexture = null;
            if (Texture == null) return;
            Texture.Release(); UnityEngine.Object.Destroy(Texture); Texture = null;
        }
        public void Dispose()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;
            RestoreRoofs();
            if (camera == null) return;
            ReleaseTexture(); UnityEngine.Object.Destroy(camera.gameObject);
        }
    }

    [Serializable]
    public sealed class MapSettings
    {
        [Tooltip("Margin around the playable world, in metres.")]
        [Min(0)] public float padding = 8;
        [Min(100)] public float altitude = 250;
        [Range(512, 4096)] public int maxTextureSize = 2048;
        [Range(1, 12)] public float maxZoom = 6;
    }
}
