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
        static float elapsed,fieldOfView;
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
                    frontend.StartMission();frontend.Campaign.CloseReport();Call("CloseOverlay");
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
                    ScreenCapture.CaptureScreenshot("Logs/ares-computer-energy.png");Next();
                }
                else if(stage==5&&elapsed>.3f)
                {
                    Press(root,"computer-oxygen");Require(!frontend.Campaign.OxygenSystem.LaboratoryEnabled,"Tab change reset oxygen settings");
                    keys=new KeyboardState(Key.Escape);Next();
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
                    frontend.StartMission();Require(frontend.Campaign.OxygenSystem.TotalConsumed==0,"New mission reused oxygen telemetry");
                    Finish("PASS: finite/nonnegative resource accounting, capacity overflow, depletion, partial/no power, modes, laboratory savings, 30/120 Hz consistency; actual E near spawn computer; cartoon prompt; camera approach/return/FOV, input ownership, Escape during approach, reduced motion; live oxygen/power use; real mode/lab buttons; energy planning tab; distance gating; mission reset. Captures: prompt, transition, oxygen, energy.");
                    EditorApplication.ExitPlaymode();return;
                }
                InputSystem.QueueStateEvent(keyboard,keys);
            }
            catch(Exception error){Finish("FAIL stage "+stage+": "+error);EditorApplication.ExitPlaymode();}
        }
        static void DomainChecks()
        {
            var settings=new OxygenSettings();var p=new OxygenPlant(settings,50);p.Tick(30,2.4f);
            Near(p.ReserveLitres,501,"Balanced reserve");Near(p.TotalConsumed,8,"Consumption");Near(p.TotalProduced,9,"Production");
            p.SetMode(OxygenMode.Economy);p.Tick(30,1.2f);Near(p.ReserveLitres,499,"Economy draws reserve");
            p.SetLaboratory(false);Near(p.Consumption,6.9f,"Lab demand");Require(!p.SetMode((OxygenMode)99),"Invalid mode");
            var full=new OxygenPlant(settings,100);full.Tick(30,2.4f);Near(full.TotalVented,1,"Overflow");Near(full.ReserveLitres,1000,"Capacity");
            var empty=new OxygenPlant(settings,0);empty.Tick(30,0);Near(empty.ReserveLitres,0,"Underflow");Near(empty.TotalUnserved,8,"Unserved demand");
            var partial=new OxygenPlant(settings,50);partial.Tick(30,1.2f);Near(partial.TotalProduced,4.5f,"Partial power");
            float reserve=partial.ReserveLitres;partial.Tick(float.NaN,1);partial.Tick(-1,1);partial.Tick(1,-1);Near(partial.ReserveLitres,reserve,"Invalid time/energy");
            foreach(int fps in new[]{30,120})
            {
                var run=new OxygenPlant(settings,50);for(int i=0;i<fps*30;i++)run.Tick(1f/fps,2.4f/30/fps);
                Require(Mathf.Abs(run.ReserveLitres-501)<.05f,"Frame-rate accounting");
                Require(Mathf.Abs(500+run.TotalProduced-run.TotalConsumed-run.TotalVented-run.ReserveLitres)<.05f,"Mass balance");
            }
            var battery=new SolarPlant(new SolarMaintenanceSettings {initialReserveKwh=1});Near(battery.DrawEnergy(2),1,"Battery delivery");Near(battery.ReserveKwh,0,"Battery underflow");
        }
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
