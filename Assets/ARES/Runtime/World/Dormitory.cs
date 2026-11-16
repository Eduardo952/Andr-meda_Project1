using System.Collections.Generic;
using Ares.Data;
using Ares.World.Rover;
using UnityEngine;

namespace Ares.World
{
    // A quiet sleeping area inside life support; keeps the radial base and its passages intact.
    public sealed class Dormitory
    {
        readonly Vector3 center;
        public readonly Vector3[] Beds = new Vector3[4];
        public Dormitory(LayoutData layout)
        {
            center = layout.Room("lifeSupport").Center;
            // Two banks along the sides leave the central route and existing terminal clear.
            for (int i = 0; i < 4; i++) Beds[i] = center + new Vector3(i < 2 ? -7.5f : 7.5f, 0, (i % 2 == 0 ? -3.2f : 3.2f));
        }
        public Vector3 Stand(int i) => Beds[i] + Vector3.right * (i < 2 ? 2.1f : -2.1f);
        public bool Blocked(Vector3 p)
        {
            foreach (var bed in Beds) if (Mathf.Abs(p.x-bed.x)<1.35f && Mathf.Abs(p.z-bed.z)<2.45f) return true;
            return false;
        }
        public int Near(Vector3 p)
        { for(int i=0;i<4;i++) if(Vector3.Distance(p,Stand(i))<1.1f) return i; return -1; }
    }

    public enum RestStage { Free, Sleeping, Waking, LyingDown, Resting }

