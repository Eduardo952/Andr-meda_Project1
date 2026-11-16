using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ares.Data;
using Ares.Domain;
using Ares.Domain.Communications;
using Ares.Infrastructure;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Focused checks: radio rules, daily-report gate, local archive, computer page and reporting UI.</summary>
    [InitializeOnLoad]
    public static class CommunicationsChecks
    {
        const string Pending = "ARES.CommunicationsCheck";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static VisualElement root;
        static int stage, frame;
        static float elapsed;
        static double started;
        static bool background;
        static CommunicationsChecks() { EditorApplication.update += Tick; }
        [MenuItem("ARES/Checks/Communications and daily reports only")]
        public static void Request() => File.WriteAllText("Temp/ares-computer-check.request", "communications");
        public static void Begin()
        {
            stage = 0; frame = -1; elapsed = 0; ui = null; started = EditorApplication.timeSinceStartup;
            background = Application.runInBackground; Application.runInBackground = true;
            SessionState.SetBool(Pending, true); File.WriteAllText("Logs/ares-communications.txt", "RUNNING");
            EditorApplication.isPaused = false; EditorApplication.EnterPlaymode();
        }
        static Campaign Fixture(GameDefinition definition)
        {
            var rules = JsonUtility.FromJson<CampaignRules>(JsonUtility.ToJson(definition.rules));
            rules.damage.integrityPerShieldKwh = rules.damage.oxygenPerLifeKwh = rules.damage.oxygenPerGreenhouseKwh = 0;
            rules.consequences.dosePerShieldKwh = rules.consequences.shelterMissedDose = 0;
            return new Campaign(definition.campaign, rules, new SolarMaintenanceSettings { reserveCapacityKwh = 1000, initialReserveKwh = 1000, consumptionKwhPerCycle = 0, dustDelaySeconds = 100000 });
        }
        static void StartDay(Campaign campaign)
        { campaign.CloseReport(); Require(campaign.SetAllocation(new Allocation { shield = campaign.EnergyPool }) && campaign.StartSol(), "Start daily fixture"); campaign.ReachShelter(); }
        static void ModelChecks(GameDefinition definition)
        {
            var mission = Fixture(definition); var radio = mission.Communications;
            Require(!mission.SendDailyReport(), "Report before day end");
            mission.Tick(11, false); Require(radio.Messages[0].Delivery == DeliveryState.InTransit, "Instant incoming delivery");
            mission.Tick(2, false, true); Require(radio.Messages[0].Delivery == DeliveryState.InTransit, "Pause must pause radio");
            mission.Tick(1, false); Require(radio.Messages[0].Delivery == DeliveryState.Delivered, "Signal arrival");
            float energy = mission.Solar.ReserveKwh;
            Require(mission.ReplyToEarth(1, ReplyKind.AskForHelp) && Math.Abs(mission.Solar.ReserveKwh - (energy - .5f)) < .001, "Reply energy cost");
            Require(!mission.ReplyToEarth(1, ReplyKind.Acknowledge), "Duplicate reply");
            mission.Tick(12, false); Require(radio.Messages.Count == 3 && radio.Messages[2].Delivery == DeliveryState.InTransit, "Return guidance must need second signal leg");
            mission.Tick(12, false); Require(radio.Messages[2].Delivery == DeliveryState.Delivered, "Round trip guidance");
            var lowPower = Fixture(definition); lowPower.Tick(12, false); lowPower.Solar.DrawEnergy(float.MaxValue);
            Require(!lowPower.ReplyToEarth(1, ReplyKind.Acknowledge) && !lowPower.Communications.Messages[0].Replied, "No energy must preserve unsent response");
            for (int day = 1; day <= 5; day++)
            {
                StartDay(mission); GreenhouseCheckFixture.CareForToday(mission.Greenhouse, mission.Watering);
                mission.Tick(MissionTime.RealSecondsPerSol, false);
                var report = radio.ReportForDay(day); Require(report != null && !report.Sent && report.Snapshot.Day == day, "Automatic report preparation");
                Require(report.Snapshot.ReservoirMl == mission.Greenhouse.ReservoirMl && report.Snapshot.AvailableEnergyKwh == mission.Solar.ReserveKwh && report.Snapshot.ShieldAllocated == mission.Allocation.shield, "Report values must match end-day telemetry");
                mission.NextSol(); Require(mission.Phase == Phase.Summary && mission.SolIndex == day - 1, "Next day bypassed required report");
                if (day == 3)
                {
                    Require(radio.Connected, "A new day must not invent antenna damage before an event");
                    radio.DamageAntenna(); // Live flare timing is covered by MissionEventChecks.
                    Require(!radio.Connected && !mission.SendDailyReport(), "Antenna failure must block report");
                    for (int step = 0; step < 3; step++)
                    {
                        Require(radio.StartRepairStep() && !radio.StartRepairStep(), "Repair stage reentrancy");
                        radio.Tick(1); Require(!radio.Connected, "Repair completed early"); radio.Tick(1);
                    }
                    Require(radio.Connected, "Repair did not reconnect");
                }
                Require(mission.SendDailyReport() && mission.CanAdvanceDay, "Report send/gate");
                float reserve = radio.ReportBatteryKwh;
                Require(!mission.SendDailyReport() && reserve == radio.ReportBatteryKwh, "Duplicate report charged twice");
                Require(report.Delivery == DeliveryState.InTransit, "Report instantly received");
                int frozenWater = report.Snapshot.ReservoirMl; mission.NextSol();
                Require(report.Snapshot.ReservoirMl == frozenWater, "Past report mutated"); radio.Tick(12);
            }
            Require(mission.Phase == Phase.Victory && radio.Reports.Count == 5 && radio.Reports.All(r => r.Sent && r.Delivery == DeliveryState.Delivered) && radio.ReportBatteryKwh < .001, "Five reports / final-day gate");
            var archive = CommunicationsArchive.Capture(radio, "verification");
            var json = JsonUtility.ToJson(archive); var loaded = JsonUtility.FromJson<CommunicationsArchive.ArchiveData>(json);
            Require(loaded.reports.Count == 5 && loaded.reports[0].reservoirMl == 8960 && loaded.reports[0].scenarioUsesSampleData && loaded.messages.Count == radio.Messages.Count, "Archive round trip and source provenance");
            var disk = new CommunicationsArchive("Logs/communications-verification"); disk.Save(radio); Require(disk.Error == null, "Local archive write");
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                Require(EditorApplication.timeSinceStartup - started < 65, "Timeout");
                if (frame == Time.frameCount) return; frame = Time.frameCount; elapsed += Mathf.Min(Time.deltaTime, .05f);
                if (ui == null)
                {
                    var candidate = UnityEngine.Object.FindAnyObjectByType<GameFrontend>(); var actor = candidate?.GetComponent<ReferenceExplorer>();
                    if (actor == null || typeof(ReferenceExplorer).GetField("player", Private).GetValue(actor) == null) return;
                    ui = candidate; explorer = actor; ModelChecks(explorer.definition);
                    ui.StartMission(false); ui.Campaign.CloseReport(); Call("CloseOverlay"); explorer.reducedMotion = true;
                    var player = (PlayerMotion)typeof(ReferenceExplorer).GetField("player", Private).GetValue(explorer);
                    player.X = explorer.computerSettings.standPoint.x; player.Z = -explorer.computerSettings.standPoint.z;
                    player.Speed = player.VelocityX = player.VelocityZ = 0;
                    root = ui.GetComponent<UIDocument>().rootVisualElement; elapsed = 0;
                }
                if (stage == 0 && elapsed > .4f) { Require(ui.Terminal.TryInteract(), "Computer access"); Next(); }
                else if (stage == 1 && elapsed > .5f) { Press("computer-energy"); Next(); }
                else if (stage == 2 && elapsed > .3f) { Require(root.Q("communications-view") != null, "Independent communication panel missing"); ui.Campaign.Communications.Tick(12); Next(); }
                else if (stage == 3 && elapsed > .4f)
                {
                    var comms = root.Q("communications-view"); Require(comms != null && comms.worldBound.xMin >= -1 && comms.worldBound.xMax <= root.worldBound.xMax + 1, "Communication page layout");
                    Press("communications-reply-1-1"); Require(ui.Campaign.Communications.Messages.Count == 2, "Native reply button");
                    ScreenCapture.CaptureScreenshot("Logs/ares-communications-computer.png"); Next();
                }
                else if (stage == 4 && elapsed > .3f)
                {
                    ui.Terminal.Reset(); ui.Campaign.SetAllocation(new Allocation { shield = ui.Campaign.EnergyPool }); ui.Campaign.StartSol(); ui.Campaign.ReachShelter();
                    ui.Campaign.Tick(MissionTime.RealSecondsPerSol, false); Next();
                }
                else if (stage == 5 && elapsed > .4f) { Press("summary-communications"); Next(); }
                else if (stage == 6 && elapsed > .4f)
                {
                    Require(!root.Q<Button>("communications-next-day").enabledSelf, "UI advance before send");
                    ScreenCapture.CaptureScreenshot("Logs/ares-communications-report.png"); Next();
                }
                else if (stage == 7 && elapsed > .3f)
                {
                    Press("communications-send-report"); Require(ui.Campaign.CanAdvanceDay, "Native report send");
                    Press("communications-next-day"); Require(ui.Campaign.Phase == Phase.Report && ui.Campaign.SolIndex == 1, "Native next day");
                    Finish("PASS: outgoing power costs, no-energy/duplicate guards, one-way and return delays, antenna repair, immutable end-day reports, mandatory five-day send gate, archival JSON/disk, central computer page, report review/send/advance UI. Only communication and end-day screens opened.");
                }
            }
            catch (Exception error) { Finish("FAIL stage " + stage + ": " + error); }
        }
        static void Press(string name) => GreenhouseCheckFixture.Press(root, name);
        static void Call(string method) => typeof(GameFrontend).GetMethod(method, Private).Invoke(ui, null);
        static void Next() { stage++; elapsed = 0; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void Finish(string result)
        {
            SessionState.SetBool(Pending, false); File.WriteAllText("Logs/ares-communications.txt", result);
            Application.runInBackground = background; EditorApplication.ExitPlaymode();
        }
    }
}
