using UnityEngine;
using UnityEngine.InputSystem;

namespace Ares.World
{
    /// <summary>A focus/capture click is consumed through release, including UI compatibility events.</summary>
    public sealed class PointerResumeGuard
    {
        public bool AwaitingClick { get; private set; }
        bool swallowing;
        int releasedFrame = -1;
        public bool SuppressPointer => AwaitingClick || swallowing;
        public void Arm() { AwaitingClick = true; }
        public void Clear() { AwaitingClick = swallowing = false; releasedFrame = -1; }
        public void Step(bool pressed, int frame)
        {
            if (swallowing && releasedFrame >= 0 && frame > releasedFrame) swallowing = false;
            if (AwaitingClick && pressed) { AwaitingClick = false; swallowing = true; releasedFrame = -1; }
            if (swallowing && !pressed && releasedFrame < 0) releasedFrame = frame;
        }
    }

    /// <summary>Owns the desktop cursor; modal UI and world tools temporarily release it.</summary>
    public sealed class GameplayCursor
    {
        public bool MouseLookActive { get; private set; }

        public void Update(bool gameplay, bool focused, bool hasMouse)
        {
            MouseLookActive = gameplay && focused && hasMouse;
            var mode = MouseLookActive ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState != mode) Cursor.lockState = mode;
            Cursor.visible = !MouseLookActive;
        }

        public void Release() => Update(false, false, false);
    }

    /// <summary>Button-free desktop orbit and wheel zoom, with touch drag/pinch retained.</summary>
    public sealed class CameraInput
    {
        bool mouseReady;
        int touchId = -1;
        float pinchDistance;
        Vector2 touchPosition;

        public void Clear() { mouseReady = false; touchId = -1; pinchDistance = 0; }

        public void Read(OrbitCamera rig, Camera camera, bool isometric, float sensitivity, bool invert, bool pointerBlocked, bool captureLeft = false, bool mouseLook = true)
        {
            var mouse = Mouse.current;
            // Cancel an ongoing gesture as soon as UI or a world tool takes ownership.
            if (pointerBlocked) { Clear(); return; }
            if (mouse != null)
            {
                // Discard the first delta after capture/focus changes (OS cursor warps).
                if (mouseReady && mouseLook && !captureLeft)
                {
                    var delta = mouse.delta.ReadValue();
                    rig.Orbit(delta.x, -delta.y, sensitivity, invert, isometric);
                }
                // Input System now normalizes a wheel notch to 1; the imported gain uses 120.
                bool normalized = InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms;
                bool windows = Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer;
                float wheelUnits = normalized || !windows ? 120 : 1;
                // Wheel zoom remains available with the world cursor visible, except during tool drags.
                if (!captureLeft || !(mouse.leftButton.isPressed || mouse.rightButton.isPressed))
                    rig.Zoom(Mathf.Exp(Mathf.Clamp(-mouse.scroll.ReadValue().y * wheelUnits * rig.WheelIntensity, -1, 1)));
                mouseReady = mouseLook && !captureLeft;
            }
            else mouseReady = false;
            if (captureLeft) { touchId = -1; pinchDistance = 0; return; }
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
