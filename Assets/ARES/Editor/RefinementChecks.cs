using System;
using System.IO;
using System.Reflection;
using Ares.World;
using Ares.UI;
using Ares.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    [InitializeOnLoad] public static class RefinementChecks
    {
        static int stage; static float elapsed; static int frame;
        static bool running,background; static GameFrontend ui; static ReferenceExplorer explorer;
        static double started;
        static RefinementChecks(){EditorApplication.update+=Tick;}
        public static void Begin()
        {
            running=true;stage=0;elapsed=0;frame=-1;ui=null;started=EditorApplication.timeSinceStartup;
            background=Application.runInBackground;Application.runInBackground=true;
            File.WriteAllText("Logs/ares-refinement.txt","RUNNING");EditorApplication.EnterPlaymode();
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Tick()
        {
            if(!running || !EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            try {
                Require(EditorApplication.timeSinceStartup-started<90,"Timeout");
                if(frame==Time.frameCount)return;frame=Time.frameCount;elapsed+=Mathf.Min(Time.unscaledDeltaTime,.05f);
                if(ui==null) {
                    explorer=UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();if(explorer?.Rover==null)return;
                    ui=explorer.GetComponent<GameFrontend>();ui.Apod.OfflineOnly=true;ui.StartMission();elapsed=0;
                    Require(explorer.Rest.Stage==RestStage.Sleeping,"Mission must start lying down");
                    Require(!ui.TutorialActive,"Tutorial appeared at spawn");
                    var navigation=new WorldNavigation(explorer.definition.layout);
                    foreach(var bed in explorer.Dorm.Beds)Require(!navigation.Walkable(bed),"Bed allows walking through furniture");
                    for(int i=0;i<4;i++)Require(navigation.Walkable(explorer.Dorm.Stand(i)),"Bed exit is blocked");
                    string payload=MissionIntegration.Payload(ui.Campaign,"pt-BR","test-contract");
                    Require(payload.Contains("\"lang\":\"pt-BR\"") && payload.Contains("\"phase\":\"hint\""),"Invalid webhook JSON quoting");
                    Require(!payload.Contains("deviceId") && !payload.Contains("x-api-key"),"Payload contains transport identity/secret");
                }
                if(stage==0 && elapsed>.5f) {
                    Require(explorer.BedBusy && !ui.TutorialActive,"Wake order broken");
                    ScreenCapture.CaptureScreenshot("Logs/ares-refinement-wake.png");stage++;elapsed=0;
                } else if(stage==1 && elapsed>4) {
                    Require(!explorer.BedBusy && !ui.BlocksWorld && !ui.TutorialActive,"Control not restored before tutorial");
                    var motion=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(explorer);
                    motion.X=0;motion.Z=3;stage++;elapsed=0;
                } else if(stage==2 && elapsed>.3f) {
                    Require(ui.TutorialActive && ui.TutorialStage==Ares.Domain.TutorialStage.Offer,"Center did not trigger tutorial");
                    ScreenCapture.CaptureScreenshot("Logs/ares-refinement-tutorial.png");
                    var rest=new BedSequence();rest.Rest(1);for(int i=0;i<40;i++)rest.Tick(.05f,false);
                    Require(rest.Stage==RestStage.Resting && rest.Lie==1,"Lie down incomplete");rest.Wake();for(int i=0;i<70;i++)rest.Tick(.05f,false);
                    Require(!rest.Busy && rest.Lie==0,"Wake did not release input");
                    Require(explorer.Rover.Model.Wheels.Length==6,"Rover wheel binding lost");
                    stage++;elapsed=0;
                } else if(stage==3 && elapsed>.4f) Finish("PASS: bed collision and exits; initial lying pose; wake and free control; center-only tutorial; lie/wake cycle; webhook payload identity; six-wheel rover. Screenshots captured.");
            }catch(Exception e){Finish("FAIL: "+e);}
        }
        static void Finish(string result){File.WriteAllText("Logs/ares-refinement.txt",result);running=false;Application.runInBackground=background;EditorApplication.ExitPlaymode();}
    }
}
