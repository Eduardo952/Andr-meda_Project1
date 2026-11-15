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
        VisualElement root, overlay, missionHudRoot;
        internal TutorialDirector Tutorial { get; private set; }
        public bool TutorialActive => Tutorial != null && Tutorial.Active;
        public TutorialStage TutorialStage => Tutorial?.Flow.Stage ?? Ares.Domain.TutorialStage.Inactive;
        Localization locale;
        Campaign campaign;
        Ares.Infrastructure.CommunicationsArchive communicationsArchive;
        MissionHud hud;
        MissionEventBanner eventBanner;
        StormWindHud stormWindHud;
        MissionCommandConsole commandConsole;
        WorldMapView worldMap;
        readonly System.Collections.Generic.List<VisualElement> mapDisabledElements = new System.Collections.Generic.List<VisualElement>();
        public bool IsMapOpen => worldMap != null;
        MissionGuidance guidance;
        readonly TourProgress tourProgress = new TourProgress();
        TaskView taskView;
        TaskAttempt attempt;
        TitleBackdrop backdrop;
        MissionMainMenu mainMenu;
        StationOffer stationOffer;
        bool title = true, paused, pointerOverUi, dirty;
        public bool BlocksWorld => title || paused || (commandConsole?.IsOpen ?? false) || overlay != null || campaign?.Phase == Phase.GameOver || campaign?.Phase == Phase.Victory || (Tutorial?.BlocksWorld ?? false) || (Terminal != null && Terminal.OwnsCamera);
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
            if(GetComponent<MissionHabitatArt>()==null)gameObject.AddComponent<MissionHabitatArt>();
            if(GetComponent<MissionAstronautArt>()==null)gameObject.AddComponent<MissionAstronautArt>();
            if(GetComponent<MissionStormPresentation>()==null)gameObject.AddComponent<MissionStormPresentation>();
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
            root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/Communications"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/Map"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/Tutorial"));
            root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/MissionTheme"));
            PresentationLocalizer.Attach(root);
            gameObject.AddComponent<PresentationLocalizationLoop>().Bind(root);
            root.pickingMode = PickingMode.Ignore;
            commandConsole = new MissionCommandConsole(root, () => campaign, () => TutorialActive);
            Terminal = gameObject.AddComponent<ComputerTerminal>();
            Terminal.Initialize(explorer, this, root);
            explorer.reducedMotion = PlayerPrefs.GetInt("ARES.ReducedMotion", 0) == 1;
            explorer.invertCamera = PlayerPrefs.GetInt("ARES.InvertCamera", 0) == 1;
            explorer.cameraSensitivity = PlayerPrefs.GetFloat("ARES.CameraSensitivity", 1);
            ShowTitle();
        }
        void OnDestroy() { commandConsole?.Dispose(); DisposeTutorial(); mainMenu?.Dispose(); CloseMap(); if (campaign != null) communicationsArchive?.Save(campaign.Communications); backdrop?.Dispose(); if (campaign != null) campaign.Changed -= Changed; if (panel != null) Destroy(panel); }
        void OnDisable() { commandConsole?.Close(); DisposeTutorial(); CloseMap(); }
        void DisposeTutorial() { Tutorial?.Dispose(); Tutorial = null; }
        void Changed() { dirty = true; }
        void Update()
        {
            if (root == null) return;
            root.EnableInClassList("reduced-motion", explorer.reducedMotion);
            if (commandConsole.HandleInput(!title && campaign != null)) return;
            backdrop?.Tick(Time.unscaledDeltaTime, explorer.reducedMotion);
            mainMenu?.Tick(Time.unscaledDeltaTime, explorer.reducedMotion);
            Tutorial?.Tick();
            if (Tutorial != null && !TutorialActive) DisposeTutorial();
            var keyboard = Keyboard.current;
            // Handle map input first so Escape never opens settings behind it.
            if (keyboard != null && !title && !TutorialActive)
            {
                if (IsMapOpen && (keyboard.mKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
                { CloseOverlay(); return; }
                if (keyboard.mKey.wasPressedThisFrame && !BlocksWorld) ToggleMap();
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !title && !TutorialActive)
            { if (Terminal.OwnsCamera) Terminal.Close(); else if (overlay == null) Settings(); else if (campaign.Phase == Phase.Plan || campaign.Phase == Phase.Tasks) CloseOverlay(); }
            if (title || campaign == null) return;
            stationOffer = stations.Resolve(campaign, explorer.PlayerPosition);
            if (TutorialActive && keyboard != null && keyboard.escapeKey.wasPressedThisFrame && !(Tutorial?.BlocksWorld ?? false)) Terminal.Close();
            campaign.Tick(Time.deltaTime, BlocksWorld, paused || TutorialActive);
            eventBanner?.Update(campaign, !TutorialActive && !paused, overlay != null || Terminal.OwnsCamera);
            stormWindHud?.Update(campaign, !TutorialActive && !BlocksWorld);
            communicationsArchive?.Save(campaign.Communications);
            if (campaign.Phase == Phase.GameOver && dirty)
            { dirty = false; attempt = null; taskView = null; ShowEnd(); return; }
            if (campaign.Phase == Phase.Summary && dirty)
            { dirty = false; attempt = null; taskView = null; ShowSummary(); return; }
            if (!TutorialActive) Solar.Tick(Time.deltaTime);
            GreenhouseCare.Tick(Time.deltaTime);
            if (!TutorialActive) Observatory.Tick();
            if (!BlocksWorld && !string.IsNullOrEmpty(campaign.Alarm))
                foreach (var room in definition.layout.rooms) if (room.id == "shield" && WorldNavigation.Inside(explorer.PlayerPosition.x, -explorer.PlayerPosition.z, room, 0)) { campaign.ReachShelter(); break; }
            attempt?.Tick(Time.unscaledDeltaTime); taskView?.Update();
            if (!IsMapOpen && !TutorialActive) hud?.Update(stationOffer, explorer.ControlledPosition);
            worldMap?.Update();
            if (!TutorialActive) guidance?.Update(campaign, explorer.PlayerPosition, BlocksWorld || explorer.IsDrivingRover, Time.deltaTime);
            if (!BlocksWorld && keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) UseStation();
            if (dirty) { dirty = false; if (campaign.Phase == Phase.Summary) ShowSummary(); else if (campaign.Phase == Phase.Report && overlay == null) ShowReport(); }
            var mouse = Mouse.current;
            if (mouse != null && root.panel != null)
            {
                var pos = mouse.position.ReadValue(); var picked = root.panel.Pick(RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(pos.x, Screen.height - pos.y)));
                pointerOverUi = picked != null && picked != root;
            }
        }
        public void StartMission() => StartMission(true);
        // The explicit overload lets existing integration fixtures enter the standard campaign directly.
        public void StartMission(bool offerTutorial)
        {
            DisposeTutorial();
            if (campaign != null) communicationsArchive?.Save(campaign.Communications);
            if (campaign != null) campaign.Changed -= Changed;
            campaign = new Campaign(definition.campaign, definition.rules, explorer.solarSettings, explorer.oxygenSettings); campaign.Changed += Changed;
            communicationsArchive = new Ares.Infrastructure.CommunicationsArchive();
            communicationsArchive.Save(campaign.Communications);
            Solar.Bind(campaign.Solar);
            Apod.BeginMission();
            Terminal.Bind(campaign);
            title = false; BuildHud();
            if (offerTutorial)
            {
                explorer.ResetMissionSpawn();
                campaign.CloseReport();
                Tutorial = new TutorialDirector(root, missionHudRoot, explorer, this);
            }
            else ShowReport();
        }
        void BuildHud()
        {
            commandConsole?.Close();
            DisposeTutorial();
            mainMenu?.Dispose(); mainMenu = null;
            CloseMap();
            Terminal.Reset();
            backdrop?.Dispose(); backdrop = null;
            root.Clear(); overlay = null; taskView = null; attempt = null; paused = false;
            missionHudRoot = Box(root, "mission-hud-root"); missionHudRoot.name = "mission-hud-root"; missionHudRoot.pickingMode = PickingMode.Ignore;
            hud = new MissionHud(missionHudRoot, definition, campaign, locale, explorer, UseStation, ShowReport, Settings, ToggleMap);
            guidance = new MissionGuidance(missionHudRoot, definition, locale, tourProgress);
            Terminal.AttachPrompt(missionHudRoot);
            eventBanner = new MissionEventBanner(root);
            stormWindHud = new StormWindHud(missionHudRoot, explorer);
        }
        VisualElement Dialog(string titleText = null)
        {
            CloseMap();
            Terminal.Reset();
            if (overlay != null) overlay.RemoveFromHierarchy();
            overlay = Box(root, "overlay"); var scroll = new ScrollView(); scroll.AddToClassList("dialog"); scroll.AddToClassList("panel"); overlay.Add(scroll);
            if (titleText != null) Text(scroll.contentContainer, titleText, "title"); MissionVisualStyle.Open(scroll); return scroll.contentContainer;
        }
        void CloseOverlay()
        {
            bool returnToMapButton = IsMapOpen;
            CloseMap();
            if (campaign != null && campaign.OpenTask != null) campaign.CancelTask();
            overlay?.RemoveFromHierarchy(); overlay = null; attempt = null; taskView = null; paused = false;
            if (returnToMapButton) root.Q<Button>("hud-map")?.Focus();
        }
        public void ToggleMap()
        {
            if (TutorialActive) return;
            if (IsMapOpen) { CloseOverlay(); return; }
            if (campaign == null || BlocksWorld || (campaign.Phase != Phase.Plan && campaign.Phase != Phase.Tasks)) return;
            // Disable underlying controls as well as world input: keyboard focus cannot leak into the HUD.
            foreach (var child in root.Children())
                if (child.enabledSelf) { mapDisabledElements.Add(child); child.SetEnabled(false); }
            overlay = Box(root); overlay.name = "map-overlay";
            overlay.style.position = Position.Absolute; overlay.style.left = 0; overlay.style.right = 0;
            overlay.style.top = 0; overlay.style.bottom = 0;
            paused = true;
            worldMap = new WorldMapView(overlay, explorer, locale, CloseOverlay);
        }
        void CloseMap()
        {
            if (worldMap == null) return;
            worldMap.Dispose(); worldMap = null;
            overlay?.RemoveFromHierarchy(); overlay = null; paused = false;
            foreach (var child in mapDisabledElements) child.SetEnabled(true);
            mapDisabledElements.Clear();
        }
        void ShowTitle()
        {
            commandConsole?.Close();
            DisposeTutorial();
            CloseMap();
            Terminal.Reset();
            title = true; hud = null; root.Clear(); overlay = null; attempt = null; taskView = null;
            backdrop?.Dispose(); backdrop = null;
            mainMenu?.Dispose();
            mainMenu = new MissionMainMenu(root, locale, StartMission, Help, Settings, Languages, ApplicationExit.Quit);
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
            paused = true;
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
        { if (attempt == null && Terminal.Near) Terminal.TryInteract(); }
        VisualElement StationPanel()
        {
            CloseMap();
            Terminal.Reset();
            overlay?.RemoveFromHierarchy();
            overlay = Box(root, "overlay challenge-backdrop");
            return Box(overlay);
        }
        void UseStation()
        {
            if (BlocksWorld || explorer.IsDrivingRover) return;
            if (TutorialActive) { Tutorial.Interact(); return; }
            if (Terminal.TryInteract()) return;
            if (Observatory.TryInteract()) return;
            if (GreenhouseCare.TryInteract()) return;
            if (Solar.TryInteract()) return;
            var offer = stations.Resolve(campaign, explorer.PlayerPosition);
            switch (offer.Action)
            {
                case StationAction.PlanEnergy: ShowEnergy(); break;
                case StationAction.SealSol: ShowSeal(); break;
                case StationAction.RegulateShield: ShowShieldRegulation(); break;
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
        internal void ShowShieldRegulation()
        {
            if (BlocksWorld || TutorialActive || explorer.IsDrivingRover) return;
            var offer = stations.Resolve(campaign, explorer.PlayerPosition);
            if (!campaign.BeginShieldRegulation(offer.Room?.id)) return;
            new ShieldRegulationView(StationPanel(), campaign, locale, CloseOverlay);
        }
        internal void BuildComputerEnergy(VisualElement parent, Action close)
        { if (Terminal.Focused && Terminal.Near) EnergyView.Build(parent, campaign, definition.rules, locale, close); }
        internal void BuildComputerCommunications(VisualElement parent, Func<bool> canInteract)
        { new CommunicationsView(parent, campaign, canInteract, null, () => communicationsArchive?.Error); }
        internal void SealFromComputer() { ShowSeal(); }
        public void ShowObservatory()
        {
            if (TutorialActive || campaign == null || title || attempt != null) return;
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
        {
            CloseMap();Terminal.Reset();overlay?.RemoveFromHierarchy();
            overlay=Box(root,"overlay");var view=Box(overlay,"mission-report-popup");
            MissionReportView.Build(view, campaign, locale, explorer, ShowDailyTransmission);
        }
        void ShowDailyTransmission()
        {
            var view = Dialog();
            view.GetFirstAncestorOfType<ScrollView>().AddToClassList("communications-dialog");
            new CommunicationsView(view, campaign, () => true, () =>
            {
                if (!campaign.CanAdvanceDay) return;
                communicationsArchive?.Save(campaign.Communications);
                campaign.NextSol(); if (campaign.Phase == Phase.Report) { BuildHud(); ShowReport(); } else ShowEnd();
            }, () => communicationsArchive?.Error);
            Button(view, "Voltar ao resumo", ShowSummary, "ghost");
        }
        void ShowEnd()
        {
            eventBanner?.Hide();
            var view = Dialog(locale.T(campaign.Phase == Phase.Victory ? "cert.title" : "gameover." + campaign.FailureCause + ".title"));
            view.name = "mission-end";
            Text(view, campaign.Phase == Phase.Victory ? locale.T("cert.tiers." + campaign.SuccessTier) : locale.T("gameover." + campaign.FailureCause + ".body"), "heading");
            if (campaign.Phase == Phase.GameOver && campaign.FailureCause == "radiation" && campaign.Demands.ImpactedToday && !campaign.Demands.ShieldRegulated)
                Text(view, "A erupção solar atingiu a base sem o escudo regulado. Nos dias 2 e 5, faça a regulagem antes do impacto.");
            if (campaign.Phase == Phase.GameOver && campaign.FailureCause == "suffocation" && campaign.Solar.BuriedPanels > 0)
                Text(view, "Os painéis ficaram soterrados. A bateria acabou, a produção de oxigênio parou e a reserva de ar chegou a zero. Limpe os painéis a tempo na próxima tentativa.");
            if(campaign.Phase==Phase.Victory)
            {
                var nickname=new TextField(locale.T("cert.nickname")) {maxLength=20,value=PlayerPrefs.GetString("ARES.Nickname","")}; view.Add(nickname);
                Text(view,locale.T("cert.local"),"small"); var certificate=Text(view,"");
                Button(view,locale.T("cert.show"),()=>{string name=nickname.value.Trim(); if(name.Length==0) name="Astronaut"; PlayerPrefs.SetString("ARES.Nickname",name); certificate.text=locale.T("cert.body","name",name)+"\n"+locale.T("cert.stats","i",campaign.Integrity,"o",campaign.Oxygen,"d",campaign.Dose);});
            }
            Button(view, locale.T("title.play"), StartMission); Button(view, locale.T("settings.home"), ShowTitle, "ghost");
            Button(view, "Diário de descobertas", ShowObservatory, "ghost").name = "end-observatory";
            Button(view, "Histórico de comunicação", ShowCommunicationHistory, "ghost").name = "end-communications";
        }
        void ShowCommunicationHistory()
        {
            var view = Dialog(); view.GetFirstAncestorOfType<ScrollView>().AddToClassList("communications-dialog");
            new CommunicationsView(view, campaign, () => false, null, () => communicationsArchive?.Error);
            Button(view, "Voltar", ShowEnd, "ghost");
        }
        // Explicit test fixture entry point: the control suite tests locomotion independently of menus.
        public void SetInspectionTestMode() { DisposeTutorial(); mainMenu?.Dispose(); mainMenu=null; CloseMap(); Terminal.Reset(); backdrop?.Dispose(); backdrop = null; title = false; root.Clear(); overlay = null; campaign = null; hud = null; }
    }
}
