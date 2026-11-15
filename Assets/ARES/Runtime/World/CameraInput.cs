using UnityEngine;
using UnityEngine.InputSystem;

namespace Ares.World
{
    /// <summary>Manual scene drag and zoom, matching CameraControls.tsx. No automatic heading follow.</summary>
    public sealed class CameraInput
    {
        bool leftDrag, rightDrag;
        int touchId = -1;
        float pinchDistance;
        Vector2 touchPosition;

        public void Clear() { leftDrag = rightDrag = false; touchId = -1; pinchDistance = 0; }

        public void Read(OrbitCamera rig, Camera camera, bool isometric, float sensitivity, bool invert, bool pointerBlocked, bool captureLeft = false)
        {
            var mouse = Mouse.current;
            if (captureLeft) leftDrag = false;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    // Only the astronaut is cameraInteractive in the reference scene.
                    bool interactive = false;
                    foreach (var hit in Physics.RaycastAll(camera.ScreenPointToRay(mouse.position.ReadValue())))
                        if (hit.collider.GetComponentInParent<AstronautAnimator>() != null) { interactive = true; break; }
                    leftDrag = !pointerBlocked && !interactive && !captureLeft;
                }
                if (mouse.rightButton.wasPressedThisFrame) rightDrag = !pointerBlocked;
                if (!mouse.leftButton.isPressed) leftDrag = false;
                if (!mouse.rightButton.isPressed) rightDrag = false;
                if (leftDrag || rightDrag)
                {
                    var delta = mouse.delta.ReadValue();
                    rig.Orbit(delta.x, -delta.y, sensitivity, invert, isometric);
                }
                if (!pointerBlocked) rig.Zoom(Mathf.Exp(-mouse.scroll.ReadValue().y * rig.WheelIntensity));
            }
            var screen = Touchscreen.current;
            if (screen == null) return;
            UnityEngine.InputSystem.Controls.TouchControl first = null, second = null;
            foreach (var touch in screen.touches)
            {
                if (!touch.press.isPressed) continue;
                if (touch.touchId.ReadValue() == touchId) first = touch;
            }
            if (first == null)
            {
                touchId = -1; pinchDistance = 0;
                if (pointerBlocked) return;
                foreach (var touch in screen.touches)
                    if (touch.press.wasPressedThisFrame && touch.position.ReadValue().x >= Screen.width / 2f)
                    { first = touch; touchId = touch.touchId.ReadValue(); touchPosition = touch.position.ReadValue(); break; }
            }
            if (first == null) return;
            foreach (var touch in screen.touches)
                if (touch.press.isPressed && touch != first) { second = touch; break; }
            var position = first.position.ReadValue();
            if (second != null)
            {
                float separation = Vector2.Distance(position, second.position.ReadValue());
                if (pinchDistance > 0 && separation > 0) rig.Zoom(pinchDistance / separation);
                pinchDistance = separation;
            }
            else
            {
                var delta = position - touchPosition;
                if (pinchDistance == 0) rig.Orbit(delta.x, -delta.y, sensitivity, invert, isometric);
                pinchDistance = 0;
            }
            touchPosition = position;
        }
    }
}
