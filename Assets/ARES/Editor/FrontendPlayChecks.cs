using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ares.Domain;
using Ares.Tasks;
using Ares.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Fixed local test hook. Drives native button/slider events, restores edit mode on completion.</summary>
    [InitializeOnLoad]
    public static class FrontendPlayChecks
    {
        const string Pending = "ARES.FrontendCheck";
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        static int stage, task;
        static double next, started;
        static bool background;
        static FrontendPlayChecks() { EditorApplication.update += Update; }
        static void Update()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists("Temp/ares-ui-check.request"))
            {
                string token = File.GetLastWriteTimeUtc("Temp/ares-ui-check.request").Ticks.ToString();
                if (SessionState.GetString("ARES.UiRefresh", "") != token) { SessionState.SetString("ARES.UiRefresh",token); AssetDatabase.Refresh(); return; }
                File.Delete("Temp/ares-ui-check.request"); SessionState.SetBool(Pending,true); stage=0; task=0;
                background=Application.runInBackground; Application.runInBackground=true; EditorApplication.isPaused=false;
                started=EditorApplication.timeSinceStartup; next=started+2; File.WriteAllText("Logs/ares-ui.txt","RUNNING"); EditorApplication.EnterPlaymode(); return;
            }
            if (!SessionState.GetBool(Pending,false) || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup < next) return;
            next=EditorApplication.timeSinceStartup+.4;
            try
            {
                Require(EditorApplication.timeSinceStartup-started<100,"UI check timed out");
                var ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>(); Require(ui!=null,"Frontend not installed");
                var root=ui.GetComponent<UIDocument>().rootVisualElement;
                switch(stage)
                {
                    case 0: Require(ui.BlocksWorld,"Title does not block movement"); ScreenCapture.CaptureScreenshot("Logs/ares-title.png"); stage++; break;
                    case 1: Press(root, "Jogar", "Play"); Require(ui.Campaign.Phase==Phase.Report,"Play did not open report"); stage++; break;
                    case 2: ScreenCapture.CaptureScreenshot("Logs/ares-report.png"); stage=11; break;
                    case 11:
                        Press(root,"Planejar energia","Plan energy"); Require(ui.Campaign.Phase==Phase.Plan,"Report continue failed");
                        var explorer=ui.GetComponent<Ares.World.ReferenceExplorer>();
                        var player=(Ares.World.PlayerMotion)typeof(Ares.World.ReferenceExplorer).GetField("player",Fields).GetValue(explorer);
                        player.X=explorer.computerSettings.standPoint.x;player.Z=-explorer.computerSettings.standPoint.z;
                        player.VelocityX=player.VelocityZ=player.Speed=0;stage=13;break;
                    case 13: Call(ui,"ShowEnergy");stage=14;break;
                    case 14:
                        if(!ui.Terminal.Focused)break;
                        var arrow=root.Q<Button>("computer-energy");Require(arrow!=null,"Computer energy arrow missing");
                        using(var navigation=NavigationSubmitEvent.GetPooled()){navigation.target=arrow;arrow.SendEvent(navigation);}
                        next=EditorApplication.timeSinceStartup+.6;stage=3;break;
                    case 3:
                        var sliders=root.Query<SliderInt>().ToList(); Require(sliders.Count==3,"Energy needs three levers");
                        sliders[0].value=ui.Campaign.Current.demand.shield; sliders[1].value=ui.Campaign.Current.demand.life;
                        sliders[2].value=ui.Campaign.EnergyPool-ui.Campaign.Allocation.Total;
                        Require(ui.Campaign.Allocation.Total==ui.Campaign.EnergyPool,"Energy allocation lost values");
                        ScreenCapture.CaptureScreenshot("Logs/ares-energy.png"); stage++; break;
                    case 4: Press(root,"Iniciar Sol","Start Sol"); Require(ui.Campaign.Phase==Phase.Tasks,"Start Sol button failed"); task=0;ui.Terminal.Close();stage=15;break;
                    case 15: if(!ui.Terminal.OwnsCamera)stage=5;break;
                    case 5:
                        var assignment=ui.Campaign.Current.tasks[task]; Require(ui.Campaign.BeginTask(assignment.id,assignment.room),"Assigned task rejected"); Call(ui,"OpenTask",assignment.id);
                        stage++; break;
                    case 6:
                        if(GreenhouseSimulation.IsTask(ui.Campaign.OpenTask)) { GreenhouseCheckFixture.CompleteThroughControls(root,ui); stage=10; break; }
                        var attempt=(TaskAttempt)typeof(GameFrontend).GetField("attempt",Fields).GetValue(ui);
                        Press(root,"Começar desafio","Begin challenge","Começar","Begin");
                        Require(attempt.Begun,"Briefing start button failed"); TaskInteractionChecks.Solve(attempt);
                        Require(attempt.Solved,"UI task state did not solve"); stage=10; break;
                    case 10: if(task==0) ScreenCapture.CaptureScreenshot("Logs/ares-task-result.png"); stage=7; break;
                    case 7:
                        Press(root,"Continuar","Continue","Registrar aprendizado","Registrar cuidados de hoje"); Require(ui.Campaign.Completed(ui.Campaign.Current.tasks[task].id),"Continue did not award result");
                        task++; stage=task<ui.Campaign.Current.tasks.Length?5:8; break;
                    case 8:
                        Require(ui.Campaign.Seal("control",ui.Campaign.Allocation),"Completed Sol rejected sealing"); stage++; break;
                    case 9:
                        ScreenCapture.CaptureScreenshot("Logs/ares-summary.png"); stage=12; break;
                    case 12:
                        Press(root,"Revisar relatório na central");
                        CommunicationCheckFixture.SendRequiredReport(ui.Campaign);
                        stage=20; break;
                    case 20:
                        Press(root,"Avançar para o próximo dia","Concluir missão");
                        if(ui.Campaign.Phase==Phase.Report) stage=2;
                        else { Require(ui.Campaign.Phase==Phase.Victory,"Five Sols did not end in victory"); Finish(null); } break;
                }
            }
            catch(Exception error) { Finish(error.ToString()); }
        }
        static void Call(GameFrontend ui,string method,params object[] args) => typeof(GameFrontend).GetMethod(method,Fields).Invoke(ui,args);
        static void Press(VisualElement root,params string[] texts)
        {
            var button=root.Query<Button>().ToList().FirstOrDefault(b=>texts.Any(t=>b.text==t || b.text=="▶ "+t)); Require(button!=null,"Missing button: "+string.Join(" / ",texts));
            Require(button.enabledInHierarchy,"Disabled button: "+button.text);
            using(var evt=NavigationSubmitEvent.GetPooled()) { evt.target=button; button.SendEvent(evt); }
        }
        static void Require(bool valid,string message) { if(!valid) throw new InvalidOperationException(message); }
        static void Finish(string error)
        { SessionState.SetBool(Pending,false); File.WriteAllText("Logs/ares-ui.txt",error==null?"PASS: native title/report/energy buttons, all 25 assigned task result screens and five-Sol victory flow. Puzzle solutions supplied by test fixture; walking/station proximity require separate tests.":"FAIL stage "+stage+": "+error); Application.runInBackground=background; EditorApplication.ExitPlaymode(); }
    }
}
