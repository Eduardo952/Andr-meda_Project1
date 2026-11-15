using Ares.Data;
using UnityEngine;

namespace Ares.World
{
    public sealed class WorldNavigation
    {
        readonly LayoutData layout;
        public WorldNavigation(LayoutData layout) { this.layout = layout; }
        public bool Walkable(Vector3 position)
        {
            float x = position.x, z = -position.z;
            foreach (var r in layout.rooms)
                if (r.shape != "park" && Inside(x, z, r, .35f) && !Inside(x, z, r, -.6f) && !Door(x, z, r)) return false;
            foreach (var r in layout.rooms)
            {
                if (Inside(x, z, r, -.6f) || Door(x, z, r)) return true;
            }
            foreach (var p in layout.paths)
            {
                var a = new Vector2(p.a.x, p.a.z); var b = new Vector2(p.b.x, p.b.z); var point = new Vector2(x, z);
                var d = b - a; float t = d.sqrMagnitude == 0 ? 0 : Mathf.Clamp01(Vector2.Dot(point - a, d) / d.sqrMagnitude);
                if (Vector2.Distance(point, a + d * t) <= p.width / 2) return true;
            }
            return false;
        }
        public static bool Inside(float x, float z, RoomData r, float padding)
        {
            float dx = x - r.x, dz = z - r.z;
            if (r.shape == "hub" || r.shape == "dome") return Mathf.Pow(dx / (r.width / 2 + padding), 2) + Mathf.Pow(dz / (r.depth / 2 + padding), 2) < 1;
            return Mathf.Abs(dx) < r.width / 2 + padding && Mathf.Abs(dz) < r.depth / 2 + padding;
        }
        static bool Door(float x, float z, RoomData room)
        {
            foreach (var p in room.ports)
            {
                var direction = new Vector2(p.x - room.x, p.z - room.z).normalized;
                var offset = new Vector2(x - p.x, z - p.z);
                if (Mathf.Abs(offset.x * direction.y - offset.y * direction.x) < 2.25f && Mathf.Abs(Vector2.Dot(offset, direction)) < 1.5f) return true;
            }
            return false;
        }
        public RoomData Near(Vector3 position, float radius = 3.6f)
        {
            RoomData nearest = null; float best = radius;
            foreach (var room in layout.rooms) { float distance = Vector3.Distance(position, room.Station); if (distance < best) { nearest = room; best = distance; } }
            return nearest;
        }
    }
}
