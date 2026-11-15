using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Ares.Data;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using MouseButton = UnityEngine.InputSystem.LowLevel.MouseButton;

namespace Ares.Editor
{
    [InitializeOnLoad]
    public static class ObservatoryChecks
    {
        const string Request = "Temp/ares-observatory-check.request", Pending = "ARES.ObservatoryChecks";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static Keyboard keyboard;
        static Mouse mouse;
        static int stage, frame;
        static float elapsed, yaw;
        static double started;
        static bool background;
        static string liveStatus;

        static ObservatoryChecks() { EditorApplication.update += Tick; }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void ModelChecks()
        {
            var journal = new DiscoveryJournal();
            Require(!journal.Record(1, 0) && !journal.Record(-1, 4) && !journal.Record(5, 4), "Future/invalid day unlocked");
            for (int i = 0; i < 5; i++) Require(journal.Record(i, i) && journal.Observed(i), "Day not recorded");
            Require(journal.Record(0, 4) && !new DiscoveryJournal().Observed(0), "History/reset");
            var old = new ApodRecord { date="2022-07-13", title="Galaxy", explanation="Real source", media_type="image", url="https://apod.nasa.gov/a.jpg" };
            Require(old.ValidFor(old.date), "Legacy image URL rejected");
            old.url="https://science.nasa.gov/image-article/example/";
            Require(!old.ValidFor(old.date), "HTML accepted as an image");
            old.hdurl="https://assets.science.nasa.gov/a.png";
            Require(old.ValidFor(old.date) && !old.ValidFor("2022-07-14"), "Migrated format/date mismatch");
            old.media_type="video"; Require(!old.ValidFor(old.date), "Video accepted"); old.media_type="image";
            old.hdurl="https://science.nasa.gov/nasa-logo@2x.png"; Require(!old.ValidFor(old.date), "NASA placeholder accepted");
            old.hdurl="http://example.com/a.jpg"; Require(!old.ValidFor(old.date), "Unencrypted image accepted");
            var collection=JsonUtility.FromJson<ObservatoryCollection>(Resources.Load<TextAsset>("ARES/Observatory/collection").text.TrimStart('\uFEFF'));
            Require(collection.entries.Length==5,"Five entries required");
            var dates=new HashSet<string>(); var images=new HashSet<string>();
            foreach(var entry in collection.entries)
            {
                Require(dates.Add(entry.date) && images.Add(entry.imageUrl),"Duplicate discovery");
                var texture=Resources.Load<Texture2D>(entry.resource);
                Require(texture!=null && texture.width>=128 && texture.height>=128,"Offline image missing: "+entry.resource);
                Require(!string.IsNullOrWhiteSpace(entry.summary) && !string.IsNullOrWhiteSpace(entry.credit),"Missing teaching text or credit");
            }
        }

        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(Request))
            {
                string token=File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if(SessionState.GetString(Pending+".refresh","")!=token){SessionState.SetString(Pending+".refresh",token);AssetDatabase.Refresh();return;}
                File.Delete(Request);
                try{ModelChecks();}catch(Exception e){File.WriteAllText("Logs/ares-observatory.txt","FAIL model: "+e);return;}
                SessionState.SetBool(Pending,true);stage=0;frame=-1;elapsed=0;started=EditorApplication.timeSinceStartup;
                background=Application.runInBackground;Application.runInBackground=true;
                File.WriteAllText("Logs/ares-observatory.txt","RUNNING "+DateTime.UtcNow.ToString("O"));EditorApplication.EnterPlaymode();return;
            }
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||frame==Time.frameCount)return;
            frame=Time.frameCount;elapsed+=Mathf.Min(Time.deltaTime,.05f);
            try
            {
                Require(EditorApplication.timeSinceStartup-started<100,"Timeout at stage "+stage);
                if(keyboard==null)
                {
                    ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();explorer=UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    if(ui==null||explorer==null||typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer)==null)return;
                    ui.Apod.OfflineOnly=true;ui.StartMission();ui.Campaign.CloseReport();Close();
                    ui.Campaign.SetAllocation(new Allocation{shield=ui.Campaign.EnergyPool});ui.Campaign.StartSol();
                    var p=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);
                    p.X=ObservatoryInteraction.StandPosition.x;p.Z=-ObservatoryInteraction.StandPosition.z;
                    keyboard=InputSystem.AddDevice<Keyboard>("ARES observatory keyboard");mouse=InputSystem.AddDevice<Mouse>("ARES observatory mouse");elapsed=0;
                }
                keyboard.MakeCurrent();mouse.MakeCurrent();
                var keys=new KeyboardState();var point=Camera.main.WorldToScreenPoint(ObservatoryInteraction.TelescopePosition);
                var pointer=new MouseState{position=new Vector2(point.x,point.y)};
                var root=ui.GetComponent<UIDocument>().rootVisualElement;
                if(stage==0 && elapsed>1){Require(ui.Observatory.CanUse,"Telescope proximity");yaw=explorer.CameraTargetYaw;pointer=pointer.WithButton(MouseButton.Left);Next();}
                else if(stage==1 && elapsed>.4f)
                {
                    Require(root.Q("observatory-view")!=null && ui.BlocksWorld,"Actual telescope click did not open diary");
                    Require(root.Q<Image>("observatory-photo").image!=null && ui.Campaign.Discoveries.Observed(0),"Day 1 photo not recorded");
                    Require(!root.Q<Button>("observatory-day-1").enabledSelf,"Future day not locked");
                    Require(Mathf.Abs(yaw-explorer.CameraTargetYaw)<.001f,"Click dragged camera");
                    ScreenCapture.CaptureScreenshot("Logs/ares-observatory-day1.png");Next();
                }
                else if(stage==2 && elapsed>.3f){keys=new KeyboardState(Key.Escape);Next();}
                else if(stage==3 && elapsed>.3f){Require(!ui.BlocksWorld,"Escape did not close diary");keys=new KeyboardState(Key.E);Next();}
                else if(stage==4 && elapsed>.3f)
                {
                    Require(root.Q("observatory-view")!=null,"E did not reopen telescope");Close();
                    // Fixture advances the day only to exercise journal UI; full campaign has a separate suite.
                    typeof(Campaign).GetProperty("SolIndex").SetValue(ui.Campaign,4);ui.ShowObservatory();Next();
                }
                else if(stage==5 && elapsed>.3f)
                {
                    for(int day=0;day<5;day++)GreenhouseCheckFixture.Press(root,"observatory-day-"+day);
                    for(int day=0;day<5;day++)Require(ui.Campaign.Discoveries.Observed(day),"Diary missing day "+day);
                    Require(root.Q<Label>("observatory-title").text==ui.Apod.Collection.entries[4].title,"Day 5 title mismatch");
                    ScreenCapture.CaptureScreenshot("Logs/ares-observatory-day5.png");Next();
                }
                else if(stage==6 && elapsed>.3f)
                {
                    GreenhouseCheckFixture.Press(root,"observatory-day-0");
                    Require(ui.Apod.RequestCount==0,"Offline navigation made network calls");
                    Close();
                    typeof(Campaign).GetProperty("Phase").SetValue(ui.Campaign,Phase.Victory);
                    typeof(GameFrontend).GetMethod("ShowEnd",Private).Invoke(ui,null);
                    GreenhouseCheckFixture.Press(root,"end-observatory");
                    Require(root.Q("observatory-view")!=null,"Victory diary missing");
                    GreenhouseCheckFixture.Press(root,"observatory-close");
                    Require(root.Q("end-observatory")!=null && root.Q("observatory-view")==null,"Diary did not restore ending");
                    ui.Apod.OfflineOnly=false;ui.Apod.Load(0,true);Next();
                }
                else if(stage==7 && !ui.Apod.Loading(0) && elapsed>.3f)
                {
                    liveStatus=ui.Apod.Status(0);Require(ui.Apod.Image(0)!=null,"Network failure removed offline image");
                    int requests=ui.Apod.RequestCount;ui.Apod.Load(0);Require(ui.Apod.RequestCount==requests,"Reopening repeated request");
                    Close();ui.StartMission();Require(!ui.Campaign.Discoveries.Observed(0),"New mission retained journal progress");
                    Finish("PASS: five unique real images; old/new API formats; video/logo/HTML/date rejection; offline photos; actual telescope click and E/Escape; camera capture; locked future days; all five diary pages; history; victory diary and return; mission reset; no duplicate requests. Live check: "+liveStatus);return;
                }
                InputSystem.QueueStateEvent(keyboard,keys);InputSystem.QueueStateEvent(mouse,pointer);
            }
            catch(Exception e){Finish("FAIL stage "+stage+": "+e);}
        }
        static void Next(){stage++;elapsed=0;}
        static void Close()=>typeof(GameFrontend).GetMethod("CloseOverlay",Private).Invoke(ui,null);
        static void Finish(string result)
        {
            File.WriteAllText("Logs/ares-observatory.txt",DateTime.UtcNow.ToString("O")+" "+result);SessionState.SetBool(Pending,false);
            if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);keyboard=null;mouse=null;
            Application.runInBackground=background;EditorApplication.ExitPlaymode();
        }
    }
}
