using System.Collections.Generic;
using Ares.UI;
using Ares.World.Rover;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ares.World
{
    /// <summary>Bounded cosmetic motion from Ambience.tsx; never modifies campaign state.</summary>
    public sealed class ReferenceAmbience : MonoBehaviour
    {
        static readonly string[] CrewNames={"mira","theo","nova"};
        Vector3[] crewPositions;
        Quaternion solarRotation;
        readonly Transform[] crew=new Transform[3],panels=new Transform[32],prints=new Transform[20];
        readonly float[] born=new float[20];
        readonly List<Mesh> meshes=new List<Mesh>();readonly List<Material> materials=new List<Material>();
        ReferenceExplorer explorer;GameFrontend frontend;Transform radar,cleaner,brush;ParticleSystem steam;Light alarm;
        float time;int footprint;Vector3 lastStep;
        public int CrewCount {get;private set;}
        public int TrackingPanels {get;private set;}
        public float RadarAngle=>radar!=null?radar.localEulerAngles.y:0;
        void Start()
        {
            explorer=GetComponent<ReferenceExplorer>();frontend=GetComponent<GameFrontend>();lastStep=explorer.PlayerPosition;
            var layout=explorer.definition.layout;solarRotation=layout.SolarRotation;
            crewPositions=new[]{layout.RoomPoint("observatory",-1.4f,.12f,-6),layout.RoomPoint("lifeSupport",0,.12f,-6),layout.RoomPoint("greenhouse",1.4f,.12f,-6)};
            cleaner=ReferenceRig.Find(transform,"cleaner");
            if(cleaner!=null){foreach(var child in cleaner.GetComponentsInChildren<Transform>())if(child.name=="brush")brush=child;cleaner.gameObject.SetActive(false);}
            for(int i=0;i<3;i++){crew[i]=ReferenceRig.Find(transform,"crew_"+CrewNames[i]);if(crew[i]!=null)CrewCount++;}
            var sources=GetComponentsInChildren<MeshRenderer>();
            for(int i=0;i<32;i++)
            {
                var expected=layout.SolarPoint(-21+(i%8)*6+(i%8<4?-1:1),1.5f,72+i/8*6);
                foreach(var source in sources)
                {
                    if(!source.name.StartsWith("decor_")||source.sharedMaterial.mainTexture==null||Vector3.Distance(source.bounds.center,expected)>.1f)continue;
                    panels[i]=new GameObject("Sun tracking panel "+i).transform;panels[i].SetParent(transform,false);panels[i].position=expected;
                    source.transform.SetParent(panels[i],true);
                    var veil=transform.Find($"Solar maintenance/Solar panel {i+1:00} maintenance/Sand veil");if(veil!=null)veil.SetParent(panels[i],true);
                    TrackingPanels++;break;
                }
            }
            radar=new GameObject("Reference observatory radar").transform;radar.SetParent(transform,false);radar.position=layout.RoomPoint("observatory",8,9.3f,0);
            var radarMesh=new RoverMesh();radarMesh.Box(Vector3.zero,new Vector3(2,.15f,.6f),ReferenceSpace.Euler(.5f,0,0),0);
            Render("Radar aerial",radar,radarMesh.Bake("Radar aerial"),Material("#3dbfb3"));
            var printMesh=new RoverMesh();printMesh.Box(Vector3.zero,new Vector3(.16f,.008f,.3f),Quaternion.identity,0);
            var footprintMesh=printMesh.Bake("Footprint");var printMaterial=Material("#72483e",true,.22f);
            for(int i=0;i<prints.Length;i++){prints[i]=Render("Footprint "+i,transform,footprintMesh,printMaterial);born[i]=-100;prints[i].gameObject.SetActive(false);}
            steam=new GameObject("Life support vent steam").AddComponent<ParticleSystem>();steam.transform.SetParent(transform,false);steam.transform.position=layout.RoomPoint("lifeSupport",10,5,0);
            steam.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=steam.main;main.maxParticles=64;main.startLifetime=4;main.startSpeed=.7f;main.startSize=new ParticleSystem.MinMaxCurve(.2f,.55f);main.startColor=new Color(1,.97f,.93f,.13f);main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=steam.emission;emission.rateOverTime=12;
            var shape=steam.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=.25f;shape.rotation=new Vector3(-90,0,0);
            var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.13f,.3f),new GradientAlphaKey(0,1)});
            var over=steam.colorOverLifetime;over.enabled=true;over.color=fade;
            var steamMaterial=new Material(Resources.Load<Shader>("ARES/Effects/WindDust"));materials.Add(steamMaterial);steam.GetComponent<ParticleSystemRenderer>().sharedMaterial=steamMaterial;
            alarm=new GameObject("Reference emergency light").AddComponent<Light>();alarm.transform.SetParent(transform,false);alarm.type=LightType.Point;alarm.range=30;alarm.color=new Color32(230,81,73,255);
        }
        Transform Render(string label,Transform parent,Mesh mesh,Material material)
        {
            if(!meshes.Contains(mesh))meshes.Add(mesh);
            var go=new GameObject(label);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;return go.transform;
        }
        Material Material(string hex,bool transparent=false,float alpha=1)
        {
            var result=new Material(Shader.Find(transparent?"ARES/Celestial":"ARES/Reference Toon"));
            ColorUtility.TryParseHtmlString(hex,out var color);color.a=alpha;result.SetColor("_BaseColor",color);materials.Add(result);return result;
        }
        void LateUpdate()
        {
            if(explorer==null)return;
            bool animate=!frontend.BlocksWorld&&!explorer.reducedMotion;
            if(animate)time+=Mathf.Min(Time.deltaTime,.05f);
            if(cleaner!=null)
            {
                var cleaning=frontend.Solar?.Cleaning;bool active=cleaning!=null&&cleaning.ActivePanel>=0&&cleaning.Progress>0&&!frontend.BlocksWorld;
                cleaner.gameObject.SetActive(active);
                if(active)
                {
                    Vector3 center=frontend.Solar.PanelCenter(cleaning.ActivePanel);
                    cleaner.position=center+solarRotation*new Vector3((cleaning.Progress-.5f)*3,.2f,2.5f);cleaner.rotation=solarRotation*Quaternion.Euler(0,180,0);
                    if(brush!=null)brush.localPosition=new Vector3(explorer.reducedMotion?0:Mathf.Sin(time*12)*.45f,.18f,-1);
                }
            }
            for(int i=0;i<crew.Length;i++)if(crew[i]!=null)
            {
                crew[i].gameObject.SetActive(Vector3.Distance(explorer.PlayerPosition,crewPositions[i])<22);
                crew[i].SetPositionAndRotation(crewPositions[i]+Vector3.up*Mathf.Sin(time*2+i+1)*.025f,Quaternion.Euler(0,180,0));
            }
            float a=2*Mathf.PI*((frontend.Campaign?.SolFraction??.35f)-.25f);
            float pitch=-.5f*(.35f/new Vector3(-Mathf.Cos(a),Mathf.Sin(a),.35f).magnitude);
            foreach(var panel in panels)if(panel!=null)panel.rotation=solarRotation*ReferenceSpace.Euler(pitch,0,0)*Quaternion.Inverse(solarRotation);
            radar.localRotation=Quaternion.Euler(0,explorer.reducedMotion?0:-time*.65f*Mathf.Rad2Deg,0);
            if(animate&&!steam.isPlaying)steam.Play();else if(!animate&&steam.isPlaying)steam.Pause();
            bool emergency=!string.IsNullOrEmpty(frontend.Campaign?.Alarm);
            alarm.transform.position=explorer.ControlledPosition+Vector3.up*6;
            alarm.intensity=emergency?(explorer.reducedMotion?3:3+Mathf.Sin(time*2)*.5f):0;
            Vector3 p=explorer.PlayerPosition;bool outside=true;
            foreach(var room in explorer.definition.layout.rooms)if(room.shape!="park"&&WorldNavigation.Inside(p.x,-p.z,room,.6f)){outside=false;break;}
            if(animate&&outside&&!explorer.IsDrivingRover&&Vector3.Distance(lastStep,p)>.7f)
            {
                int i=footprint++%prints.Length;born[i]=time;lastStep=p;
                var side=Vector3.Cross(Vector3.up,explorer.ControlledForward)*(i%2==0?.18f:-.18f);
                prints[i].SetPositionAndRotation(new Vector3(p.x,.012f,p.z)+side,Quaternion.LookRotation(explorer.ControlledForward));
            }
            for(int i=0;i<prints.Length;i++)
            {float fade=Mathf.Clamp01(1-(time-born[i])/12);prints[i].gameObject.SetActive(!explorer.reducedMotion&&fade>0);prints[i].localScale=new Vector3(fade,1,fade);}
            if(!outside||Vector3.Distance(lastStep,p)>4)lastStep=p;
        }
        void OnDestroy(){foreach(var mesh in meshes)Destroy(mesh);foreach(var material in materials)Destroy(material);}
    }
}
