using System;
using System.IO;
using System.Reflection;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    [InitializeOnLoad]
    public static class ComputerOxygenChecks
    {
        const string Request="Temp/ares-computer-check.request", Pending="ARES.ComputerCheck";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static Keyboard keyboard;
        static ReferenceExplorer explorer;
        static GameFrontend frontend;
        static int stage,frame;
        static float elapsed,fieldOfView,pausedAt;
        static double started;
        static Vector3 player, cameraPosition;
        static Quaternion cameraRotation;
        static bool background;
        static ComputerOxygenChecks()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Pending,false))Finish("FAIL: Play interrupted");};
        }
        [MenuItem("ARES/Checks/Computer and oxygen")]
        public static void RequestCheck() {Directory.CreateDirectory("Temp");File.WriteAllText(Request,"check");}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Request))
            {
                string token=File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if(SessionState.GetString(Pending+".refresh","")!=token){SessionState.SetString(Pending+".refresh",token);AssetDatabase.Refresh();return;}
                if(File.ReadAllText(Request).Trim()=="dormitory")
                {File.Delete(Request);DormitoryChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="camera-controls")
                {File.Delete(Request);CameraControlChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="reference-ux")
                {File.Delete(Request);ReferenceUxChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="refinement")
                {File.Delete(Request);RefinementChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="oxygen-ui")
                {File.Delete(Request);OxygenPresentationChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="wind-preview")
                {File.Delete(Request);MartianWindPreview.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="storm-preview")
                {File.Delete(Request);DustStormPreview.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="command-console")
                {File.Delete(Request);CommandConsolePreview.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="telescope-preview")
                {File.Delete(Request);ObservatoryPresentationChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="casa-marte")
                {File.Delete(Request);GreenhouseChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="communications")
                {File.Delete(Request);CommunicationsChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="visual-learning")
                {File.Delete(Request);VisualLearningChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="world-map")
                {File.Delete(Request);WorldMapChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="tutorial")
                {File.Delete(Request);TutorialChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="mission-visual")
                {File.Delete(Request);MissionVisualChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="mission-events")
                {File.Delete(Request);MissionEventChecks.Begin();return;}
                if(File.ReadAllText(Request).Trim()=="english")
                {File.Delete(Request);EnglishPresentationChecks.Begin();return;}
                File.Delete(Request);Directory.CreateDirectory("Logs");File.WriteAllText("Logs/ares-computer.txt","RUNNING");
                stage=0;frame=-1;elapsed=0;started=EditorApplication.timeSinceStartup;SessionState.SetBool(Pending,true);
                background=Application.runInBackground;Application.runInBackground=true;EditorApplication.isPaused=false;EditorApplication.EnterPlaymode();return;
            }
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying)return;
            try
            {
                Require(EditorApplication.timeSinceStartup-started<100,"Timeout");
                if(frame==Time.frameCount)return;frame=Time.frameCount;elapsed+=Mathf.Min(Time.deltaTime,.05f);
                if(keyboard==null)
                {
                    frontend=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();explorer=UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    if(frontend==null||explorer?.Rover==null)return;
                    DomainChecks();keyboard=InputSystem.AddDevice<Keyboard>("ARES computer check");
                    frontend.StartMission(false);frontend.Campaign.CloseReport();Call("CloseOverlay");
                    frontend.Apod.BeginMission(10);string firstDate=frontend.Apod.Collection.entries[0].date;
                    var dates=new System.Collections.Generic.HashSet<string>();
                    for(int i=0;i<5;i++){var entry=frontend.Apod.Collection.entries[i];Require(dates.Add(entry.date)&&frontend.Apod.Image(i)!=null&&!string.IsNullOrWhiteSpace(entry.credit),"Shuffled photo lost identity, credit or local image");}
                    frontend.Apod.BeginMission(20);bool newOrder=frontend.Apod.Collection.entries[0].date!=firstDate;
                    if(!newOrder){frontend.Apod.BeginMission(30);newOrder=frontend.Apod.Collection.entries[0].date!=firstDate;}
                    Require(newOrder&&frontend.Apod.RequestCount==0,"Mission shuffle/reset did not vary photos locally");
                    frontend.Campaign.SetAllocation(new Ares.Data.Allocation {shield=30,life=40,greenhouse=30});frontend.Campaign.StartSol();
                    explorer.reducedMotion=false;
                    SetPlayer(explorer.computerSettings.standPoint);explorer.RecenterCamera();elapsed=0;
                }
                keyboard.MakeCurrent();var keys=new KeyboardState();var terminal=frontend.Terminal;
                var root=frontend.GetComponent<UIDocument>().rootVisualElement;
                if(stage==0&&elapsed>1.2f)
                {
                    Require(terminal.Near,"Console proximity failed");
                    // Establish an unobstructed view of the imported wall computer for the world-space cue.
                    var orbit=(OrbitCamera)typeof(ReferenceExplorer).GetField("rig",Private).GetValue(explorer);
                    orbit.Yaw=orbit.TargetYaw=-Mathf.PI/4;orbit.Pitch=orbit.TargetPitch=.48f;orbit.Distance=orbit.TargetDistance=13;
                    var motion=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);
                    typeof(ReferenceExplorer).GetField("cameraPosition",Private).SetValue(explorer,orbit.Position(motion.X,motion.Z,true));
                    Next();
                }
                else if(stage==1&&elapsed>.2f)
                {
                    Require(terminal.PromptVisible,"Cartoon E is not projected above computer; projected="+Camera.main.WorldToScreenPoint(explorer.computerSettings.screenCenter+Vector3.up*1.64f)+"; blocked="+frontend.BlocksWorld+"; near="+terminal.Near);
                    ScreenCapture.CaptureScreenshot("Logs/ares-computer-prompt.png");
                    CaptureMonitor();
                    player=explorer.PlayerPosition;cameraPosition=Camera.main.transform.position;cameraRotation=Camera.main.transform.rotation;fieldOfView=Camera.main.fieldOfView;
                    stage=20;elapsed=0;
                }
                else if(stage==20 && elapsed>.25f) {keys=new KeyboardState(Key.E);stage=2;elapsed=0;}
                else if(stage==2&&elapsed>.85f)
                {
                    Require(terminal.OwnsCamera&&!terminal.Focused&&frontend.BlocksWorld,"E did not start camera transition");
                    Require(Vector3.Distance(Camera.main.transform.position,cameraPosition)>1,"Camera never left player");
                    ScreenCapture.CaptureScreenshot("Logs/ares-computer-transition.png");Next();
                }
                else if(stage==3&&elapsed>1.1f)
                {
                    Require(terminal.Focused,"Camera did not finish approach");
                    Require(!terminal.PromptVisible&&explorer.PlayerPosition==player,"Prompt/player stayed active in terminal");
                    Require(root.Q<Button>("oxygen-mode-0")!=null,"Oxygen manager missing");
                    ScreenCapture.CaptureScreenshot("Logs/ares-computer-oxygen.png");
                    Press(root,"oxygen-mode-0");Require(frontend.Campaign.OxygenSystem.Mode==OxygenMode.Economy,"Economy button did not reach model");Next();
                }
                else if(stage==4&&elapsed>.5f)
                {
                    var system=frontend.Campaign.OxygenSystem;
                    Require(system.Balance<0&&system.TotalConsumed>0&&system.TotalEnergyKwh>0,"No live consumption/cost");
                    float consumption=system.Consumption;Press(root,"oxygen-laboratory");Require(system.Consumption<consumption,"Laboratory control did not save oxygen");
                    Press(root,"oxygen-mode-2");Require(system.Mode==OxygenMode.Boost,"Boost control failed");
                    Press(root,"computer-energy");Require(root.Query<SliderInt>().ToList().Count==3,"Energy planning lost from computer");
                    Next();
                }
                else if(stage==5&&elapsed>.6f)
                {
                    Require(root.Q("computer-track").worldBound.x-root.Q("computer-viewport").worldBound.x < -100,"Energy page did not slide left");
                    ScreenCapture.CaptureScreenshot("Logs/ares-computer-energy.png");
                    stage=21;elapsed=0;
                }
                else if(stage==21&&elapsed>.25f)
                {
                    Press(root,"computer-oxygen");Require(!frontend.Campaign.OxygenSystem.LaboratoryEnabled,"Tab change reset oxygen settings");
                    stage=22;elapsed=0;
                }
                else if(stage==22&&elapsed>.6f)
                {
                    Require(Mathf.Abs(root.Q("computer-track").worldBound.x-root.Q("computer-viewport").worldBound.x)<1,"Oxygen page did not slide back");
                    root.Q<Foldout>("oxygen-science").value=true;stage=23;elapsed=0;
                }
                else if(stage==23&&elapsed>.25f)
                {
                    root.Q<Foldout>("oxygen-science").GetFirstAncestorOfType<ScrollView>().scrollOffset=new Vector2(0,10000);
                    stage=24;elapsed=0;
                }
                else if(stage==24&&elapsed>.3f)
                {
                    ScreenCapture.CaptureScreenshot("Logs/ares-computer-science.png");
                    keys=new KeyboardState(Key.Escape);stage=6;elapsed=0;
                }
                else if(stage==6&&elapsed>1.9f)
                {
                    Require(!terminal.OwnsCamera&&!frontend.BlocksWorld,"Escape failed to return control");
                    Require(Mathf.Abs(Camera.main.fieldOfView-fieldOfView)<.01f,"Camera FOV not restored");
                    Require(explorer.PlayerPosition==player,"Return moved astronaut");
                    explorer.reducedMotion=true;keys=new KeyboardState(Key.E);Next();
                }
                else if(stage==7&&elapsed>.4f)
                {
                    Require(terminal.Focused,"Reduced motion transition failed");
                    keys=new KeyboardState(Key.Escape);Next();
                }
                else if(stage==8&&elapsed>.4f)
                {
                    Require(!terminal.OwnsCamera,"Reduced motion return failed");explorer.reducedMotion=false;keys=new KeyboardState(Key.E);Next();
                }
                else if(stage==9&&elapsed>.45f) {keys=new KeyboardState(Key.Escape);Next();}
                else if(stage==10&&elapsed>.8f)
                {
                    Require(!terminal.OwnsCamera&&!frontend.BlocksWorld,"Interrupted approach trapped player");
                    SetPlayer(new Vector3(-7,0,-7));Next();
                }
                else if(stage==11&&elapsed>.2f)
                {
                    Require(!terminal.Near&&!terminal.PromptVisible&&!terminal.TryInteract(),"Remote console access or prompt");
                    Call("ShowEnergy");Call("UseStation");Require(!frontend.BlocksWorld&&root.Query<SliderInt>().ToList().Count==0,"Energy opened away from physical computer");
                    Call("Settings");pausedAt=frontend.Campaign.SolElapsedSeconds;Next();
                }
                else if(stage==12&&elapsed>.4f)
                {
                    Require(Mathf.Abs(frontend.Campaign.SolElapsedSeconds-pausedAt)<.001f,"Settings failed to pause the actual day clock");Call("CloseOverlay");
                    frontend.StartMission(false);Require(frontend.Campaign.OxygenSystem.TotalConsumed==0,"New mission reused oxygen telemetry");
                    Finish("PASS: 600-second day, pause/terminal/task clock, unfinished task close; NASA metabolic loads, electrolysis/water, separate CO2, energy limits and mass balance at 30/120 Hz; randomized discovery deck; actual E near computer, oxygen controls, animated energy/oxygen arrows, remote energy blocked, camera return/reduced motion; mission reset. Captures: prompt, transition, oxygen, energy.");
                    EditorApplication.ExitPlaymode();return;
                }
                InputSystem.QueueStateEvent(keyboard,keys);
            }
            catch(Exception error){Finish("FAIL stage "+stage+": "+error);EditorApplication.ExitPlaymode();}
        }
        static void DomainChecks() => LifeSupportChecks.VerifyModel();
        static void SetPlayer(Vector3 p)
        {var player=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);player.X=p.x;player.Z=-p.z;player.Speed=player.VelocityX=player.VelocityZ=0;}
        static void Call(string name) => typeof(GameFrontend).GetMethod(name,Private).Invoke(frontend,null);
        static void CaptureMonitor()
        {
            var surface=GameObject.Find("Computer / live screen surface").GetComponent<MeshRenderer>();
            var target=(RenderTexture)surface.sharedMaterial.GetTexture("_BaseMap");var previous=RenderTexture.active;
            var texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try {RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes("Logs/ares-computer-monitor.png",texture.EncodeToPNG());}
            finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(texture);}
            foreach(var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
                if(doc.name.Contains("offscreen"))File.WriteAllText("Logs/ares-computer-monitor-layout.txt","Document: "+doc.rootVisualElement.layout+"; session: "+doc.rootVisualElement.Q("computer-session").layout+"; labels: "+doc.rootVisualElement.Query<Label>().ToList().Count);
        }
        static void Press(VisualElement root,string name)
        {var button=root.Q<Button>(name);Require(button!=null,"Missing "+name);using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=button;button.SendEvent(evt);}}
        static void Next(){stage++;elapsed=0;}
        static void Near(float a,float b,string message) => Require(Mathf.Abs(a-b)<.01f,message+": "+a+" != "+b);
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static void Finish(string result)
        {SessionState.SetBool(Pending,false);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);keyboard=null;Application.runInBackground=background;File.WriteAllText("Logs/ares-computer.txt",DateTime.UtcNow.ToString("O")+" "+result);}
    }
}
