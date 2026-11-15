using System;
using System.IO;
using System.Reflection;
using Ares.Data;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    /// <summary>Integrated art review: actual UI, shader compilation, clean new geometry and alarm phases.</summary>
    [InitializeOnLoad]
    public static class MissionVisualChecks
    {
        const string Pending="ARES.MissionVisual";const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
        static GameFrontend ui;static ReferenceExplorer explorer;static VisualElement root;
        static int stage;static double next,started;static bool background;
        static MissionVisualChecks(){EditorApplication.update+=Tick;}
        public static void Begin()
        {stage=0;ui=null;started=0;next=0;background=Application.runInBackground;Application.runInBackground=true;SessionState.SetBool(Pending,true);File.WriteAllText("Logs/ares-mission-visual.txt","RUNNING");EditorApplication.EnterPlaymode();}
        static void Tick()
        {
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try
            {
                if(started==0)started=EditorApplication.timeSinceStartup;
                Require(EditorApplication.timeSinceStartup-started<100,"Visual review timeout");
                if(EditorApplication.timeSinceStartup<next)return;
                if(ui==null)
                {ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();if(ui==null)return;explorer=ui.GetComponent<ReferenceExplorer>();root=ui.GetComponent<UIDocument>().rootVisualElement;ui.Apod.OfflineOnly=true;next=EditorApplication.timeSinceStartup+3;return;}
                next=EditorApplication.timeSinceStartup+1.2;
                switch(stage++)
                {
                    case 0:Require(root.Q("mission-main-menu")!=null,"Menu missing");Screen("menu");break;
                    case 1:Call("Settings");break;
                    case 2:Screen("settings");break;
                    case 3:Call("CloseOverlay");ui.StartMission(false);break;
                    case 4:Screen("briefing");break;
                    case 5:ui.Campaign.CloseReport();Call("CloseOverlay");break;
                    case 6:Screen("hud");Character();break;
                    case 7:
                        var player=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);var stand=explorer.computerSettings.standPoint;player.X=stand.x;player.Z=-stand.z;
                        ui.Terminal.TryInteract();next=EditorApplication.timeSinceStartup+2.5;break;
                    case 8:
                        var energy=root.Q<Button>("computer-energy");Require(energy!=null,"Computer navigation missing");using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=energy;energy.SendEvent(evt);}stage=80;break;
                    case 80:Require(root.Q("energy-confirm")!=null,"Energy controls missing");Screen("energy");stage=9;break;
                    case 9:
                        Call("CloseOverlay");
                        var copy=JsonUtility.FromJson<CampaignData>(JsonUtility.ToJson(explorer.definition.campaign));
                        copy.sols[0].sol=142;copy.sols[0].demand=new Allocation{shield=65,life=25,greenhouse=22};
                        var report=new Campaign(copy,explorer.definition.rules,explorer.solarSettings,explorer.oxygenSettings);
                        report.CloseReport();Require(report.SetAllocation(new Allocation{shield=50,life=25,greenhouse=25}),"Report allocation rejected");Require(report.StartSol(),"Report did not start");report.Tick(600,false);
                        Require(report.Outcome!=null&&report.Outcome.shield.missing==15,"Real consequence model lost report deficit");
                        typeof(GameFrontend).GetField("campaign",Private).SetValue(ui,report);Call("ShowSummary");break;
                    case 10:
                        Require(root.Q("report-resource-shield")!=null&&root.Q("report-mentor-explanation")!=null,"Report UI missing");
                        var nextButton=root.Q<Button>("summary-communications");Screen("report");File.AppendAllText("Logs/ares-mission-visual.txt","\nReport action="+nextButton?.worldBound+" root="+root.worldBound);Require(nextButton!=null&&nextButton.worldBound.yMax<=root.worldBound.yMax,"Next Sol action clipped");break;
                    case 11:
                        root.style.width=1280;root.style.height=720;break;
                    case 12:Require(root.Q<Button>("summary-communications").worldBound.yMax<=root.worldBound.yMax,"Compact report action clipped");Screen("report-compact");stage=120;break;
                    case 120:root.style.width=StyleKeyword.Null;root.style.height=StyleKeyword.Null;stage=13;break;
                    case 13:
                        Call("CloseOverlay");typeof(Campaign).GetProperty("Alarm").SetValue(ui.Campaign,"solarStorm");typeof(Campaign).GetProperty("SolFraction").SetValue(ui.Campaign,.4f);next=EditorApplication.timeSinceStartup+3.5;break;
                    case 14:
                        Require(explorer.GetComponent<MissionStormPresentation>().Strength>.9f,"Storm never developed");World("storm",new Vector3(105,58,-100),new Vector3(0,12,0));
                        typeof(Campaign).GetProperty("Alarm").SetValue(ui.Campaign,null);next=EditorApplication.timeSinceStartup+4.8;break;
                    case 15:
                        Require(explorer.GetComponent<MissionStormPresentation>().Strength<.05f,"Storm did not dissipate");World("calm",new Vector3(105,58,-100),new Vector3(0,12,0));
                        foreach(string shader in new[]{"ARES/Reference Toon","ARES/Reference Terrain","ARES/Reference Sky","ARES/Reference Shield"})Require(!ShaderUtil.ShaderHasError(Shader.Find(shader)),"Shader error "+shader);
                        foreach(var mesh in Resources.FindObjectsOfTypeAll<Mesh>())if(mesh.name.Contains(" / rounded portal frame")||mesh.name.Contains(" / sector accents")||mesh.name.Contains("Mission rounded detail"))
                        {var vertices=mesh.vertices;var triangles=mesh.triangles;for(int i=0;i<triangles.Length;i+=3)Require(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).sqrMagnitude>1e-14f,"Zero-area new triangle "+mesh.name);}
                        File.AppendAllText("Logs/ares-mission-visual.txt","\nPASS: menu/settings/briefing/HUD/energy/report captures; SOL 142 50/65, 25/25, 25/22 from real model; next action visible; storm onset/recovery; shaders; nondegenerate new meshes. Accent triangles="+explorer.GetComponent<MissionHabitatArt>().AccentTriangles+", shared variants="+explorer.GetComponent<MissionHabitatArt>().SharedVariants+".");Finish();break;
                }
            }
            catch(Exception error){File.AppendAllText("Logs/ares-mission-visual.txt","\nFAIL: "+error);Debug.LogException(error);Finish();}
        }
        static void Screen(string name)=>ScreenCapture.CaptureScreenshot("Logs/ares-mission-"+name+".png");
        static void Call(string name)=>typeof(GameFrontend).GetMethod(name,Private).Invoke(ui,null);
        static void Character()
        {var actor=explorer.GetComponentInChildren<AstronautAnimator>().transform;World("astronaut",actor.TransformPoint(new Vector3(2,1.8f,-4)),actor.TransformPoint(new Vector3(0,1.25f,0)));}
        static void World(string name,Vector3 position,Vector3 target)
        {
            var camera=Camera.main;var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;var oldTarget=camera.targetTexture;var active=RenderTexture.active;
            var texture=new RenderTexture(1600,1000,24);var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try{camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes("Logs/ares-mission-"+name+".png",image.EncodeToPNG());}
            finally{camera.targetTexture=oldTarget;camera.transform.SetPositionAndRotation(oldPosition,oldRotation);RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
        }
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static void Finish(){SessionState.SetBool(Pending,false);Application.runInBackground=background;ui=null;EditorApplication.ExitPlaymode();}
    }
}
