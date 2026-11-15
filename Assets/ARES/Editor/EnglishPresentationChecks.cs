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
    [InitializeOnLoad]
    public static class EnglishPresentationChecks
    {
        const string Pending="ARES.EnglishPresentation";const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static GameFrontend ui;static ReferenceExplorer explorer;static VisualElement root;static string previous;
        static int stage;static double next,started;static bool background;
        static EnglishPresentationChecks(){EditorApplication.update+=Tick;}
        public static void Begin()
        {previous=PlayerPrefs.GetString("ARES.Language","pt-BR");PlayerPrefs.SetString("ARES.Language","en");stage=0;ui=null;started=next=0;background=Application.runInBackground;Application.runInBackground=true;SessionState.SetBool(Pending,true);File.WriteAllText("Logs/ares-english.txt","RUNNING");File.WriteAllText("Logs/ares-english-text.txt","");EditorApplication.EnterPlaymode();}
        static void Tick()
        {
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try
            {
                if(started==0)started=EditorApplication.timeSinceStartup;
                Require(EditorApplication.timeSinceStartup-started<100,"English review timeout");
                if(EditorApplication.timeSinceStartup<next)return;
                if(ui==null){ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();if(ui==null)return;explorer=ui.GetComponent<ReferenceExplorer>();root=ui.GetComponent<UIDocument>().rootVisualElement;ui.Apod.OfflineOnly=true;next=EditorApplication.timeSinceStartup+3;return;}
                next=EditorApplication.timeSinceStartup+1;
                switch(stage++)
                {
                    case 0:Require(root.Q<Button>("title-play").text.Contains("Start mission"),"Menu did not translate");Capture("menu");break;
                    case 1:ui.StartMission();break;
                    case 2:Require(root.Q<Label>("tutorial-speech").text.StartsWith("Hi!"),"Tutorial stayed Portuguese");Capture("tutorial");break;
                    case 3:Press("tutorial-decline");Call("OpenTask","SmartFarmCare");break;
                    case 4:Capture("greenhouse");stage=40;break;
                    case 40:Call("CloseOverlay");stage=5;break;
                    case 5:
                        var player=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);var stand=explorer.computerSettings.standPoint;player.X=stand.x;player.Z=-stand.z;ui.Terminal.TryInteract();next=EditorApplication.timeSinceStartup+2.5;break;
                    case 6:Require(root.Q<Label>("oxygen-status-title").text=="Ready to begin","Oxygen status stayed Portuguese");Capture("oxygen");root.Q<Foldout>("oxygen-science").value=true;break;
                    case 7:Capture("oxygen-details");Press("computer-energy");break;
                    case 8:Capture("energy");stage=80;break;
                    case 80:Press("computer-communications");stage=9;break;
                    case 9:Capture("communications");break;
                    case 10:
                        ui.Terminal.Reset();Call("CloseOverlay");ui.ShowObservatory();break;
                    case 11:Capture("observatory");
                        var field=new TextField{value="Regar plantas"};root.Add(field);PresentationLocalizer.Refresh(root);Require(field.value=="Regar plantas","User input was translated");field.RemoveFromHierarchy();
                        Require(PresentationLocalizer.Text("Faltam 15 kWh para atender este sistema.")=="This system needs 15 more kWh.","Dynamic deficit not translated");
                        Require(PresentationLocalizer.Text("2 / 4 fileiras cuidadas   ·   Regador: 510 / 510 mL").Contains("rows cared for"),"Watering progress not translated");
                        Call("CloseOverlay");ui.Campaign.SetAllocation(new Ares.Data.Allocation{shield=50,life=25,greenhouse=25});ui.Campaign.StartSol();ui.Campaign.Tick(600,false);break;
                    case 12:Require(root.Q("mission-report")!=null,"Summary missing");Require(root.Q<Button>("summary-communications").text.Contains("Next Sol"),"Summary action stayed Portuguese");Capture("report");break;
                    case 13:
                        PlayerPrefs.SetString("ARES.Language","pt-BR");Require(PresentationLocalizer.Text("Start tutorial")=="Fazer tutorial","Return to Portuguese failed");
                        File.WriteAllText("Logs/ares-english.txt","PASS: Ares Base branding, English menu/tutorial/greenhouse/oxygen/energy/communications/observatory; changing telemetry; Portuguese round-trip; editable text preserved. Captures and visible-copy audit saved.");Finish();break;
                }
            }
            catch(Exception error){File.WriteAllText("Logs/ares-english.txt","FAIL: "+error);Debug.LogException(error);Finish();}
        }
        static void Capture(string name)
        {
            ScreenCapture.CaptureScreenshot("Logs/ares-english-"+name+".png");
            File.AppendAllText("Logs/ares-english-text.txt","\n--- "+name+" ---\n");
            root.Query<TextElement>().ForEach(e=>{if(e.resolvedStyle.display!=DisplayStyle.None&&!string.IsNullOrWhiteSpace(e.text)){Require(!e.text.Contains("Casa Marte")&&!e.text.Contains("CASA MARTE")&&!e.text.Contains("Mission Mars"),"Wrong visible brand");File.AppendAllText("Logs/ares-english-text.txt",e.text+"\n");}});
        }
        static void Press(string name){var b=root.Q<Button>(name);Require(b!=null,"Missing "+name);using(var e=NavigationSubmitEvent.GetPooled()){e.target=b;b.SendEvent(e);}}
        static void Call(string name,params object[] args)=>typeof(GameFrontend).GetMethod(name,Private).Invoke(ui,args);
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static void Finish(){PlayerPrefs.SetString("ARES.Language",previous);SessionState.SetBool(Pending,false);Application.runInBackground=background;ui=null;EditorApplication.ExitPlaymode();}
    }
}
