using System;
using System.IO;
using System.Reflection;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace Ares.Editor
{
    /// <summary>Input-device checks plus real UI ownership transitions in the generated scene.</summary>
    [InitializeOnLoad] public static class CameraControlChecks
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static bool running, background, reportCaptured;
        static int stage;
        static double started, next;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        const string Request = "Temp/ares-camera-controls.request";

        static CameraControlChecks() { EditorApplication.update += Tick; }
        [MenuItem("ARES/Checks/HUD cursor and camera controls")]
        public static void Begin() => File.WriteAllText(Request, "run");
        static void StartChecks()
        {
            running = true; stage = 0; ui = null; next = 0; reportCaptured = false;
            started = EditorApplication.timeSinceStartup;
            background = Application.runInBackground; Application.runInBackground = true;
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/ares-camera-controls.txt", "RUNNING");
            // Enter Play can leave the Scene tab focused on subsequent runs.
            EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
            EditorApplication.EnterPlaymode();
        }
        static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
        static void Call(string method) => typeof(GameFrontend).GetMethod(method, Private).Invoke(ui, null);
        static void Place(Vector3 position)
        {
            var motion = (PlayerMotion)typeof(ReferenceExplorer).GetField("player", Private).GetValue(explorer);
            motion.X = position.x; motion.Z = -position.z; motion.Speed = motion.VelocityX = motion.VelocityZ = 0;
        }
        static void Press(string name)
        {
            // Simulate deliberate HUD interaction after using R.
            ((GameplayCursor)typeof(ReferenceExplorer).GetField("cursor", Private).GetValue(explorer)).Release();
            var button = ui.GetComponent<UIDocument>().rootVisualElement.Q<Button>(name);
            Require(button != null && button.enabledInHierarchy, "Missing UI button: " + name);
            using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
        }
        static void InputChecks()
        {
            var config = explorer.definition.world.camera;
            var rig = new OrbitCamera(config, explorer.definition.layout);
            var input = new CameraInput();
            var previous = Mouse.current;
            var mouse = InputSystem.AddDevice<Mouse>("ARES camera verification");
            var cursor = new GameplayCursor();
            try
            {
                mouse.MakeCurrent();
                Action<Vector2, float> state = (delta, wheel) => InputState.Change(mouse,
                    new MouseState { delta = delta, scroll = new Vector2(0, wheel) }, InputState.currentUpdateType);
                Action<bool, bool> read = (blocked, tool) => input.Read(rig, Camera.main, false, 1, false, blocked, tool);
                state(new Vector2(40, 10), 0);
                read(false, false); // Ignore the cursor capture/warp frame.
                Require(rig.TargetYaw == config.defaultYaw, "Capture frame moved the camera");
                read(false, false);
                Require(Mathf.Abs(rig.TargetYaw - config.defaultYaw) > .01f && !mouse.leftButton.isPressed && !mouse.rightButton.isPressed,
                    "Mouse movement without buttons did not orbit: delta=" + mouse.delta.ReadValue() + "; current=" + Mouse.current?.name + "; yaw=" + rig.TargetYaw + "; sensitivity=" + config.radiansPerPixel);
                float yaw = rig.TargetYaw, pitch = rig.TargetPitch, distance = rig.TargetDistance;
                float notch = InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.UniformAcrossAllPlatforms ? 1 : 120;
                state(new Vector2(100, 100), notch); read(true, false);
                Require(rig.TargetYaw == yaw && rig.TargetPitch == pitch && rig.TargetDistance == distance, "UI leaked camera input");
                state(new Vector2(100, 100), 0); read(false, true);
                Require(rig.TargetYaw == yaw && rig.TargetDistance == distance, "World tool leaked camera rotation");
                read(false, false);
                Require(rig.TargetYaw == yaw && rig.TargetDistance == distance, "Resume consumed a stale delta/wheel event");
                state(Vector2.zero, notch); read(false, false);
                Require(rig.TargetDistance < distance - .5f && rig.Distance == distance, "Normalized wheel notch did not produce perceptible zoom");
                rig.Step(.016f, false);
                Require(rig.Distance < distance && rig.Distance > rig.TargetDistance, "Zoom snapped instead of easing");
                state(Vector2.zero, -notch); read(false, false);
                Require(rig.TargetDistance > config.minDistance, "Zoom out failed");
                for (int i = 0; i < 100; i++) { state(Vector2.zero, notch); read(false, false); }
                Require(rig.TargetDistance == config.minDistance, "Minimum zoom limit failed");
                for (int i = 0; i < 100; i++) { state(Vector2.zero, -notch); read(false, false); }
                Require(rig.TargetDistance == config.maxDistance, "Maximum zoom limit failed");
                distance = rig.TargetDistance; yaw = rig.TargetYaw;
                input.Clear(); state(new Vector2(50, 10), notch);
                input.Read(rig, Camera.main, false, 1, false, false, true, false);
                Require(rig.TargetDistance < distance && rig.TargetYaw == yaw, "World cursor mode blocks wheel or rotates camera");
                var resume = new PointerResumeGuard();
                resume.Arm(); resume.Step(false, 0); Require(resume.AwaitingClick && resume.SuppressPointer, "Resume not armed");
                resume.Step(true, 1); Require(!resume.AwaitingClick && resume.SuppressPointer, "First click was not consumed");
                resume.Step(true, 2); Require(resume.SuppressPointer, "Held resume click leaked");
                resume.Step(false, 3); Require(resume.SuppressPointer, "Release/click event leaked");
                resume.Step(false, 4); Require(!resume.SuppressPointer, "Subsequent deliberate clicks remain blocked");
                rig.Orbit(100000, 100000, 1, false, false);
                Require(Mathf.Abs(rig.TargetYaw) <= Mathf.PI && Mathf.Approximately(rig.TargetPitch, config.maxPitchDegrees * Mathf.Deg2Rad), "Upper orbit bounds failed");
                rig.Orbit(-100000, -100000, 1, false, false);
                Require(Mathf.Abs(rig.TargetYaw) <= Mathf.PI && Mathf.Approximately(rig.TargetPitch, config.minPitchDegrees * Mathf.Deg2Rad), "Lower orbit bounds failed");
                rig.Recenter(false); pitch = rig.TargetPitch;
                rig.Orbit(0, 10, 1, true, false);
                Require(rig.TargetPitch < pitch, "Invert setting stopped working");
                yaw = rig.TargetYaw; pitch = rig.TargetPitch; rig.Orbit(10, 10, 1, false, true);
                Require(rig.TargetYaw == yaw && rig.TargetPitch == pitch, "Isometric rotation lock regressed");
                rig.Yaw = Mathf.PI - .01f; rig.TargetYaw = -Mathf.PI + .01f; rig.Step(.016f, false);
                Require(Mathf.Abs(Mathf.DeltaAngle((Mathf.PI - .01f) * Mathf.Rad2Deg, rig.Yaw * Mathf.Rad2Deg)) < 1,
                    "Yaw boundary took the long way around");
                cursor.Update(true, true, true); Require(cursor.MouseLookActive, "Gameplay capture policy failed");
                cursor.Update(false, true, true); Require(!cursor.MouseLookActive && Cursor.visible, "UI cursor release failed");
                cursor.Update(true, false, true); Require(!cursor.MouseLookActive && Cursor.visible, "Focus loss did not release cursor");
                cursor.Update(true, true, false); Require(!cursor.MouseLookActive, "Touch-only input locked cursor");
                cursor.Update(true, true, true); Require(cursor.MouseLookActive, "Gameplay did not resume capture");
            }
            finally { cursor.Release(); InputSystem.RemoveDevice(mouse); previous?.MakeCurrent(); }
        }
        static void ResumeClickChecks()
        {
            var guard = (PointerResumeGuard)typeof(ReferenceExplorer).GetField("resumePointer", Private).GetValue(explorer);
            var capture = (GameplayCursor)typeof(ReferenceExplorer).GetField("cursor", Private).GetValue(explorer);
            var button = ui.GetComponent<UIDocument>().rootVisualElement.Q<Button>("hud-settings");
            int received = 0;
            EventCallback<PointerDownEvent> observe = e => received++;
            button.RegisterCallback(observe, TrickleDown.TrickleDown);
            Action click = () => {
                var source = new Event { type = EventType.MouseDown, button = 0, mousePosition = button.worldBound.center };
                using (var down = PointerDownEvent.GetPooled(source)) { down.target = button; button.SendEvent(down); }
                source.type = EventType.MouseUp;
                using (var up = PointerUpEvent.GetPooled(source))
                {
                    up.target = button; button.SendEvent(up);
                    using (var completed = ClickEvent.GetPooled(source)) { completed.target = button; button.SendEvent(completed); }
                }
            };
            try
            {
                capture.Release(); guard.Arm(); click();
                Require(received == 0 && !ui.BlocksWorld, "Resume click reached the HUD/menu");
                Require(ui.BlocksPointer, "Resume click can reach world tools");
                guard.Step(false, Time.frameCount); guard.Step(false, Time.frameCount + 1);
                click();
                Require(received == 1 && ui.BlocksWorld, "Deliberate second HUD click did not open settings: received=" + received + "; blocked=" + ui.BlocksWorld + "; guard=" + guard.SuppressPointer + "; focused=" + Application.isFocused);
            }
            finally { button.UnregisterCallback(observe, TrickleDown.TrickleDown); }
        }
        static void ToggleChecks()
        {
            var old = Keyboard.current;
            var keyboard = InputSystem.AddDevice<Keyboard>("ARES HUD keyboard verification");
            var root = ui.GetComponent<UIDocument>().rootVisualElement;
            void KeyState(params Key[] keys)
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); InputSystem.Update();
                typeof(ReferenceExplorer).GetField("pointerInputFrame", Private).SetValue(explorer, -1);
                explorer.PreparePointerInput();
            }
            try
            {
                keyboard.MakeCurrent(); Call("CloseOverlay");
                if (explorer.CursorInteraction) explorer.ToggleCursorInteraction();
                float yaw = explorer.CameraTargetYaw;
                KeyState(Key.R);
                Require(explorer.CursorInteraction && !explorer.MouseLookActive && Cursor.visible, "R failed to release cursor");
                Require(explorer.CameraTargetYaw == yaw, "R still recenters the camera");
                KeyState();
                var button = root.Q<Button>("hud-settings");
                var source = new Event { type = EventType.MouseDown, button = 0, mousePosition = button.worldBound.center };
                using (var down = PointerDownEvent.GetPooled(source)) { down.target = button; button.SendEvent(down); }
                source.type = EventType.MouseUp;
                using (var up = PointerUpEvent.GetPooled(source)) { up.target = button; button.SendEvent(up); }
                Require(ui.BlocksWorld && explorer.CursorInteraction, "First HUD click after R was swallowed or captured the camera");
                KeyState(Key.R); Require(explorer.CursorInteraction, "R captured cursor inside a modal");
                KeyState(); Call("CloseOverlay");
                Require(explorer.CursorInteraction, "Closing the menu lost the selected cursor mode");
                KeyState(Key.S); Call("Update");
                var before = explorer.PlayerPosition;
                typeof(ReferenceExplorer).GetMethod("Update", Private).Invoke(explorer, null);
                Require(!ui.BlocksWorld && explorer.CursorInteraction && explorer.PlayerPosition != before, "S opened UI, toggled cursor or failed to move backward");
                KeyState(); KeyState(Key.R);
                Require(!explorer.CursorInteraction && explorer.MouseLookActive, "Second R failed to restore mouse look");
                KeyState(); KeyState(Key.LeftAlt);
                Require(!explorer.CursorInteraction, "Alt still toggles cursor");
                KeyState();
                var rover = root.Q<Button>("rover-controls");
                Require(rover != null && rover.resolvedStyle.left < 30 && rover.resolvedStyle.bottom < 30 && rover.resolvedStyle.width < 240,
                    "Rover button is not compact and bottom-left");
                Require(root.Q<Label>("hud-cursor-hint")?.pickingMode == PickingMode.Ignore, "Cursor instruction intercepts clicks");
                explorer.ToggleCursorInteraction();
                Press("rover-controls"); Require(explorer.IsDrivingRover && explorer.CursorInteraction, "Rover HUD button failed or recaptured cursor");
                Press("rover-controls"); Require(!explorer.IsDrivingRover, "Rover HUD button cannot return to astronaut");
                explorer.ToggleCursorInteraction();
            }
            finally { InputSystem.RemoveDevice(keyboard); old?.MakeCurrent(); }
        }
        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!running && File.Exists(Request))
            {
                if (EditorApplication.isPlaying) { EditorApplication.ExitPlaymode(); return; }
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(Request); StartChecks(); return;
            }
            if (!running || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 90, "Camera check timed out");
                if (EditorApplication.timeSinceStartup < next) return;
                next = EditorApplication.timeSinceStartup + .6;
                if (ui == null)
                {
                    ui = UnityEngine.Object.FindAnyObjectByType<GameFrontend>();
                    explorer = UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    if (ui == null || explorer?.Rover == null) { ui = null; return; }
                    ui.Apod.OfflineOnly = true; return;
                }
                if (stage == 1 && !reportCaptured)
                {
                    ScreenCapture.CaptureScreenshot("Logs/ares-technical-report.png"); reportCaptured = true; return;
                }
                switch (stage++)
                {
                    case 0: Require(ui.RequiresCursor, "Title lost cursor"); InputChecks(); ui.StartMission(false); break;
                    case 1: Require(ui.RequiresCursor, "Report lost cursor"); ui.Campaign.CloseReport(); Call("CloseOverlay"); Place(new Vector3(0, 0, -3)); break;
                    case 2: Require(!ui.RequiresCursor, "Gameplay retains UI ownership"); ResumeClickChecks(); break;
                    case 3: Require(ui.RequiresCursor, "Settings did not release mouse"); Call("CloseOverlay"); ToggleChecks(); break;
                    case 4: Require(!ui.RequiresCursor, "Closing settings did not restore gameplay ownership"); Press("hud-map"); break;
                    case 5: Require(ui.IsMapOpen && ui.RequiresCursor, "Map did not release mouse"); Call("CloseOverlay"); break;
                    case 6: Require(!ui.RequiresCursor, "Closing map did not restore gameplay ownership"); Place(ui.GreenhouseCare.PickupPosition); break;
                    case 7: Require(ui.RequiresCursor && ui.GreenhouseCare.CanPickUp, "Watering pickup cannot use cursor"); Place(ui.Observatory.StandPosition); break;
                    case 8: Require(ui.RequiresCursor, "Telescope cannot use cursor"); Place(new Vector3(0, 0, -3)); explorer.ToggleRover(); break;
                    case 9: Require(explorer.IsDrivingRover && !ui.RequiresCursor, "Rover mouse-look ownership failed"); explorer.ToggleRover(); ui.StartMission(true); for (int i = 0; i < 100; i++) explorer.Rest.Tick(.05f, false); Place(new Vector3(0, 0, -3)); break;
                    case 10: Require(ui.TutorialActive && ui.RequiresCursor, "Tutorial did not release cursor"); Press("tutorial-decline"); break;
                    case 11: Require(!ui.TutorialActive && !ui.RequiresCursor, "Tutorial dismissal retained mouse ownership"); ScreenCapture.CaptureScreenshot("Logs/ares-hud-cursor.png"); Finish("PASS: R toggles cursor without recentering; first HUD click works; modal and closing preserve cursor mode; S moves without opening menu; Alt does not toggle; compact bottom-left rover button enters/exits rover; hint ignores pointer; focus resume guard; mouse-look and scroll zoom; zoom limits/easing; map/tutorial/tools ownership."); break;
                }
            }
            catch (Exception e) { Finish("FAIL: " + e); }
        }
        static void Finish(string result)
        {
            File.WriteAllText("Logs/ares-camera-controls.txt", result); running = false;
            Application.runInBackground = background; EditorApplication.ExitPlaymode();
        }
    }
}
