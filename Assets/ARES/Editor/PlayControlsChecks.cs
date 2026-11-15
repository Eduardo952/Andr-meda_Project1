using System;
using System.IO;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Ares.Editor
{
    /// <summary>Play integration test using isolated virtual devices, never OS keyboard/mouse injection.</summary>
    [InitializeOnLoad]
    public static class PlayControlsChecks
    {
        const string Pending = "ARES.PlayControlsPending";
        const string Request = "Temp/ares-play-check.request";
        static Keyboard keyboard;
        static Mouse mouse;
        static ReferenceExplorer explorer;
        static AstronautAnimator astronaut;
        static Vector3 origin;
        static Quaternion armOrigin;
        static float maxSwing, minHeight, maxHeight, walked, orbitYaw, stageTime;
        static int stage, lastFrame;
        static bool background;
        static double started;

        static PlayControlsChecks()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode && keyboard != null)
                    Finish(false, "Play ended before the integration test finished.");
            };
        }

        [MenuItem("ARES/Validate controls in Play")]
        public static void Begin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ares-play.txt", "RUNNING: controls integration test\n");
            SessionState.SetBool(Pending, true);
            EditorApplication.isPaused = false;
            EditorApplication.EnterPlaymode();
        }

        static void Update()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request))
            { File.Delete(Request); Begin(); return; }
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying) return;
            try
            {
                if (keyboard == null)
                {
                    explorer = UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    astronaut = UnityEngine.Object.FindAnyObjectByType<AstronautAnimator>();
                    Require(explorer != null && astronaut != null, "Open the generated BaseARES scene before running this test.");
                    keyboard = InputSystem.AddDevice<Keyboard>("ARES test keyboard");
                    mouse = InputSystem.AddDevice<Mouse>("ARES test mouse");
                    background = Application.runInBackground; Application.runInBackground = true;
                    EditorApplication.isPaused = false;
                    stage = 0; stageTime = 0; maxSwing = 0; minHeight = float.PositiveInfinity; maxHeight = float.NegativeInfinity;
                    lastFrame = -1; started = EditorApplication.timeSinceStartup;
                }
                Require(EditorApplication.timeSinceStartup - started < 30, "Play test timed out.");
                if (Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                stageTime += Mathf.Min(Time.deltaTime, .05f);
                keyboard.MakeCurrent(); mouse.MakeCurrent();
                var keys = new KeyboardState();
                var pointer = new MouseState { position = new Vector2(100, 100) };
                if (stage == 0 && stageTime > .25f)
                {
                    origin = explorer.PlayerPosition; armOrigin = astronaut.leftArm.localRotation; Next();
                }
                else if (stage == 1)
                {
                    keys = new KeyboardState(Key.W);
                    maxSwing = Mathf.Max(maxSwing, Quaternion.Angle(armOrigin, astronaut.leftArm.localRotation));
                    minHeight = Mathf.Min(minHeight, astronaut.body.localPosition.y);
                    maxHeight = Mathf.Max(maxHeight, astronaut.body.localPosition.y);
                    if (stageTime > 1.1f)
                    {
                        walked = Vector3.Distance(origin, explorer.PlayerPosition);
                        Require(walked > 1, "W input did not move the player.");
                        Require(maxSwing > 15 && maxHeight - minHeight > .05f, "Player moved without the reference walking animation.");
                        keys = new KeyboardState(); Next();
                    }
                }
                else if (stage == 2)
                {
                    pointer = pointer.WithButton(MouseButton.Left);
                    pointer.delta = new Vector2(3, 0);
                    if (stageTime > .5f)
                    {
                        orbitYaw = explorer.CameraTargetYaw;
                        Require(Mathf.Abs(orbitYaw) > .1f, "Left scene drag did not rotate the camera.");
                        pointer = new MouseState { position = new Vector2(100, 100) }; Next();
                    }
                }
                else if (stage == 3)
                {
                    keys = new KeyboardState(Key.R);
                    if (stageTime > .15f) { keys = new KeyboardState(); Next(); }
                }
                else if (stage == 4 && stageTime > .25f)
                {
                    Require(Mathf.Abs(explorer.CameraTargetYaw) < .0001f, "R did not recenter the camera.");
                    Finish(true, $"W moved {walked:F2}m, arm swing {maxSwing:F1} degrees, body bob {maxHeight - minHeight:F3}m, left drag yaw {orbitYaw:F3}rad, R recentered; virtual devices removed.");
                    return;
                }
                InputSystem.QueueStateEvent(keyboard, keys);
                InputSystem.QueueStateEvent(mouse, pointer);
            }
            catch (Exception error) { Finish(false, error.Message); }
        }

        static void Next() { stage++; stageTime = 0; }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static void Finish(bool passed, string message)
        {
            SessionState.SetBool(Pending, false);
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            keyboard = null; mouse = null;
            Application.runInBackground = background;
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ares-play.txt", (passed ? "PASS: " : "FAIL: ") + message + "\n");
            if (passed) Debug.Log("ARES Play controls checks passed: " + message); else Debug.LogError("ARES Play controls checks failed: " + message);
            EditorApplication.ExitPlaymode();
        }
    }
}
