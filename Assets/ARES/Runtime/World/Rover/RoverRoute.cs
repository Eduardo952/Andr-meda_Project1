using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ares.World.Rover
{
    /// <summary>Survey trail sampled against the same clearance used for driving, including rocks.</summary>
    public sealed class RoverRoute : MonoBehaviour
    {
        Mesh mesh; Material material;
        public void Build(Vector3 start,float yaw,Func<Vector3,bool> clear,RoverGround ground)
        {
            var author=new RoverMesh(); var visited=new List<Vector3>{start};var current=start;
            for(int step=0;step<24;step++)
            {
                bool found=false;
                foreach(float turn in new[]{0f,30f,-30f,60f,-60f,90f,-90f})
                {
                    var direction=Quaternion.Euler(0,yaw+turn,0)*Vector3.forward;var next=current+direction*4;
                    bool allowed=true;
                    for(int n=1;n<=8;n++) if(!clear(current+direction*(n*.5f))) {allowed=false;break;}
                    foreach(var old in visited)if(Vector3.Distance(old,next)<3){allowed=false;break;}
                    if(!allowed)continue;
                    for(int n=0;n<4;n++) {
                        var p=current+direction*n;p.y=ground.Height(p)+.035f;
                        author.Box(p,new Vector3(.16f,.035f,.55f),Quaternion.Euler(0,yaw+turn,0),.015f);
                    }
                    current=next;yaw+=turn;visited.Add(next);found=true;break;
                }
                if(!found)break;
            }
            material=new Material(Shader.Find("ARES/Reference Toon"));material.SetColor("_BaseColor",new Color(.86f,.68f,.36f));
            mesh=MissionGeometry.Finish(author,"Rover surveyed trail");MissionGeometry.Render(transform,"Rover trail",mesh,material,Vector3.zero,Vector3.one);
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