    /// <summary>Visual rest only. Never awards resources or advances the mission clock.</summary>
    public sealed class BedSequence
    {
        public RestStage Stage { get; private set; }
        public bool Busy => Stage != RestStage.Free;
        public float Lie { get; private set; }
        public float Stretch { get; private set; }
        public int Bed { get; private set; }
        public float FadeOpacity { get; private set; }
        float time, wakeTime;
        public void Start() { Bed=0; Stage=RestStage.Sleeping; Lie=1; Stretch=0; time=wakeTime=0; FadeOpacity=1; }
        public void Cancel() { Stage=RestStage.Free; Lie=Stretch=time=wakeTime=FadeOpacity=0; }
        public void Rest(int bed) { Bed=bed; Stage=RestStage.LyingDown; time=0; FadeOpacity=0; }
        public void Wake() { if(Stage==RestStage.Resting) { Stage=RestStage.Waking; time=wakeTime=0; FadeOpacity=1; } }
        public void Tick(float dt, bool reduced)
        {
            time+=dt;
            if(Stage==RestStage.Sleeping || Stage==RestStage.Waking) {
                wakeTime+=dt;
                FadeOpacity=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((wakeTime-.2f)/(reduced?.25f:2f)));
            }
            if(Stage==RestStage.Sleeping && time>=1) { Stage=RestStage.Waking; time=0; }
            if(Stage==RestStage.Waking)
            {
                float t=Mathf.Clamp01(time/(reduced?.5f:2.8f));
                Lie=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(t*1.6f));
                Stretch=reduced?0:Mathf.Sin(Mathf.PI*Mathf.Clamp01((t-.5f)*2));
                if(t>=1) {Stage=RestStage.Free; Lie=Stretch=FadeOpacity=0;}
            }
            if(Stage==RestStage.LyingDown)
            { Lie=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/(reduced?.3f:1.5f))); if(Lie>=1)Stage=RestStage.Resting; }
        }
        public void Apply(AstronautAnimator actor, Dormitory dorm)
        {
            if(!Busy)return;
            var feet=dorm.Beds[Bed]+new Vector3(0,1.4f,-1.1f);
            actor.transform.position=Vector3.Lerp(dorm.Stand(Bed),feet,Lie);
            actor.transform.rotation=Quaternion.Euler(90*Lie,0,0);
            actor.Apply(new AstronautPose { leftArm=-3*Stretch,rightArm=-3*Stretch });
        }
    }

    [DefaultExecutionOrder(-10)]
    public sealed class DormitoryArt : MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        void Start()
        {
            var dorm=new Dormitory(GetComponent<ReferenceExplorer>().definition.layout);
            KeepOnlyTerminal();
            Color[] colors={new Color(.97f,.71f,.78f),new Color(.66f,.83f,.96f),new Color(.98f,.89f,.63f),new Color(.80f,.72f,.94f)};
            for(int i=0;i<4;i++)
            {
                var frame=new RoverMesh(); var linen=new RoverMesh(); var pillow=new RoverMesh(); var p=dorm.Beds[i];
                frame.Box(p+Vector3.up*.38f,new Vector3(2.2f,.5f,4.4f),Quaternion.identity,.18f);
                frame.Box(p+new Vector3(0,.9f,2.1f),new Vector3(2.2f,1.4f,.22f),Quaternion.identity,.1f);
                linen.Box(p+Vector3.up*.77f,new Vector3(2.1f,.32f,4.15f),Quaternion.identity,.14f);
                pillow.Box(p+new Vector3(0,1.02f,1.45f),new Vector3(1.65f,.23f,.85f),Quaternion.identity,.11f);
                Add(frame,new Color(.17f,.23f,.3f),"Bed frame "+i);Add(linen,colors[i],"Bed blanket "+i);Add(pillow,Color.white,"Bed pillow "+i);
            }
        }
        void KeepOnlyTerminal()
        {
            // The imported interior combines terminal, six tanks and connecting pipes by material.
            // Retain only the terminal's original triangles in runtime copies, before static batching.
            // Its authored enclosure is 1.7 m to the right of the station interaction marker.
            var station=GetComponent<ReferenceExplorer>().definition.layout.Room("lifeSupport").Station;
            var terminal=new Bounds(station+new Vector3(1.7f,1.5f,-.1f),new Vector3(2.2f,3f,1.3f));
            var interior=transform.Find("lifeSupport_interior");
            if(interior==null)return;
            foreach(var filter in interior.GetComponentsInChildren<MeshFilter>())
            {
                var source=filter.sharedMesh;if(source==null)continue;
                var vertices=source.vertices;var normals=source.normals;var uv=source.uv;
                var kept=new List<int>();var points=new List<Vector3>();var directions=new List<Vector3>();var tex=new List<Vector2>();
                var remap=new Dictionary<int,int>();var triangles=source.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                {
                    bool keep=true;
                    for(int j=0;j<3;j++)if(!terminal.Contains(filter.transform.TransformPoint(vertices[triangles[i+j]]))) {keep=false;break;}
                    if(!keep)continue;
                    for(int j=0;j<3;j++) {
                        int index=triangles[i+j];
                        if(!remap.TryGetValue(index,out int mapped)) {
                            mapped=points.Count;remap.Add(index,mapped);points.Add(vertices[index]);
                            if(normals.Length==vertices.Length)directions.Add(normals[index]);
                            if(uv.Length==vertices.Length)tex.Add(uv[index]);
                        }
                        kept.Add(mapped);
                    }
                }
                var mesh=new Mesh {name=source.name+" / dormitory terminal only"};
                mesh.SetVertices(points);mesh.SetTriangles(kept,0);
                if(directions.Count==points.Count)mesh.SetNormals(directions);else mesh.RecalculateNormals();
                if(tex.Count==points.Count)mesh.SetUVs(0,tex);
                mesh.RecalculateBounds();meshes.Add(mesh);filter.sharedMesh=mesh;
            }
        }
        void Add(RoverMesh source,Color color,string name)
        {
            var material=new Material(Shader.Find("ARES/Reference Toon"));material.SetColor("_BaseColor",color);materials.Add(material);
            var mesh=MissionGeometry.Finish(source,name);meshes.Add(mesh);
            MissionGeometry.Render(transform,name,mesh,material,Vector3.zero,Vector3.one);
        }
        void OnDestroy(){foreach(var m in materials)Destroy(m);foreach(var m in meshes)Destroy(m);}
    }
}
