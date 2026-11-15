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
    [InitializeOnLoad]
    public static class CommandConsolePreview
    {
        const string Pending = "ARES.CommandConsole", Log = "Logs/ares-command-console.txt";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static GameFrontend ui;
        static VisualElement root;
        static Keyboard keyboard;
        static int stage;
        static double next, began;
        static float clock;
        static bool background;
        static CommandConsolePreview() { EditorApplication.update += Tick; }
        public static void Begin()
        {
            ui = null; keyboard = null; stage = 0; next = began = 0;
            background = Application.runInBackground; Application.runInBackground = true;
            File.WriteAllText(Log, "RUNNING"); SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (began == 0) began = EditorApplication.timeSinceStartup;
                Require(EditorApplication.timeSinceStartup - began < 65, "Console preview timeout");
                keyboard?.MakeCurrent();
                if (EditorApplication.timeSinceStartup < next) return;
                next = EditorApplication.timeSinceStartup + 1.2;
                if (ui == null)
                {
                    ui = UnityEngine.Object.FindAnyObjectByType<GameFrontend>(); if (ui == null) return;
                    root = ui.GetComponent<UIDocument>().rootVisualElement; ui.Apod.OfflineOnly = true; next += 2; return;
                }
                switch (stage++)
                {
                    case 0:
                        ui.StartMission(false); ui.Campaign.CloseReport(); Call("CloseOverlay");
                        Require(MissionCommands.Execute(ui.Campaign, "/event (WindStorm)") == MissionCommandResult.Unavailable, "Command started before energy plan");
                        ui.Campaign.SetAllocation(new Allocation { shield = 50, life = 25, greenhouse = 25 }); ui.Campaign.StartSol();
                        keyboard = InputSystem.AddDevice<Keyboard>("Ares console preview"); keyboard.MakeCurrent(); break;
                    case 1: InputSystem.QueueTextEvent(keyboard, '\''); break;
                    case 2:
                        Require(root.Q("command-console") != null && ui.BlocksWorld, "Apostrophe did not open console/lock controls");
                        Require(root.panel.focusController.focusedElement != null, "Console input unfocused");
                        clock = ui.Campaign.SolElapsedSeconds;
                        root.Q<TextField>("command-input").value = "/event (Wrong)"; Press();
                        Require(!ui.Campaign.Demands.DustInAir && root.Q<Label>("command-feedback").text.Contains("WindStorm"), "Invalid command changed weather or omitted usage");
                        root.Q<TextField>("command-input").value = "/event (WindStorm)";
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter)); break;
                    case 3:
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Require(ui.Campaign.Demands.DustInAir && ui.Campaign.Solar.BuriedPanels == 32 && ui.Campaign.MandatoryCompleted == 0, "Enter did not start real storm independently of mandatory gate");
                        Require(ui.Campaign.SolElapsedSeconds == clock, "Clock advanced while typing");
                        Require(root.Q<TextField>("command-input").value == "/event (WindStorm)", "Command text changed unexpectedly");
                        ScreenCapture.CaptureScreenshot("Logs/ares-command-console.png"); break;
                    case 4: InputSystem.QueueTextEvent(keyboard, '\''); break;
                    case 5:
                        Require(root.Q("command-console") == null && !ui.BlocksWorld && ui.Campaign.SolElapsedSeconds > clock, "Apostrophe did not resume gameplay");
                        Require(MissionCommands.Execute(ui.Campaign, "/event WindStorm") == MissionCommandResult.EventActive, "Duplicate command restarted active event");
                        ui.Campaign.Tick(35, false);
                        Require(MissionCommands.Execute(ui.Campaign, "/event windstorm") == MissionCommandResult.StormStarted, "Bare/case-insensitive alias failed");
                        InputSystem.QueueTextEvent(keyboard, '\''); break;
                    case 6:
                        Require(root.Q("command-console") != null, "Console could not reopen");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); break;
                    case 7:
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Require(root.Q("command-console") == null && !ui.BlocksWorld, "Escape opened settings or left input blocked");
                        var explorer = ui.GetComponent<ReferenceExplorer>();
                        var player = (PlayerMotion)typeof(ReferenceExplorer).GetField("player", Private).GetValue(explorer);
                        player.X = explorer.computerSettings.standPoint.x; player.Z = -explorer.computerSettings.standPoint.z;
                        Require(ui.Terminal.TryInteract(), "Computer did not open"); next += 2; break;
                    case 8: InputSystem.QueueTextEvent(keyboard, '\''); break;
                    case 9:
                        Require(root.Q("command-console") != null && root.Q("command-console").resolvedStyle.opacity > .9f, "Computer hid the console");
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); break;
                    case 10:
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        Require(root.Q("command-console") == null && ui.Terminal.OwnsCamera, "Closing console lost underlying computer view");
                        File.AppendAllText(Log, "\nPASS: actual apostrophe input; Enter execution; invalid-command feedback; real panel burial without mandatory rewards; paused clock; aliases; active-event guard; apostrophe/Escape close; computer overlay visibility and restoration."); Finish(); break;
                }
            }
            catch (Exception error) { File.AppendAllText(Log, "\nFAIL: " + error); Debug.LogException(error); Finish(); }
        }
        static void Call(string method) => typeof(GameFrontend).GetMethod(method, Private).Invoke(ui, null);
        static void Press() => GreenhouseCheckFixture.Press(root, "command-execute");
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish() { SessionState.SetBool(Pending, false); if (keyboard != null) InputSystem.RemoveDevice(keyboard); keyboard = null; Application.runInBackground = background; ui = null; EditorApplication.ExitPlaymode(); }
    }
}
