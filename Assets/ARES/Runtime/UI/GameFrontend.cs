using System;
using Ares.Data;
using Ares.Domain;
using Ares.Tasks;
using Ares.World;
using Ares.World.Computer;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Composition root: owns navigation between views, never mission reward rules.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class GameFrontend : MonoBehaviour
    {
        ReferenceExplorer explorer;
        GameDefinition definition;
        StationInteraction stations;
        UIDocument document;
        PanelSettings panel;
        VisualElement root, overlay;
        Localization locale;
        Campaign campaign;
        MissionHud hud;
        MissionGuidance guidance;
        readonly TourProgress tourProgress = new TourProgress();
        TaskView taskView;
        TaskAttempt attempt;
        TitleBackdrop backdrop;
        StationOffer stationOffer;
        bool title = true, paused, pointerOverUi, dirty;
        public bool BlocksWorld => title || paused || overlay != null || (Terminal != null && Terminal.OwnsCamera);
        public bool BlocksPointer => BlocksWorld || pointerOverUi;
        public Campaign Campaign => campaign;
        public SolarMaintenance Solar { get; private set; }
        public GreenhouseCare GreenhouseCare { get; private set; }
        public ObservatoryInteraction Observatory { get; private set; }
        public ApodService Apod { get; private set; }
        public ComputerTerminal Terminal { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var explorer = FindAnyObjectByType<ReferenceExplorer>();
            if (explorer != null && explorer.GetComponent<GameFrontend>() == null) explorer.gameObject.AddComponent<GameFrontend>();
        }
        void Awake()
        {
            explorer = GetComponent<ReferenceExplorer>(); definition = explorer.definition;
            Solar = GetComponent<SolarMaintenance>();
            if (Solar == null) Solar = gameObject.AddComponent<SolarMaintenance>();
            GreenhouseCare=GetComponent<GreenhouseCare>();
            if(GreenhouseCare==null)GreenhouseCare=gameObject.AddComponent<GreenhouseCare>();
            Apod = GetComponent<ApodService>();
            if (Apod == null) Apod = gameObject.AddComponent<ApodService>();
            Observatory = GetComponent<ObservatoryInteraction>();
            if (Observatory == null) Observatory = gameObject.AddComponent<ObservatoryInteraction>();
            stations = new StationInteraction(definition.layout, definition.world.useRadius);
            locale = new Localization(PlayerPrefs.GetString("ARES.Language", "pt-BR"));
            document = gameObject.AddComponent<UIDocument>(); panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("ARES/UI/RuntimeTheme");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution = new Vector2Int(1920,1080); panel.match = .5f; document.panelSettings = panel;
            root = document.rootVisualElement; root.AddToClassList("ares-root"); root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/Game"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/ChallengeColors"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/Challenges"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/Greenhouse"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/Observatory"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/Oxygen"));
            root.pickingMode = PickingMode.Ignore;
            Terminal = gameObject.AddComponent<ComputerTerminal>();
            Terminal.Initialize(explorer, this, root);
            explorer.reducedMotion = PlayerPrefs.GetInt("ARES.ReducedMotion", 0) == 1;
            explorer.invertCamera = PlayerPrefs.GetInt("ARES.InvertCamera", 0) == 1;
            explorer.cameraSensitivity = PlayerPrefs.GetFloat("ARES.CameraSensitivity", 1);
            ShowTitle();
        }
        void OnDestroy() { backdrop?.Dispose(); if (campaign != null) campaign.Changed -= Changed; if (panel != null) Destroy(panel); }
        void Changed() { dirty = true; }
        void Update()
        {
            if (root == null) return;
            backdrop?.Tick(Time.unscaledDeltaTime, explorer.reducedMotion);
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !title)
            { if (Terminal.OwnsCamera) Terminal.Close(); else if (overlay == null) Settings(); else if (campaign.Phase == Phase.Plan || campaign.Phase == Phase.Tasks) CloseOverlay(); }
            if (title || campaign == null) return;
            stationOffer = stations.Resolve(campaign, explorer.PlayerPosition);
            campaign.Tick(Time.deltaTime, BlocksWorld);
            if (!BlocksWorld || Terminal.Focused)
            {
                // Inside the live terminal, generation/oxygen continue while the campaign clock remains paused.
                if (Terminal.Focused && campaign.Phase == Phase.Tasks) campaign.Solar.Tick(Time.deltaTime);
                campaign.TickOxygen(Time.deltaTime);
            }
            Solar.Tick(Time.deltaTime);
            GreenhouseCare.Tick(Time.deltaTime);
            Observatory.Tick();
            if (!BlocksWorld && !string.IsNullOrEmpty(campaign.Alarm))
                foreach (var room in definition.layout.rooms) if (room.id == "shield" && WorldNavigation.Inside(explorer.PlayerPosition.x, -explorer.PlayerPosition.z, room, 0)) { campaign.ReachShelter(); break; }
            attempt?.Tick(Time.unscaledDeltaTime); taskView?.Update(); hud?.Update(stationOffer, explorer.ControlledPosition);
            guidance?.Update(campaign, explorer.PlayerPosition, BlocksWorld || explorer.IsDrivingRover, Time.deltaTime);
            if (!BlocksWorld && keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) UseStation();
            if (dirty) { dirty = false; if (campaign.Phase == Phase.Summary) ShowSummary(); else if (campaign.Phase == Phase.Report && overlay == null) ShowReport(); }
            var mouse = Mouse.current;
            if (mouse != null && root.panel != null)
            {
                var pos = mouse.position.ReadValue(); var picked = root.panel.Pick(RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(pos.x, Screen.height - pos.y)));
                pointerOverUi = picked != null && picked != root;
            }
        }
        public void StartMission()
        {
            if (campaign != null) campaign.Changed -= Changed;
            campaign = new Campaign(definition.campaign, definition.rules, explorer.solarSettings, explorer.oxygenSettings); campaign.Changed += Changed;
            Solar.Bind(campaign.Solar);
            Terminal.Bind(campaign);
            title = false; BuildHud(); ShowReport();
        }
        void BuildHud()
        {
            Terminal.Reset();
            backdrop?.Dispose(); backdrop = null;
            root.Clear(); overlay = null; taskView = null; attempt = null; paused = false;
            hud = new MissionHud(root, definition, campaign, locale, explorer, UseStation, ShowReport, Settings);
            guidance = new MissionGuidance(root, definition, locale, tourProgress);
            Terminal.AttachPrompt();
        }
        VisualElement Dialog(string titleText = null)
        {
            Terminal.Reset();
            if (overlay != null) overlay.RemoveFromHierarchy();
            overlay = Box(root, "overlay"); var scroll = new ScrollView(); scroll.AddToClassList("dialog"); scroll.AddToClassList("panel"); overlay.Add(scroll);
            if (titleText != null) Text(scroll.contentContainer, titleText, "title"); return scroll.contentContainer;
        }
        void CloseOverlay()
        {
            if (campaign != null && campaign.OpenTask != null) campaign.CancelTask();
            overlay?.RemoveFromHierarchy(); overlay = null; attempt = null; taskView = null; paused = false;
        }
        void ShowTitle()
        {
            Terminal.Reset();
            title = true; hud = null; root.Clear(); overlay = null; attempt = null; taskView = null;
            backdrop?.Dispose(); backdrop = null;
            var screen = Box(root, "title-screen"); backdrop = new TitleBackdrop(screen, explorer.GetComponentInChildren<BubbleAnimator>());
            Text(screen, locale.T("title.name"), "logo"); Text(screen, locale.T("title.tagline"), "panel tagline");
            var buttons = Box(screen, "row"); Button(buttons, "▶ " + locale.T("title.play"), StartMission);
            Button(buttons, locale.T("title.language"), Languages, "alt"); Button(buttons, locale.T("title.howTo"), Help, "ghost");
            Button(buttons, locale.T("title.exit"), ApplicationExit.Quit, "ghost").name = "title-exit";
        }
        void Languages()
        {
            var view = Dialog(locale.T("title.language"));
            foreach (var lang in new[] { "pt-BR", "en" }) Button(view, locale.T("lang." + lang), () => { locale.Set(lang); PlayerPrefs.SetString("ARES.Language", lang); if (title) ShowTitle(); else { BuildHud(); Settings(); } });
            Button(view, locale.T("settings.close"), CloseOverlay, "ghost");
        }
        void Help()
        {
            var view = Dialog(locale.T("howTo.title")); foreach (var key in new[] { "move", "sprint", "use", "solar", "follow", "bubble" }) Text(view, locale.T("howTo." + key));
            Button(view, locale.T("howTo.close"), CloseOverlay);
        }
        void Settings()
        {
            var view = Dialog(locale.T("settings.title"));
            Toggle(view, "settings.reducedMotion", explorer.reducedMotion, value => { explorer.reducedMotion = value; PlayerPrefs.SetInt("ARES.ReducedMotion", value ? 1 : 0); });
            Toggle(view, "settings.invertCamera", explorer.invertCamera, value => { explorer.invertCamera = value; PlayerPrefs.SetInt("ARES.InvertCamera", value ? 1 : 0); });
            Slider(view, locale.T("settings.cameraSensitivity"), Mathf.RoundToInt(explorer.cameraSensitivity * 100), 200, 5, value => { explorer.cameraSensitivity = Mathf.Max(.25f, value / 100f); PlayerPrefs.SetFloat("ARES.CameraSensitivity", explorer.cameraSensitivity); });
            Button(view, locale.T("settings.language"), Languages, "alt"); Button(view, locale.T("settings.home"), ShowTitle, "ghost"); Button(view, locale.T("settings.close"), CloseOverlay);
        }
        void Toggle(VisualElement view, string key, bool initial, Action<bool> changed)
        { var toggle = new Toggle(locale.T(key)) { value = initial }; view.Add(toggle); toggle.RegisterValueChangedCallback(e => changed(e.newValue)); }
        void ShowReport()
        { if (attempt != null) return; CampaignScreens.Report(Dialog(), campaign, locale, () => { campaign.CloseReport(); CloseOverlay(); }); }
        void ShowEnergy()
        { if (attempt != null || (campaign.Phase != Phase.Plan && campaign.Phase != Phase.Tasks)) return; EnergyView.Build(StationPanel(), campaign, definition.rules, locale, CloseOverlay); }
        VisualElement StationPanel()
        {
            Terminal.Reset();
            overlay?.RemoveFromHierarchy();
            overlay = Box(root, "overlay challenge-backdrop");
            return Box(overlay);
        }
        void UseStation()
        {
            if (BlocksWorld || explorer.IsDrivingRover) return;
            if (Terminal.TryInteract()) return;
            if (Observatory.TryInteract()) return;
            if (GreenhouseCare.TryInteract()) return;
            if (Solar.TryInteract()) return;
            var offer = stations.Resolve(campaign, explorer.PlayerPosition);
            switch (offer.Action)
            {
                case StationAction.PlanEnergy: ShowEnergy(); break;
                case StationAction.SealSol: ShowSeal(); break;
                case StationAction.OpenTask:
                    if (campaign.BeginTask(offer.TaskId, offer.Room.id)) OpenTask(offer.TaskId);
                    break;
            }
        }
        void OpenTask(string id)
        {
            if (GreenhouseSimulation.IsTask(id))
            {
                attempt = null; taskView = null;
                new GreenhouseView(StationPanel(), campaign.Greenhouse, campaign.Watering, () => { if (campaign.FinishGreenhouseTask()) CloseOverlay(); }, CloseOverlay);
                return;
            }
            attempt = new TaskAttempt(id, definition, campaign);
            var content = StationPanel();
            taskView = new TaskView(content, attempt, locale, () => { OpenTask(id); attempt.Begin(); taskView.Render(); }, () =>
            { if (campaign.FinishTask(id, attempt.Solved, attempt.Seconds, attempt.Mistakes)) { attempt = null; CloseOverlay(); } }, CloseOverlay, explorer.reducedMotion);
        }
        internal void BuildComputerEnergy(VisualElement parent, Action close)
        { EnergyView.Build(parent, campaign, definition.rules, locale, close); }
        internal void SealFromComputer() { ShowSeal(); }
        public void ShowObservatory()
        {
            if (campaign == null || title || attempt != null) return;
            new ObservatoryView(StationPanel(), campaign, Apod, () =>
            {
                CloseOverlay();
                if (campaign.Phase == Phase.Victory || campaign.Phase == Phase.GameOver) ShowEnd();
            });
        }
        void ShowSeal()
        {
            var view = Dialog(locale.T("hud.endSol"));
            Text(view, locale.T("sys.shield") + " " + campaign.Allocation.shield + " · " + locale.T("sys.life") + " " + campaign.Allocation.life + " · " + locale.T("sys.greenhouse") + " " + campaign.Allocation.greenhouse);
            Button(view, locale.T("tasks.check"), () => {
                var offer = stations.Resolve(campaign, explorer.PlayerPosition);
                if (offer.Action == StationAction.SealSol && campaign.Seal(offer.Room.id, campaign.Allocation)) ShowSummary();
            });
            Button(view, locale.T("settings.close"), CloseOverlay, "ghost");
        }
        void ShowSummary()
        { CampaignScreens.Summary(Dialog(), campaign, locale, () => { campaign.NextSol(); if (campaign.Phase == Phase.Report) { BuildHud(); ShowReport(); } else ShowEnd(); }); }
        void ShowEnd()
        {
            var view = Dialog(locale.T(campaign.Phase == Phase.Victory ? "cert.title" : "gameover." + campaign.FailureCause + ".title"));
            Text(view, campaign.Phase == Phase.Victory ? locale.T("cert.tiers." + campaign.SuccessTier) : locale.T("gameover." + campaign.FailureCause + ".body"), "heading");
            if(campaign.Phase==Phase.Victory)
            {
                var nickname=new TextField(locale.T("cert.nickname")) {maxLength=20,value=PlayerPrefs.GetString("ARES.Nickname","")}; view.Add(nickname);
                Text(view,locale.T("cert.local"),"small"); var certificate=Text(view,"");
                Button(view,locale.T("cert.show"),()=>{string name=nickname.value.Trim(); if(name.Length==0) name="Astronaut"; PlayerPrefs.SetString("ARES.Nickname",name); certificate.text=locale.T("cert.body","name",name)+"\n"+locale.T("cert.stats","i",campaign.Integrity,"o",campaign.Oxygen,"d",campaign.Dose);});
            }
            Button(view, locale.T("title.play"), StartMission); Button(view, locale.T("settings.home"), ShowTitle, "ghost");
            Button(view, "Diário de descobertas", ShowObservatory, "ghost").name = "end-observatory";
        }
        // Explicit test fixture entry point: the control suite tests locomotion independently of menus.
        public void SetInspectionTestMode() { Terminal.Reset(); backdrop?.Dispose(); backdrop = null; title = false; root.Clear(); overlay = null; campaign = null; hud = null; }
    }
}
