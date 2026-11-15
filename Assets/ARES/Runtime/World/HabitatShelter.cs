using Ares.Data;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Shared indoor mask for weather and telemetry. The solar park and open paths are outdoors.</summary>
    public static class HabitatShelter
    {
        public static bool Contains(LayoutData layout, Vector3 position, float padding = .8f)
        {
            if (layout == null) return false;
            foreach (var room in layout.rooms)
                if (room.shape != "park" && WorldNavigation.Inside(position.x, -position.z, room, padding)) return true;
            if (layout.corridors == null) return false;
            var point = new Vector2(position.x, -position.z);
            foreach (var corridor in layout.corridors)
            {
                var a = new Vector2(corridor.a.x, corridor.a.z); var b = new Vector2(corridor.b.x, corridor.b.z);
                var delta = b - a; float t = delta.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude) : 0;
                if (Vector2.Distance(point, a + t * delta) <= corridor.width * .5f + padding) return true;
            }
            return false;
        }
    }
}
