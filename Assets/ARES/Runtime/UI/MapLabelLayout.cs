using System.Collections.Generic;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Places map captions near their projected anchors without overlap or viewport clipping.</summary>
    internal sealed class MapLabelLayout
    {
        internal sealed class Place
        {
            internal VisualElement Label;
            internal Vector3 Position;
            internal Vector2 Measured;
        }

        readonly List<Place> places = new List<Place>();
        readonly List<Rect> occupied = new List<Rect>();
        Rect previousProjection;
        Vector2 previousSize;
        bool dirty = true;

        public Place Add(VisualElement label, Vector3 position)
        {
            var place = new Place { Label = label, Position = position };
            places.Add(place); dirty = true;
            return place;
        }

        public void Move(Place place, Vector3 position)
        {
            if (place == null || place.Position == position) return;
            place.Position = position; dirty = true;
        }

        public void Arrange(MapProjection projection, Vector2 size)
        {
            if (size.x < 1 || size.y < 1) return;
            foreach (var place in places)
            {
                var measured = place.Label.layout.size;
                // Absolute labels otherwise inherit a stale auto width when copy/fonts change.
                // Reserve their actual unwrapped text bounds before solving collisions.
                if(place.Label is Label caption && caption.resolvedStyle.fontSize>0)
                {
                    var text=caption.MeasureTextSize(caption.text,0,VisualElement.MeasureMode.Undefined,0,VisualElement.MeasureMode.Undefined);
                    var style=caption.resolvedStyle;
                    measured=new Vector2(Mathf.Ceil(text.x+style.paddingLeft+style.paddingRight+style.borderLeftWidth+style.borderRightWidth+2),Mathf.Ceil(text.y+style.paddingTop+style.paddingBottom+style.borderTopWidth+style.borderBottomWidth+2));
                    if(measured.x>0&&measured.y>0){caption.style.width=measured.x;caption.style.height=measured.y;}
                }
                if (float.IsNaN(measured.x) || measured.x < 1 || measured.y < 1) continue;
                if (place.Measured != measured) { place.Measured = measured; dirty = true; }
            }
            if (!dirty && previousProjection == projection.WorldRect && previousSize == size) return;
            dirty = false; previousProjection = projection.WorldRect; previousSize = size;
            occupied.Clear();
            foreach (var place in places)
            {
                Vector2 point = projection.Normalized(place.Position);
                bool visible = point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1;
                place.Label.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (!visible) continue;
                Vector2 dimensions = place.Measured.x > 0 ? place.Measured : new Vector2(170, 36);
                Vector2 preferred = Vector2.Scale(point, size) + new Vector2(-dimensions.x * .5f, 20);
                Rect best = new Rect(); float bestScore = float.PositiveInfinity;
                // Search nearby rows first; labels keep their exact world anchor in the projection.
                for (int row = 0; row < 21; row++)
                {
                    float dy = ((row + 1) / 2) * (dimensions.y + 6) * (row % 2 == 1 ? -1 : 1);
                    for (int column = -1; column <= 1; column++)
                    {
                        var candidate = new Rect(preferred + new Vector2(column * (dimensions.x + 6), dy), dimensions);
                        candidate.x = Mathf.Clamp(candidate.x, 6, Mathf.Max(6, size.x - dimensions.x - 6));
                        candidate.y = Mathf.Clamp(candidate.y, 6, Mathf.Max(6, size.y - dimensions.y - 6));
                        var padded = new Rect(candidate.x - 3, candidate.y - 3, candidate.width + 6, candidate.height + 6);
                        bool collides = false;
                        foreach (var other in occupied) if (padded.Overlaps(other)) { collides = true; break; }
                        if (collides) continue;
                        float score = (candidate.position - preferred).sqrMagnitude;
                        if (score < bestScore) { best = candidate; bestScore = score; }
                    }
                }
                if (float.IsPositiveInfinity(bestScore)) { place.Label.style.display = DisplayStyle.None; continue; }
                place.Label.style.left = best.x; place.Label.style.top = best.y;
                occupied.Add(best);
            }
        }
    }
}
