using System;
using Ares.Data;
using Ares.Domain;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    internal sealed class MissionHud
    {
        readonly Label next, stars, returnControl;
        readonly Label[] taskLabels;
        readonly Foldout checklist;
        readonly VisualElement taskList;
        readonly ProgressBar taskProgress;
        readonly ProgressBar integrity, oxygen, dose, energy;
        readonly Button use;
        readonly Button optionalShield;
        readonly Label shelterObjective;
        readonly Campaign campaign;
        readonly Localization l;
        readonly GameDefinition definition;
        readonly SolarMaintenance solar;
        readonly SolarMaintenanceHud solarHud;
        readonly GreenhouseCare greenhouseCare;
        readonly GreenhouseCareHud greenhouseHud;
        readonly ObservatoryInteraction observatory;
        readonly ReferenceExplorer explorer;
        readonly Label roverStatus;
        readonly Button roverButton;
        readonly Label cursorHint;
        public MissionHud(VisualElement root, GameDefinition definition, Campaign campaign, Localization l, ReferenceExplorer explorer, Action interact, Action report, Action settings, Action openMap)
        {
            this.campaign = campaign; this.l = l; this.definition = definition;
            this.explorer = explorer;
            observatory = explorer.GetComponent<ObservatoryInteraction>();
            solar = explorer.GetComponent<SolarMaintenance>();
            solarHud = new SolarMaintenanceHud(root, solar, l);
            greenhouseCare=explorer.GetComponent<GreenhouseCare>();greenhouseHud=new GreenhouseCareHud(root,greenhouseCare);
            var top = Box(root, "hud hud-reference"); top.name = "hud-interactive-top";
            root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/HudReference"));
            top.RegisterCallback<GeometryChangedEvent>(e => {
                top.EnableInClassList("hud-tight", e.newRect.width < 1300);
                top.EnableInClassList("hud-narrow", e.newRect.width < 1000);
            });
            var bars = Box(top, "hud-resource-strip"); bars.name = "hud-resource-strip";
            integrity = Bar(bars,l.T("hud.integrity"),"integrity"); oxygen = Bar(bars,l.T("hud.oxygen"),"oxygen"); dose = Bar(bars,l.T("hud.dose"),"dose"); this.energy = Bar(bars,l.T("hud.energy"),"energy");
            // The reference's information shortcut uses the existing report action.
            HudButton(bars,"info",l.T("hud.report"),report).AddToClassList("hud-resource-info");
            var clockBox=Box(top,"panel hud-clock"); next=Text(clockBox,"","small");
            stars=Text(root,"","panel hud-stars");
            taskList = Box(root, "task-list panel");
            taskList.style.display = campaign.Phase == Phase.Tasks ? DisplayStyle.Flex : DisplayStyle.None;
            checklist = new Foldout { value = true }; taskList.Add(checklist);
            taskLabels = new Label[campaign.Current.tasks.Length];
            for (int i=0;i<taskLabels.Length;i++)
            {
                var assignment=campaign.Current.tasks[i];
                taskLabels[i]=Text(checklist,"","task-item"); taskLabels[i].tooltip=l.T("mini."+assignment.id+".name");
            }
            taskProgress=new ProgressBar {lowValue=0,highValue=taskLabels.Length,title="",tooltip=l.T("tasks.progress")}; checklist.Add(taskProgress);
            returnControl=Text(checklist,l.T("mini.returnControl"),"small");
            shelterObjective=Text(checklist,"","small");
            optionalShield = Button(checklist, "", () => explorer.GetComponent<GameFrontend>().ShowShieldRegulation(), "ghost optional-shield");
            optionalShield.name = "optional-shield-mission";
            optionalShield.tooltip = PresentationLocalizer.Text("Opcional: use no setor do escudo. Não conta para as missões obrigatórias.");
            var tools = Box(top, "row hud-tools");
            HudButton(tools,"roof",l.T("settings.roofs"),explorer.ToggleRoofs);
            HudButton(tools,"focus",l.T("hud.recenterCamera"),explorer.RecenterCamera);
            HudButton(tools,"report",l.T("hud.report"),report);
            HudButton(tools,"camera",l.T("hud.camera"),explorer.ToggleCamera);
            HudButton(tools,"settings",l.T("hud.settings"),settings);
            HudButton(tools,"observatory","Diário",() => explorer.GetComponent<GameFrontend>().ShowObservatory());
            use = Button(root, "", interact); use.AddToClassList("use"); use.AddToClassList("ux-action");
            roverButton = Button(root, "", explorer.ToggleRover, "hud-rover");
            roverButton.name = "rover-controls";
            roverButton.Add(new HudGlyph("rover", 24));
            roverStatus = Text(roverButton, "", "hud-rover-label");
            roverStatus.pickingMode = PickingMode.Ignore;
            cursorHint = Text(root, l.T("hud.cursorUnlock"), "hud-cursor-hint");
            cursorHint.name = "hud-cursor-hint"; cursorHint.pickingMode = PickingMode.Ignore;
            var mapButton = Button(root, "", openMap, "map-launcher");
            mapButton.name = "hud-map"; mapButton.tooltip = l.T("map.open");
            mapButton.Add(new ChallengeIcon("map",MissionVisualStyle.Hex("#282139"),24));
            var mapLabel = Box(mapButton); mapLabel.pickingMode = PickingMode.Ignore;
            Text(mapLabel, l.T("map.button")).pickingMode = PickingMode.Ignore;
            Text(mapLabel, "[M]", "map-key").pickingMode = PickingMode.Ignore;
        }
        public void Update(StationOffer offer, Vector3 player)
        {
            // StartSol enters Tasks only after the terminal's energy plan is confirmed.
            taskList.style.display = campaign.Phase == Phase.Tasks ? DisplayStyle.Flex : DisplayStyle.None;
            var c = campaign; integrity.value=c.Integrity; oxygen.value=c.Oxygen; dose.value=c.Dose;
            energy.value = 100f * c.Solar.ReserveKwh / c.Solar.CapacityKwh;
            string target=c.Phase==Phase.Plan?"control":null;
            if(c.Phase==Phase.Tasks) { target="control"; foreach(var task in c.Current.tasks) if(!c.Completed(task.id)) {target=task.room;break;} }
            if(c.HazardTarget != null) target=c.HazardTarget;
            next.text=target==null?"":l.T("hud.next","room",l.T("rooms."+target));
            int total=c.BonusStars; for(int s=0;s<=c.SolIndex;s++) foreach(var task in definition.campaign.sols[s].tasks) total+=c.Stars(s,task.id);
            stars.text="★ "+total;
            int completed=0;
            for(int i=0;i<taskLabels.Length;i++)
            {
                var task=c.Current.tasks[i]; bool done=c.Completed(task.id); if(done) completed++;
                taskLabels[i].text=(done?"✓ ":"○ ")+l.T(Ares.Domain.GreenhouseSimulation.IsTask(task.id) ? "task.waterPlants" : "rooms."+task.room);
                taskLabels[i].EnableInClassList("success",done);
                taskLabels[i].tooltip=l.T("mini."+task.id+".name");
            }
            checklist.text=l.T("tasks.title","n",c.Current.sol)+"  "+completed+"/"+taskLabels.Length;
            returnControl.text = "Tudo pronto! Explore a base até o fim do dia.";
            if(c.NativeHazards)returnControl.text=l.T("hazard.seal");
            shelterObjective.style.display=c.NativePlan?.Has(Ares.Domain.Hazards.HazardKind.Radiation)==true?DisplayStyle.Flex:DisplayStyle.None;shelterObjective.text=(c.ShelterReached?"✓ ":"○ ")+l.T("hazard.shelterTask");
            taskProgress.value=completed; returnControl.style.display=completed==taskLabels.Length?DisplayStyle.Flex:DisplayStyle.None;
            optionalShield.style.display = c.Demands.ShieldTaskAvailable ? DisplayStyle.Flex : DisplayStyle.None;
            optionalShield.text = PresentationLocalizer.Text(c.Demands.ShieldRegulated ? "✓ Escudo regulado para este dia" : "○ Regular Escudo Eletromagnético\nOPCIONAL");
            optionalShield.SetEnabled(c.Phase == Phase.Tasks && c.OpenTask == null && offer.Room?.id == "shield" && !c.Demands.ShieldRegulated);
            use.text = offer.CanUse ? l.T("hud.use") + " [E] · " + l.T("rooms." + offer.Room.id)
                : l.T(offer.Action == StationAction.NeedsPlan ? "station.needsPlan" : offer.Action == StationAction.Completed ? "station.completed" : "station.approach");
            if (solar.CanPickUp) use.text = l.T("solar.pickup");
            if (offer.Action == StationAction.FindComputer) use.text = "Divida a energia no computador";
            if (offer.Action == StationAction.RegulateShield) use.text = "[E] Regular Escudo Eletromagnético";
            if (solar.CanReturn) use.text = l.T("solar.return");
            if(greenhouseCare.InteractionPrompt!=null)use.text=greenhouseCare.InteractionPrompt;
            if(observatory.CanUse)use.text="[E] Olhar pelo telescópio";
            use.SetEnabled(offer.CanUse || solar.CanPickUp || solar.CanReturn || greenhouseCare.InteractionPrompt!=null || observatory.CanUse);
            var terminal = explorer.GetComponent<GameFrontend>().Terminal;
            if (terminal != null && terminal.Near) { use.text = "[E] Usar computador"; use.SetEnabled(true); }
            if(explorer.NearBed || explorer.BedBusy) {
                use.text=explorer.BedBusy ? (explorer.Rest.Stage==RestStage.Resting ? "[E] Levantar" : "Aguarde a animação · [W A S D] mover · [E] interagir") : "[E] Deitar";
                use.SetEnabled(!explorer.BedBusy || explorer.Rest.Stage==RestStage.Resting);
            }
            bool driving = explorer.IsDrivingRover;
            roverStatus.text = driving
                ? l.T("hud.roverReturn") + $"\nNOMAD 06 · {Mathf.Abs(explorer.Rover.Motion.Speed) * 3.6f:00} km/h"
                : l.T("hud.roverDrive") + "\nNOMAD 06";
            roverButton.tooltip = l.T(driving ? "hud.roverControls" : "hud.roverDrive");
            roverButton.EnableInClassList("hud-rover-active", driving);
            roverButton.SetEnabled(!explorer.BedBusy && !explorer.GetComponent<GameFrontend>().BlocksWorld);
            cursorHint.text = l.T(explorer.CursorInteraction ? "hud.cursorLock" : "hud.cursorUnlock");
            cursorHint.style.display = MouseAvailable() && !explorer.BedBusy && !explorer.GetComponent<GameFrontend>().RequiresCursor
                ? DisplayStyle.Flex : DisplayStyle.None;
            if (driving) { use.text = "Controle remoto ativo · [Y] Voltar ao astronauta"; use.SetEnabled(false); }
            solarHud.Update();
            solarHud.SetVisible(!greenhouseCare.Inside && (player.z < -64 || solar.Cleaning.HasBroom));greenhouseHud.Update();
        }
        static bool MouseAvailable() => UnityEngine.InputSystem.Mouse.current != null;
        static ProgressBar Bar(VisualElement parent,string title,string tone)
        {
            var cell=Box(parent,"hud-resource-cell "+tone);cell.name="gauge-"+tone;
            var icon=Box(cell,"hud-resource-icon");icon.pickingMode=PickingMode.Ignore;icon.Add(new HudGlyph(tone));
            var readout=Box(cell,"hud-resource-readout");
            Text(readout,title.ToUpperInvariant(),"hud-resource-label");
            var bar=new ProgressBar {lowValue=0,highValue=100,title="",name="hud-value-"+tone};
            bar.AddToClassList("hud-resource-bar");readout.Add(bar);
            cell.tooltip=tone=="dose"?"Radiação recebida: quanto menos, melhor. É uma escala do jogo."
                :tone=="oxygen"?"Ar guardado para a equipe respirar."
                :tone=="energy"?"Quanto a bateria ainda tem de energia.":"Estado da base: cuide dos equipamentos.";
            return bar;
        }
        static Button HudButton(VisualElement parent,string icon,string label,Action action)
        {
            var button=Button(parent,"",action,"hud-nav");button.tooltip=label;button.name="hud-"+icon;
            if(icon=="roof" || icon=="focus")button.AddToClassList("hud-nav-neutral");
            button.Add(new HudGlyph(icon,icon=="info"?20:21));
            return button;
        }
    }
}
