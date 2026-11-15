using System;
using System.IO;
using System.Reflection;
using Ares.Data;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Opt-in integration checks for map input, projection, modal lifetime and real world rendering.</summary>
    [InitializeOnLoad]
    public static class WorldMapChecks
    {
        const string Pending = "ARES.WorldMapCheck";
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static VisualElement root;
        static Keyboard keyboard, previousKeyboard;
        static int stage, frame;
        static float elapsed, missionTime;
        static Vector3 origin;
        static double started;
        static bool background;
        static WorldMapChecks()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending, false))
                    Finish("FAIL: Play interrupted");
            };
        }
        [MenuItem("ARES/Checks/Fullscreen world map")]
        public static void Request() => File.WriteAllText("Temp/ares-computer-check.request", "world-map");
        public static void Begin()
        {
            stage = 0; frame = -1; elapsed = 0; ui = null; started = EditorApplication.timeSinceStartup;
            background = Application.runInBackground; Application.runInBackground = true;
            SessionState.SetBool(Pending, true); File.WriteAllText("Logs/ares-map.txt", "RUNNING");
            EditorApplication.isPaused = false; EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 75, "Timeout");
                if (frame == Time.frameCount) return;
                frame = Time.frameCount; elapsed += Mathf.Min(Time.deltaTime, .05f);
                if (ui == null)
                {
                    explorer = UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    if (explorer?.Rover == null) return;
                    ui = explorer.GetComponent<GameFrontend>();
                    VerifyProjection(); VerifyArt();
                    previousKeyboard = Keyboard.current;
                    keyboard = InputSystem.AddDevice<Keyboard>("ARES map checks");
                    ui.Apod.OfflineOnly = true; ui.StartMission(false);
                    root = ui.GetComponent<UIDocument>().rootVisualElement;
                    ui.ToggleMap(); Require(!ui.IsMapOpen, "Map opened over daily report");
                    ui.Campaign.CloseReport(); Call("CloseOverlay");
                    ui.Campaign.SetAllocation(new Allocation { shield = ui.Campaign.EnergyPool });
                    Require(ui.Campaign.StartSol(), "Could not start Sol");
                    elapsed = 0;
                }
                keyboard.MakeCurrent(); var keys = new KeyboardState();
                if (stage == 0 && elapsed > .5f)
                {
                    Require(root.Q<Button>("hud-map") != null, "Map button missing");
                    ScreenCapture.CaptureScreenshot("Logs/ares-map-button.png");
                    origin = explorer.PlayerPosition; Next();
                }
                else if (stage == 1)
                {
                    keys = new KeyboardState(Key.M, Key.W, Key.Y);
                    if (elapsed > .6f)
                    {
                        Require(ui.IsMapOpen && ui.BlocksWorld && !explorer.IsDrivingRover, "M/input blocking failed");
                        Require(explorer.PlayerPosition == origin, "Player moved through modal");
                        missionTime = ui.Campaign.SolElapsedSeconds;
                        var photo = root.Q<Image>("map-panorama");
                        Require(photo.image is RenderTexture texture && texture.IsCreated(), "Panorama texture missing");
                        Require(root.panel.focusController.focusedElement is VisualElement focused && root.Q("world-map").Contains(focused), "Focus escaped map controls");
                        root.Query<Button>().ForEach(button => Require(root.Q("world-map").Contains(button) || !button.enabledInHierarchy, "Underlying HUD control still enabled: " + button.name));
                        CheckMarker(); CheckLayout();
                        ScreenCapture.CaptureScreenshot("Logs/ares-map-fullscreen.png"); Next();
                    }
                }
                else if (stage == 2 && elapsed > .4f)
                {
                    Require(ui.Campaign.SolElapsedSeconds == missionTime, "Map did not pause mission clock");
                    Require(ui.IsMapOpen, "Held M closed map");
                    CheckNavigationControls(); stage = 21; elapsed = 0;
                }
                else if (stage == 21 && elapsed > .3f)
                {
                    CheckMarker();
                    Require(ui.Campaign.SolElapsedSeconds == missionTime && explorer.PlayerPosition == origin, "Navigation changed gameplay");
                    ScreenCapture.CaptureScreenshot("Logs/ares-map-zoom.png"); stage = 22; elapsed = 0;
                }
                else if (stage == 22 && elapsed > .2f) { keys = new KeyboardState(Key.Escape); stage = 3; elapsed = 0; }
                else if (stage == 3 && elapsed > .3f)
                {
                    Require(!ui.IsMapOpen && !ui.BlocksWorld, "Escape did not return to world");
                    Require(root.panel.focusController.focusedElement == root.Q<Button>("hud-map"), "HUD focus not restored");
                    Require(MapCameras() == 0, "Map camera leaked after close");
                    Press("hud-map"); Next();
                }
                else if (stage == 4 && elapsed > .3f)
                {
                    Require(ui.IsMapOpen, "Button did not open map");
                    var screen = root.Q("world-map"); screen.style.width = 960; screen.style.height = 600;
                    Next();
                }
                else if (stage == 5 && elapsed > .4f)
                {
                    CheckLayout(); CheckMarker();
                    ScreenCapture.CaptureScreenshot("Logs/ares-map-compact.png");
                    stage = 51; elapsed = 0;
                }
                else if (stage == 51 && elapsed > .2f) { keys = new KeyboardState(Key.M); stage = 6; elapsed = 0; }
                else if (stage == 6 && elapsed > .3f)
                {
                    Require(!ui.IsMapOpen && !ui.BlocksWorld, "Second M failed");
                    explorer.ToggleRover(); Require(explorer.IsDrivingRover, "Rover fixture failed");
                    ui.ToggleMap(); Next();
                }
                else if (stage == 7 && elapsed > .4f)
                {
                    CheckMarker(); CheckLayout();
                    ScreenCapture.CaptureScreenshot("Logs/ares-map-rover.png");
                    stage = 71; elapsed = 0;
                }
                else if (stage == 71 && elapsed > .2f) { Press("map-close"); stage = 8; elapsed = 0; }
                else if (stage == 8 && elapsed > .3f)
                {
                    Require(!ui.IsMapOpen && explorer.IsDrivingRover && !ui.BlocksWorld, "Close did not preserve rover control");
                    Require(MapCameras() == 0, "Rover map camera leaked");
                    explorer.ToggleRover(); Call("Settings");
                    keys = new KeyboardState(Key.M); Next();
                }
                else if (stage == 9 && elapsed > .3f)
                {
                    Require(!ui.IsMapOpen && ui.BlocksWorld, "Map opened over settings");
                    Call("CloseOverlay"); ui.ToggleMap(); Call("ShowTitle"); Next();
                }
                else if (stage == 10 && elapsed > .3f)
                {
                    Require(!ui.IsMapOpen && MapCameras() == 0, "Title transition leaked map");
                    Finish("PASS: projection, zoom anchors/limits, pan bounds, resize, wheel, drag/release, zoom buttons, locate/overview, marker alignment after navigation, generated icon, M/hold/Escape, pause, focus, fullscreen/compact labels, rover, modal guards, camera cleanup. Screenshots: ares-map-*.png");
                    return;
                }
                InputSystem.QueueStateEvent(keyboard, keys);
            }
            catch (Exception error) { Finish("FAIL: " + error); }
        }
        static void VerifyProjection()
        {
            VerifyNavigation();
            foreach (float aspect in new[] { 16f/9, 21f/9, 4f/3, .7f })
            {
                var p = new MapProjection(new Rect(-125,-125,250,250), aspect);
                Require(Vector2.Distance(p.Normalized(Vector3.zero), new Vector2(.5f,.5f)) < .0001f, "Center drift");
                Require(p.Normalized(Vector3.forward * 10).y < .5f && p.Normalized(Vector3.right * 10).x > .5f, "Mirrored projection");
                Require(p.WorldRect.width >= 249.99f && p.WorldRect.height >= 249.99f, "Bounds cropped");
            }
            Require(MapProjection.Direction(Vector3.forward) == Vector2.down, "North must point up on screen");
            Require(MapProjection.Direction(Vector3.back) == Vector2.up, "South heading wrong");
            Require(MapProjection.Direction(Vector3.right) == Vector2.right && MapProjection.Direction(Vector3.left) == Vector2.left, "East/west heading wrong");
            var player = (PlayerMotion)typeof(ReferenceExplorer).GetField("player", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(explorer);
            float savedHeading = player.Heading;
            try
            {
                var expected = new[] { Vector3.back, Vector3.right, Vector3.forward, Vector3.left };
                for (int i = 0; i < 4; i++)
                {
                    player.Heading = i * Mathf.PI * .5f;
                    Require(Vector3.Distance(explorer.ControlledForward, expected[i]) < .0001f, "Source heading reflection incorrect");
                }
            }
            finally { player.Heading = savedHeading; }
        }
        static void VerifyNavigation()
        {
            var navigation = new MapNavigation(new Rect(-125,-125,250,250), 6);
            navigation.Resize(16f/9);
            var anchor = new Vector3(40,0,30);
            Vector2 normalized = navigation.Projection.Normalized(anchor);
            navigation.ZoomAt(2, normalized);
            Require(Vector2.Distance(navigation.Projection.Normalized(anchor), normalized) < .0001f, "Zoom lost cursor anchor");
            var before = navigation.Projection.WorldRect.center;
            navigation.Pan(new Vector2(.1f,.1f));
            var after = navigation.Projection.WorldRect.center;
            Require(after.x < before.x && after.y > before.y, "Drag direction inverted");
            navigation.ZoomAt(100, Vector2.one * .5f); Require(navigation.Zoom == 6, "Maximum zoom failed");
            navigation.Pan(Vector2.one * 100);
            var overview = new MapProjection(new Rect(-125,-125,250,250), 16f/9).WorldRect;
            var view = navigation.Projection.WorldRect;
            Require(view.xMin >= overview.xMin - .001f && view.yMax <= overview.yMax + .001f, "Pan escaped bounds");
            navigation.Locate(new Vector3(20,0,-15));
            Require(Vector2.Distance(navigation.Projection.Normalized(new Vector3(20,0,-15)), Vector2.one*.5f) < .0001f, "Locate missed target");
            navigation.Resize(4f/3); Require(navigation.Zoom == 6, "Resize reset zoom");
            navigation.ZoomAt(.001f, Vector2.one*.5f); Require(navigation.Zoom == 1, "Minimum zoom failed");
            navigation.Pan(Vector2.one * 100); Require(navigation.Projection.WorldRect.center.sqrMagnitude < .001f, "Overview can drift");
        }
        static Camera MapCamera() => Array.Find(Resources.FindObjectsOfTypeAll<Camera>(), c => c.name == "ARES / map camera");
        static void CheckNavigationControls()
        {
            var camera = MapCamera();
            var texture = camera.targetTexture;
            float baseSize = camera.orthographicSize;
            Press("map-reset");
            Require(camera.orthographicSize >= baseSize, "Overview cropped the initial base frame");
            float initial = camera.orthographicSize;
            Press("map-base");
            Require(Mathf.Abs(camera.orthographicSize - baseSize) < .01f, "View base did not restore the authored layout frame");
            Press("map-reset");
            Require(!root.Q<Button>("map-zoom-out").enabledSelf, "Zoom out enabled at overview");
            Press("map-zoom-in"); Require(camera.orthographicSize < initial, "Plus button did not zoom");
            Press("map-zoom-out"); Require(Mathf.Abs(camera.orthographicSize - initial) < .01f, "Minus button did not zoom out");
            var viewport = root.Q("map-viewport");
            var point = viewport.LocalToWorld(viewport.contentRect.position + viewport.contentRect.size * .6f);
            var anchoredWorld = camera.ViewportToWorldPoint(new Vector3(.6f,.4f,camera.transform.position.y));
            using (var evt = WheelEvent.GetPooled(new Event { type = EventType.ScrollWheel, mousePosition = point, delta = new Vector2(0,-6) }))
            { evt.target = viewport; viewport.SendEvent(evt); }
            Require(camera.orthographicSize < initial, "Mouse wheel did not zoom");
            var anchoredViewport = camera.WorldToViewportPoint(anchoredWorld);
            Require(Vector2.Distance(new Vector2(anchoredViewport.x,anchoredViewport.y), new Vector2(.6f,.4f)) < .001f, "Wheel zoom did not preserve the cursor anchor");
            Vector3 before = camera.transform.position;
            Vector2 end = point + new Vector2(65,35);
            using (var evt = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point }))
            { evt.target = viewport; viewport.SendEvent(evt); }
            using (var evt = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = end }))
            { evt.target = viewport; viewport.SendEvent(evt); }
            Require(camera.transform.position.x < before.x && camera.transform.position.z > before.z, "Pointer drag did not pan");
            using (var evt = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = end }))
            { evt.target = viewport; viewport.SendEvent(evt); }
            before = camera.transform.position;
            using (var evt = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = end + Vector2.one * 100 }))
            { evt.target = viewport; viewport.SendEvent(evt); }
            Require(camera.transform.position == before, "Drag continued after release");
            viewport.Focus();
            using (var evt = NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Right, EventModifiers.None))
            { evt.target = viewport; viewport.SendEvent(evt); }
            Require(camera.transform.position.x > before.x && root.panel.focusController.focusedElement == viewport, "Directional navigation lost map focus");
            float previousSize = camera.orthographicSize;
            using (var evt = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.KeypadPlus }))
            { evt.target = viewport; viewport.SendEvent(evt); }
            Require(camera.orthographicSize < previousSize, "Keyboard zoom failed");
            for (int i = 0; i < 20 && root.Q<Button>("map-zoom-in").enabledSelf; i++) Press("map-zoom-in");
            Require(!root.Q<Button>("map-zoom-in").enabledSelf, "Maximum zoom button not disabled");
            Press("map-reset");
            Require(Mathf.Abs(camera.orthographicSize - initial) < .01f, "Overview failed");
            Press("map-locate");
            Require(camera.targetTexture == texture, "Zoom/pan reallocated render texture");
            var position = camera.WorldToViewportPoint(explorer.ControlledPosition);
            Require(Vector2.Distance(new Vector2(position.x,position.y), Vector2.one*.5f) < .001f, "Locate failed");
        }
        static void VerifyArt()
        {
            const string path = "Assets/ARES/Resources/ARES/UI/HudIcons/map-v1.png";
            var texture = new Texture2D(2, 2);
            try
            {
                Require(ImageConversion.LoadImage(texture, File.ReadAllBytes(path)), "Generated icon is not a valid PNG");
                int transparent = 0, opaque = 0;
                foreach (var pixel in texture.GetPixels32()) { if (pixel.a == 0) transparent++; if (pixel.a == 255) opaque++; }
                Require(transparent > 0 && opaque > 0 && texture.width == texture.height, "Icon lacks real alpha or square source");
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Require(importer.alphaIsTransparency && !importer.mipmapEnabled && importer.maxTextureSize == 256, "Icon import settings changed");
                File.WriteAllText("Logs/ares-map-art.json", $"{{\"validator\":\"Unity ImageConversion / TextureImporter\",\"width\":{texture.width},\"height\":{texture.height},\"transparentPixels\":{transparent},\"opaquePixels\":{opaque},\"passed\":true}}");
            }
            finally { UnityEngine.Object.Destroy(texture); }
        }
        static void CheckMarker()
        {
            var camera = Array.Find(Resources.FindObjectsOfTypeAll<Camera>(), c => c.name == "ARES / map camera");
            Require(camera != null && camera.orthographic, "No orthographic map camera");
            var viewport = root.Q("map-viewport");
            Vector3 projected = camera.WorldToViewportPoint(explorer.ControlledPosition);
            var expected = viewport.LocalToWorld(viewport.contentRect.position + new Vector2(projected.x * viewport.contentRect.width, (1-projected.y)*viewport.contentRect.height));
            Require(Vector2.Distance(root.Q("map-player-marker").worldBound.center, expected) < 5, "Marker not aligned to actual camera");
        }
        static void CheckLayout()
        {
            var screen = root.Q("world-map").worldBound;
            var view = root.Q("map-viewport").worldBound;
            var close = root.Q("map-close").worldBound;
            Require(view.height > 200 && screen.Contains(view.center) && close.xMax <= screen.xMax, "Map layout clipped");
            Require(root.Q<Label>(className:"map-hint").worldBound.yMax <= screen.yMax, "Footer clipped");
            var labels = root.Query<Label>(className:"map-place").ToList().FindAll(label=>label.resolvedStyle.display!=DisplayStyle.None&&label.worldBound.width>0&&label.worldBound.height>0);
            for (int i = 0; i < labels.Count; i++)
                for (int j = i + 1; j < labels.Count; j++)
                    Require(!labels[i].worldBound.Overlaps(labels[j].worldBound), "Map labels overlap: " + labels[i].name + " "+labels[i].worldBound+" / " + labels[j].name+" "+labels[j].worldBound);
        }
        static int MapCameras() => Array.FindAll(Resources.FindObjectsOfTypeAll<Camera>(), c => c.name == "ARES / map camera").Length;
        static void Call(string name) => typeof(GameFrontend).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
        static void Press(string name)
        {
            var button = root.Q<Button>(name); Require(button != null && button.enabledInHierarchy, "Button unavailable: " + name);
            using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
        }
        static void Next() { stage++; elapsed = 0; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish(string result)
        {
            SessionState.SetBool(Pending, false);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            keyboard = null; if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            File.WriteAllText("Logs/ares-map.txt", result);
            Application.runInBackground = background;
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }
    }
}
