using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Constant-size location dot with a directional tip. Independent of camera orbit and zoom.</summary>
    internal sealed class MapMarker : VisualElement
    {
        Vector2 direction = Vector2.up;
        public MapMarker()
        {
            name = "map-player-marker"; pickingMode = PickingMode.Ignore;
            AddToClassList("map-marker"); generateVisualContent += Draw;
        }
        public void SetForward(Vector3 forward)
        {
            var next = MapProjection.Direction(forward);
            if (next.sqrMagnitude < .01f || (next - direction).sqrMagnitude < .0001f) return;
            direction = next; MarkDirtyRepaint();
        }
        void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D; var center = contentRect.center;
            var side = new Vector2(-direction.y, direction.x);
            p.fillColor = new Color32(48, 36, 62, 80);
            p.BeginPath(); p.Arc(center, 23, 0, 360); p.Fill();
            p.fillColor = new Color32(244, 202, 84, 255);
            p.strokeColor = new Color32(48, 36, 62, 255); p.lineWidth = 3;
            p.BeginPath(); p.MoveTo(center + direction * 27);
            p.LineTo(center - direction * 2 + side * 12);
            p.LineTo(center - direction * 2 - side * 12); p.ClosePath(); p.Fill(); p.Stroke();
            p.fillColor = new Color32(99, 211, 196, 255);
            p.strokeColor = new Color32(255, 246, 224, 255); p.lineWidth = 3;
            p.BeginPath(); p.Arc(center, 8, 0, 360); p.Fill(); p.Stroke();
        }
    }
}
