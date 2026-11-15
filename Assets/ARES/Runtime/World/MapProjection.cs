using UnityEngine;

namespace Ares.World
{
    /// <summary>North is Unity +Z. UI coordinates have their origin at the top left.</summary>
    public readonly struct MapProjection
    {
        public readonly Rect WorldRect;
        public MapProjection(Rect bounds, float aspect)
        {
            aspect = Mathf.Max(.01f, aspect);
            float height = Mathf.Max(bounds.height, bounds.width / aspect);
            var size = new Vector2(height * aspect, height);
            WorldRect = new Rect(bounds.center - size * .5f, size);
        }
        public Vector2 Normalized(Vector3 world) => new Vector2(
            (world.x - WorldRect.xMin) / WorldRect.width,
            1 - (world.z - WorldRect.yMin) / WorldRect.height);
        public static Vector2 Direction(Vector3 forward) => new Vector2(forward.x, -forward.z).normalized;
    }
}
