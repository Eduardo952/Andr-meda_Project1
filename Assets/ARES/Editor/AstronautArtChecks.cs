using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Ares.Editor
{
    [InitializeOnLoad]
    public static class AstronautArtChecks
    {
        const string Request="Temp/ares-astronaut-art.request",Log="Logs/ares-astronaut-art.txt";
        static bool running,background;static double began;static int stage;
        static GameFrontend ui;static ReferenceExplorer explorer;static AstronautAnimator actor;
        static AstronautArtChecks(){EditorApplication.update+=Tick;}
        [MenuItem("ARES/Checks/Astronaut suit and visor")]
        public static void Begin()=>File.WriteAllText(Request,"run");
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!running&&File.Exists(Request)){
                if(EditorApplication.isPlaying){EditorApplication.ExitPlaymode();return;}
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete(Request);running=true;stage=0;ui=null;began=EditorApplication.timeSinceStartup;
                background=Application.runInBackground;Application.runInBackground=true;
                Directory.CreateDirectory("Logs");File.WriteAllText(Log,"RUNNING");
                EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView")).Focus();
                EditorApplication.EnterPlaymode();return;
            }
            if(!running||!EditorApplication.isPlaying)return;
            try{
                Require(EditorApplication.timeSinceStartup-began<75,"Timed out");
                if(ui==null){ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();explorer=ui?.GetComponent<ReferenceExplorer>();actor=ui?.GetComponentInChildren<AstronautAnimator>();
                    if(actor?.body.Find("Expressive helmet pivot")?.Find("Translucent visor")==null){ui=null;return;}
                    ui.Apod.OfflineOnly=true;ui.StartMission(false);ui.Campaign.CloseReport();
                    typeof(GameFrontend).GetMethod("CloseOverlay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,null);
                    ui.GetComponent<UIDocument>().rootVisualElement.style.display=DisplayStyle.None;
                    explorer.enabled=false;ui.enabled=false;actor.transform.SetPositionAndRotation(new Vector3(0,0,-3),Quaternion.identity);
                    foreach(var part in actor.GetComponentsInChildren<Transform>())part.gameObject.layer=30;
                    return;
                }
                var head=actor.body.Find("Expressive helmet pivot");head.localRotation=Quaternion.identity;
                actor.Apply(new AstronautPose());
                switch(stage++){
                    case 0:
                        var visor=head.Find("Translucent visor").GetComponent<MeshRenderer>();var glass=visor.sharedMaterial;
                        Require(glass.renderQueue==(int)RenderQueue.Transparent && glass.GetFloat("_ZWrite")==0,"Visor is opaque or writes depth");
                        Require(glass.GetColor("_BaseColor").a>.1f && glass.GetColor("_BaseColor").a<.5f,"Visor tint hides face");
                        Require(!glass.GetShaderPassEnabled("DepthOnly")&&!glass.GetShaderPassEnabled("ShadowCaster")&&visor.shadowCastingMode==ShadowCastingMode.Off,"Transparent visor casts opaque shadow/depth");
                        foreach(var renderer in actor.GetComponentsInChildren<MeshRenderer>())
                            if(new[]{"astronaut_3","astronaut_4","astronaut_5","astronaut_6"}.Contains(renderer.name))Require(!renderer.enabled,"Old helmet still overlaps new shell");
                        foreach(string name in new[]{"Translucent visor","Visor perimeter seal","Pressure helmet shell"}){
                            var mesh=head.Find(name).GetComponent<MeshFilter>().sharedMesh;var v=mesh.vertices;var n=mesh.normals;var t=mesh.triangles;
                            for(int i=0;i<t.Length;i+=3){var cross=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);Require(cross.sqrMagnitude>1e-14f&&Vector3.Dot(cross,n[t[i]])>0,"Degenerate or inward helmet face: "+name);}
                        }
                        var face=head.Find("Face inside pressure helmet");
                        foreach(var vertex in face.GetComponent<MeshFilter>().sharedMesh.vertices){var p=head.InverseTransformPoint(face.TransformPoint(vertex));Require(p.x*p.x/(.55f*.55f)+p.y*p.y/(.52f*.52f)+p.z*p.z/(.53f*.53f)<1,"Face protrudes through helmet");}
                        Require(actor.rightArm.Find("astronaut_17").localPosition==new Vector3(0,-.55f,0),"Tool hand socket changed");
                        Capture("front",new Vector3(0,1.65f,-4.7f));break;
                    case 1:Capture("three-quarter",new Vector3(3,2.2f,-4.5f));break;
                    case 2:Capture("side",new Vector3(4.7f,1.7f,0));break;
                    case 3:Capture("rear",new Vector3(2.4f,2.1f,4.5f));break;
                    case 4:actor.Apply(CharacterAnimation.Astronaut(3,1.1f,5,5,0,0,false,explorer.definition.world.ambience.jumpSeconds));Capture("walking",new Vector3(3,2.2f,-4.5f));break;
                    case 5:Capture("environment",new Vector3(3,2.2f,-4.5f),true);break;
                    default:Finish("PASS: face inside helmet; old surfaces removed; visor blending/depth/shadows; outward normals and nondegenerate triangles; tool hand socket preserved; front/side/rear/walking renders saved.");break;
                }
            }catch(Exception error){Finish("FAIL: "+error);}
        }
        static void Capture(string name,Vector3 offset,bool environment=false)
        {
            var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
            var target=camera.targetTexture;var active=RenderTexture.active;var mask=camera.cullingMask;var clear=camera.clearFlags;var color=camera.backgroundColor;
            bool ortho=camera.orthographic;float size=camera.orthographicSize;
            var rt=new RenderTexture(1000,1000,24);var texture=new Texture2D(1000,1000,TextureFormat.RGB24,false);
            try{
                camera.cullingMask=environment?mask|1<<30:1<<30;camera.clearFlags=environment?clear:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.66f,.70f,.72f);
                camera.orthographic=!environment;camera.orthographicSize=1.45f;
                camera.transform.position=actor.transform.position+offset;camera.transform.LookAt(actor.transform.position+Vector3.up*1.16f);
                camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1000,1000),0,0);texture.Apply();File.WriteAllBytes("Logs/astronaut-"+name+".png",texture.EncodeToPNG());
            }finally{camera.targetTexture=target;RenderTexture.active=active;camera.cullingMask=mask;camera.clearFlags=clear;camera.backgroundColor=color;camera.orthographic=ortho;camera.orthographicSize=size;camera.transform.SetPositionAndRotation(position,rotation);UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
        }
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        static void Finish(string result){File.WriteAllText(Log,result);running=false;Application.runInBackground=background;EditorApplication.ExitPlaymode();}
    }
}
