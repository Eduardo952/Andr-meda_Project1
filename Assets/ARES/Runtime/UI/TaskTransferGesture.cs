using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Mouse/touch drag with pointer capture. Click and keyboard alternatives stay on the buttons.</summary>
    internal sealed class TaskTransferGesture : PointerManipulator
    {
        readonly VisualElement[] destinations;
        readonly Action<int> drop;
        int pointer = -1;
        Vector2 origin;
        bool dragging;
        public TaskTransferGesture(VisualElement[] destinations, Action<int> drop)
        { this.destinations = destinations; this.drop = drop; }
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(Move);
            target.RegisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCancelEvent>(Cancel);
            target.RegisterCallback<PointerCaptureOutEvent>(Lost);
            target.RegisterCallback<DetachFromPanelEvent>(Detach);
        }
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(Down, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(Move);
            target.UnregisterCallback<PointerUpEvent>(Up, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerCancelEvent>(Cancel);
            target.UnregisterCallback<PointerCaptureOutEvent>(Lost);
            target.UnregisterCallback<DetachFromPanelEvent>(Detach);
        }
        void Down(PointerDownEvent e)
        {
            if (e.button != 0 || !target.enabledInHierarchy) return;
            pointer = e.pointerId; origin = e.position; dragging = false;
        }
        void Move(PointerMoveEvent e)
        {
            if (e.pointerId != pointer || (e.pressedButtons & 1) == 0) return;
            if (!dragging && Vector2.Distance(origin, e.position) > 8)
            { dragging = true; target.CapturePointer(pointer); target.AddToClassList("is-dragging"); }
            if (!dragging) return;
            foreach (var destination in destinations) destination.EnableInClassList("drop-hover", destination.enabledInHierarchy && destination.worldBound.Contains(e.position));
        }
        void Up(PointerUpEvent e)
        {
            if (e.pointerId != pointer) return;
            bool wasDragging = dragging; int index = -1;
            if (wasDragging) for (int i=0;i<destinations.Length;i++) if(destinations[i].enabledInHierarchy && destinations[i].worldBound.Contains(e.position)) { index=i; break; }
            Reset();
            if (!wasDragging) return;
            e.StopImmediatePropagation();
            if (index >= 0) drop(index);
        }
        void Cancel(PointerCancelEvent e) => Reset();
        void Lost(PointerCaptureOutEvent e) => Reset();
        void Detach(DetachFromPanelEvent e) => Reset();
        void Reset()
        {
            int id = pointer; pointer = -1; dragging = false;
            target.RemoveFromClassList("is-dragging");
            foreach (var destination in destinations) destination.RemoveFromClassList("drop-hover");
            if (id >= 0 && target.HasPointerCapture(id)) target.ReleasePointer(id);
        }
    }
}
