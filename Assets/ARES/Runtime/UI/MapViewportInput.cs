using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Pointer capture keeps dragging reliable outside the viewport; never reads gameplay input.</summary>
    internal sealed class MapViewportInput : PointerManipulator
    {
        readonly Action<float, Vector2> zoom;
        readonly Action<Vector2> pan;
        int pointer = -1;
        Vector2 previous;
        public MapViewportInput(Action<float, Vector2> zoom, Action<Vector2> pan)
        { this.zoom = zoom; this.pan = pan; }
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<WheelEvent>(Wheel);
            target.RegisterCallback<PointerDownEvent>(Down);
            target.RegisterCallback<PointerMoveEvent>(Move);
            target.RegisterCallback<PointerUpEvent>(Up);
            target.RegisterCallback<PointerCancelEvent>(Cancel);
            target.RegisterCallback<PointerCaptureOutEvent>(LostCapture);
        }
        protected override void UnregisterCallbacksFromTarget()
        {
            CancelDrag();
            target.UnregisterCallback<WheelEvent>(Wheel);
            target.UnregisterCallback<PointerDownEvent>(Down);
            target.UnregisterCallback<PointerMoveEvent>(Move);
            target.UnregisterCallback<PointerUpEvent>(Up);
            target.UnregisterCallback<PointerCancelEvent>(Cancel);
            target.UnregisterCallback<PointerCaptureOutEvent>(LostCapture);
        }
        void Wheel(WheelEvent evt)
        {
            var rect = target.contentRect;
            if (rect.width < 1 || rect.height < 1) return;
            Vector2 point = target.WorldToLocal(evt.mousePosition) - rect.position;
            zoom(Mathf.Exp(Mathf.Clamp(-evt.delta.y * .08f, -.7f, .7f)), new Vector2(point.x / rect.width, point.y / rect.height));
            evt.StopPropagation();
        }
        void Down(PointerDownEvent evt)
        {
            // Controls floating above the map must remain clickable, never start a drag.
            if (evt.button != 0 || evt.target != target || pointer >= 0) return;
            pointer = evt.pointerId; previous = target.WorldToLocal(evt.position);
            target.CapturePointer(pointer); target.Focus(); target.AddToClassList("map-dragging");
            evt.StopPropagation();
        }
        void Move(PointerMoveEvent evt)
        {
            if (evt.pointerId != pointer || !target.HasPointerCapture(pointer)) return;
            var point = (Vector2)target.WorldToLocal(evt.position);
            var size = target.contentRect.size;
            if (size.x > 0 && size.y > 0) pan(new Vector2((point.x - previous.x) / size.x, (point.y - previous.y) / size.y));
            previous = point; evt.StopPropagation();
        }
        void Up(PointerUpEvent evt) { if (evt.pointerId == pointer && evt.button == 0) { CancelDrag(); evt.StopPropagation(); } }
        void Cancel(PointerCancelEvent evt) { if (evt.pointerId == pointer) CancelDrag(); }
        void LostCapture(PointerCaptureOutEvent evt) { if (evt.pointerId == pointer) { pointer = -1; target.RemoveFromClassList("map-dragging"); } }
        public void CancelDrag()
        {
            int captured = pointer; pointer = -1;
            target.RemoveFromClassList("map-dragging");
            if (captured >= 0 && target.HasPointerCapture(captured)) target.ReleasePointer(captured);
        }
    }
}
