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
        readonly Label alarm, next, stars, returnControl;
        readonly Label[] taskLabels;
        readonly Foldout checklist;
        readonly ProgressBar taskProgress;
        readonly ProgressBar integrity, oxygen, dose, energy;
        readonly Button use;
        readonly Campaign campaign;
        readonly Localization l;
        readonly BaseMap map;
        readonly GameDefinition definition;
        readonly SolarMaintenance solar;
        readonly SolarMaintenanceHud solarHud;
        readonly GreenhouseCare greenhouseCare;
        readonly GreenhouseCareHud greenhouseHud;
        readonly ObservatoryInteraction observatory;
        readonly ReferenceExplorer explorer;
        readonly Label roverStatus;
        public MissionHud(VisualElement root, GameDefinition definition, Campaign campaign, Localization l, ReferenceExplorer explorer, Action interact, Action report, Action settings)
        {
            this.campaign = campaign; this.l = l; this.definition = definition;
            this.explorer = explorer;
            observatory = explorer.GetComponent<ObservatoryInteraction>();
            solar = explorer.GetComponent<SolarMaintenance>();
            solarHud = new SolarMaintenanceHud(root, solar, l);
            greenhouseCare=explorer.GetComponent<GreenhouseCare>();greenhouseHud=new GreenhouseCareHud(root,greenhouseCare);
            var top = Box(root, "hud"); var bars = Box(top, "panel row");
            integrity = Bar(bars,l.T("hud.integrity"),"integrity"); oxygen = Bar(bars,l.T("hud.oxygen"),"oxygen"); dose = Bar(bars,l.T("hud.dose"),"dose"); this.energy = Bar(bars,l.T("hud.energy"),"energy");
            var clockBox=Box(top,"panel hud-clock"); next=Text(clockBox,"","small");
            stars=Text(root,"","panel hud-stars");
            var list = Box(root, "task-list panel");
            checklist = new Foldout { value = true }; list.Add(checklist);
            taskLabels = new Label[campaign.Current.tasks.Length];
            for (int i=0;i<taskLabels.Length;i++)
            {
                var assignment=campaign.Current.tasks[i];
                taskLabels[i]=Text(checklist,"","task-item"); taskLabels[i].tooltip=l.T("mini."+assignment.id+".name");
            }
            taskProgress=new ProgressBar {lowValue=0,highValue=taskLabels.Length,title="",tooltip=l.T("tasks.progress")}; checklist.Add(taskProgress);
            returnControl=Text(checklist,l.T("mini.returnControl"),"small");
            var tools = Box(top, "row hud-tools");
            HudButton(tools,"roof",l.T("settings.roofs"),explorer.ToggleRoofs);
            HudButton(tools,"focus",l.T("hud.recenterCamera"),explorer.RecenterCamera);
            HudButton(tools,"report",l.T("hud.report"),report);
            HudButton(tools,"camera",l.T("hud.camera"),explorer.ToggleCamera);
            HudButton(tools,"settings",l.T("hud.settings"),settings);
            Button(tools,"Diário",() => explorer.GetComponent<GameFrontend>().ShowObservatory(),"ghost").name = "hud-observatory";
            use = Button(root, "", interact); use.AddToClassList("use");
            var roverPanel = new VisualElement { name = "rover-controls", pickingMode = PickingMode.Ignore };
            roverPanel.style.position = Position.Absolute; roverPanel.style.bottom = 88;
            roverPanel.style.left = new Length(50, LengthUnit.Percent); roverPanel.style.width = 440; roverPanel.style.marginLeft = -220;
            roverPanel.style.backgroundColor = new Color(.06f,.1f,.12f,.92f);
            roverPanel.style.borderTopWidth = 3; roverPanel.style.borderTopColor = new Color(.96f,.44f,.16f);
            roverPanel.style.paddingTop = 10; roverPanel.style.paddingBottom = 10;
            roverStatus = new Label { pickingMode = PickingMode.Ignore };
            roverStatus.style.color = new Color(.95f,.92f,.81f); roverStatus.style.fontSize = 14;
            roverStatus.style.unityTextAlign = TextAnchor.MiddleCenter; roverStatus.style.whiteSpace = WhiteSpace.Normal;
            roverPanel.Add(roverStatus); root.Add(roverPanel);
            alarm = Text(root, "", "alarm warning"); map = new BaseMap(definition); root.Add(map);
        }
        public void Update(StationOffer offer, Vector3 player)
        {
            var c = campaign; integrity.value=c.Integrity; oxygen.value=c.Oxygen; dose.value=c.Dose;
            var demand=c.Current.demand; var allocation=c.Allocation;
            energy.value = 100f*(Mathf.Min(allocation.shield,demand.shield)+Mathf.Min(allocation.life,demand.life)+Mathf.Min(allocation.greenhouse,demand.greenhouse))/Mathf.Max(1,demand.Total);
            string target=c.Phase==Phase.Plan?"control":null;
            if(c.Phase==Phase.Tasks) { target="control"; foreach(var task in c.Current.tasks) if(!c.Completed(task.id)) {target=task.room;break;} }
            if(!string.IsNullOrEmpty(c.Alarm)) target="shield";
            next.text=target==null?"":l.T("hud.next","room",l.T("rooms."+target));
            int total=c.BonusStars; for(int s=0;s<=c.SolIndex;s++) foreach(var task in definition.campaign.sols[s].tasks) total+=c.Stars(s,task.id);
            stars.text="★ "+total;
            int completed=0;
            for(int i=0;i<taskLabels.Length;i++)
            {
                var task=c.Current.tasks[i]; bool done=c.Completed(task.id); if(done) completed++;
                taskLabels[i].text=(done?"✓ ":"○ ")+l.T("rooms."+task.room);
                taskLabels[i].EnableInClassList("success",done);
            }
            checklist.text=l.T("tasks.title","n",c.Current.sol)+"  "+completed+"/"+taskLabels.Length;
            returnControl.text = "Tudo pronto! Explore a base até o fim do dia.";
            taskProgress.value=completed; returnControl.style.display=completed==taskLabels.Length?DisplayStyle.Flex:DisplayStyle.None;
            use.text = offer.CanUse ? l.T("hud.use") + " [E] · " + l.T("rooms." + offer.Room.id)
                : l.T(offer.Action == StationAction.NeedsPlan ? "station.needsPlan" : offer.Action == StationAction.Completed ? "station.completed" : "station.approach");
            if (solar.CanPickUp) use.text = l.T("solar.pickup");
            if (offer.Action == StationAction.FindComputer) use.text = "Vá ao computador para gerenciar a energia";
            if (solar.CanReturn) use.text = l.T("solar.return");
            if(greenhouseCare.InteractionPrompt!=null)use.text=greenhouseCare.InteractionPrompt;
            if(observatory.CanUse)use.text="[E] Olhar pelo telescópio";
            use.SetEnabled(offer.CanUse || solar.CanPickUp || solar.CanReturn || greenhouseCare.InteractionPrompt!=null || observatory.CanUse);
            var terminal = explorer.GetComponent<GameFrontend>().Terminal;
            if (terminal != null && terminal.Near) { use.text = "[E] Acessar computador · Oxigênio / Energia"; use.SetEnabled(true); }
            bool driving = explorer.IsDrivingRover;
            roverStatus.text = driving
                ? $"NOMAD 06   /   {Mathf.Abs(explorer.Rover.Motion.Speed) * 3.6f:00} km/h   /   {(explorer.Rover.Motion.Speed < -.05f ? "RÉ" : "EXPLORAÇÃO")}\nW/S acelerar e frear · A/D virar\nArraste: câmera · Roda: zoom · R: centralizar · Y: voltar"
                : "[Y] Pilotar rover  ·  NOMAD 06";
            if (driving) { use.text = "Controle remoto ativo · [Y] Voltar ao astronauta"; use.SetEnabled(false); }
            solarHud.Update();
            solarHud.SetVisible(!greenhouseCare.Inside);greenhouseHud.Update();
            alarm.style.display = string.IsNullOrEmpty(c.Alarm) ? DisplayStyle.None : DisplayStyle.Flex;
            alarm.text = l.T("tasks.T05.goal") + " " + Mathf.CeilToInt(60 - c.AlarmSeconds) + " s";
            map.Player = player; map.MarkDirtyRepaint();
        }
        static ProgressBar Bar(VisualElement parent,string title,string tone)
        { var cell=Box(parent,"resource"); Text(cell,title,"small"); var bar=new ProgressBar {lowValue=0,highValue=100}; bar.AddToClassList(tone); cell.Add(bar); return bar; }
        static void HudButton(VisualElement parent,string icon,string label,Action action)
        {
            var button=Button(parent,"",action,"ghost hud-tool"); button.tooltip=label; button.name="hud-"+icon;
            button.Add(new ChallengeIcon(icon,ChallengePalette.Ink,24));
        }
    }
    internal sealed class BaseMap : VisualElement
    {
        readonly GameDefinition definition;
        public Vector3 Player;
        public BaseMap(GameDefinition definition) { this.definition = definition; AddToClassList("minimap"); pickingMode = PickingMode.Ignore; generateVisualContent += Draw; }
        Vector2 Point(float x, float z) => new Vector2(110 + x * 1.05f, 170 - z * 1.05f);
        void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D; p.strokeColor = new Color32(180,170,160,255); p.lineWidth = 4;
            foreach (var path in definition.layout.paths) { p.BeginPath(); p.MoveTo(Point(path.a.x,path.a.z)); p.LineTo(Point(path.b.x,path.b.z)); p.Stroke(); }
            foreach (var room in definition.layout.rooms) { ColorUtility.TryParseHtmlString(room.color, out var color); p.fillColor = color; p.BeginPath(); p.Arc(Point(room.x,room.z), Mathf.Max(5,room.width*.4f),0,360); p.Fill(); }
            p.fillColor = new Color32(48,36,62,255); p.BeginPath(); p.Arc(Point(Player.x,-Player.z),4,0,360); p.Fill();
        }
    }
}
