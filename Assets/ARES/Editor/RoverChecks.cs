using System;
using System.IO;
using System.Reflection;
using Ares.UI;
using Ares.World;
using Ares.World.Rover;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Project-local request, deterministic driving checks and real input integration in Play.</summary>
    [InitializeOnLoad]
    public static class RoverChecks
    {
        const string Request="Temp/ares-rover-check.request", Pending="ARES.RoverChecks";
        static Keyboard keyboard;
        static Mouse mouse;
        static ReferenceExplorer explorer;
        static GameFrontend frontend;
        static int stage,lastFrame,puffs;
        static float elapsed,earlyDust,heading,cameraYaw;
        static Vector3 astronaut,roverPosition;
        static bool background;
        static double start;
        static RoverChecks()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=> {
                if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Pending,false))return;
                Finish("FAIL: Play interrupted before rover checks completed.");
            };
        }
        [MenuItem("ARES/Checks/Rover driving and presentation")]
        public static void RequestCheck() { Directory.CreateDirectory("Temp"); File.WriteAllText(Request,"check"); }
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request))
            {
                string token=File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if(SessionState.GetString(Pending+".refresh","")!=token) { SessionState.SetString(Pending+".refresh",token); AssetDatabase.Refresh(); return; }
                File.Delete(Request); Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/ares-rover.txt","RUNNING");
                SessionState.SetBool(Pending,true); stage=0; elapsed=0; lastFrame=-1;
                start=EditorApplication.timeSinceStartup; background=Application.runInBackground; Application.runInBackground=true;
                EditorApplication.isPaused=false; EditorApplication.EnterPlaymode(); return;
            }
            if(!SessionState.GetBool(Pending,false) || !EditorApplication.isPlaying)return;
            try
            {
                Require(EditorApplication.timeSinceStartup-start<100,"Timeout");
                if(lastFrame==Time.frameCount)return; lastFrame=Time.frameCount; elapsed+=Mathf.Min(Time.deltaTime,.05f);
                if(keyboard==null)
                {
                    explorer=UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    frontend=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();
                    if(explorer==null || explorer.Rover==null || explorer.Rover.Motion==null)return;
                    CheckMotion();
                    Require(new RoverNavigation(explorer.definition.layout,explorer.roverSettings).CanOccupy(explorer.roverSettings.spawn),"Spawn overlaps base");
                    Require(explorer.Rover.Model.Wheels.Length==6,"Six wheel model missing");
                    foreach(var renderer in explorer.Rover.GetComponentsInChildren<MeshRenderer>())
                        Require(renderer.sharedMaterial!=null && renderer.sharedMaterial.shader.isSupported,"Unsupported rover material");
                    keyboard=InputSystem.AddDevice<Keyboard>("ARES rover check"); mouse=InputSystem.AddDevice<Mouse>("ARES rover mouse");
                    frontend.SetInspectionTestMode(); astronaut=explorer.PlayerPosition;
                    elapsed=0;
                }
                keyboard.MakeCurrent(); mouse.MakeCurrent();
                var keys=new KeyboardState(); var pointer=new MouseState {position=new Vector2(Screen.width*.55f,Screen.height*.55f)};
                var rover=explorer.Rover;
                if(stage==0 && elapsed>.2f) { keys=new KeyboardState(Key.Y); Next(); }
                else if(stage==1 && elapsed>1)
                {
                    Require(explorer.IsDrivingRover,"Y failed to enter rover");
                    Require(Vector3.Distance(Camera.main.transform.position,rover.transform.position)<12,"Camera did not move close to rover");
                    Capture("Logs/ares-rover-rear.png",false);
                    Capture("Logs/ares-rover-model.png",true);
                    Next();
                }
                else if(stage==2)
                {
                    keys=new KeyboardState(Key.W);
                    if(elapsed>1) { Require(rover.Motion.Speed>1 && rover.Motion.Speed<4,$"W did not gradually accelerate: speed={rover.Motion.Speed}, position={rover.Motion.Position}, blocked={rover.Motion.Obstructed}"); earlyDust=rover.Dust.Intensity; Next(); }
                }
                else if(stage==3)
                {
                    keys=new KeyboardState(Key.W);
                    if(elapsed>1.5f)
                    {
                        Require(rover.Motion.Speed>4,"W acceleration stopped");
                        Require(rover.Dust.Intensity>earlyDust && rover.Dust.Emitted>0,"Dust failed to build with travel");
                        Require(explorer.PlayerPosition==astronaut,"Rover input moved astronaut");
                        heading=rover.Motion.Yaw; cameraYaw=rover.CameraRig.TargetYaw;
                        Capture("Logs/ares-rover-dust.png",false); Next();
                    }
                }
                else if(stage==4)
                {
                    keys=new KeyboardState(Key.W,Key.D);
                    if(elapsed>.6f)
                    {
                        float turned=Mathf.DeltaAngle(heading,rover.Motion.Yaw);
                        Require(turned>5,"D did not steer right");
                        Require(Mathf.Abs(Mathf.DeltaAngle(cameraYaw*Mathf.Rad2Deg,rover.CameraRig.TargetYaw*Mathf.Rad2Deg)+turned)<2,"Camera did not follow rover turn");
                        cameraYaw=rover.CameraRig.TargetYaw; Next();
                    }
                }
                else if(stage==5)
                {
                    pointer.buttons=2; pointer.delta=new Vector2(5,0);
                    if(elapsed>.5f) { Require(Mathf.Abs(rover.CameraRig.TargetYaw-cameraYaw)>.15f,"Mouse orbit ignored"); Next(); }
                }
                else if(stage==6)
                {
                    keys=new KeyboardState(Key.S);
                    if(elapsed>2)
                    {
                        Require(rover.Motion.Speed<-.2f,"S failed to brake into reverse");
                        typeof(GameFrontend).GetMethod("ShowTitle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(frontend,null);
                        roverPosition=rover.Motion.Position; puffs=rover.Dust.Emitted; Next();
                    }
                }
                else if(stage==7)
                {
                    keys=new KeyboardState(Key.W,Key.Y);
                    if(elapsed>.3f)
                    {
                        Require(explorer.IsDrivingRover && rover.Motion.Position==roverPosition && rover.Dust.Emitted==puffs,"Menu failed to block driving, Y or dust");
                        frontend.SetInspectionTestMode(); keys=new KeyboardState(); Next();
                    }
                }
                else if(stage==8 && elapsed>.15f) {keys=new KeyboardState(Key.Y); Next();}
                else if(stage==9 && elapsed>.5f)
                {
                    Require(!explorer.IsDrivingRover && rover.Motion.Speed==0,"Y failed to park rover");
                    Require(explorer.PlayerPosition==astronaut,"Astronaut position changed during remote control");
                    frontend.StartMission(false); frontend.Campaign.CloseReport();
                    typeof(GameFrontend).GetMethod("CloseOverlay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(frontend,null);
                    Next();
                }
                else if(stage==10 && elapsed>.2f) { keys=new KeyboardState(Key.Y); Next(); }
                else if(stage==11 && elapsed>1.2f)
                {
                    Require(explorer.IsDrivingRover,"Y failed with mission HUD");
                    var panel=frontend.GetComponent<UIDocument>().rootVisualElement.Q("rover-controls");
                    Require(panel!=null && panel.Q<Label>().text.Contains("km/h"),"Rover speed/control HUD missing");
                    ScreenCapture.CaptureScreenshot("Logs/ares-rover-hud.png"); Next();
                }
                else if(stage==12 && elapsed>.5f)
                {
                    Finish("PASS: acceleration to cap, reverse cap, braking/coasting, frame-rate consistency, swept collision, building/corridor/park boundaries; six wheels and shaders; real Y/W/D/S input; close camera, heading follow and manual mouse orbit; progressive contact dust; astronaut stays parked; menu blocks movement/dust/Y; Y returns control; mission HUD controls/speed. Captures: model, rear, dust, HUD.");
                    EditorApplication.ExitPlaymode(); return;
                }
                InputSystem.QueueStateEvent(keyboard,keys); InputSystem.QueueStateEvent(mouse,pointer);
            }
            catch(Exception error) { Finish("FAIL stage "+stage+": "+error); EditorApplication.ExitPlaymode(); }
        }
        static void Next() {stage++;elapsed=0;}
        static void Finish(string result)
        {
            SessionState.SetBool(Pending,false);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard); if(mouse!=null)InputSystem.RemoveDevice(mouse);
            keyboard=null;mouse=null; Application.runInBackground=background;
            File.WriteAllText("Logs/ares-rover.txt",DateTime.UtcNow.ToString("O")+" "+result);
        }
        static void CheckMotion()
        {
            var settings=new RoverSettings {spawn=Vector3.zero,spawnYaw=0};
            RoverMotion Drive(float dt) {var m=new RoverMotion(settings,p=>true);for(int i=0;i<Mathf.RoundToInt(8/dt);i++)m.Step(1,0,dt);return m;}
            var slow=Drive(1/30f);var fast=Drive(1/120f);
            Require(Mathf.Abs(slow.Speed-settings.maxSpeed)<.01f,"Maximum speed cap");
            Require(Vector3.Distance(slow.Position,fast.Position)<.2f,"Frame-rate divergence");
            float initial=slow.Speed;slow.Step(0,0,.1f);Require(slow.Speed<initial && slow.Speed>0,"Coasting");
            for(int i=0;i<300;i++)slow.Step(-1,0,1/60f);
            Require(Mathf.Abs(slow.Speed+settings.reverseSpeed)<.01f,"Reverse cap");
            var collision=new RoverMotion(settings,p=>p.z<2);
            for(int i=0;i<100;i++)collision.Step(1,0,.1f);
            Require(collision.Position.z<2 && collision.Speed==0,"Tunnelling or wheels spinning into obstruction");
            var nav=new RoverNavigation(explorer.definition.layout,explorer.roverSettings);
            foreach(var room in explorer.definition.layout.rooms)Require(!nav.CanOccupy(room.Center),"Building/solar exclusion");
            foreach(var corridor in explorer.definition.layout.corridors) Require(!nav.CanOccupy(new Vector3((corridor.a.x+corridor.b.x)/2,0,-(corridor.a.z+corridor.b.z)/2)),"Covered corridor exclusion");
            Require(!nav.CanOccupy(new Vector3(150,0,0)),"World boundary");
        }
        static void Capture(string path,bool hero)
        {
            var camera=Camera.main;Vector3 oldPosition=camera.transform.position;Quaternion oldRotation=camera.transform.rotation;
            var target=camera.targetTexture;var active=RenderTexture.active;
            var rt=new RenderTexture(1600,1000,24);var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try
            {
                if(hero) {camera.transform.position=explorer.Rover.transform.TransformPoint(new Vector3(5,3.4f,6));camera.transform.LookAt(explorer.Rover.transform.position+Vector3.up*1.25f);}
                camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally {camera.targetTexture=target;RenderTexture.active=active;camera.transform.SetPositionAndRotation(oldPosition,oldRotation);UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
        static void Require(bool value,string message) {if(!value)throw new InvalidOperationException(message);}
    }
}
