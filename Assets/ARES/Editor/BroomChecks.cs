using System;
using System.IO;
using System.Reflection;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Ares.Editor
{
    /// <summary>Opt-in regression for broom pickup, grip and return. Does not open task windows.</summary>
    [InitializeOnLoad]
    public static class BroomChecks
    {
        const string Request = "Temp/ares-broom-check.request", Pending = "ARES.BroomCheck";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static Keyboard keyboard;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static AstronautAnimator actor;
        static Transform broom;
        static int stage, frame;
        static float elapsed;
        static double started;
        static bool background;
        static BroomChecks() { EditorApplication.update += Tick; }
        [MenuItem("ARES/Checks/Broom pickup and return")]
        public static void RequestCheck() { File.WriteAllText(Request, "check"); }
        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request))
            {
                string token = File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if (SessionState.GetString(Pending + ".refresh", "") != token)
                { SessionState.SetString(Pending + ".refresh", token); AssetDatabase.Refresh(); return; }
                File.Delete(Request);
                stage = 0; frame = -1; elapsed = 0; started = EditorApplication.timeSinceStartup;
                SessionState.SetBool(Pending, true); background = Application.runInBackground; Application.runInBackground = true;
                File.WriteAllText("Logs/ares-broom.txt", "RUNNING");
                EditorApplication.isPaused = false; EditorApplication.EnterPlaymode(); return;
            }
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying) return;
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 50, "Timeout");
                if (frame == Time.frameCount) return;
                frame = Time.frameCount; elapsed += Mathf.Min(Time.deltaTime, .05f);
                if (keyboard == null)
                {
                    ui = UnityEngine.Object.FindAnyObjectByType<GameFrontend>();
                    explorer = UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    if (ui == null || explorer == null || ui.Solar == null) return;
                    var plant = new SolarPlant(new SolarMaintenanceSettings()); plant.Tick(650);
                    var session = new SolarCleaningSession(plant); session.PickUpBroom(); session.Tick(0, true, false, 1);
                    Require(session.Progress > 0, "Cleaning fixture"); session.ReturnBroom();
                    Require(!session.HasBroom && session.Progress == 0 && session.ActivePanel == -1, "Return cancels cleaning");
                    Require(!session.Tick(0, true, false, 5), "Cannot clean after return");
                    keyboard = InputSystem.AddDevice<Keyboard>("ARES broom check");
                    ui.StartMission(false); ui.Campaign.CloseReport();
                    typeof(GameFrontend).GetMethod("CloseOverlay", Private).Invoke(ui, null);
                    ui.Campaign.SetAllocation(new Ares.Data.Allocation { shield = ui.Campaign.EnergyPool }); ui.Campaign.StartSol();
                    actor = explorer.GetComponentInChildren<AstronautAnimator>();
                    broom = ui.transform.Find("Solar maintenance/Solar cleaning broom");
                    SetPlayer(ui.Solar.BroomPickupPosition + Vector3.back); elapsed = 0;
                }
                keyboard.MakeCurrent(); var keys = new KeyboardState();
                if (stage == 0 && elapsed > .6f)
                { Require(ui.Solar.CanPickUp, "Pickup zone"); keys = new KeyboardState(Key.E); Next(); }
                else if (stage == 1 && elapsed > .3f)
                {
                    Require(ui.Solar.Cleaning.HasBroom && !ui.BlocksWorld && ui.Solar.CanReturn, "E pickup and return availability");
                    CheckGrip(); Capture("held"); Next();
                }
                else if (stage == 2)
                {
                    keys = new KeyboardState(Key.W);
                    if (elapsed > .4f) { CheckGrip(); Capture("walking"); Next(); }
                }
                else if (stage == 3 && elapsed > .2f)
                {
                    SetPlayer(ui.Solar.BroomPickupPosition + Vector3.back);
                    var view = (SolarBroomView)typeof(SolarMaintenance).GetField("broom", Private).GetValue(ui.Solar);
                    view.Update(actor, true, true, false, actor.transform.position - actor.transform.forward * 2 + Vector3.up * 1.5f);
                    CheckGrip(); Capture("cleaning"); Next();
                }
                else if (stage == 4 && elapsed > .3f) { keys = new KeyboardState(Key.E); Next(); }
                else if (stage == 5 && elapsed > .3f)
                {
                    Require(!ui.Solar.Cleaning.HasBroom && !ui.BlocksWorld && ui.Solar.CanPickUp, "E must return before opening a station task");
                    Require(Vector3.Distance(broom.position, ui.Solar.BroomPickupPosition + Vector3.up * .21f) < .001f, "Returned to rack");
                    keys = new KeyboardState(Key.E); Next();
                }
                else if (stage == 6 && elapsed > .3f)
                {
                    Require(ui.Solar.Cleaning.HasBroom && !ui.BlocksWorld, "Broom can be picked up again");
                    CheckGrip(); Finish("PASS: actual E pickup/return/re-pickup; no task overlay; return cancels cleaning; glove alignment idle/walking/cleaning; rack placement. Captures: ares-broom-*.png."); return;
                }
                InputSystem.QueueStateEvent(keyboard, keys);
            }
            catch (Exception error) { Finish("FAIL stage " + stage + ": " + error); }
        }
        static void CheckGrip()
        {
            var glove = actor.rightArm.Find("astronaut_17");
            Require(glove != null && Vector3.Distance(glove.position, broom.Find("Grip").position) < .001f, "Grip must remain inside imported glove");
        }
        static void Capture(string name)
        {
            var go = new GameObject("Broom inspection camera"); var camera = go.AddComponent<Camera>(); camera.CopyFrom(Camera.main);
            camera.transform.position = actor.transform.position + new Vector3(3, 2.1f, 4);
            camera.transform.LookAt(actor.transform.position + Vector3.up * 1.05f); camera.fieldOfView = 40;
            var target = new RenderTexture(800, 800, 24); var previous = RenderTexture.active;
            var texture = new Texture2D(800, 800, TextureFormat.RGB24, false);
            try { camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 800, 800), 0, 0); texture.Apply(); File.WriteAllBytes("Logs/ares-broom-" + name + ".png", texture.EncodeToPNG()); }
            finally { RenderTexture.active = previous; camera.targetTexture = null; UnityEngine.Object.DestroyImmediate(go);
                target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture); }
        }
        static void SetPlayer(Vector3 position)
        { var player = (PlayerMotion)typeof(ReferenceExplorer).GetField("player", Private).GetValue(explorer);
            player.X = position.x; player.Z = -position.z; player.Speed = player.VelocityX = player.VelocityZ = 0; }
        static void Next() { stage++; elapsed = 0; }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static void Finish(string result)
        {
            SessionState.SetBool(Pending, false); if (keyboard != null) InputSystem.RemoveDevice(keyboard); keyboard = null;
            Application.runInBackground = background; File.WriteAllText("Logs/ares-broom.txt", DateTime.UtcNow.ToString("O") + " " + result);
            EditorApplication.ExitPlaymode();
        }
    }
}
