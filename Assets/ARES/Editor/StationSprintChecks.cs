using System;
using System.IO;
using System.Linq;
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
    /// <summary>Real E/Shift events through an isolated virtual keyboard, including editor-safe Exit.</summary>
    [InitializeOnLoad]
    public static class StationSprintChecks
    {
        const string Request = "Temp/ares-station-sprint-check.request";
        const string Pending = "ARES.StationSprint", ExitExpected = Pending + ".exit";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static Keyboard keyboard;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static SprintDust dust;
        static GameDefinition definition;
        static int stage, station, lastFrame, puffs;
        static float elapsed, walked, ran;
        static double started;
        static Vector3 origin;
        static bool background;

        static StationSprintChecks()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Pending, false)) return;
                bool expected = SessionState.GetBool(ExitExpected, false);
                SessionState.SetBool(Pending, false); SessionState.SetBool(ExitExpected, false); Cleanup();
                File.WriteAllText("Logs/ares-station-sprint.txt", DateTime.UtcNow.ToString("O") + (expected
                    ? $" PASS: all 25 Sol/area policies; actual E at all five room centers and station points; Escape/reopen; walk {walked:F2}m, sprint {ran:F2}m; dust only during movement with Shift; overlays block running/emission; menu Exit stopped Play without closing the editor."
                    : " FAIL: Play interrupted before checks completed."));
            };
        }
        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request))
            {
                string token = File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if (SessionState.GetString(Pending + ".refresh", "") != token)
                { SessionState.SetString(Pending + ".refresh", token); AssetDatabase.Refresh(); return; }
                File.Delete(Request); SessionState.SetBool(Pending, true); SessionState.SetBool(ExitExpected, false);
                stage = station = 0; elapsed = 0; lastFrame = -1; started = EditorApplication.timeSinceStartup;
                background = Application.runInBackground; Application.runInBackground = true;
                File.WriteAllText("Logs/ares-station-sprint.txt", "RUNNING " + DateTime.UtcNow.ToString("O"));
                EditorApplication.isPaused = false; EditorApplication.EnterPlaymode(); return;
            }
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || SessionState.GetBool(ExitExpected, false)) return;
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 60, "Test timed out");
                if (Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount; elapsed += Mathf.Min(Time.deltaTime, .05f);
                if (keyboard == null)
                {
                    ui = UnityEngine.Object.FindAnyObjectByType<GameFrontend>();
                    explorer = UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    Require(ui != null && explorer != null, "Open BaseARES before running checks");
                    definition = explorer.definition; dust = explorer.GetComponent<SprintDust>();
                    if (dust == null) return;
                    VerifyPolicy();
                    var shader = Resources.Load<Shader>("ARES/Effects/CartoonDust");
                    Require(shader != null && !ShaderUtil.ShaderHasError(shader), "Dust shader failed to compile");
                    keyboard = InputSystem.AddDevice<Keyboard>("ARES station/sprint test");
                    ui.StartMission(); ui.Campaign.CloseReport(); Call("CloseOverlay");
                    ui.Campaign.SetAllocation(new Allocation { shield = ui.Campaign.EnergyPool });
                    Require(ui.Campaign.StartSol(), "Could not start Sol fixture");
                    MoveToStation(); elapsed = 0;
                }
                keyboard.MakeCurrent(); var keys = new KeyboardState();
                if (stage == 0 && elapsed > .15f) { keys = new KeyboardState(Key.E); Next(); }
                else if (stage == 1 && elapsed > .15f)
                {
                    string id = ui.Campaign.Current.tasks[station % 5].id;
                    Require(ui.Campaign.OpenTask == id && ui.BlocksWorld, "E failed in " + ui.Campaign.Current.tasks[station % 5].room);
                    keys = new KeyboardState(Key.Escape); Next();
                }
                else if (stage == 2 && elapsed > .15f)
                {
                    Require(ui.Campaign.OpenTask == null && !ui.BlocksWorld, "Escape did not release task");
                    if (++station < 10) { MoveToStation(); stage = 0; elapsed = 0; }
                    else { ui.SetInspectionTestMode(); SetPlayer(Vector3.zero); explorer.RecenterCamera(); origin = explorer.PlayerPosition; stage = 10; elapsed = 0; }
                }
                else if (stage == 10)
                {
                    keys = new KeyboardState(Key.W);
                    if (elapsed > 1)
                    {
                        walked = Vector3.Distance(origin, explorer.PlayerPosition); Require(walked > 2, "Walking did not move");
                        Require(dust.EmittedPuffs == 0, "Walking emitted sprint dust");
                        SetPlayer(Vector3.zero); origin = explorer.PlayerPosition; keys = new KeyboardState(); Next();
                    }
                }
                else if (stage == 11)
                {
                    keys = new KeyboardState(Key.LeftShift);
                    if (elapsed > .3f)
                    { Require(!explorer.IsRunning && dust.EmittedPuffs == 0, "Shift while idle emitted dust"); Next(); }
                }
                else if (stage == 12)
                {
                    keys = new KeyboardState(Key.W, Key.LeftShift);
                    if (elapsed > 1)
                    {
                        ran = Vector3.Distance(origin, explorer.PlayerPosition);
                        Require(ran > walked * 1.4f && explorer.IsRunning, "Shift did not increase speed");
                        Require(dust.EmittedPuffs > 0 && dust.LiveParticles > 0, "Running did not emit visible particles");
                        ScreenCapture.CaptureScreenshot("Logs/ares-sprint.png"); puffs = dust.EmittedPuffs;
                        keys = new KeyboardState(Key.W); Next();
                    }
                }
                else if (stage == 13)
                {
                    keys = new KeyboardState(Key.W);
                    if (elapsed > .4f)
                    {
                        Require(!explorer.IsRunning && dust.EmittedPuffs == puffs, "Releasing Shift kept emission active");
                        Call("ShowTitle"); origin = explorer.PlayerPosition; keys = new KeyboardState(); Next();
                    }
                }
                else if (stage == 14)
                {
                    keys = new KeyboardState(Key.W, Key.RightShift);
                    if (elapsed > .4f)
                    {
                        Require(explorer.PlayerPosition == origin && !explorer.IsRunning && dust.EmittedPuffs == puffs, "Menu did not block sprint");
                        var root = ui.GetComponent<UIDocument>().rootVisualElement;
                        var button = root.Q<Button>("title-exit"); Require(button != null && (button.text == "Sair" || button.text == "Exit"), "Exit button missing/untranslated");
                        ScreenCapture.CaptureScreenshot("Logs/ares-title-exit.png"); Next();
                    }
                }
                else if (stage == 15 && elapsed > .2f)
                {
                    var button = ui.GetComponent<UIDocument>().rootVisualElement.Q<Button>("title-exit");
                    Cleanup(); SessionState.SetBool(ExitExpected, true);
                    using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
                    return;
                }
                InputSystem.QueueStateEvent(keyboard, keys);
            }
            catch (Exception error)
            {
                SessionState.SetBool(Pending, false); Cleanup();
                File.WriteAllText("Logs/ares-station-sprint.txt", "FAIL stage " + stage + ": " + error);
                EditorApplication.ExitPlaymode();
            }
        }
        static void VerifyPolicy()
        {
            var interaction = new StationInteraction(definition.layout, definition.world.useRadius);
            var control = definition.layout.rooms.First(room => room.id == "control");
            foreach (var sol in definition.campaign.sols)
            {
                var campaign = new Campaign(new CampaignData { sols = new[] { sol } }, definition.rules);
                campaign.CloseReport();
                foreach (var task in sol.tasks)
                {
                    var room = definition.layout.rooms.First(r => r.id == task.room);
                    Require(interaction.Resolve(campaign, room.Center).Action == StationAction.NeedsPlan, "Unplanned task offered");
                }
                Require(interaction.Resolve(campaign, control.Center).Action == StationAction.PlanEnergy, "Control center cannot plan");
                campaign.SetAllocation(new Allocation { shield = campaign.EnergyPool }); campaign.StartSol();
                Require(!interaction.Resolve(campaign, new Vector3(0, 0, 24)).CanUse, "Station offered from corridor");
                foreach (var task in sol.tasks)
                {
                    var room = definition.layout.rooms.First(r => r.id == task.room);
                    Require(interaction.Resolve(campaign, room.Center).TaskId == task.id, "Room center resolved wrong task");
                    Require(interaction.Resolve(campaign, room.Station).TaskId == task.id, "Original station lost interaction");
                    Require(campaign.BeginTask(task.id, room.id) && GreenhouseCheckFixture.Complete(campaign, task.id), "Fixture task failed");
                    Require(interaction.Resolve(campaign, room.Center).Action == StationAction.Completed, "Completed task offered twice");
                }
                Require(interaction.Resolve(campaign, control.Center).Action == StationAction.SealSol, "Seal differs from use area");
            }
        }
        static void MoveToStation()
        {
            var task = ui.Campaign.Current.tasks[station % 5];
            var room = definition.layout.rooms.First(r => r.id == task.room);
            SetPlayer(station < 5 ? room.Center : room.Station);
        }
        static void SetPlayer(Vector3 position)
        {
            var player = (PlayerMotion)typeof(ReferenceExplorer).GetField("player", Private).GetValue(explorer);
            player.X = position.x; player.Z = -position.z; player.VelocityX = player.VelocityZ = player.Speed = 0;
        }
        static void Call(string method) => typeof(GameFrontend).GetMethod(method, Private).Invoke(ui, null);
        static void Next() { stage++; elapsed = 0; }
        static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
        static void Cleanup()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            keyboard = null; Application.runInBackground = background;
        }
    }
}
