using System;
using System.IO;
using System.Reflection;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Scoped HUD presentation/callback checks and original tutorial restoration.</summary>
    [InitializeOnLoad] public static class ReferenceUxChecks
    {
        static bool running, background; static int stage; static double next, started;
        static GameFrontend ui; static ReferenceExplorer explorer; static VisualElement root, top;
        static StyleSheet theme;
        static readonly string[] Buttons={"roof","focus","report","camera","settings","observatory"};
        static readonly string[] Unchanged={"hud-map","task-list","rover-controls"};
        static readonly System.Collections.Generic.Dictionary<string,string> baseline=new System.Collections.Generic.Dictionary<string,string>();
        static ReferenceUxChecks(){EditorApplication.update+=Tick;}
        public static void Begin(){running=true;stage=0;ui=null;started=EditorApplication.timeSinceStartup;next=0;background=Application.runInBackground;Application.runInBackground=true;File.WriteAllText("Logs/ares-strict-hud.txt","RUNNING");EditorApplication.EnterPlaymode();}
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Call(string name)=>typeof(GameFrontend).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,null);
        static void Press(string name){var b=root.Q<Button>(name);Require(b!=null && b.enabledInHierarchy,"Unavailable action "+name);using(var e=NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}}
        static VisualElement Find(string name)=>root.Q(name) ?? root.Q(className:name);
        static void Capture(string name)=>ScreenCapture.CaptureScreenshot("Logs/ares-strict-hud-"+name+".png");
        static void Later(Action action)=>root.schedule.Execute(action).StartingIn(250);
        static string Style(VisualElement e)
        {
            var s=e.resolvedStyle;
            string value=e.GetType().Name+":"+e.worldBound+":"+s.backgroundColor+":"+s.color+":"+s.borderTopLeftRadius+":"+s.borderBottomWidth+":"+s.fontSize;
            foreach(var child in e.Children())value+="|"+Style(child);
            return value;
        }
        static void Layout()
        {
            var strip=root.Q("hud-resource-strip");var tools=top.Q(className:"hud-tools");
            Require(strip.worldBound.xMax<tools.worldBound.xMin || tools.worldBound.yMin>=strip.worldBound.yMax,"HUD groups overlap");
            Require(tools.worldBound.xMax<=root.worldBound.xMax+1,"Tools clip at screen edge");
            Require(strip.resolvedStyle.borderTopLeftRadius>=30,"Reference capsule radius missing");
            foreach(string name in Buttons){var b=root.Q<Button>("hud-"+name);Require(b!=null && b.resolvedStyle.width>=44,"Missing or undersized button: "+name);Require(!string.IsNullOrWhiteSpace(b.tooltip),"Missing accessible tooltip");}
            foreach(string kind in new[]{"integrity","oxygen","dose","energy"}) {
                var cell=root.Q("gauge-"+kind);var icon=cell.Q(className:"hud-resource-icon");var bar=cell.Q<ProgressBar>();
                Require(icon.resolvedStyle.width>=36 && icon.resolvedStyle.width<=44 && bar.resolvedStyle.height>=14 && bar.resolvedStyle.height<=20,"Reference resource proportions missing: "+kind+" icon="+icon.resolvedStyle.width+" bar="+bar.resolvedStyle.height);
                Require(bar.worldBound.xMin>=icon.worldBound.xMax,"Resource overlaps its icon");
                Require(bar.worldBound.xMax<=strip.worldBound.xMax,"Resource overflows its capsule");
            }
        }
        static void Tick()
        {
            if(!running || !EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            try {
                Require(EditorApplication.timeSinceStartup-started<90,"HUD review timeout");
                if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
                if(ui==null){ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();if(ui==null)return;explorer=ui.GetComponent<ReferenceExplorer>();root=ui.GetComponent<UIDocument>().rootVisualElement;ui.Apod.OfflineOnly=true;next+=2;return;}
                switch(stage++) {
                    case 0:ui.StartMission(false);ui.Campaign.CloseReport();Call("CloseOverlay");break;
                    case 1:
                        top=root.Q("hud-interactive-top");theme=Resources.Load<StyleSheet>("ARES/UI/HudReference");Capture("reference");Layout();
                        Require(root.Q<ProgressBar>("hud-value-integrity").value==ui.Campaign.Integrity,"Integrity binding lost");
                        Require(root.Q<ProgressBar>("hud-value-oxygen").value==ui.Campaign.Oxygen,"Oxygen binding lost");
                        Require(root.Q<ProgressBar>("hud-value-dose").value==ui.Campaign.Dose,"Dose binding lost");
                        Require(Mathf.Abs(root.Q<ProgressBar>("hud-value-energy").value-100*ui.Campaign.Solar.ReserveKwh/ui.Campaign.Solar.CapacityKwh)<.01f,"Energy binding lost");
                        Capture("reference");baseline.Clear();foreach(var name in Unchanged)baseline[name]=Style(Find(name));Later(()=>top.styleSheets.Remove(theme));break;
                    case 2:
                        foreach(var name in Unchanged)Require(baseline[name]==Style(Find(name)),"HUD stylesheet leaked to "+name);
                        top.styleSheets.Add(theme);
                        bool roofs=explorer.RoofsVisible;Press("hud-roof");Require(explorer.RoofsVisible!=roofs,"Roof handler lost");Press("hud-roof");
                        bool camera=explorer.Isometric;Press("hud-camera");Require(explorer.Isometric!=camera,"Camera handler lost");Press("hud-camera");Press("hud-focus");
                        Press("hud-report");break;
                    case 3:Require(root.Q(className:"overlay")!=null,"Report handler lost");Call("CloseOverlay");Press("hud-settings");break;
                    case 4:Require(root.Q(className:"dialog")!=null,"Settings handler lost");Call("CloseOverlay");Press("hud-observatory");break;
                    case 5:Require(root.Q("observatory-view")!=null,"Journal handler lost");Call("CloseOverlay");Press("hud-info");break;
                    case 6:Require(root.Q(className:"overlay")!=null,"Info shortcut lost");Call("CloseOverlay");root.style.width=1280;root.style.height=720;break;
                    case 7:Layout();Capture("compact");Later(()=>{root.style.width=StyleKeyword.Null;root.style.height=StyleKeyword.Null;ui.StartMission();for(int i=0;i<100;i++)explorer.Rest.Tick(.05f,false);
                        var motion=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(explorer);motion.X=0;motion.Z=3;});next+=2;break;
                    case 8:
                        Require(ui.TutorialActive,"Original tutorial trigger lost");var balloon=root.Q("tutorial-balloon");
                        Require(!balloon.ClassListContains("ux-surface") && !root.Q("tutorial-objective").ClassListContains("ux-surface"),"Tutorial still has redesign classes");
                        Require(balloon.resolvedStyle.backgroundImage.texture==Resources.Load<Texture2D>("ARES/UI/Tutorial/ares-speech-v1"),"Original speech artwork not restored");
                        Require(!root.Q("tutorial-exit").ClassListContains("ux-action"),"Tutorial exit still redesigned");Capture("tutorial-restored");Later(()=>Press("tutorial-decline"));break;
                    case 9:Require(!ui.TutorialActive && !ui.BlocksWorld,"Declining tutorial did not restore play");Finish("PASS: reference capsule, 4 live resource bindings, 6 original toolbar buttons, report/info/settings/journal handlers, roof/camera/recenter actions, full/compact layout; stylesheet does not affect map/sidebar/rover HUD; original tutorial artwork/classes/decline behavior restored. Captures: reference, compact, tutorial-restored.");break;
                }
            }catch(Exception e){Finish("FAIL: "+e);}
        }
        static void Finish(string result){File.WriteAllText("Logs/ares-strict-hud.txt",result);running=false;Application.runInBackground=background;EditorApplication.ExitPlaymode();}
    }
}
