using System;
using System.IO;
using Ares.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Opt-in visual check of the telescope only; uses attributed local images, without API calls.</summary>
    [InitializeOnLoad]
    public static class ObservatoryPresentationChecks
    {
        const string Pending = "ARES.ObservatoryPresentation";
        static GameFrontend frontend;
        static VisualElement root;
        static int stage, frame;
        static float elapsed;
        static double started;
        static bool background;
        static ObservatoryPresentationChecks() { EditorApplication.update += Tick; }
        [MenuItem("ARES/Checks/Telescope screen only")]
        public static void Request() => File.WriteAllText("Temp/ares-computer-check.request", "telescope-preview");
        public static void Begin()
        {
            stage = 0; frame = -1; elapsed = 0; frontend = null; started = EditorApplication.timeSinceStartup;
            background = Application.runInBackground; Application.runInBackground = true;
            SessionState.SetBool(Pending, true); File.WriteAllText("Logs/ares-telescope.txt", "RUNNING");
            EditorApplication.isPaused = false; EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 45, "Timeout");
                if (frame == Time.frameCount) return;
                frame = Time.frameCount; elapsed += Mathf.Min(Time.deltaTime, .05f);
                if (frontend == null)
                {
                    frontend = UnityEngine.Object.FindAnyObjectByType<GameFrontend>(); if (frontend == null) return;
                    frontend.Apod.OfflineOnly = true; frontend.StartMission(false); frontend.Campaign.CloseReport();
                    frontend.ShowObservatory(); root = frontend.GetComponent<UIDocument>().rootVisualElement; elapsed = 0;
                }
                if (stage == 0 && elapsed > 1)
                {
                    var lens = root.Q("observatory-lens");
                    Require(Mathf.Abs(lens.worldBound.width - lens.worldBound.height) < 1, "Lens must be circular");
                    Require(root.Q<Image>("observatory-photo").image != null, "Local image missing");
                    Require(lens.worldBound.yMin >= 0 && lens.worldBound.yMax < root.worldBound.yMax - 145, "Lens clips controls");
                    Require(frontend.Apod.RequestCount == 0, "Preview should remain offline");
                    ScreenCapture.CaptureScreenshot("Logs/ares-telescope.png"); Next();
                }
                else if (stage == 1 && elapsed > .3f)
                {
                    Press("observatory-zoom-in");
                    Require(root.Q<Slider>("observatory-zoom").value > 1, "Zoom button failed"); Next();
                }
                else if (stage == 2 && elapsed > .3f)
                {
                    Require(root.Q<Image>("observatory-photo").worldBound.width > root.Q("observatory-lens").worldBound.width, "Zoom must enlarge photo inside mask");
                    root.Q<Slider>("observatory-zoom").value = 3;
                    Require(!root.Q<Button>("observatory-zoom-in").enabledSelf, "Zoom maximum not enforced");
                    Press("observatory-observe"); Next();
                }
                else if (stage == 3 && elapsed > .3f)
                {
                    Require(root.Q("observatory-details").resolvedStyle.display == DisplayStyle.Flex, "Observe details failed");
                    Require(!root.Q<Button>("observatory-day-1").enabledSelf, "Future discovery unlocked");
                    ScreenCapture.CaptureScreenshot("Logs/ares-telescope-details.png"); Next();
                }
                else if (stage == 4 && elapsed > .3f)
                {
                    Press("observatory-details-close");
                    var view = root.Q("observatory-view"); view.style.width = 480; view.style.height = 600;
                    root.Q<Slider>("observatory-zoom").value = 1; Next();
                }
                else if (stage == 5 && elapsed > .4f)
                {
                    var view = root.Q("observatory-view");
                    Require(root.Q<Button>("observatory-observe").worldBound.yMax < view.worldBound.yMax, "Compact controls clipped");
                    Require(root.Q("observatory-lens").worldBound.xMax <= view.worldBound.xMax, "Compact lens clipped");
                    ScreenCapture.CaptureScreenshot("Logs/ares-telescope-compact.png"); Next();
                }
                else if (stage == 6 && elapsed > .3f)
                {
                    Press("observatory-close"); Require(root.Q("observatory-view") == null, "Close failed");
                    Finish("PASS: circular lens, local photo, zoom controls/limits, observe details, locked future discoveries, compact layout, close. No API calls.");
                }
            }
            catch (Exception error) { Finish("FAIL: " + error); }
        }
        static void Press(string name)
        {
            var button = root.Q<Button>(name); Require(button != null && button.enabledSelf, "Button unavailable: " + name);
            using (var evt = NavigationSubmitEvent.GetPooled()) { evt.target = button; button.SendEvent(evt); }
        }
        static void Next() { stage++; elapsed = 0; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish(string result)
        {
            SessionState.SetBool(Pending, false); File.WriteAllText("Logs/ares-telescope.txt", result);
            Application.runInBackground = background; EditorApplication.ExitPlaymode();
        }
    }
}
