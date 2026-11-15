using System;
using System.IO;
using System.Reflection;
using Ares.Domain;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Ares.Editor
{
    /// <summary>Opt-in checks for tool interaction and false foliage occlusion, without opening tasks.</summary>
    [InitializeOnLoad]
    public static class GreenhouseToolChecks
    {
        const string Request="Temp/ares-greenhouse-tools-check.request", Pending="ARES.GreenhouseTools";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static Keyboard keyboard;
        static GameFrontend ui;
        static ReferenceExplorer explorer;
        static int stage,frame,water;
        static float elapsed;
        static double started;
        static bool background;
        static GreenhouseToolChecks(){EditorApplication.update+=Tick;}
        [MenuItem("ARES/Checks/Greenhouse tools and visibility")]
        public static void RequestCheck(){File.WriteAllText(Request,"check");}
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists(Request))
            {
                string token=File.GetLastWriteTimeUtc(Request).Ticks.ToString();
                if(SessionState.GetString(Pending+".refresh","")!=token){SessionState.SetString(Pending+".refresh",token);AssetDatabase.Refresh();return;}
                File.Delete(Request);stage=0;frame=-1;elapsed=0;started=EditorApplication.timeSinceStartup;
                SessionState.SetBool(Pending,true);background=Application.runInBackground;Application.runInBackground=true;
                File.WriteAllText("Logs/ares-greenhouse-tools.txt","RUNNING");EditorApplication.isPaused=false;EditorApplication.EnterPlaymode();return;
            }
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying)return;
            try
            {
                Require(EditorApplication.timeSinceStartup-started<50,"Timeout");
                if(frame==Time.frameCount)return;frame=Time.frameCount;elapsed+=Mathf.Min(Time.deltaTime,.05f);
                if(keyboard==null)
                {
                    ui=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();explorer=UnityEngine.Object.FindAnyObjectByType<ReferenceExplorer>();
                    if(ui==null||explorer==null||ui.GreenhouseCare==null)return;
                    VerifyVisibility();
                    keyboard=InputSystem.AddDevice<Keyboard>("ARES greenhouse tools check");
                    ui.StartMission(false);ui.Campaign.CloseReport();typeof(GameFrontend).GetMethod("CloseOverlay",Private).Invoke(ui,null);
                    SetPlayer(ui.GreenhouseCare.PickupPosition);elapsed=0;
                }
                keyboard.MakeCurrent();var keys=new KeyboardState();var care=ui.GreenhouseCare;var can=care.Watering;
                if(stage==0&&elapsed>.5f){Require(ui.Campaign.Phase==Phase.Plan&&care.CanPickUp,"Pickup must be available before planning");keys=new KeyboardState(Key.E);Next();}
                else if(stage==1&&elapsed>.25f){Require(can.HasCan&&care.CanReturn&&!ui.BlocksWorld,"Actual E pickup");keys=new KeyboardState(Key.E);Next();}
                else if(stage==2&&elapsed>.25f){Require(!can.HasCan&&care.CanPickUp&&!ui.BlocksWorld,"Actual E return");keys=new KeyboardState(Key.E);Next();}
                else if(stage==3&&elapsed>.25f)
                {
                    Require(can.HasCan,"Re-pickup");ui.Campaign.SetAllocation(new Ares.Data.Allocation{shield=ui.Campaign.EnergyPool});ui.Campaign.StartSol();
                    SetPlayer(ui.GreenhouseCare.RefillPosition);Next();
                }
                else if(stage==4&&elapsed>.25f){Require(care.AtRefill,"Refill zone");keys=new KeyboardState(Key.E);Next();}
                else if(stage==5&&elapsed>.25f){Require(can.CanMl>0&&!ui.BlocksWorld,"Fill");water=can.CanMl;keys=new KeyboardState(Key.E);Next();}
                else if(stage==6&&elapsed>.25f){Require(can.CanMl==water&&!ui.BlocksWorld,"E at full tap must not open task");SetPlayer(ui.GreenhouseCare.PickupPosition);Next();}
                else if(stage==7&&elapsed>.25f){keys=new KeyboardState(Key.E);Next();}
                else if(stage==8&&elapsed>.25f){Require(!can.HasCan&&can.CanMl==water&&can.ActiveShelf==-1&&!ui.BlocksWorld,"Return preserves water and stops pouring");keys=new KeyboardState(Key.E);Next();}
                else if(stage==9&&elapsed>.25f)
                {Require(can.HasCan&&can.CanMl==water&&!ui.BlocksWorld,"Re-pickup preserves water");Finish("PASS: actual E pickup/return before planning; re-pickup; refill/full tap priority; stored water preserved; exact mesh occlusion rejects empty bounds and geometry behind player; smooth fade and restore.");return;}
                InputSystem.QueueStateEvent(keyboard,keys);
            }
            catch(Exception error){Finish("FAIL stage "+stage+": "+error);}
        }
        static void VerifyVisibility()
        {
            var root=new GameObject("Visibility regression fixture");root.transform.position=new Vector3(52,1.25f,0);
            var mesh=new Mesh{vertices=new[]{new Vector3(-2,-2,0),new Vector3(2,-2,0),new Vector3(-2,2,0)},triangles=new[]{0,1,2}};mesh.RecalculateBounds();
            root.AddComponent<MeshFilter>().sharedMesh=mesh;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",Color.green);
            var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            var cameraObject=new GameObject("Visibility fixture camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            GreenhouseOcclusion visibility=null;
            try
            {
                visibility=new GreenhouseOcclusion(root.transform);
                Action<Vector3,Vector3,bool> sample=(position,player,enabled)=>
                {camera.transform.position=position;camera.transform.LookAt(player+Vector3.up*1.25f);for(int i=0;i<120;i++)visibility.Update(camera,player,enabled);};
                sample(new Vector3(53,2.25f,-4),new Vector3(53,1,4),true);
                Require(renderer.sharedMaterial==material,"Empty triangle bounding-box corner must remain opaque");
                sample(new Vector3(51,1.25f,-4),new Vector3(51,0,4),true);
                Require(renderer.sharedMaterial!=material,"Actual obstruction must fade");
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);Require(block.GetColor("_BaseColor").a>=.34f,"Plant must remain visible");
                sample(new Vector3(51,1.25f,-4),new Vector3(51,0,-2),true);
                Require(renderer.sharedMaterial==material,"Geometry behind player must restore");
                sample(new Vector3(51,1.25f,-4),new Vector3(51,0,4),true);
                sample(new Vector3(51,1.25f,-4),new Vector3(51,0,4),false);
                Require(renderer.sharedMaterial==material,"Leaving caregiver mode must restore");
            }
            finally{visibility?.Dispose();UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(material);}
        }
        static void SetPlayer(Vector3 p){var player=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",Private).GetValue(explorer);player.X=p.x;player.Z=-p.z;player.Speed=player.VelocityX=player.VelocityZ=0;}
        static void Next(){stage++;elapsed=0;}
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static void Finish(string result){SessionState.SetBool(Pending,false);if(keyboard!=null)InputSystem.RemoveDevice(keyboard);keyboard=null;Application.runInBackground=background;File.WriteAllText("Logs/ares-greenhouse-tools.txt",DateTime.UtcNow.ToString("O")+" "+result);EditorApplication.ExitPlaymode();}
    }
}
