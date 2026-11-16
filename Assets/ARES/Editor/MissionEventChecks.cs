using System;
using System.IO;
using System.Reflection;
using Ares.Data;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Deterministic gameplay boundaries plus the real shield UI and immediate death screen.</summary>
    [InitializeOnLoad]
    public static class MissionEventChecks
    {
        const string Pending = "ARES.MissionEvents", Log = "Logs/ares-mission-events.txt";
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static VisualElement root;
        static int stage, seed;
        static double began, next;
        static string language;
        static bool background;
        static MissionEventChecks() { EditorApplication.update += Tick; }
        public static void Begin()
        {
            stage = 0; ui = null; began = next = 0;
            language = Ares.UI.Localization.CurrentLanguage; PlayerPrefs.SetString("ARES.Language", "pt-BR");
            background = Application.runInBackground; Application.runInBackground = true;
            Directory.CreateDirectory("Logs"); File.WriteAllText(Log, "RUNNING");
            SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (began == 0) began = EditorApplication.timeSinceStartup;
                Require(EditorApplication.timeSinceStartup - began < 100, "Event review timeout");
                if (EditorApplication.timeSinceStartup < next) return;
                if (ui == null)
                {
                    ui = UnityEngine.Object.FindAnyObjectByType<GameFrontend>(); if (ui == null) return;
                    explorer = ui.GetComponent<ReferenceExplorer>(); root = ui.GetComponent<UIDocument>().rootVisualElement;
                    ui.Apod.OfflineOnly = true; next = EditorApplication.timeSinceStartup + 2; return;
                }
                next = EditorApplication.timeSinceStartup + 1;
                switch (stage++)
                {
                    case 0: DomainChecks(); ui.StartMission(false); Install(AtDay(seed, 2)); break;
                    case 1:
                        SetPlayer("shield"); Call("ShowShieldRegulation"); break;
                    case 2:
                        Require(root.Q("shield-regulation") != null, "Optional shield popup missing");
                        Require(!root.Q<Button>("shield-activate").enabledSelf, "Unaligned activation enabled");
                        Require(root.panel.focusController.focusedElement != null, "Popup has no keyboard focus");
                        ScreenCapture.CaptureScreenshot("Logs/ares-events-shield-pt.png"); stage = 20; break;
                    case 20: root.style.width = 1280; root.style.height = 720; stage = 3; break;
                    case 3:
                        var action = root.Q<Button>("shield-activate");
                        Require(action.worldBound.yMax <= root.worldBound.yMax && action.worldBound.xMax <= root.worldBound.xMax, "Compact shield action clipped");
                        for (int i = 0; i < 3; i++) Require(root.Q<SliderInt>("shield-dial-" + i).worldBound.yMax <= action.worldBound.yMin, "Compact shield dial clipped");
                        PlayerPrefs.SetString("ARES.Language", "en");
                        var locale = typeof(GameFrontend).GetField("locale", Private).GetValue(ui); locale.GetType().GetMethod("Set").Invoke(locale, new object[] { "en" }); break;
                    case 4:
                        Require(root.Q<Button>("shield-activate").text == "Activate protection for today", "Shield control not translated");
                        ScreenCapture.CaptureScreenshot("Logs/ares-events-shield-en-compact.png"); stage = 40; break;
                    case 40:
                        for (int i = 0; i < 3; i++) root.Q<SliderInt>("shield-dial-" + i).value = ui.Campaign.ShieldCalibration.Target(i);
                        Press("shield-activate");
                        Require(ui.Campaign.Demands.ShieldRegulated && ui.Campaign.MandatoryCompleted == 0, "UI calibration counted as mandatory");
                        root.style.width = StyleKeyword.Null; root.style.height = StyleKeyword.Null;
                        Complete(ui.Campaign, 2); ui.Campaign.Tick(1, false); stage = 5; break;
                    case 5:
                        Require(root.Q<Label>("mission-event-banner").text.Contains("Solar flare"), "Translated live warning missing");
                        Require(explorer.GetComponent<MissionStormPresentation>().Strength > 0, "Solar shield effect missing");
                        ScreenCapture.CaptureScreenshot("Logs/ares-events-flare-warning.png"); stage = 50; break;
                    case 50: Call("Settings"); stage = 6; break;
                    case 6:
                        Require(root.Q<Label>("mission-event-banner").resolvedStyle.display == DisplayStyle.None, "Pause did not hide live alert");
                        Call("CloseOverlay"); ui.Campaign.Tick(61, true); break;
                    case 7:
                        Require(ui.Campaign.Phase == Phase.Tasks && !ui.Campaign.Communications.Connected, "Protected flare did not damage antenna");
                        Install(AtDay(seed, 2)); Complete(ui.Campaign, 2); ui.Campaign.Tick(61, false); break;
                    case 8:
                        Require(ui.Campaign.Phase == Phase.GameOver && root.Q("mission-end") != null && ui.BlocksWorld, "Immediate radiation death screen/input lock missing");
                        ScreenCapture.CaptureScreenshot("Logs/ares-events-radiation-death.png"); stage = 80; break;
                    case 80:
                        PlayerPrefs.SetString("ARES.Language", "pt-BR"); Install(AtDay(seed, 1)); Complete(ui.Campaign, 2); ui.Campaign.Tick(21, false); SetPlayer("solar"); stage = 9; break;
                    case 9:
                        Require(ui.Campaign.Solar.BuriedPanels == 32 && ui.Campaign.Solar.GeneratedKwh == 0, "Physical solar park not buried");
                        ScreenCapture.CaptureScreenshot("Logs/ares-events-dust.png"); stage = 90; break;
                    case 90:
                        ui.Campaign.Solar.DrawEnergy(ui.Campaign.Solar.ReserveKwh); Reserve(ui.Campaign, .2f); ui.Campaign.Tick(10, false); stage = 10; break;
                    case 10:
                        Require(ui.Campaign.FailureCause == "suffocation" && root.Q("mission-end") != null && ui.BlocksWorld, "Immediate oxygen death screen missing");
                        File.AppendAllText(Log, "\nPASS: domain boundaries; optional UI PT/EN and 1280x720; focus; flare warning/VFX; protected antenna damage; immediate radiation/oxygen death screens."); Finish(); break;
                }
            }
            catch (Exception error) { File.AppendAllText(Log, "\nFAIL: " + error); Debug.LogException(error); Finish(); }
        }
        static void DomainChecks()
        {
            int chosen = -1; var signatures = new System.Collections.Generic.HashSet<string>();
            for (int s = 0; s < 64; s++)
            {
                var demands = new ConflictingDemands(5, s); string pattern = ""; int dust = 0, flares = 0;
                for (int day = 1; day <= 5; day++)
                {
                    Invoke(demands, "BeginDay", day); Invoke(demands, "Arm", 1);
                    Require(demands.Stage == DemandStage.Dormant && demands.Event == DemandEvent.None, "Event before two mandatory tasks");
                    Invoke(demands, "Arm", 2); pattern += (int)demands.Event;
                    if (demands.Event == DemandEvent.DustStorm) dust++;
                    if (demands.Event == DemandEvent.SolarFlare) { flares++; Require(day == 2 || day == 5, "Flare on a day without calibration"); }
                    Require(demands.ShieldTaskAvailable == (day == 2 || day == 5), "Optional task wrong day");
                }
                Require(dust == 2 && flares >= 1, "Random schedule missing events"); signatures.Add(pattern);
                if (pattern[0] == '1' && pattern[1] == '2') chosen = s;
            }
            Require(chosen >= 0 && signatures.Count > 1, "Schedule not varied/reproducible"); seed = chosen;
            var c = AtDay(seed, 2);
            Require(!c.BeginShieldRegulation("solar") && c.BeginShieldRegulation("shield"), "Shield proximity gate broken");
            Require(!c.FinishShieldRegulation() && !c.FinishTask(ConflictingDemands.ShieldTaskId, true, 1, 0), "Unaligned shield accepted");
            for (int i = 0; i < 3; i++) c.ShieldCalibration.Adjust(i, c.ShieldCalibration.Target(i));
            Require(c.FinishShieldRegulation() && c.MandatoryCompleted == 0 && !c.AllTasksDone, "Optional task altered mandatory progression");
            Complete(c, 1); c.Tick(5, false); Require(c.Demands.Stage == DemandStage.Dormant, "One task plus calibration triggered event");
            Complete(c, 2); c.Tick(1, false); float timer = c.Demands.RemainingSeconds;
            c.Tick(100, false, true); Require(c.Demands.RemainingSeconds == timer, "Pause/tutorial advanced event");
            c.ReachShelter(); Require(c.Demands.Warning, "Walking into shelter canceled event");
            c.Tick(61, true); Require(c.Phase == Phase.Tasks && c.Dose == 12 && !c.Communications.Connected, "Calibrated flare outcome wrong");
            for (int i = 0; i < 3; i++) { Require(c.Communications.StartRepairStep(), "Repair rejected"); c.Tick(2, true); }
            Require(c.Communications.Connected, "Antenna repair did not recover connection");
            c.Tick(31, false); Require(c.Demands.Stage == DemandStage.Resolved && c.Dose == 12, "Event replayed instead of resolving");
            Complete(c, c.Current.tasks.Length); c.Tick(600, false);
            Require(c.Phase == Phase.Summary, "Protected day did not finish");
            var snapshot = c.Communications.ReportForDay(2).Snapshot;
            Require(snapshot.DemandEvent == DemandEvent.SolarFlare && snapshot.ShieldRegulated, "Event missing from frozen report");
            CommunicationCheckFixture.SendRequiredReport(c); c.NextSol();
            Require(c.Phase == Phase.Report && !c.Demands.ShieldTaskAvailable && !c.Demands.ShieldRegulated && c.Communications.Connected, "Day reset or unsolicited day-three antenna fault");
            var fatal = AtDay(seed, 2); Complete(fatal, 2); fatal.Tick(61, false);
            Require(fatal.Phase == Phase.GameOver && fatal.FailureCause == "radiation", "Uncalibrated flare did not kill");
            float elapsed = fatal.SolElapsedSeconds, energy = fatal.Solar.ReserveKwh; fatal.Tick(600, false);
            Require(fatal.SolElapsedSeconds == elapsed && fatal.Solar.ReserveKwh == energy && !fatal.BeginTask(fatal.Current.tasks[2].id, fatal.Current.tasks[2].room), "Simulation continued after death");
            var late = AtDay(seed, 2); late.Tick(595, false); Complete(late, 2); late.Tick(5, false);
            Require(late.Phase == Phase.Tasks && late.Demands.RemainingSeconds >= 54, "Late warning unfairly truncated");
            late.Tick(56, false); Require(late.FailureCause == "radiation", "Clock skipped flare");
            var dustCase = AtDay(seed, 1); Complete(dustCase, 2); dustCase.Tick(21, false);
            Require(dustCase.Solar.BuriedPanels == 32 && dustCase.Solar.GeneratedKwh == 0, "Dust failed to stop generation");
            dustCase.Solar.DrawEnergy(dustCase.Solar.ReserveKwh); Reserve(dustCase, 1);
            float air = dustCase.OxygenSystem.ReserveKg; dustCase.Tick(2, false);
            Require(dustCase.OxygenSystem.ReserveKg < air && dustCase.OxygenSystem.Production == 0, "Blackout did not stop oxygen production");
            var broom = new SolarCleaningSession(dustCase.Solar); broom.PickUpBroom();
            for (int i = 0; i < SolarPlant.PanelCount; i++) Require(broom.Tick(i, true, false, dustCase.Solar.CleaningSeconds), "Real broom failed to recover buried panel");
            air = dustCase.OxygenSystem.ReserveKg; dustCase.Tick(10, false);
            Require(dustCase.Phase == Phase.Tasks && dustCase.Solar.BuriedPanels == 0 && dustCase.OxygenSystem.ReserveKg > air, "Panel recovery did not restore life support");
            var suffocation = AtDay(seed, 1); Complete(suffocation, 2); suffocation.Tick(21, false); suffocation.Solar.DrawEnergy(suffocation.Solar.ReserveKwh); Reserve(suffocation, .2f); suffocation.Tick(10, false);
            Require(suffocation.Phase == Phase.GameOver && suffocation.FailureCause == "suffocation" && suffocation.OxygenSystem.ReserveKg == 0, "Zero oxygen did not cause immediate death");
            var carry = AtDay(seed, 1); Complete(carry, carry.Current.tasks.Length); Require(!carry.Seal("control", carry.Allocation), "Manual seal skipped warning"); carry.Tick(21, false);
            Require(carry.Seal("control", carry.Allocation), "Day could not finish after event"); CommunicationCheckFixture.SendRequiredReport(carry); carry.NextSol();
            Require(carry.Solar.BuriedPanels == 32, "New day silently cleaned panels");
            File.AppendAllText(Log, "\nDomain PASS: 64 seeded schedules, mandatory gate, optional exclusion, pause, once-only impact, warning overtime, antenna repair, frozen report, resets, real broom, supply loss/recovery and both deaths. Seed=" + seed);
        }
        static Campaign AtDay(int eventSeed, int day)
        {
            var c = new Campaign(explorer.definition.campaign, explorer.definition.rules,
                new SolarMaintenanceSettings { dustDelaySeconds = 10000 }, new OxygenSettings(), eventSeed);
            typeof(Campaign).GetProperty("SolIndex").SetValue(c, day - 1); Invoke(c.Demands, "BeginDay", day);
            for (int n = 2; n <= day; n++) { c.Communications.BeginDay(n); c.Greenhouse.BeginDay(n); }
            c.CloseReport(); Require(c.SetAllocation(new Allocation { shield = 50, life = 25, greenhouse = 25 }) && c.StartSol(), "Fixture cannot start"); return c;
        }
        static void Complete(Campaign c, int count)
        {
            for (int i = 0; i < count; i++)
            { var task = c.Current.tasks[i]; if (c.Completed(task.id)) continue; Require(c.BeginTask(task.id, task.room) && GreenhouseCheckFixture.Complete(c, task.id), "Mandatory fixture failed " + task.id); }
        }
        static void Reserve(Campaign c, float percent) => Invoke(c.OxygenSystem, "SetReservePercent", percent);
        static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
        static void Call(string method) => Invoke(ui, method);
        static void Install(Campaign c)
        {
            Call("CloseOverlay"); typeof(GameFrontend).GetField("campaign", Private).SetValue(ui, c);
            c.Changed += () => typeof(GameFrontend).GetField("dirty", Private).SetValue(ui, true);
            ui.Solar.Bind(c.Solar); ui.Terminal.Bind(c); Call("BuildHud");
        }
        static void SetPlayer(string room)
        {
            foreach (var sector in explorer.definition.layout.rooms) if (sector.id == room)
            { var player = (PlayerMotion)typeof(ReferenceExplorer).GetField("player", Private).GetValue(explorer); player.X = sector.Station.x; player.Z = -sector.Station.z; explorer.RecenterCamera(); return; }
        }
        static void Press(string name) => GreenhouseCheckFixture.Press(root, name);
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish() { SessionState.SetBool(Pending, false); PlayerPrefs.SetString("ARES.Language", language); Application.runInBackground = background; ui = null; EditorApplication.ExitPlaymode(); }
    }
}
