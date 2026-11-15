using System;
using System.IO;
using System.Reflection;
using Ares.Data;
using Ares.Tasks;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Scoped check: four HUD assets, source parsing and one changed equipment preview.
    /// Does not traverse missions or inspect rover controls.</summary>
    [InitializeOnLoad]
    public static class VisualLearningChecks
    {
        const string Pending="ARES.VisualLearning";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static GameFrontend ui;
        static VisualElement root,preview;
        static TaskAttempt attempt;
        static object taskView;
        static int stage,frame;
        static float elapsed;
        static double started;
        static bool background;
        static VisualLearningChecks(){EditorApplication.update+=Tick;}
        public static void Begin()
        {
            stage=0;frame=-1;elapsed=0;ui=null;started=0;
            background=Application.runInBackground;Application.runInBackground=true;SessionState.SetBool(Pending,true);
            Directory.CreateDirectory("Logs");File.WriteAllText("Logs/ares-visual-learning.txt","RUNNING");
            foreach(string key in new[]{"integrity","oxygen","dose","energy"})
                AssetDatabase.ImportAsset("Assets/ARES/Resources/ARES/UI/HudIcons/"+key+"-v1.png",ImportAssetOptions.ForceUpdate);
            EditorApplication.isPaused=false;EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try
            {
                if(started==0)started=EditorApplication.timeSinceStartup;
                Require(EditorApplication.timeSinceStartup-started<65,"Timeout");
                if(frame==Time.frameCount)return;frame=Time.frameCount;elapsed+=Mathf.Min(Time.deltaTime,.05f);
                if(ui==null)
                {
                    var found=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();if(found==null)return;
                    var explorer=found.GetComponent<ReferenceExplorer>();
                    if(typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer)==null)return;
                    ui=found;ui.Apod.OfflineOnly=true;ui.StartMission(false);ui.Campaign.CloseReport();Call("CloseOverlay");
                    root=ui.GetComponent<UIDocument>().rootVisualElement;
                    Sources();elapsed=0;
                }
                if(stage==0&&elapsed>.8f)
                {
                    foreach(string key in new[]{"integrity","oxygen","dose","energy"})
                    {
                        var gauge=root.Q("gauge-"+key);Require(gauge!=null,"Missing gauge "+key);
                        var icon=gauge.Q<Image>();Require(icon?.image!=null,"Missing icon "+key);
                        Require(icon.worldBound.width>=32&&icon.worldBound.xMax<=gauge.worldBound.xMax,"Icon layout "+key);
                    }
                    Require(ui.GetComponent<ReferenceEnvironment>()!=null,"Reference world presentation missing");
                    ScreenCapture.CaptureScreenshot("Logs/ares-visual-hud.png");
                    ui.Campaign.Solar.DrawEnergy(ui.Campaign.Solar.CapacityKwh*.25f);Next();
                }
                else if(stage==1&&elapsed>.25f)
                {
                    Require(Mathf.Abs(root.Q("gauge-energy").Q<ProgressBar>().value-75)<.1f,"HUD battery did not follow real reserve");
                    preview=new VisualElement();preview.AddToClassList("overlay");preview.AddToClassList("challenge-backdrop");root.Add(preview);
                    var panel=new VisualElement();preview.Add(panel);
                    var explorer=ui.GetComponent<ReferenceExplorer>();attempt=new TaskAttempt("PipeFlow",explorer.definition,ui.Campaign);
                    var type=typeof(GameFrontend).Assembly.GetType("Ares.UI.TaskView");
                    taskView=Activator.CreateInstance(type,panel,attempt,new Localization("pt-BR"),(Action)(()=>{}),(Action)(()=>{}),(Action)(()=>{}),true);
                    Next();
                }
                else if(stage==2&&elapsed>.4f)
                {
                    var equipment=preview.Q(className:"mini-equipment-preview");
                    Require(equipment!=null&&!equipment.enabledInHierarchy&&!attempt.Begun&&attempt.Seconds==0,"Preview changed task state");
                    Require(preview.Q(className:"mini-overlay").worldBound.width>root.worldBound.width*.65f,"Window too narrow");
                    ScreenCapture.CaptureScreenshot("Logs/ares-visual-equipment.png");Next();
                }
                else if(stage==3&&elapsed>.25f)
                {
                    attempt.Begin();taskView.GetType().GetMethod("Render").Invoke(taskView,null);
                    Require(preview.Q(className:"mini-equipment-preview")==null,"Equipment still disabled after Begin");
                    root.style.width=1280;root.style.height=720;Next();
                }
                else if(stage==4&&elapsed>.4f)
                {
                    var shell=preview.Q(className:"mini-overlay");
                    Require(shell.worldBound.xMin>=0&&shell.worldBound.xMax<=1280,"Compact window overflow");
                    Require(preview.Q(className:"mini-footer").worldBound.yMax<=720,"Compact footer clipped");
                    ScreenCapture.CaptureScreenshot("Logs/ares-visual-compact.png");Next();
                }
                else if(stage==5&&elapsed>.25f)
                {
                    root.style.width=StyleKeyword.Null;root.style.height=StyleKeyword.Null;
                    preview.RemoveFromHierarchy();preview=null;
                    var explorer=ui.GetComponent<ReferenceExplorer>();
                    var player=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);
                    player.X=-8;player.Z=43;player.Speed=player.VelocityX=player.VelocityZ=0;explorer.RecenterCamera();Next();
                }
                else if(stage==6&&elapsed>1.1f)
                {ScreenCapture.CaptureScreenshot("Logs/ares-visual-life-support.png");Next();}
                else if(stage==7&&elapsed>.25f){Call("ShowReport");Next();}
                else if(stage==8&&elapsed>.4f){ScreenCapture.CaptureScreenshot("Logs/ares-visual-report.png");Next();}
                else if(stage==9&&elapsed>.25f)
                {Finish("PASS: four HUD icons; battery semantics; world props; official archive validation; APOD object/feed/date handling; one equipment preview with clock/input gating.");}
            }
            catch(Exception e){Finish("FAIL: "+e);}
        }
        static void Sources()
        {
            var archive=ScienceArchive.Load();Require(archive?.records?.Length>=5,"Missing real archive");
            foreach(var r in archive.records)Require(r.Valid,"Invalid official record "+r.id);
            Require(!OfficialSources.IsOfficial("https://nasa.gov.example.org/test"),"Fake NASA host accepted");
            Require(!OfficialSources.IsOfficial("http://nasa.gov/test"),"Insecure source accepted");
            string fixture=File.ReadAllText("Tools/ScienceEvidence/apod-contract.json");
            Require(ApodResponse.Parse(fixture,"2022-07-13")!=null,"APOD official object parse");
            Require(ApodResponse.Parse("["+fixture+"]","2022-07-13")!=null,"APOD feed parse");
            Require(ApodResponse.Parse(fixture,"2001-01-01")==null,"Wrong date accepted");
            Require(ApodResponse.Parse("<html>outage</html>","2022-07-13")==null,"HTML accepted");
            var def=ui.GetComponent<ReferenceExplorer>().definition;
            var a=new TaskAttempt("CmeCountdown",def,ui.Campaign);
            Require(!a.Sample&&a.Readings.ArchivedCme!=null,"Real record not used by task");
            Require(ui.Campaign.Current.sample,"Archive mislabeled whole simulated mission as real");
            var photo=new TaskAttempt("PhotoDetective",def,ui.Campaign);
            Require(!photo.Sample&&photo.Readings.ArchivedPhoto!=null,"Photo puzzle lost real APOD source");
        }
        static void Call(string name)=>typeof(GameFrontend).GetMethod(name,Private).Invoke(ui,null);
        static void Next(){stage++;elapsed=0;}
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        static void Finish(string result)
        {
            File.WriteAllText("Logs/ares-visual-learning.txt",result);SessionState.SetBool(Pending,false);
            Application.runInBackground=background;EditorApplication.ExitPlaymode();
        }
    }
}
