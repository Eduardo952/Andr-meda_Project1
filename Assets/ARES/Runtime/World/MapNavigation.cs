using UnityEngine;

namespace Ares.World
{
    /// <summary>Input-independent map framing. Coordinates use Unity X/Z; anchors/deltas use top-left UI space.</summary>
    public sealed class MapNavigation
    {
        readonly Rect bounds;
        Rect overview;
        float aspect = 1;
        Vector2 center;
        public float Zoom { get; private set; } = 1;
        public float MaxZoom { get; }
        public MapProjection Projection { get; private set; }

        public MapNavigation(Rect bounds, float maxZoom)
        {
            this.bounds = bounds; MaxZoom = Mathf.Clamp(maxZoom, 1, 12);
            center = bounds.center; Resize(1);
        }
        public void Resize(float aspect)
        {
            this.aspect = Mathf.Max(.01f, aspect);
            overview = new MapProjection(bounds, this.aspect).WorldRect;
            Refresh();
        }
        public void ZoomAt(float factor, Vector2 anchor)
        {
            anchor = new Vector2(Mathf.Clamp01(anchor.x), Mathf.Clamp01(anchor.y));
            var offset = new Vector2(anchor.x - .5f, .5f - anchor.y);
            Vector2 worldAnchor = center + Vector2.Scale(offset, Projection.WorldRect.size);
            Zoom = Mathf.Clamp(Zoom * factor, 1, MaxZoom);
            center = worldAnchor - Vector2.Scale(offset, overview.size / Zoom);
            Refresh();
        }
        public void Pan(Vector2 normalizedDrag)
        {
            center += Vector2.Scale(new Vector2(-normalizedDrag.x, normalizedDrag.y), Projection.WorldRect.size);
            Refresh();
        }
        public void Locate(Vector3 position)
        {
            Zoom = Mathf.Max(Zoom, Mathf.Min(2, MaxZoom));
            center = new Vector2(position.x, position.z); Refresh();
        }
        public void Reset() { Zoom = 1; center = bounds.center; Refresh(); }
        public void Frame(Rect area)
        {
            var fit = new MapProjection(area, aspect).WorldRect;
            Zoom = Mathf.Clamp(overview.height / Mathf.Max(1, fit.height), 1, MaxZoom);
            center = area.center; Refresh();
        }
        void Refresh()
        {
            Vector2 half = overview.size / (2 * Zoom);
            center.x = Mathf.Clamp(center.x, overview.xMin + half.x, overview.xMax - half.x);
            center.y = Mathf.Clamp(center.y, overview.yMin + half.y, overview.yMax - half.y);
            Projection = new MapProjection(new Rect(center - half, half * 2), aspect);
        }
    }
}
