using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    [InitializeOnLoad]
    public static class ReferenceSceneChecks
    {
        const string Request="Temp/ares-scenery-check.request", Pending="ARES.SceneryCheck";
        static double started;
        static int stage;
        static bool background;
        static ReferenceExplorer explorer;
        static ReferenceSceneChecks() { EditorApplication.update += Tick; }
        [MenuItem("ARES/Checks/Reference scenery and geometry")]
        public static void RequestCheck() => File.WriteAllText(Request,"check");
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Request))
            {
                string token=File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if(SessionState.GetString(Pending+".refresh","")!=token){SessionState.SetString(Pending+".refresh",token);AssetDatabase.Refresh();return;}
                File.Delete(Request); Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/ares-scenery.txt","RUNNING");
                SessionState.SetBool(Pending,true); stage=0; started=EditorApplication.timeSinceStartup;
                background=Application.runInBackground; Application.runInBackground=true;
                EditorApplication.isPaused=false;EditorApplication.EnterPlaymode(); return;
            }
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying)return;
            try
            {
                if(started<=0){started=EditorApplication.timeSinceStartup;stage=0;}
                Application.runInBackground=true;
                Require(EditorApplication.timeSinceStartup-started<90,"Play validation timeout");
                if(explorer==null) explorer=UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                if(explorer?.Rover==null||Time.timeSinceLevelLoad<2)return;
                if(stage==0)
                {
                    var ui=explorer.GetComponent<GameFrontend>(); ui.Apod.OfflineOnly=true; ui.StartMission(false);
                    ui.Campaign.CloseReport();
                    typeof(GameFrontend).GetMethod("CloseOverlay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,null);
                    ui.Campaign.SetAllocation(new Ares.Data.Allocation {shield=30,life=40,greenhouse=30});
                    Require(ui.Campaign.StartSol(),"Mission failed to start");
                    Require(ui.Solar.BoundPanels==32,"Solar panels no longer bind");
                    Require(explorer.GetComponent<HabitatStoryProps>()==null,"Non-reference replacement scenery active");
                    var crops=explorer.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("greenhouse_interior_")).ToArray();
                    Require(crops.Length>300 && crops.Where(r=>r.GetComponent<ReferenceWorldPart>()==null).All(r=>r.enabled),"Original garden is missing or hidden");
                    var parts=explorer.GetComponentsInChildren<ReferenceWorldPart>(true);
                    Require(parts.Count(p=>p.role=="cultivation_container")==40,"Expected forty original growing containers");
                    Require(explorer.GetComponent<ReferenceLandmarks>().AnimatedParts==32,"Hub tree, holograms, fireflies or shield missing");
                    Require(explorer.Rover.Model!=null&&explorer.Rover.Model.Wheels.Length==6,"Imported rover rig missing");
                    Require(parts.Count(p=>p.role=="watering_can")==1,"Original watering can missing");
                    File.AppendAllText("Logs/ares-scenery.txt","\n"+SceneryNavigationChecks.Run(explorer.definition.layout));
                    foreach(string shaderName in new[]{"ARES/Reference Toon","ARES/Reference Sky","ARES/Reference Terrain","ARES/Reference Stars","ARES/Celestial","ARES/Reference Shield"})
                    { var shader=Shader.Find(shaderName); Require(shader!=null&&!ShaderUtil.ShaderHasError(shader),"Shader failed: "+shaderName); }
                    Require(explorer.GetComponent<ReferenceWorldBatches>().BatchedRenderers>500,"Static batching missing");
                    Require(explorer.GetComponentsInChildren<MeshRenderer>().Count(r=>r.isPartOfStaticBatch)>500,"Renderers did not join static batches");
                    Require(ReferenceRig.Find(explorer.transform,"sky")?.GetChild(0).childCount==4,"Reference celestial bodies missing");
                    Require(explorer.GetComponent<ReferenceAmbience>().CrewCount==3,"Crew prefab identities were lost");
                    File.AppendAllText("Logs/ares-scenery.txt","\nStatic batch candidates: "+explorer.GetComponent<ReferenceWorldBatches>().BatchedRenderers);
                    File.AppendAllText("Logs/ares-scenery.txt","\n"+WorldImportChecks.Run());
                }
                if(stage<Views.Length)
                {
                    var v=Views[stage];
                    var cutaway=new RoomCutaway(explorer.transform,explorer.definition.layout);
                    cutaway.Update(v.target,v.position,false,true,1.2f,1);
                    Capture(v.name,v.position,v.target);
                    // Turning roofs off must never disable floors.
                    foreach(Transform group in explorer.transform)
                        if(group.name.EndsWith("_floor"))Require(group.gameObject.activeInHierarchy,"Floor hidden by roof cutaway");
                    stage++; return;
                }
                File.AppendAllText("Logs/ares-scenery.txt","\nPASS: reference garden, station details, 32 solar bindings, shaders, static batches, floor cutaway, all sector captures.\n");
                Finish();
            }
            catch(Exception e){File.AppendAllText("Logs/ares-scenery.txt","\nFAIL: "+e);Debug.LogException(e);Finish();}
        }
        static (string name,Vector3 position,Vector3 target)[] Views=>new[]{
            ("overview",new Vector3(112,150,-152),new Vector3(10,0,-22)),
            ("control",new Vector3(20,22,-25),new Vector3(0,0,0)),
            ("observatory",explorer.definition.layout.RoomPoint("observatory",22,22,21),explorer.definition.layout.RoomPoint("observatory",0,1,0)),
            ("shield",explorer.definition.layout.RoomPoint("shield",24,23,22),explorer.definition.layout.RoomPoint("shield",0,0,0)),
            ("greenhouse",new Vector3(81,36,-32),new Vector3(52,1,0)),
            ("life-support",explorer.definition.layout.RoomPoint("lifeSupport",23,22,21),explorer.definition.layout.RoomPoint("lifeSupport",0,0,0)),
            ("solar",explorer.definition.layout.SolarPoint(38,30,112),explorer.definition.layout.SolarPoint(0,0,80)),
            ("rocket",explorer.definition.layout.landmarks.First(l=>l.id=="landingPad").Center+new Vector3(20,18,-25),explorer.definition.layout.landmarks.First(l=>l.id=="landingPad").Center+Vector3.up*4),
            ("rover",explorer.Rover.transform.position+new Vector3(7,5,-8),explorer.Rover.transform.position+Vector3.up),
            ("horizon",new Vector3(0,7,-8),new Vector3(-30,14,60)),
            ("night-sky",new Vector3(0,7,-8),new Vector3(-150,70,112))
        };
        static void Capture(string name,Vector3 position,Vector3 target)
        {
            var camera=Camera.main; var oldTarget=camera.targetTexture;var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            var texture=new RenderTexture(1600,1000,24);var active=RenderTexture.active;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=texture;camera.transform.position=position;camera.transform.LookAt(target);
                if(name=="night-sky")
                {
                    var campaign=explorer.GetComponent<GameFrontend>().Campaign;
                    typeof(Ares.Domain.Campaign).GetProperty("SolFraction").SetValue(campaign,.9f);
                    explorer.GetComponent<ReferenceEnvironment>().SendMessage("Update");
                }
                explorer.GetComponent<ReferenceCelestials>().SendMessage("LateUpdate");
                camera.Render();
                RenderTexture.active=texture;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();
                File.WriteAllBytes("Logs/ares-scenery-"+name+".png",image.EncodeToPNG());
            }
            finally{camera.targetTexture=oldTarget;camera.transform.SetPositionAndRotation(oldPosition,oldRotation);RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(image);texture.Release();UnityEngine.Object.DestroyImmediate(texture);}
        }
        static void Finish(){SessionState.SetBool(Pending,false);Application.runInBackground=background;explorer=null;EditorApplication.ExitPlaymode();}
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
