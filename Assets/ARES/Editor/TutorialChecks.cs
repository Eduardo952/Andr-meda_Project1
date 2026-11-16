using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>One focused onboarding walkthrough: actual UI, proximity, energy commit and watering ledger.</summary>
    [InitializeOnLoad]
    public static class TutorialChecks
    {
        const string Pending = "ARES.TutorialCheck";
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static VisualElement root;
        static int stage, frame, shelf;
        static float elapsed;
        static double started;
        static bool background;
        static Action afterCapture;
        static int captureFrames;
        static TutorialChecks() { EditorApplication.update += Tick; }
        [MenuItem("ARES/Checks/Ares guided tutorial")]
        public static void Request() => File.WriteAllText("Temp/ares-computer-check.request", "tutorial");
        public static void Begin()
        {
            ui = null; stage = 0; frame = -1; elapsed = 0; shelf = 0; afterCapture = null; started = EditorApplication.timeSinceStartup;
            background = Application.runInBackground; Application.runInBackground = true;
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/ares-tutorial.txt", "RUNNING");
            SessionState.SetBool(Pending, true); EditorApplication.isPaused = false; EditorApplication.EnterPlaymode();
        }
        static void StartAtCenter()
        {
            ui.StartMission();
            // Wake ordering is tested in RefinementChecks; this fixture exercises tutorial content.
            for(int i=0;i<100;i++)explorer.Rest.Tick(.05f,false);
            var motion=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(explorer);
            motion.X=0;motion.Z=3;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 110, "Tutorial walkthrough timeout at stage " + stage);
                if (frame == Time.frameCount) return;
                frame = Time.frameCount; elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
                if (afterCapture != null)
                {
                    if (--captureFrames <= 0) { var next = afterCapture; afterCapture = null; next(); }
                    return;
                }
                if (ui == null)
                {
                    explorer = UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    if (explorer?.Rover == null) return;
                    ui = explorer.GetComponent<GameFrontend>(); ui.Apod.OfflineOnly = true; explorer.reducedMotion = false;
                    StartAtCenter(); root = ui.GetComponent<UIDocument>().rootVisualElement; elapsed = 0;
                }
                Require(ui.Campaign.SolElapsedSeconds == 0 || !ui.TutorialActive, "Tutorial spent mission time");
                if (stage == 0 && elapsed > 1.3f)
                {
                    Require(ui.TutorialStage == TutorialStage.Offer && ui.BlocksWorld, "Center offer did not own input");
                    CheckDialogue(); Capture("offer"); Next();
                }
                else if (stage == 1 && elapsed > .2f)
                {
                    root.Q("tutorial-layer").style.width = 1100; root.Q("tutorial-layer").style.height = 700;
                    stage = 101; elapsed = 0;
                }
                else if (stage == 101 && elapsed > .4f)
                {
                    CheckDialogue(); CaptureThen("offer-compact", () => { Press("tutorial-decline"); Require(!ui.TutorialActive, "Decline did not leave onboarding"); stage = 2; elapsed = 0; });
                }
                else if (stage == 2 && elapsed > .3f)
                {
                    Require(!ui.BlocksWorld && root.Q("mission-hud-root").enabledInHierarchy, "Decline did not restore normal play");
                    StartAtCenter(); Next();
                }
                else if (stage == 3 && elapsed > .3f) { Press("tutorial-accept"); Next(); }
                else if (stage == 4 && elapsed > 1)
                { Require(ui.TutorialStage == TutorialStage.EnergyIntro, "First task is not energy"); CheckDialogue(); CaptureThen("energy-intro", () => { Press("tutorial-continue"); SetPlayer(explorer.computerSettings.standPoint); Next(); }); }
                else if (stage == 5 && elapsed > .4f) { Press("tutorial-interact"); Next(); }
                else if (stage == 6 && ui.TutorialStage == TutorialStage.EnergyExplain && elapsed > 2.6f)
                { CheckDialogue(); CaptureThen("energy-mentor", () => { Press("tutorial-continue"); Next(); }); }
                else if (stage == 7 && elapsed > .8f && root.Q<SliderInt>("energy-shield") != null)
                {
                    Require(!root.Q<Button>("energy-confirm").enabledInHierarchy, "Incomplete energy budget accepted");
                    root.Q<SliderInt>("energy-shield").value = 30;
                    root.Q<SliderInt>("energy-life").value = 40;
                    root.Q<SliderInt>("energy-greenhouse").value = 30;
                    Require(ui.Campaign.Allocation.Total == ui.Campaign.EnergyPool, "Sliders failed to allocate energy");
                    Capture("energy-controls"); Next();
                }
                else if (stage == 8 && elapsed > .3f) { Press("energy-confirm"); Next(); }
                else if (stage == 9 && ui.TutorialStage == TutorialStage.PlantsIntro && elapsed > 2.5f)
                { CheckDialogue(); CaptureThen("plants-intro", () => { Press("tutorial-continue"); SetPlayer(ui.GreenhouseCare.PickupPosition); Next(); }); }
                else if (stage == 10 && elapsed > .4f) { Press("tutorial-interact"); Next(); }
                else if (stage == 11 && ui.TutorialStage == TutorialStage.FillIntro && elapsed > .9f)
                { CheckDialogue(); CaptureThen("fill-mentor", () => { Press("tutorial-continue"); SetPlayer(ui.GreenhouseCare.RefillPosition); Next(); }); }
                else if (stage == 12 && elapsed > .4f) { Press("tutorial-interact"); Next(); }
                else if (stage == 13 && ui.TutorialStage == TutorialStage.WaterIntro && elapsed > .9f)
                { CheckDialogue(); CaptureThen("water-mentor", () => { Press("tutorial-continue"); SetPlayer(ui.GreenhouseCare.ShelfPositions[shelf]); Next(); }); }
                else if (stage == 14 && elapsed > .4f)
                {
                    Require(ui.TutorialStage == TutorialStage.WaterPlants, "Watering objective missing");
                    CaptureThen("water-objective-" + shelf, () => {
                        Press("tutorial-interact");
                        Require(ui.Campaign.Watering.ActiveShelf == shelf, "Real watering interaction did not start");
                        // The editor may be unfocused. Advance only the domain's bounded pour clock after the real interaction.
                        Require(ui.Campaign.Watering.Tick(shelf, false, 3), "Water dose was not delivered");
                        shelf++; Next();
                    });
                }
                else if (stage == 15 && elapsed > .4f)
                {
                    if (shelf < 4) { Require(ui.TutorialStage == TutorialStage.FillCan, "Refill guidance missing"); SetPlayer(ui.GreenhouseCare.RefillPosition); stage = 16; elapsed = 0; }
                    else { Require(ui.TutorialStage == TutorialStage.Complete, "Tutorial finished before all rows were cared for"); stage = 18; elapsed = 0; }
                }
                else if (stage == 16 && elapsed > .4f) { Press("tutorial-interact"); Next(); }
                else if (stage == 17 && elapsed > .3f) { SetPlayer(ui.GreenhouseCare.ShelfPositions[shelf]); stage = 14; elapsed = 0; }
                else if (stage == 18 && elapsed > 1)
                {
                    Require(ui.Campaign.Watering.DeliveredMl == 2040 && ui.Campaign.Watering.ReservoirMl == 8960, "Tutorial changed water accounting");
                    Require(ui.Campaign.Completed("SmartFarmCare"), "Completed watering task was not recorded");
                    CheckDialogue(); CaptureThen("complete", () => { Press("tutorial-continue"); Next(); });
                }
                else if (stage == 19 && elapsed > .3f)
                {
                    Require(!ui.TutorialActive && !ui.BlocksWorld && root.Q("tutorial-layer") == null, "Tutorial did not release UI/input");
                    Require(explorer.GetComponentInChildren<BubbleAnimator>().GetComponentsInChildren<Renderer>().All(r => !r.forceRenderingOff), "World pet was not restored");
                    Require(!Resources.FindObjectsOfTypeAll<Camera>().Any(c => c.name == "ARES / mentor camera"), "Mentor camera leaked");
                    File.WriteAllText("Logs/ares-tutorial.txt", "PASS: spawn opt-in/decline; isolated HUD; real terminal/sliders/commit; Ares dialogue and floating portrait; can pickup/refill; all four rows through proximity/UI with controlled pour clock; exact 2040 mL ledger; task recording; paused tutorial clock; restored pet/input/HUD and disposed portrait camera. Captures: ares-tutorial-*.png");
                    Finish();
                }
            }
            catch (Exception e) { File.WriteAllText("Logs/ares-tutorial.txt", "FAIL stage " + stage + ": " + e); Debug.LogException(e); Finish(); }
        }
        static void CheckDialogue()
        {
            Require(root.Q("mission-hud-root").resolvedStyle.display == DisplayStyle.None, "Unrelated HUD visible");
            var balloon = root.Q("tutorial-balloon").worldBound;
            var speech = root.Q("tutorial-speech").worldBound;
            Require(balloon.Contains(speech.min) && balloon.Contains(speech.max), "Speech clipped outside balloon");
            foreach (var button in root.Q("tutorial-dialogue").Query<Button>().ToList())
                Require(balloon.Contains(button.worldBound.min) && balloon.Contains(button.worldBound.max), "Dialogue button clipped outside balloon");
            Require(root.Q<Image>("tutorial-ares").image is RenderTexture target && target.IsCreated(), "Mentor portrait not rendered");
            Require(root.panel.focusController.focusedElement is VisualElement focused && root.Q("tutorial-layer").Contains(focused), "Dialogue lost focus");
        }
        static void SetPlayer(Vector3 position)
        {
            var motion = (PlayerMotion)typeof(ReferenceExplorer).GetField("player", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(explorer);
            motion.X = position.x; motion.Z = -position.z; motion.VelocityX = motion.VelocityZ = motion.Speed = 0;
        }
        static void Press(string name)
        {
            var button = root.Q<Button>(name); Require(button != null && button.enabledInHierarchy && button.resolvedStyle.display != DisplayStyle.None, "Missing tutorial control: " + name);
            using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
        }
        static void Capture(string name) => ScreenCapture.CaptureScreenshot("Logs/ares-tutorial-" + name + ".png");
        static void CaptureThen(string name, Action next) { Capture(name); afterCapture = next; captureFrames = 3; }
        static void Next() { stage++; elapsed = 0; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish() { SessionState.SetBool(Pending, false); Application.runInBackground = background; EditorApplication.ExitPlaymode(); }
    }
}
