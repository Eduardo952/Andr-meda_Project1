using System;
using System.IO;
using System.Reflection;
using Ares.Data;
using Ares.Domain;
using Ares.Domain.Hazards;
using Ares.Infrastructure;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Profiling;

namespace Ares.Editor
{
    [InitializeOnLoad]
    public static class HazardPlayChecks
    {
        const string Request="Temp/ares-hazard-check.request",Log="Docs/Hazards/playmode-results.txt",Captures="Docs/Hazards/Captures/";
        const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static bool running,background;static double began,next;static int stage,caseIndex,frameCount;static double frameTotal,gcTotal;static string language;
        static GameFrontend ui;static ReferenceExplorer explorer;static ProfilerRecorder gc;
        static UnityEngine.InputSystem.Keyboard keyboard;static UnityEngine.InputSystem.Mouse mouse;
        static HazardPlayChecks(){EditorApplication.update+=Tick;}
        [MenuItem("ARES/Checks/Hazard integration and captures")]
        public static void Begin(){Directory.CreateDirectory("Temp");File.WriteAllText(Request,"run");}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!running && File.Exists(Request) && !EditorApplication.isPlayingOrWillChangePlaymode){File.Delete(Request);HazardAssets.Ensure();Directory.CreateDirectory(Captures);File.WriteAllText(Log,"RUNNING\nSynthetic presentation fixtures; no synthetic observations are saved in the NASA catalog.\n");running=true;stage=caseIndex=0;ui=null;began=next=EditorApplication.timeSinceStartup;background=Application.runInBackground;Application.runInBackground=true;language=Ares.UI.Localization.CurrentLanguage;EditorApplication.EnterPlaymode();return;}
            if(!running||!EditorApplication.isPlaying)return;
            try {
                Require(EditorApplication.timeSinceStartup-began<240,"Play check timeout");
                if(ui==null){ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();if(ui==null)return;explorer=ui.GetComponent<ReferenceExplorer>();if(explorer.Rover==null){ui=null;return;}ui.Apod.OfflineOnly=true;keyboard=UnityEngine.InputSystem.Keyboard.current;mouse=UnityEngine.InputSystem.Mouse.current;if(keyboard!=null)UnityEngine.InputSystem.InputSystem.DisableDevice(keyboard);if(mouse!=null)UnityEngine.InputSystem.InputSystem.DisableDevice(mouse);PlayerPrefs.SetString("ARES.Language","pt-BR");typeof(GameFrontend).GetField("locale",Private).SetValue(ui,new Localization("pt-BR"));gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);next=EditorApplication.timeSinceStartup+1;return;}
                if(stage==1||stage==3){frameCount++;frameTotal+=Time.unscaledDeltaTime*1000;gcTotal+=gc.Valid?gc.LastValue:0;}
                if(EditorApplication.timeSinceStartup<next)return;
                next=EditorApplication.timeSinceStartup+1;
                if(stage==0){NewCampaign(null,1);frameCount=0;frameTotal=gcTotal=0;stage=1;next+=2;return;}
                if(stage==1){Metrics("calm");Capture("hazards-calm");stage=2;return;}
                if(stage==2){if(caseIndex>=21){stage=4;return;}NewCampaign((HazardKind)(caseIndex/3),caseIndex%3+1);frameCount=0;frameTotal=gcTotal=0;stage=3;next+=2;return;}
                if(stage==3){var kind=(HazardKind)(caseIndex/3);int tier=caseIndex%3+1;Capture(kind+"-"+tier);Require(explorer.GetComponent<WeatherVfxController>().ParticleCount<=1152,"Unbounded weather pool");Require(explorer.GetComponent<MartianWind>().LiveParticles<=960,"Unbounded dust pool");Metrics(kind+"-"+tier);caseIndex++;stage=2;return;}
                if(stage==4){NewCampaign(HazardKind.Radiation,3);typeof(Campaign).GetProperty("Dose").SetValue(ui.Campaign,100);ui.Campaign.SetExposureContext(true,false,false,false);ui.Campaign.Tick(25,false);Require(ui.Campaign.Condition.Stage==CrewCondition.Warning,"Collapsed before visible warning");Capture("warning-pt-BR");stage=5;return;}
                if(stage==5){ui.Campaign.SetExposureContext(true,false,true,false);ui.Campaign.Tick(15,false);Require(ui.Campaign.Condition.Stage==CrewCondition.Incapacitated,"No collapse after visible grace");stage=6;return;}
                if(stage==6){Capture("incapacitated");ui.Campaign.Tick(12,false);Require(ui.Campaign.RescueVersion>0 && ui.Campaign.Dose>=60 && ui.Campaign.Dose<75 && ui.Campaign.Condition.TreatedThisSol,"Recovery values");stage=7;return;}
                if(stage==7){Require(Vector3.Distance(explorer.PlayerPosition,explorer.Dorm.Stand(0))<.1f,"Rescue did not return to dormitory");Capture("rescue-bed");PlayerPrefs.SetString("ARES.Language","en");typeof(GameFrontend).GetField("locale",Private).SetValue(ui,new Localization("en"));Call("BuildHud");stage=8;return;}
                if(stage==8){Capture("hazards-en");var root=ui.GetComponent<UIDocument>().rootVisualElement;Require(root.Q("hazard-status")!=null,"Missing warning HUD");Require(Resources.Load<UnityEngine.Audio.AudioMixer>("ARES/Hazards/HazardMixer")!=null,"Missing mixer asset");Require(Resources.Load<TextAsset>("ARES/Hazards/snapshots")!=null,"Missing offline snapshot bundle");ui.Campaign.Tick(650,false);HazardChecks.CompleteTasks(ui.Campaign);ui.Campaign.ReachShelter();ui.Campaign.SetAllocation(MissionDirector.Recommend(ui.Campaign.EnergyPool,ui.Campaign.Current.demand,ui.Campaign.Solar.ReserveKwh));Require(ui.Campaign.Seal("control",ui.Campaign.Allocation),"Explicit Sol seal failed");Require(ui.Campaign.SendDailyReport(),"Native report cannot be sent");File.AppendAllText(Log,"PASS: 21 tier captures, bounded pools, visible-warning grace, collapse, medical recovery, dormitory relocation, locales, mixer, snapshots, explicit sealing and communication report.\n");Finish();}
            }catch(Exception error){File.AppendAllText(Log,"FAIL: "+error+"\n");Debug.LogException(error);Finish();}
        }
        static void NewCampaign(HazardKind? kind,int tier)
        {
            ui.StartMission(false);var c=ui.Campaign;c.EnableNativeHazards();
            var data=HazardChecks.Fixture("presentation-only",1,kind==HazardKind.Flare||kind==HazardKind.Radiation?(tier==1?"C":tier==2?"M":"X"):null,2,
                kind==HazardKind.Cme?(double?)(tier==1?400:tier==2?800:1500):null,kind==HazardKind.Cme?"glancing_blow":"none",kind==HazardKind.Cold?(double?)(tier==1?-70:tier==2?-85:-100):null,kind==HazardKind.Dust?(tier==1?"low":tier==2?"medium":"high"):"low");
            c.ApplyNativeSol(data,"synthetic-test","Curated");var integration=ui.GetComponent<MissionIntegration>();typeof(MissionIntegration).GetField("boundCampaign",Private).SetValue(integration,c);typeof(MissionIntegration).GetField("boundSol",Private).SetValue(integration,1);
            foreach(var e in c.NativePlan.events){e.startsAt=60;e.duration=180;}c.CloseReport();Call("CloseOverlay");c.SetAllocation(MissionDirector.Recommend(c.EnergyPool,c.Current.demand,c.Solar.ReserveKwh));c.StartSol();c.SetExposureContext(false,false,false,false);c.Tick(65,false);
            if(kind==HazardKind.LowOxygen)typeof(OxygenPlant).GetMethod("SetReservePercent",Private).Invoke(c.OxygenSystem,new object[]{tier==1?45f:tier==2?35f:20f});
            if(kind==HazardKind.Power){c.Solar.DrawEnergy(100);c.Grid.Shock(new HazardEvent {kind=HazardKind.Cme,tier=(HazardTier)tier,intensity=1,duration=180},default,new Allocation {shield=75});}
            var motion=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);var position=explorer.definition.layout.Room("solar").Station;motion.X=position.x;motion.Z=-position.z;explorer.Rest.Cancel();explorer.RecenterCamera();
        }
        static void Metrics(string name){File.AppendAllText(Log,name+": editor frame average="+(frameTotal/Math.Max(1,frameCount)).ToString("0.00")+" ms; whole-frame managed GC average="+(gcTotal/Math.Max(1,frameCount)).ToString("0")+" bytes; particles="+explorer.GetComponent<WeatherVfxController>().ParticleCount+" + dust "+explorer.GetComponent<MartianWind>().LiveParticles+". GPU time not measured.\n");}
        static void Capture(string name)=>ScreenCapture.CaptureScreenshot(Captures+name+".png");
        static void Call(string method)=>typeof(GameFrontend).GetMethod(method,Private).Invoke(ui,null);
        static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        static void Finish(){running=false;if(gc.Valid)gc.Dispose();if(keyboard!=null)UnityEngine.InputSystem.InputSystem.EnableDevice(keyboard);if(mouse!=null)UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);PlayerPrefs.SetString("ARES.Language",language);Application.runInBackground=background;EditorApplication.ExitPlaymode();}
    }
}
