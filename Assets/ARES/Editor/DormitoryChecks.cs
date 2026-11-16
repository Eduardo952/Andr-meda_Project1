using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    [InitializeOnLoad] public static class DormitoryChecks
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        const string Scene="Assets/ARES/Scenes/BaseARES.unity";
        static GameFrontend ui;static ReferenceExplorer explorer;static VisualElement root;
        static bool running,background;static int stage,frame;static float elapsed;static double started;
        static string sceneHash,layout;
        static DormitoryChecks(){EditorApplication.update+=Tick;}
        static string Hash(){using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(Scene)));}
        public static void Begin()
        {
            running=true;stage=0;frame=-1;elapsed=0;ui=null;started=EditorApplication.timeSinceStartup;sceneHash=Hash();
            background=Application.runInBackground;Application.runInBackground=true;
            File.WriteAllText("Logs/ares-dormitory.txt","RUNNING");EditorApplication.EnterPlaymode();
        }
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static void Call(string name)=>typeof(GameFrontend).GetMethod(name,Private).Invoke(ui,null);
        static void Place(Vector3 p){var motion=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);motion.X=p.x;motion.Z=-p.z;motion.VelocityX=motion.VelocityZ=motion.Speed=0;}
        static void Wake(){for(int i=0;i<100;i++)explorer.Rest.Tick(.05f,false);}
        static void Press(string name)
        {
            ((GameplayCursor)typeof(ReferenceExplorer).GetField("cursor",Private).GetValue(explorer)).Release();
            var button=root.Q<Button>(name);Require(button!=null,"Missing button "+name);
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}
        }
        static TutorialFlow Flow()
        {
            var director=typeof(GameFrontend).GetProperty("Tutorial",Private).GetValue(ui);
            return (TutorialFlow)director.GetType().GetProperty("Flow").GetValue(director);
        }
        static void FadeChecks()
        {
            foreach(bool reduced in new[]{false,true}) {
                var rest=new BedSequence();rest.Start();Require(rest.FadeOpacity==1,"Wake does not start fully black");
                float last=1;bool partial=false;
                for(int i=0;i<100;i++) {rest.Tick(.05f,reduced);Require(rest.FadeOpacity<=last,"Fade brightens/darkens out of order");partial|=rest.FadeOpacity>0 && rest.FadeOpacity<1;last=rest.FadeOpacity;}
                Require(partial && !rest.Busy && rest.FadeOpacity==0,"Fade does not finish smoothly");
                rest.Rest(1);for(int i=0;i<40;i++)rest.Tick(.05f,reduced);rest.Wake();Require(rest.FadeOpacity==1,"Later wake skips blackout");
                rest.Cancel();Require(rest.FadeOpacity==0,"Cancel leaves black screen");
            }
        }
        static void RoomChecks()
        {
            var navigation=new WorldNavigation(explorer.definition.layout);var dorm=explorer.Dorm;
            for(int i=0;i<4;i++) {
                Require(!navigation.Walkable(dorm.Beds[i]) && navigation.Walkable(dorm.Stand(i)),"Bed collision or exit is wrong");
                var center=explorer.definition.layout.Room("lifeSupport").Center;
                for(int j=0;j<=20;j++)Require(navigation.Walkable(Vector3.Lerp(dorm.Stand(i),center,j/20f)),"Bed blocks central aisle");
            }
            var room=explorer.definition.layout.Room("lifeSupport");Require(navigation.Walkable(room.Station),"Life support terminal access blocked");
            var copies=(List<Mesh>)typeof(DormitoryArt).GetField("meshes",Private).GetValue(explorer.GetComponent<DormitoryArt>());
            var envelope=new Bounds(room.Station+new Vector3(1.7f,1.5f,-.1f),new Vector3(2.2f,3,1.3f));int triangles=0,parts=0;
            foreach(var mesh in copies)if(mesh.name.EndsWith("/ dormitory terminal only")) {
                parts++;triangles+=mesh.triangles.Length/3;foreach(var p in mesh.vertices)Require(envelope.Contains(p),"Tank or pipe remains in dormitory interior");
            }
            Require(parts==2 && triangles==120,"Original terminal geometry not preserved");
            foreach(string group in new[]{"lifeSupport_floor","lifeSupport_walls","lifeSupport_roof"})Require(explorer.transform.Find(group)!=null,"Base structure was removed: "+group);
            Require(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/ARES/Generated/Meshes/lifeSupport_interior_770.asset").triangles.Length==1548*3,"Imported source asset was modified");
            Require(explorer.transform.Find("Life support vent steam")==null,"Removed pipes still emit steam");
        }
        static void ArrivalChecks()
        {
            var hub=explorer.definition.layout.Room("control");
            foreach(var port in hub.ports) {
                ui.StartMission();Wake();Call("UpdateTutorialArrival");Require(!ui.TutorialActive,"Tutorial fires in the dormitory");
                var entry=Vector3.Lerp(hub.Center,new Vector3(port.x,0,-port.z),.9f);Require(Vector3.Distance(entry,hub.Center)>6,"Entry does not test the former dead zone");
                Place(entry);Call("UpdateTutorialArrival");Require(ui.TutorialActive,"Central entrance failed: "+entry);
                Press("tutorial-decline");Call("UpdateTutorialArrival");Require(!ui.TutorialActive,"Tutorial offered twice");
            }
            ui.StartMission();Wake();Call("Settings");Place(new Vector3(0,0,-12));Call("UpdateTutorialArrival");
            Require(!ui.TutorialActive,"Tutorial opened behind settings");
            Place(explorer.Dorm.Stand(0));Call("CloseOverlay");Call("UpdateTutorialArrival");Require(ui.TutorialActive,"Deferred arrival was forgotten");
            Flow().Choose(true);var progress=Flow();Call("BuildHud");
            Require(ReferenceEquals(progress,Flow()) && ui.TutorialStage==TutorialStage.EnergyIntro,"HUD rebuild lost tutorial progress");
            ui.StartMission(false);ui.Campaign.CloseReport();Call("CloseOverlay");Place(hub.Center);Call("UpdateTutorialArrival");Require(!ui.TutorialActive,"Explicit no-tutorial entry changed");
            ui.StartMission();Require(!ui.TutorialActive && explorer.Rest.FadeOpacity==1,"New mission did not reset onboarding");
        }
        static void Tick()
        {
            if(!running || !EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            try {
                Require(EditorApplication.timeSinceStartup-started<90,"Dormitory checks timed out");
                if(frame==Time.frameCount)return;frame=Time.frameCount;elapsed+=Mathf.Min(Time.deltaTime,.05f);
                if(ui==null) {
                    explorer=UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();if(explorer?.Dorm==null)return;
                    ui=explorer.GetComponent<GameFrontend>();root=ui.GetComponent<UIDocument>().rootVisualElement;ui.Apod.OfflineOnly=true;explorer.reducedMotion=false;
                    layout=JsonUtility.ToJson(explorer.definition.layout);FadeChecks();RoomChecks();ui.StartMission();elapsed=0;
                    Require(root.Q("wake-fade")?.style.opacity.value==1,"Black overlay is not installed synchronously");
                    ScreenCapture.CaptureScreenshot("Logs/ares-dormitory-black.png");return;
                }
                if(stage==0 && elapsed>1.1f) {
                    var fade=root.Q("wake-fade");Require(fade!=null && fade.resolvedStyle.opacity>0 && fade.resolvedStyle.opacity<1,"No visible fade midpoint");
                    Require(fade.worldBound.width>=root.worldBound.width-1 && fade.worldBound.height>=root.worldBound.height-1,"Blackout does not cover the screen");
                    ScreenCapture.CaptureScreenshot("Logs/ares-dormitory-fade.png");stage++;
                } else if(stage==1 && elapsed>4.2f) {
                    Require(root.Q("wake-fade")==null && !explorer.BedBusy && !ui.TutorialActive,"Wake did not restore normal exploration");
                    ScreenCapture.CaptureScreenshot("Logs/ares-dormitory-room.png");stage++;elapsed=0;
                } else if(stage==2 && elapsed>.3f) {
                    ArrivalChecks();Require(JsonUtility.ToJson(explorer.definition.layout)==layout && Hash()==sceneHash,"Base layout/scene changed");
                    Finish("PASS: full-screen initial black, gradual fade and removal, reduced-motion/repeated wake; 4 beds with clear aisles/exits; only 120 original terminal triangles remain in interior; no source mesh/scene/base layout changes; all 5 central entries beyond old 6m radius trigger once; menu deferral, tutorial progress across HUD rebuild, opt-out and mission reset. Captures: black/fade/room.");
                }
            }catch(Exception e){Finish("FAIL: "+e);}
        }
        static void Finish(string result){File.WriteAllText("Logs/ares-dormitory.txt",result);running=false;Application.runInBackground=background;EditorApplication.ExitPlaymode();}
    }
}
