using System;
using System.IO;
using System.Reflection;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Presentation-only checks. No rover, campaign traversal or puzzle windows.</summary>
    [InitializeOnLoad]
    public static class OxygenPresentationChecks
    {
        const string Pending="ARES.OxygenPresentation";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static VisualElement root;
        static int stage,frame;
        static float elapsed;
        static double started;
        static bool background;
        static OxygenPresentationChecks(){EditorApplication.update+=Tick;}
        [MenuItem("ARES/Checks/Oxygen screen only")]
        public static void Request(){File.WriteAllText("Temp/ares-computer-check.request","oxygen-ui");}
        public static void Begin()
        {
            stage=0;frame=-1;elapsed=0;ui=null;started=EditorApplication.timeSinceStartup;
            background=Application.runInBackground;Application.runInBackground=true;SessionState.SetBool(Pending,true);
            File.WriteAllText("Logs/ares-oxygen-ui.txt","RUNNING");EditorApplication.isPaused=false;EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try
            {
                Require(EditorApplication.timeSinceStartup-started<55,"Timeout");
                if(frame==Time.frameCount)return;frame=Time.frameCount;elapsed+=Mathf.Min(Time.deltaTime,.05f);
                if(ui==null)
                {
                    ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();if(ui==null)return;
                    explorer=ui.GetComponent<ReferenceExplorer>();
                    ui.StartMission(false);ui.Campaign.CloseReport();Call("CloseOverlay");
                    ui.Campaign.SetAllocation(new Ares.Data.Allocation{shield=30,life=40,greenhouse=30});ui.Campaign.StartSol();
                    var player=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);
                    player.X=explorer.computerSettings.standPoint.x;player.Z=-explorer.computerSettings.standPoint.z;
                    player.Speed=player.VelocityX=player.VelocityZ=0;explorer.reducedMotion=true;
                    root=ui.GetComponent<UIDocument>().rootVisualElement;elapsed=0;
                }
                if(stage==0&&elapsed>.4f){Require(ui.Terminal.TryInteract(),"Open terminal");Next();}
                else if(stage==1&&elapsed>.5f)
                {
                    Require(ui.Terminal.Focused,"Terminal focus");
                    Require(!root.Q<Foldout>("oxygen-science").value,"Details must start collapsed");
                    for(int i=0;i<3;i++)
                    {var button=root.Q<Button>("oxygen-mode-"+i);Require(button.resolvedStyle.backgroundImage.texture!=null,"Generated skin not loaded");Require(button.worldBound.height>=44&&button.worldBound.width>=100,"Button touch target");}
                    Require(root.Q<Button>("oxygen-laboratory").worldBound.yMax<root.worldBound.yMax,"Main actions require scrolling at normal resolution");
                    ScreenCapture.CaptureScreenshot("Logs/ares-oxygen-ui-overview.png");Next();
                }
                else if(stage==2&&elapsed>.2f){Press("oxygen-mode-0");Next();}
                else if(stage==3&&elapsed>.4f)
                {
                    Require(ui.Campaign.OxygenSystem.Mode==OxygenMode.Economy&&root.Q<Label>("oxygen-status-title").text.Contains("reserva"),"Economy feedback");
                    ScreenCapture.CaptureScreenshot("Logs/ares-oxygen-ui-economy.png");Press("oxygen-laboratory");Require(!ui.Campaign.OxygenSystem.LaboratoryEnabled,"Pause experiments");
                    Press("oxygen-mode-2");Next();
                }
                else if(stage==4&&elapsed>.4f)
                {
                    Require(root.Q<Label>("oxygen-advice").text.Contains("Equilibrar"),"Full tank must advise lower cost instead of needless boost");
                    typeof(GameFrontend).GetField("paused",Private).SetValue(ui,true);
                    typeof(OxygenPlant).GetMethod("SetReservePercent",Private).Invoke(ui.Campaign.OxygenSystem,new object[]{15f});Next();
                }
                else if(stage==5&&elapsed>.3f)
                {
                    Require(root.Q<Label>("oxygen-advice").text.Contains("recuperando"),"Low but recovering reserve must not ask for another boost");
                    ScreenCapture.CaptureScreenshot("Logs/ares-oxygen-ui-low.png");ui.Campaign.OxygenSystem.Tick(1,0);Next();
                }
                else if(stage==6&&elapsed>.3f)
                {
                    Require(root.Q<Label>("oxygen-advice").text.Contains("painéis"),"Power warning must give the available repair action");
                    ui.Campaign.OxygenSystem.Tick(6000,1000);Next();
                }
                else if(stage==7&&elapsed>.3f)
                {
                    Require(root.Q<Label>("oxygen-status-title").text.Contains("água"),"Dry generator priority");
                    root.Q<Foldout>("oxygen-science").value=true;Next();
                }
                else if(stage==8&&elapsed>.2f)
                {root.Q<Foldout>("oxygen-science").GetFirstAncestorOfType<ScrollView>().scrollOffset=new Vector2(0,10000);Next();}
                else if(stage==9&&elapsed>.25f)
                {
                    Require(root.Q<Foldout>("oxygen-numbers")!=null&&root.Q<Foldout>("oxygen-sources")!=null,"Optional telemetry and science preserved");
                    ScreenCapture.CaptureScreenshot("Logs/ares-oxygen-ui-learn.png");
                    stage=90;elapsed=0;
                }
                else if(stage==90&&elapsed>.2f)
                {
                    root.Q<Foldout>("oxygen-science").value=false;
                    root.Q(className:"oxygen-view").style.width=780;
                    root.Q<Foldout>("oxygen-science").GetFirstAncestorOfType<ScrollView>().scrollOffset=Vector2.zero;stage=10;elapsed=0;
                }
                else if(stage==10&&elapsed>.3f)
                {
                    var view=root.Q(className:"oxygen-view");Require(view.ClassListContains("ox-compact"),"Compact layout");
                    for(int i=0;i<3;i++){var button=root.Q<Button>("oxygen-mode-"+i);Require(button.worldBound.xMin>=view.worldBound.xMin&&button.worldBound.xMax<=view.worldBound.xMax,"Compact button overflow");}
                    ScreenCapture.CaptureScreenshot("Logs/ares-oxygen-ui-compact.png");Next();
                }
                else if(stage==11&&elapsed>.2f){Finish("PASS: generated button textures, normal/compact layout, collapsed optional detail, real mode/laboratory controls, economy/full/low/power/dry guidance. Screenshots ares-oxygen-ui-*.png.");}
            }
            catch(Exception error){Finish("FAIL stage "+stage+": "+error);}
        }
        static void Call(string method)=>typeof(GameFrontend).GetMethod(method,Private).Invoke(ui,null);
        static void Press(string name){var button=root.Q<Button>(name);using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=button;button.SendEvent(evt);}}
        static void Next(){stage++;elapsed=0;}
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static void Finish(string result){SessionState.SetBool(Pending,false);Application.runInBackground=background;File.WriteAllText("Logs/ares-oxygen-ui.txt",DateTime.UtcNow.ToString("O")+" "+result);EditorApplication.ExitPlaymode();}
    }
}
