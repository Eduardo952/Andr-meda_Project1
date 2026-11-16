using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Ares.Infrastructure;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Exercises menu -> new mission -> telescope, inspecting the texture actually bound to the lens.</summary>
    [InitializeOnLoad]
    public static class TelescopeRestartChecks
    {
        const string Request="Temp/ares-telescope-restart.request",Log="Logs/telescope-restart.txt";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static bool running,enteredPlay,background,hadHistory,offline,keyboardEnabled,mouseEnabled;
        static string savedHistory;static object originalMemory;static double began,next,loadStarted;
        static int stage,index,onlineLoaded;static GameFrontend ui;static ReferenceExplorer explorer;
        static Keyboard keyboard;static Mouse mouse;static string lastMission,previousFirst,onlineFirst;static string[] previousDeck;
        static readonly HashSet<string> firstFive=new HashSet<string>();
        static TelescopeRestartChecks(){EditorApplication.update+=Tick;}
        [MenuItem("ARES/Checks/Telescope menu restarts and image loading")]
        public static void Begin(){Directory.CreateDirectory("Temp");File.WriteAllText(Request,"run");}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!running&&File.Exists(Request)){
                // A script refresh during an existing game invalidates nonserialized UI state.
                // Always run this regression from a cold Play session, like launching the player.
                if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(Request);Directory.CreateDirectory("Logs");File.WriteAllText(Log,"RUNNING: real menu/new-mission entry points; inspect rendered lens texture.\n");
                enteredPlay=!EditorApplication.isPlaying;hadHistory=PlayerPrefs.HasKey(ApodService.HistoryKey);savedHistory=PlayerPrefs.GetString(ApodService.HistoryKey,"");
                background=Application.runInBackground;Application.runInBackground=true;running=true;stage=index=onlineLoaded=0;ui=null;lastMission=previousFirst=onlineFirst=null;previousDeck=null;firstFive.Clear();began=next=EditorApplication.timeSinceStartup;
                if(enteredPlay)EditorApplication.EnterPlaymode();return;
            }
            if(!running||!EditorApplication.isPlaying)return;
            try{
                Require(EditorApplication.timeSinceStartup-began<150,"Timeout at stage "+stage);
                if(ui==null){ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();if(ui==null)return;explorer=ui.GetComponent<ReferenceExplorer>();if(explorer.Rover==null){ui=null;return;}
                    offline=ui.Apod.OfflineOnly;var field=typeof(ApodService).GetField("history",Private);originalMemory=field.GetValue(ui.Apod);field.SetValue(ui.Apod,JsonUtility.FromJson("{\"previous\":[],\"seen\":[]}",field.FieldType));
                    keyboard=Keyboard.current;mouse=Mouse.current;keyboardEnabled=keyboard?.enabled==true;mouseEnabled=mouse?.enabled==true;if(keyboardEnabled)InputSystem.DisableDevice(keyboard);if(mouseEnabled)InputSystem.DisableDevice(mouse);
                }
                if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.35;
                if(stage==0){StartMission(true);stage=1;return;}
                if(stage==1){
                    var root=ui.GetComponent<UIDocument>().rootVisualElement;var image=root.Q<Image>("observatory-photo");string resource=ui.Apod.Collection.entries[0].resource;
                    Require(image?.image!=null && image.image==ui.Apod.Image(0),"Lens did not display the current service texture");Require(resource!=previousFirst,"Repeated first local image on restart "+index);
                    if(index<5)Require(firstFive.Add(resource),"Local image repeated before exhausting five images");
                    var deck=ui.Apod.Collection.entries.Select(e=>e.resource).ToArray();if(previousDeck!=null)for(int n=0;n<5;n++)Require(deck[n]!=previousDeck[n],"Same image stayed in Sol slot "+n);
                    Require(root.Q<Label>("observatory-image-status").text==ui.Apod.Status(0),"Loading/offline status not visible on lens");
                    previousDeck=deck;previousFirst=resource;var texture=image.image;Call("CloseOverlay");ui.ShowObservatory();Require(root.Q<Image>("observatory-photo").image==texture,"Reopening the same Sol rerolled its image");
                    ScreenCapture.CaptureScreenshot("Logs/telescope-offline-restart-"+index+".png");File.AppendAllText(Log,"Offline restart "+index+": "+resource+"; lens texture verified; all five Sol slots changed.\n");
                    if(++index<6)stage=0;else{stage=2;index=0;}return;
                }
                if(stage==2){StartMission(false);Require(ui.Apod.RequestCount==1,"First image was not prefetched at mission start");loadStarted=EditorApplication.timeSinceStartup;stage=3;return;}
                if(stage==3){
                    if(ui.Apod.Loading(0)){Require(EditorApplication.timeSinceStartup-loadStarted<50,"Image load stalled");return;}
                    var root=ui.GetComponent<UIDocument>().rootVisualElement;Require(root.Q<Image>("observatory-photo")?.image==ui.Apod.Image(0),"Online lens stale after refresh");
                    var urls=(string[])typeof(ApodService).GetField("selectedUrls",Private).GetValue(ui.Apod);var url=urls[0];
                    if(url!=null){Require(index==0||url!=onlineFirst,"New online mission repeated the previously viewed image");onlineFirst=url;onlineLoaded++;File.AppendAllText(Log,"Online restart "+index+": downloaded texture shown; "+ui.Apod.Collection.entries[0].title+"; "+ui.Apod.Status(0)+"\n");}
                    else File.AppendAllText(Log,"Online restart "+index+": NETWORK UNAVAILABLE; fallback/status verified. "+ui.Apod.Status(0)+"\n");
                    ScreenCapture.CaptureScreenshot("Logs/telescope-online-restart-"+index+".png");if(++index<2){stage=2;return;}
                    File.AppendAllText(Log,"PASS: six menu restarts; first five offline images unique; no immediate repeat; all Sol slots change; reopen same Sol stable; prefetch, metadata/status and actual lens binding verified. Online downloads displayed: "+onlineLoaded+"/2.\n");Finish();
                }
            }catch(Exception error){File.AppendAllText(Log,"FAIL: "+error+"\n");Debug.LogException(error);Finish();}
        }
        static void StartMission(bool local)
        {
            Call("ShowTitle");ui.Apod.OfflineOnly=local;ui.StartMission(false);
            string mission=ui.GetComponent<MissionIntegration>().MissionId;Require(mission!=lastMission,"Mission identity did not change");lastMission=mission;
            ui.Campaign.CloseReport();Call("CloseOverlay");explorer.Rest.Cancel();var motion=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);var pos=ui.Observatory.StandPosition;motion.X=pos.x;motion.Z=-pos.z;motion.Speed=motion.VelocityX=motion.VelocityZ=0;
            Require(ui.Observatory.TryInteract(),"Real telescope interaction failed");
        }
        static void Call(string name)=>typeof(GameFrontend).GetMethod(name,Private).Invoke(ui,null);
        static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        static void Finish()
        {
            running=false;if(ui!=null){ui.Apod.EndMission();ui.Apod.OfflineOnly=offline;typeof(ApodService).GetField("history",Private).SetValue(ui.Apod,originalMemory);}
            if(hadHistory)PlayerPrefs.SetString(ApodService.HistoryKey,savedHistory);else PlayerPrefs.DeleteKey(ApodService.HistoryKey);PlayerPrefs.Save();
            if(keyboardEnabled&&keyboard!=null)InputSystem.EnableDevice(keyboard);if(mouseEnabled&&mouse!=null)InputSystem.EnableDevice(mouse);Application.runInBackground=background;
            if(enteredPlay)EditorApplication.ExitPlaymode();
        }
    }
}
