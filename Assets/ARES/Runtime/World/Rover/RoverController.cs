using Ares.Data;
using UnityEngine;

namespace Ares.World.Rover
{
    /// <summary>Composition for the rover. ReferenceExplorer owns the single input/camera authority.</summary>
    public sealed class RoverController : MonoBehaviour
    {
        public RoverMotion Motion { get; private set; }
        public RoverModel Model { get; private set; }
        public RoverDust Dust { get; private set; }
        public RoverCamera CameraRig { get; private set; }
        public bool IsDriving { get; private set; }
        RoverSettings settings;
        RoverGround ground;
        public void Initialize(GameDefinition definition, RoverSettings settings)
        {
            this.settings=settings;
            var navigation=new RoverNavigation(definition.layout,settings);
            ground=new RoverGround(transform.parent);
            bool CanOccupy(Vector3 p) => navigation.CanOccupy(p)&&ground.Clear(p,settings.footprintRadius);
            bool CanSpawn(Vector3 p)
            {
                Vector3 forward=Quaternion.Euler(0,settings.spawnYaw,0)*Vector3.forward;
                for(int i=0;i<=16;i++)if(!CanOccupy(p+forward*(i*.75f)))return false;
                return true;
            }
            Vector3 spawn=settings.spawn;
            if(!CanSpawn(spawn))
            {
                bool found=false;
                for(int ring=1;ring<=12 && !found;ring++)for(int step=0;step<16;step++)
                {
                    var candidate=settings.spawn+Quaternion.Euler(0,step*22.5f,0)*Vector3.forward*(ring*2);
                    if(!CanSpawn(candidate))continue;
                    spawn=candidate;found=true;break;
                }
                if(!found)throw new System.InvalidOperationException("No clear rover spawn within 24 metres. Adjust roverSettings.spawn.");
            }
            Motion=new RoverMotion(settings,CanOccupy,spawn);
            ground.Place(transform,Motion.Position,Motion.Yaw);
            Model=gameObject.AddComponent<RoverModel>(); Model.Build(settings);
            var effect=new GameObject("Six wheel contact dust"); effect.transform.SetParent(transform,false);
            Dust=effect.AddComponent<RoverDust>(); Dust.Initialize();
            CameraRig=new RoverCamera(settings,definition.layout);
            var trail=new GameObject("NOMAD / surveyed route");trail.transform.SetParent(transform.parent,false);
            trail.AddComponent<RoverRoute>().Build(spawn,settings.spawnYaw,CanOccupy,ground);
        }
        public void SetDriving(bool driving)
        {
            IsDriving=driving; Motion.Stop(); Dust.Clear(); CameraRig.ClearInput();
            if(driving)CameraRig.Enter(transform);
        }
        public void Tick(float throttle,float steer,float dt,bool blocked,bool reducedMotion)
        {
            if(blocked || !IsDriving)Motion.Stop(); else Motion.Step(throttle,steer,dt);
            ground.Place(transform,Motion.Position,Motion.Yaw);
            Model.Animate(Motion,dt,reducedMotion); Dust.Tick(Motion,Model,settings,dt,IsDriving&&!blocked,reducedMotion);
        }
        void OnDisable() { Motion?.Stop(); Dust?.Clear(); CameraRig?.ClearInput(); }
        void OnDestroy() { ground?.Dispose(); }
    }
}
