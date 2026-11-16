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
    /// <summary>Opt-in Casa Marte checks: resource invariants, five-day persistence and greenhouse-only presentation.</summary>
    [InitializeOnLoad]
    public static class GreenhouseChecks
    {
        const string Pending = "ARES.CasaMarteCheck";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static VisualElement root;
        static int stage, frame, gardenRendererCount;
        static float elapsed;
        static double started;
        static bool background;
        static GreenhouseChecks() { EditorApplication.update += Tick; }
        [MenuItem("ARES/Checks/Casa Marte - water and daily care")]
        public static void RequestCheck() => File.WriteAllText("Temp/ares-computer-check.request", "casa-marte");
        public static void Begin()
        {
            stage = 0; frame = -1; elapsed = 0; ui = null; started = EditorApplication.timeSinceStartup;
            background = Application.runInBackground; Application.runInBackground = true;
            SessionState.SetBool(Pending, true); File.WriteAllText("Logs/ares-casa-marte.txt", "RUNNING");
            EditorApplication.isPaused = false; EditorApplication.EnterPlaymode();
        }
        static GreenhouseSimulation Uniform(SoilMoisture soil) => new GreenhouseSimulation(new GreenhouseDayConditions("Fixture", GrowLight.Moderate, soil, soil, soil, soil));
        static void Conservation(GreenhouseSimulation game, GreenhouseWatering can)
            => Require(game.ReservoirMl + can.CanMl + game.TotalDeliveredMl == 11000, "Water created/lost");
        static void ModelChecks(GameDefinition definition)
        {
            foreach (var state in new[] { SoilMoisture.Moderate, SoilMoisture.Dry, SoilMoisture.Wet })
            {
                var game = Uniform(state); var can = new GreenhouseWatering(game);
                GreenhouseCheckFixture.CareForToday(game, can);
                Require(game.TodayDeliveredMl == GreenhouseRules.DailyMl(state), "Daily arithmetic: " + state);
                Require(game.TodayDeliveredMl == (state == SoilMoisture.Dry ? 4080 : state == SoilMoisture.Moderate ? 2040 : 0), "Requested totals");
                Require(game.Day == 1 && !game.DayEnded && game.CareReady, "Watering must not advance day");
                can.Fill(); int before = can.CanMl;
                Require(!can.StartPour(0) && can.CanMl == before, "Extra watering must not consume water"); Conservation(game, can);
            }
            var interrupted = Uniform(SoilMoisture.Dry); var tool = new GreenhouseWatering(interrupted);
            Require(!tool.Fill() && !tool.StartPour(0), "Cannot water without tool");
            tool.PickUp(); tool.Fill(); Require(tool.ReservoirMl == 10490 && tool.CanMl == 510, "Refill debit must be immediate");
            tool.StartPour(0); tool.Tick(0, false, 1); tool.ReturnCan();
            Require(tool.ActiveShelf == -1 && tool.CanMl == 510 && interrupted.TodayDeliveredMl == 0, "Return must cancel only pending action");
            tool.PickUp(); tool.StartPour(0); tool.Tick(0, true, 3); Conservation(interrupted, tool);
            Require(tool.ActiveShelf == -1 && tool.CanMl == 510, "Blocked pouring must stop");
            tool.StartPour(0); tool.Tick(1, false, 3); Require(tool.CanMl == 510, "Moving away must cancel");
            tool.StartPour(0); tool.Tick(0, false, float.NaN); Require(tool.CanMl == 510, "NaN time");
            tool.Tick(0, false, 3); Require(interrupted.Shelves[0].Soil == SoilMoisture.Dry && interrupted.Shelves[0].DeliveredDoses == 1, "Dry shelf needs two distinct doses");
            tool.Fill(); tool.StartPour(0); tool.Tick(0, false, 3); Require(interrupted.Shelves[0].Soil == SoilMoisture.Moderate, "Second dry dose");
            Conservation(interrupted, tool);
            var mission = new GreenhouseSimulation(); var canMission = new GreenhouseWatering(mission);
            for (int day = 1; day <= 5; day++)
            {
                GreenhouseCheckFixture.CareForToday(mission, canMission); Conservation(mission, canMission);
                mission.EndDay(); int total = mission.TotalDeliveredMl; mission.EndDay();
                Require(total == mission.TotalDeliveredMl, "Repeated EndDay changed water");
                mission.BeginDay(day + 1);
            }
            Require(mission.Finished && mission.HealthyDays == 5 && mission.ReservoirMl == 800, "Five-day sustainable route must leave 800 mL");
            Require(!canMission.Fill() && !canMission.StartPour(0), "Mission-end watering");
            var shortage = new GreenhouseSimulation(); var emptyCan = new GreenhouseWatering(shortage); emptyCan.PickUp();
            // Simulate other habitat reservoir withdrawals through the same ledger, including a partial final fill.
            typeof(GreenhouseSimulation).GetMethod("Withdraw", Private).Invoke(shortage, new object[] { 10800 });
            emptyCan.Fill(); Require(shortage.ReservoirMl == 0 && emptyCan.CanMl == 200, "Last partial refill");
            Require(!emptyCan.StartPour(0) && !emptyCan.Fill(), "Insufficient water must not create a full dose");
            shortage.AdjustLight(-1); shortage.EndDay();
            Require(shortage.Shelves[0].Health == ShelfHealth.AtRisk, "Missed irrigation and low light consequences");
            var campaign = new Campaign(definition.campaign, definition.rules);
            campaign.CloseReport(); var allocation = campaign.Current.demand; allocation.shield += campaign.EnergyPool - allocation.Total;
            Require(campaign.SetAllocation(allocation) && campaign.StartSol(), "Campaign fixture start"); campaign.ReachShelter();
            GreenhouseCheckFixture.CareForToday(campaign.Greenhouse, campaign.Watering);
            campaign.Watering.Fill(); int stored = campaign.Watering.CanMl, reservoir = campaign.Greenhouse.ReservoirMl;
            var original = campaign.Greenhouse; var originalCan = campaign.Watering;
            campaign.Tick(MissionTime.RealSecondsPerSol, false); CommunicationCheckFixture.SendRequiredReport(campaign); campaign.NextSol();
            Require(ReferenceEquals(original, campaign.Greenhouse) && ReferenceEquals(originalCan, campaign.Watering), "Campaign replaced greenhouse ledger");
            Require(campaign.Greenhouse.Day == 2 && campaign.Greenhouse.ReservoirMl == reservoir && campaign.Watering.CanMl == stored, "Campaign day transition refilled/lost water");
            Conservation(campaign.Greenhouse, campaign.Watering);
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 70, "Timeout");
                if (frame == Time.frameCount) return; frame = Time.frameCount; elapsed += Mathf.Min(Time.deltaTime, .05f);
                if (ui == null)
                {
                    var found = UnityEngine.Object.FindAnyObjectByType<GameFrontend>();
                    var actor = found?.GetComponent<ReferenceExplorer>();
                    if (actor == null || typeof(ReferenceExplorer).GetField("player", Private).GetValue(actor) == null) return;
                    ui = found; explorer = actor; ModelChecks(explorer.definition);
                    gardenRendererCount=0;
                    foreach(var renderer in ui.GetComponentsInChildren<MeshRenderer>(true))
                        if(renderer.TryGetComponent<ReferenceWorldPart>(out var part) && part.role.StartsWith("foliage"))gardenRendererCount++;
                    Require(gardenRendererCount>0,"Imported garden is missing");
                    ui.StartMission(false); ui.Campaign.CloseReport(); Call("CloseOverlay");
                    ui.Campaign.SetAllocation(new Allocation { shield = ui.Campaign.EnergyPool }); ui.Campaign.StartSol();
                    root = ui.GetComponent<UIDocument>().rootVisualElement;
                    SetPlayer(ui.GreenhouseCare.PickupPosition); elapsed = 0;
                }
                var care = ui.GreenhouseCare; var water = care.Watering;
                if (stage == 0 && elapsed > .4f)
                {
                    Require(care.TryClick(DownRay(ui.GreenhouseCare.PickupPosition)), "Click pickup target");
                    Require(water.HasCan, "Click did not pick up");
                    Require(care.TryClick(DownRay(ui.GreenhouseCare.PickupPosition)) && !water.HasCan, "Click return");
                    care.TryClick(DownRay(ui.GreenhouseCare.PickupPosition)); SetPlayer(ui.GreenhouseCare.RefillPosition); Next();
                }
                else if (stage == 1 && elapsed > .3f)
                {
                    Require(care.TryClick(DownRay(ui.GreenhouseCare.RefillPosition)), "Click reservoir");
                    Require(water.CanMl == 510 && water.ReservoirMl == 10490, "World fill amount");
                    SetPlayer(ui.GreenhouseCare.ShelfPositions[0]); Next();
                }
                else if (stage == 2 && elapsed > .3f)
                {
                    Require(care.TryClick(DownRay(ui.GreenhouseCare.Layout.rows[0].Center)), "Click shelf");
                    Require(water.ActiveShelf == 0, "Shelf click did not start dose");
                    water.Tick(0, false, 3); Require(water.CanMl == 0 && ui.Campaign.Greenhouse.TodayDeliveredMl == 510, "World dose amount");
                    int gardenMeshes = 0;
                    foreach (var renderer in ui.GetComponentsInChildren<MeshRenderer>())
                        if (renderer.enabled && renderer.TryGetComponent<ReferenceWorldPart>(out var part) && part.role.StartsWith("foliage")) gardenMeshes++;
                    Require(gardenMeshes == gardenRendererCount, "Reference garden visibility changed: "+gardenMeshes+" / "+gardenRendererCount);
                    var orbit = (OrbitCamera)typeof(ReferenceExplorer).GetField("rig", Private).GetValue(explorer);
                    SetPlayer(new Vector3(51, 0, -1)); orbit.Yaw = orbit.TargetYaw = .35f; orbit.Pitch = orbit.TargetPitch = .75f; orbit.Distance = orbit.TargetDistance = 16;
                    Next();
                }
                else if (stage == 3 && elapsed > .7f)
                { ScreenCapture.CaptureScreenshot("Logs/ares-casa-marte-world.png"); Next(); }
                else if (stage == 4 && elapsed > .3f)
                { GreenhouseCheckFixture.Reopen(ui); Next(); }
                else if (stage == 5 && elapsed > .5f)
                {
                    Require(root.Q("greenhouse-water") != null, "Daily care UI missing");
                    Require(!root.Q<Button>("greenhouse-accept").enabledSelf, "Incomplete care accepted");
                    GreenhouseCheckFixture.Press(root, "greenhouse-light-less"); Require(ui.Campaign.Greenhouse.Light == GrowLight.Low, "Decrease light");
                    GreenhouseCheckFixture.Press(root, "greenhouse-light-more"); Require(ui.Campaign.Greenhouse.Light == GrowLight.Moderate, "Increase light");
                    ScreenCapture.CaptureScreenshot("Logs/ares-casa-marte-ui.png"); Next();
                }
                else if (stage == 6 && elapsed > .3f)
                {
                    GreenhouseCheckFixture.CareForToday(ui.Campaign.Greenhouse, water); GreenhouseCheckFixture.Reopen(ui);
                    GreenhouseCheckFixture.Press(root, "greenhouse-accept");
                    Require(ui.Campaign.Completed("SmartFarmCare") && !ui.BlocksWorld, "Daily care reward/close");
                    Finish("PASS: 2040/4080/0 mL daily totals, exact refill/dose ledger, cancellation/return, insufficient water, five days with 800 mL left, campaign persistence, click pickup/return/refill/shelf, original garden visible, light UI and daily completion. Only greenhouse views opened.");
                }
            }
            catch (Exception error) { Finish("FAIL stage " + stage + ": " + error); }
        }
        static Ray DownRay(Vector3 point) => new Ray(point + Vector3.up * 6, Vector3.down);
        static void SetPlayer(Vector3 p)
        { var player = (PlayerMotion)typeof(ReferenceExplorer).GetField("player", Private).GetValue(explorer); player.X = p.x; player.Z = -p.z; player.Speed = player.VelocityX = player.VelocityZ = 0; }
        static void Call(string method) => typeof(GameFrontend).GetMethod(method, Private).Invoke(ui, null);
        static void Next() { stage++; elapsed = 0; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish(string result)
        {
            SessionState.SetBool(Pending, false); File.WriteAllText("Logs/ares-casa-marte.txt", result);
            Application.runInBackground = background; EditorApplication.ExitPlaymode();
        }
    }
}
