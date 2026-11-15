using Ares.Data;
using UnityEngine;

namespace Ares.World.Rover
{
    /// <summary>The imported world has no physics walls. Reserve buildings, covered corridors and solar park explicitly.</summary>
    public sealed class RoverNavigation
    {
        readonly LayoutData layout;
        readonly RoverSettings settings;
        public RoverNavigation(LayoutData layout, RoverSettings settings) { this.layout = layout; this.settings = settings; }
        public bool CanOccupy(Vector3 point)
        {
            float radius = settings.footprintRadius;
            if (Mathf.Abs(point.x) + radius > settings.worldLimit || Mathf.Abs(point.z) + radius > settings.worldLimit) return false;
            foreach (var room in layout.rooms)
                if (WorldNavigation.Inside(point.x, -point.z, room, radius + .5f)) return false;
            foreach (var path in layout.corridors)
            {
                var a = new Vector2(path.a.x, -path.a.z); var b = new Vector2(path.b.x, -path.b.z);
                var delta = b - a; var p = new Vector2(point.x, point.z);
                float t = delta.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(p - a, delta) / delta.sqrMagnitude) : 0;
                if (Vector2.Distance(p, a + delta * t) < path.width * .5f + radius + .5f) return false;
            }
            return true;
        }
    }
}
