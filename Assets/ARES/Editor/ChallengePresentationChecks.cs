using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ares.Tasks;
using Ares.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Exercises the native controls independently of the pure puzzle fixtures.</summary>
    [InitializeOnLoad]
    public static class ChallengePresentationChecks
    {
        const string Request = "Temp/ares-challenges-check.request", Pending = "ARES.ChallengePresentation";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string[] Tasks = { "TiltSun", "PipeFlow", "WireShield", "AlignCoils", "CmeCountdown", "IrrigationPlan", "SmartFarmCare", "CleanPanels", "SortFlares", "PowerBalance", "ResourcePriority", "ThermostatDial", "WindReading", "PhotoDetective" };
        static int task, stage;
        static double next, started;
        static bool background;
        static ChallengePresentationChecks() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request))
            {
                string token = File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if (SessionState.GetString(Pending + ".refresh", "") != token)
                { SessionState.SetString(Pending + ".refresh", token); AssetDatabase.Refresh(); return; }
                File.Delete(Request); SessionState.SetBool(Pending, true);
                task = stage = 0; background = Application.runInBackground; Application.runInBackground = true;
                started = EditorApplication.timeSinceStartup; next = started + 3;
                File.WriteAllText("Logs/ares-challenges.txt", DateTime.UtcNow.ToString("O") + " RUNNING");
                EditorApplication.isPaused = false; EditorApplication.EnterPlaymode(); return;
            }
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + .5;
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 120, "Presentation check timed out");
                var ui = UnityEngine.Object.FindAnyObjectByType<GameFrontend>(); Require(ui != null, "Frontend missing");
                var root = ui.GetComponent<UIDocument>().rootVisualElement;
                if (stage == 0)
                {
                    ui.StartMission(false);
                    var explorer=ui.GetComponent<Ares.World.ReferenceExplorer>();
                    bool iso=explorer.Isometric, roofs=explorer.RoofsVisible;
                    Press(root.Q<Button>("hud-camera")); Require(explorer.Isometric!=iso,"HUD camera toggle failed");
                    Press(root.Q<Button>("hud-camera")); Require(explorer.Isometric==iso,"HUD camera toggle did not restore");
                    Press(root.Q<Button>("hud-roof")); Require(explorer.RoofsVisible!=roofs,"HUD roofs toggle failed");
                    Press(root.Q<Button>("hud-roof")); Press(root.Q<Button>("hud-focus"));
                    stage++; return;
                }
                if (stage == 1) { Call(ui, "OpenTask", Tasks[task]); stage++; return; }
                if (stage == 2)
                {
                    CheckFrame(root);
                    if (Ares.Domain.GreenhouseSimulation.IsTask(Tasks[task])) { Require(root.Q("greenhouse-view")!=null,"Greenhouse view missing"); stage++; return; }
                    Require(Attempt(ui).Seconds == 0, "Briefing consumed time");
                    PressText(root, "Começar desafio", "Begin challenge"); stage++; return;
                }
                if (stage == 3)
                {
                    CheckFrame(root);
                    ScreenCapture.CaptureScreenshot("Logs/ares-challenge-" + Tasks[task] + ".png");
                    stage++; return;
                }
                if (stage == 4)
                {
                    if(Ares.Domain.GreenhouseSimulation.IsTask(Tasks[task])) { GreenhouseCheckFixture.CareForToday(ui.Campaign.Greenhouse,ui.Campaign.Watering); Call(ui,"OpenTask",Tasks[task]); }
                    else Exercise(root, Attempt(ui)); stage++; return;
                }
                if (stage == 5)
                {
                    CheckFrame(root);
                    ScreenCapture.CaptureScreenshot("Logs/ares-challenge-" + Tasks[task] + "-changed.png");
                    stage++; return;
                }
                if (++task < Tasks.Length) stage = 1; else Finish(null);
            }
            catch (Exception error) { Finish(error.ToString()); }
        }
        static TaskAttempt Attempt(GameFrontend ui) => (TaskAttempt)typeof(GameFrontend).GetField("attempt", Private).GetValue(ui);
        static void Call(GameFrontend ui, string method, params object[] args) => typeof(GameFrontend).GetMethod(method, Private).Invoke(ui, args);
        static void CheckFrame(VisualElement root)
        {
            var frame = root.Q(className: "mini-overlay"); var header = root.Q(className: "mini-header"); var footer = root.Q(className: "mini-footer");
            Require(frame != null && header != null && footer != null, "Challenge frame missing");
            Require(frame.worldBound.width > 300 && frame.worldBound.width <= root.worldBound.width + 1, "Frame horizontal overflow");
            Require(header.worldBound.yMin >= -1 && footer.worldBound.yMax <= root.worldBound.yMax + 1, "Header/footer outside viewport");
            Require(root.Query<Label>().ToList().All(label => !label.text.StartsWith("mini.") && !label.text.StartsWith("challenge.")), "Untranslated task key");
        }
        static void Exercise(VisualElement root, TaskAttempt a)
        {
            switch (a.Id)
            {
                case "TiltSun":
                    var angle = root.Q<SliderInt>("solar-angle"); Require(angle != null, "Solar angle slider missing");
                    foreach (int value in new[] { 0, 45, 90 })
                    {
                        angle.value = value; var art = root.Q("solar-tilt-art");
                        Require((int)art.GetType().GetProperty("Angle").GetValue(art) == a.Values[0], "Panel does not follow slider");
                        Require(a.Values[0] == value, "Solar angle input lost");
                    }
                    angle.value = a.Readings.Tilt; Require(!a.Solved, "Moving solar control auto-completed task"); break;
                case "PipeFlow":
                    for (int i = 0; i < 6; i++)
                        for (int turn = 0; turn < a.Rules.miniGames.dialTargets[i % 3] / 3; turn++) Press(root.Q<Button>("pipe-" + i));
                    Require(root.Query<Button>(className: "is-connected").ToList().Count == 6, "Pipe connection feedback missing");
                    Require(!a.Solved, "Pipes must wait for flow confirmation"); break;
                case "WireShield":
                    Drag(root.Q<Button>("wire-start-3"), root.Q<Button>("wire-end-3"));
                    Require(a.Done[3], "Wire drag did not connect");
                    Press(root.Q<Button>("wire-start-0")); Press(root.Q<Button>("wire-end-1"));
                    Require(a.Mistakes == 1 && !a.Done[0], "Wrong wire was accepted");
                    Press(root.Q<Button>("wire-start-0")); Press(root.Q<Button>("wire-end-0"));
                    Press(root.Q<Button>("wire-start-2")); Press(root.Q<Button>("wire-end-2"));
                    Require(a.Done[0] && a.Done[2], "Wire buttons do not connect"); break;
                case "AlignCoils":
                    for (int i = 0; i < 3; i++) root.Q<SliderInt>("coil-" + i).value = a.Rules.miniGames.dialTargets[i];
                    Require(a.Values.Take(3).SequenceEqual(a.Rules.miniGames.dialTargets), "Coil inputs lost"); break;
                case "CmeCountdown":
                    var speed = root.Q<SliderInt>("cme-speed"); speed.value = 1000;
                    Require(root.Q<Label>("cme-travel-hours").text.Contains("63"), "CME preview should show 63.3 hours at 1000 km/s");
                    speed.value = 0; Require(root.Q<Label>("cme-travel-hours").text.EndsWith(" 0"), "Zero speed produced invalid estimate");
                    speed.value = 1000; break;
                case "SortFlares":
                    Press(root.Q<Button>("flare-M")); Require(a.Mistakes == 1 && a.Stage == 0, "Flare order accepted wrong card");
                    Press(root.Q<Button>("flare-A")); Press(root.Q<Button>("flare-B"));
                    Require(!root.Q<Button>("flare-A").enabledInHierarchy && a.Stage == 2, "Flare slots not updated"); break;
                case "CleanPanels":
                    if (a.ActiveArray >= 0) { int i = a.ActiveArray; Press(root.Q<Button>("clean-array-" + i)); Require(a.Values[i] == 1, "Cleaning pass lost"); } break;
                case "IrrigationPlan":
                    var add = root.Query<Button>().ToList().Where(b => b.text == "+").ToList();
                    Press(add[0]); add = root.Query<Button>().ToList().Where(b => b.text == "+").ToList();
                    Press(add[0]); add = root.Query<Button>().ToList().Where(b => b.text == "+").ToList(); Press(add[2]);
                    Require(root.Query<Button>().ToList().Where(b => b.text == "+").All(b => !b.enabledInHierarchy), "Exhausted water still enabled"); break;
                case "SmartFarmCare":
                    var source = root.Q(className: "mini-play").Children().OfType<Button>().First();
                    Drag(source, root.Q<Button>("farm-bed-0"));
                    Require(a.Done[0] && a.Selected == -1, "Water drag did not irrigate/reset selection"); break;
            }
        }
        static void PressText(VisualElement root, params string[] values) => Press(root.Query<Button>().ToList().FirstOrDefault(b => values.Contains(b.text)));
        static void Drag(VisualElement source, VisualElement destination)
        {
            Require(source != null && destination != null, "Missing drag endpoints");
            Vector2 start = source.worldBound.center, end = destination.worldBound.center;
            using (var evt = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = start }))
            { evt.target = source; source.SendEvent(evt); }
            using (var evt = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseDrag, button = 0, mousePosition = end }))
            { evt.target = source; source.SendEvent(evt); }
            using (var evt = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = end }))
            { evt.target = source; source.SendEvent(evt); }
        }
        static void Press(Button button)
        {
            Require(button != null && button.enabledInHierarchy, "Missing or disabled control");
            using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish(string error)
        {
            SessionState.SetBool(Pending, false); Application.runInBackground = background;
            File.WriteAllText("Logs/ares-challenges.txt", DateTime.UtcNow.ToString("O") + (error == null
                ? " PASS: 14 native boards; frame bounds/localization; solar 0/45/90 and target; six pipe controls; correct/wrong wires and pointer drag; water pointer drag; coil sliders; live CME estimate; water budget; flare slots; cleaning passes. Screenshots captured. Synthetic UI events; physical touch devices require separate checks."
                : " FAIL " + Tasks[task] + " stage " + stage + ": " + error));
            EditorApplication.ExitPlaymode();
        }
    }
}
