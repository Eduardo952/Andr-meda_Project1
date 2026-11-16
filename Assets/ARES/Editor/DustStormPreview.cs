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
    /// <summary>Short visual review of the storm front, outdoor instrument and indoor exclusion.</summary>
    [InitializeOnLoad]
    public static class DustStormPreview
    {
        const string Pending = "ARES.DustPreview", Log = "Logs/ares-dust-preview.txt";
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static VisualElement root;
        static int stage;
        static double began, next;
        static bool background;
        static string language;
        static DustStormPreview() { EditorApplication.update += Tick; }
        public static void Begin()
        {
            stage = 0; ui = null; began = next = 0;
            background = Application.runInBackground; Application.runInBackground = true;
            language = Ares.UI.Localization.CurrentLanguage; PlayerPrefs.SetString("ARES.Language", "pt-BR");
            Directory.CreateDirectory("Logs"); File.WriteAllText(Log, "RUNNING");
            SessionState.SetBool(Pending, true); EditorApplication.isPaused = false; EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (began == 0) began = EditorApplication.timeSinceStartup;
                Require(EditorApplication.timeSinceStartup - began < 80, "Preview timeout");
                if (EditorApplication.timeSinceStartup < next) return;
                if (ui == null)
                {
                    ui = UnityEngine.Object.FindAnyObjectByType<GameFrontend>(); if (ui == null) return;
                    explorer = ui.GetComponent<ReferenceExplorer>(); root = ui.GetComponent<UIDocument>().rootVisualElement;
                    ui.Apod.OfflineOnly = true; next = EditorApplication.timeSinceStartup + 2; return;
                }
                next = EditorApplication.timeSinceStartup + 1.5;
                var wind = explorer.GetComponent<MartianWind>();
                switch (stage++)
                {
                    case 0:
                        ui.StartMission(false);
                        var campaign = new Campaign(explorer.definition.campaign, explorer.definition.rules, eventSeed: 63);
                        typeof(GameFrontend).GetField("campaign", Private).SetValue(ui, campaign);
                        campaign.Changed += () => typeof(GameFrontend).GetField("dirty", Private).SetValue(ui, true);
                        ui.Solar.Bind(campaign.Solar); ui.Terminal.Bind(campaign); Call("BuildHud");
                        campaign.CloseReport(); campaign.SetAllocation(new Allocation { shield = 50, life = 25, greenhouse = 25 }); campaign.StartSol();
                        explorer.reducedMotion = false; Move(explorer.definition.layout.Room("solar").Station);
                        next = EditorApplication.timeSinceStartup + 3; break;
                    case 1: ScreenCapture.CaptureScreenshot("Logs/ares-dust-calm.png"); break;
                    case 2:
                        for (int i = 0; i < 2; i++) { var task = ui.Campaign.Current.tasks[i]; Require(ui.Campaign.BeginTask(task.id, task.room) && ui.Campaign.FinishTask(task.id, true, 10, 0), "Cannot trigger scheduled storm"); }
                        ui.Campaign.Tick(20, false); next = EditorApplication.timeSinceStartup + 4; break;
                    case 3:
                        Require(ui.Campaign.Demands.Event == DemandEvent.DustStorm && wind.LiveParticles > 150 && wind.LiveParticles <= MartianWind.Capacity, "Storm pool not dense/bounded");
                        Require(RenderSettings.fogEndDistance < 65, "Storm did not reduce visibility");
                        Require(root.Q("storm-wind-hud").resolvedStyle.display == DisplayStyle.Flex, "Outdoor wind instrument missing");
                        Require(root.Q<Label>("storm-wind-speed").text.EndsWith(" km/h"), "Wind units missing");
                        foreach (var ps in explorer.GetComponentsInChildren<ParticleSystem>()) if (ps.name == "Martian wind / airborne dust")
                        {
                            var particles = new ParticleSystem.Particle[MartianWind.Capacity]; int count = ps.GetParticles(particles);
                            for (int i = 0; i < count; i++) Require(particles[i].remainingLifetime <= 0 || !HabitatShelter.Contains(explorer.definition.layout, particles[i].position, .7f), "Dust entered shelter");
                        }
                        foreach (string shader in new[] { "ARES/Effects/WindDust", "ARES/Effects/ReferenceSky" }) Require(!ShaderUtil.ShaderHasError(Resources.Load<Shader>(shader)), "Shader failed " + shader);
                        File.AppendAllText(Log, "\nPeak: particles=" + wind.LiveParticles + ", wind=" + root.Q<Label>("storm-wind-speed").text + ", visibility=" + RenderSettings.fogEndDistance);
                        ScreenCapture.CaptureScreenshot("Logs/ares-dust-storm.png"); break;
                    case 4: Move(explorer.definition.layout.Room("control").Station); break;
                    case 5:
                        Require(root.Q("storm-wind-hud").resolvedStyle.display == DisplayStyle.None && RenderSettings.fogEndDistance > 130, "Interior weather isolation failed");
                        ScreenCapture.CaptureScreenshot("Logs/ares-dust-inside.png"); break;
                    case 6:
                        var corridor = explorer.definition.layout.corridors[0]; Move(new Vector3((corridor.a.x + corridor.b.x) * .5f, 0, -(corridor.a.z + corridor.b.z) * .5f)); break;
                    case 7:
                        Require(root.Q("storm-wind-hud").resolvedStyle.display == DisplayStyle.None, "Covered corridor treated as outdoors");
                        Move(explorer.definition.layout.Room("solar").Station); explorer.reducedMotion = true;
                        PlayerPrefs.SetString("ARES.Language", "en");
                        var locale = typeof(GameFrontend).GetField("locale", Private).GetValue(ui); locale.GetType().GetMethod("Set").Invoke(locale, new object[] { "en" }); break;
                    case 8:
                        Require(root.Q<Label>("storm-wind-speed").text == Mathf.RoundToInt(new DustStormWeather(ui.Campaign.Demands).SpeedKmh) + " km/h", "Reduced motion altered wind telemetry");
                        Require(root.Q("storm-wind-hud").Q<Label>(className: "storm-wind-title").text == "OUTDOOR WIND", "Instrument not localized");
                        ScreenCapture.CaptureScreenshot("Logs/ares-dust-reduced-en.png"); break;
                    case 9: ui.Campaign.Tick(40, false); next = EditorApplication.timeSinceStartup + 7; break;
                    case 10:
                        Require(root.Q("storm-wind-hud").resolvedStyle.display == DisplayStyle.None && RenderSettings.fogEndDistance > 225 && wind.StormStrength < .01f, "Storm did not dissipate/restore visibility");
                        File.AppendAllText(Log, "\nPASS: dense bounded storm, shaders, visibility recovery, outdoor km/h, room/corridor exclusion, reduced motion and English instrument."); Finish(); break;
                }
            }
            catch (Exception error) { File.AppendAllText(Log, "\nFAIL: " + error); Debug.LogException(error); Finish(); }
        }
        static void Move(Vector3 position)
        { var player = (PlayerMotion)typeof(ReferenceExplorer).GetField("player", Private).GetValue(explorer); player.X = position.x; player.Z = -position.z; player.Speed = player.VelocityX = player.VelocityZ = 0; explorer.RecenterCamera(); }
        static void Call(string name) => typeof(GameFrontend).GetMethod(name, Private).Invoke(ui, null);
        static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
        static void Finish() { SessionState.SetBool(Pending, false); PlayerPrefs.SetString("ARES.Language", language); Application.runInBackground = background; ui = null; EditorApplication.ExitPlaymode(); }
    }
}
