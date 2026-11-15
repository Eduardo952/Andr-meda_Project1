using System.Collections.Generic;
using Ares.UI;
using Ares.World.Rover;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ares.World
{
    /// <summary>Non-destructive art pass. Shared variants precede static batching; original colliders/layout remain authoritative.</summary>
    [DefaultExecutionOrder(-20)]
    public sealed class MissionHabitatArt : MonoBehaviour
    {
        readonly Dictionary<Material,Material> variants=new Dictionary<Material,Material>();
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        public int AccentTriangles {get;private set;}
        public int SharedVariants=>variants.Count;
        void Start()
        {
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>(true))
            {
                var source=renderer.sharedMaterial;
                if(source==null||source.shader.name!="ARES/Reference Toon")continue;
                if(!variants.TryGetValue(source,out var material))
                {
                    material=new Material(source){name=source.name+" / Mission finish"};materials.Add(material);variants.Add(source,material);
                    var color=source.GetColor("_BaseColor");Color.RGBToHSV(color,out float hue,out float saturation,out float value);
                    if(source.mainTexture==null)
                    {
                        Color finish=color;
                        if(saturation<.24f&&value>.62f)finish=MissionVisualStyle.Hex("#f6e7d0");
                        else if(saturation<.28f&&value<.58f)finish=Color.Lerp(MissionVisualStyle.Ink,MissionVisualStyle.Hex("#7086a4"),value);
                        else if(hue>.19f&&hue<.46f)finish=Color.HSVToRGB(hue,Mathf.Clamp(saturation,.48f,.72f),Mathf.Min(.82f,value*1.08f));
                        else if(hue<.12f&&saturation>.35f)finish=Color.Lerp(color,MissionVisualStyle.Coral,.24f);
                        finish.a=color.a;material.SetColor("_BaseColor",finish);
                    }
                    material.SetFloat("_Smoothness",.38f);
                    if(material.GetFloat("_Surface")>.5f&&color.a<.6f)
                    {var glass=MissionVisualStyle.Hex("#82c8d2");glass.a=color.a*.72f;material.SetColor("_BaseColor",glass);material.SetFloat("_Smoothness",.15f);}
                }
                renderer.sharedMaterial=material;
            }
            var explorer=GetComponent<ReferenceExplorer>();
            foreach(var room in explorer.definition.layout.rooms)
            {
                if(room.id=="solar"||room.shape=="park"||room.id=="control")continue;
                var color=MissionVisualStyle.Sector(room.id);
                var dark=Surface("Sector trim / "+room.id,MissionVisualStyle.Ink);
                var accent=Surface("Sector identity / "+room.id,color);
                var light=Surface("Sector guide light / "+room.id,Color.Lerp(color,Color.white,.55f),.25f);
                var shell=new RoverMesh();var stripes=new RoverMesh();var glow=new RoverMesh();
                // Door-sized frames are placed at imported ports; their six-metre opening stays clear.
                if(room.ports!=null)foreach(var port in room.ports)
                {
                    var center=new Vector3(port.x,.03f,-port.z);
                    var toward=(room.Center-center).normalized;
                    if(toward.sqrMagnitude<.1f)continue;
                    var turn=Quaternion.LookRotation(toward);
                    Vector3 P(float x,float y,float z)=>center+turn*new Vector3(x,y,z);
                    for(int side=-1;side<=1;side+=2)
                    {
                        shell.Box(P(side*3.35f,2.2f,0),new Vector3(.5f,4.4f,.6f),turn,.16f);
                        stripes.Box(P(side*3.35f,2.4f,-.33f),new Vector3(.30f,2.7f,.08f),turn,.035f);
                        glow.Box(P(side*2.85f,.09f,1.1f),new Vector3(.16f,.08f,2.8f),turn,.025f);
                    }
                    shell.Box(P(0,4.45f,0),new Vector3(7.2f,.6f,.65f),turn,.18f);
                    stripes.Box(P(0,4.43f,-.36f),new Vector3(3.3f,.28f,.09f),turn,.04f);
                }
                // A small inlaid equipment apron communicates interaction without occupying the walkway.
                var station=room.Station;
                for(int i=-1;i<=1;i++)stripes.Box(station+new Vector3(i*.5f,.055f,1.4f),new Vector3(.26f,.04f,.6f),Quaternion.identity,.015f);
                Add(shell,dark,room.id+" / rounded portal frame");Add(stripes,accent,room.id+" / sector accents");Add(glow,light,room.id+" / passage lights");
                var lamp=new GameObject("Mission sector light / "+room.id).AddComponent<Light>();lamp.transform.SetParent(transform,false);
                lamp.transform.position=room.Center+Vector3.up*4;lamp.type=LightType.Point;lamp.range=14;lamp.intensity=2.1f;
                lamp.color=Color.Lerp(color,Color.white,.6f);lamp.shadows=LightShadows.None;lamp.renderMode=LightRenderMode.ForcePixel;
            }
        }
        Material Surface(string name,Color color,float emission=0)
        {var m=new Material(Shader.Find("ARES/Reference Toon")){name=name};m.SetColor("_BaseColor",color);m.SetColor("_EmissionColor",color*emission);m.SetFloat("_Smoothness",.45f);materials.Add(m);return m;}
        void Add(RoverMesh author,Material material,string name)
        {var mesh=MissionGeometry.Finish(author,name);meshes.Add(mesh);AccentTriangles+=mesh.triangles.Length/3;MissionGeometry.Render(transform,name,mesh,material,Vector3.zero,Vector3.one);}
        void OnDestroy(){foreach(var material in materials)Destroy(material);foreach(var mesh in meshes)Destroy(mesh);}
    }
}
