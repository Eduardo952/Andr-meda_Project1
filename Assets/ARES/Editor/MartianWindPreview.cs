using System;
using System.IO;
using System.Reflection;
using Ares.UI;
using Ares.World;
using UnityEditor;
using UnityEngine;

namespace Ares.Editor
{
    /// <summary>One opt-in visual check of ambient particles and indoor exclusion.</summary>
    [InitializeOnLoad]
    public static class MartianWindPreview
    {
        const string Pending="ARES.WindPreview";
        static GameFrontend ui;
        static float elapsed;
        static int frame;
        static bool background;
        static double started;
        static MartianWindPreview(){EditorApplication.update+=Tick;}
        public static void Begin()
        {ui=null;elapsed=0;frame=-1;started=EditorApplication.timeSinceStartup;background=Application.runInBackground;Application.runInBackground=true;SessionState.SetBool(Pending,true);File.WriteAllText("Logs/ares-wind.txt","RUNNING");EditorApplication.isPaused=false;EditorApplication.EnterPlaymode();}
        static void Tick()
        {
            if(!SessionState.GetBool(Pending,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
            try
            {
                if(EditorApplication.timeSinceStartup-started>35)throw new Exception("Preview timeout");
                if(frame==Time.frameCount)return;frame=Time.frameCount;
                if(ui==null)
                {
                    var ready=UnityEngine.Object.FindAnyObjectByType<GameFrontend>();if(ready==null||ready.GetComponent<ReferenceExplorer>()?.Rover==null)return;
                    var readyExplorer=ready.GetComponent<ReferenceExplorer>();
                    if(typeof(ReferenceExplorer).GetField("player",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(readyExplorer)==null)return;
                    ui=ready;
                    ui.StartMission(false);ui.Campaign.CloseReport();
                    typeof(GameFrontend).GetMethod("CloseOverlay",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(ui,null);
                    var explorer=ui.GetComponent<ReferenceExplorer>();explorer.reducedMotion=false;
                    var player=(PlayerMotion)typeof(ReferenceExplorer).GetField("player",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(explorer);
                    player.X=0;player.Z=65;player.Speed=player.VelocityX=player.VelocityZ=0;explorer.RecenterCamera();
                }
                elapsed+=Mathf.Min(Time.deltaTime,.05f);if(elapsed<5)return;
                var wind=ui.GetComponent<MartianWind>();if(wind==null||wind.LiveParticles<10)throw new Exception("Ambient dust did not emit");
                ParticleSystem system=null;
                foreach(var candidate in ui.GetComponentsInChildren<ParticleSystem>())if(candidate.name=="Martian wind / airborne dust"){system=candidate;break;}
                if(system==null)throw new Exception("Wind particle system not found");
                var particles=new ParticleSystem.Particle[260];int count=system.GetParticles(particles);
                foreach(var room in ui.GetComponent<ReferenceExplorer>().definition.layout.rooms)
                    if(room.shape!="park")for(int i=0;i<count;i++)if(particles[i].remainingLifetime>0&&WorldNavigation.Inside(particles[i].position.x,-particles[i].position.z,room,.75f))throw new Exception("Dust inside "+room.id);
                if(ShaderUtil.ShaderHasError(Resources.Load<Shader>("ARES/Effects/WindDust")))throw new Exception("Wind shader failed");
                ScreenCapture.CaptureScreenshot("Logs/ares-wind.png");
                Finish("PASS: bounded ambient pool; "+count+" live particles; indoor exclusion; shader compiled. Screenshot ares-wind.png.");
            }
            catch(Exception error){Finish("FAIL: "+error);}
        }
        static void Finish(string result){SessionState.SetBool(Pending,false);Application.runInBackground=background;File.WriteAllText("Logs/ares-wind.txt",result);EditorApplication.ExitPlaymode();}
    }
}
