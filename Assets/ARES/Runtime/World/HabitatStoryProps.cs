using System.Collections.Generic;
using Ares.Domain;
using Ares.UI;
using Ares.World.Rover;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Authored low-poly environmental cues. Reads mission state; never changes resources.
    /// Batched meshes live at room edges, outside the five-unit walking paths.</summary>
    public sealed class HabitatStoryProps : MonoBehaviour
    {
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Material> materials = new List<Material>();
        readonly Dictionary<Material, RoverMesh> batches = new Dictionary<Material, RoverMesh>();
        readonly List<Transform> oxygenCells = new List<Transform>(), batteryCells = new List<Transform>();
        Transform art;
        GameFrontend frontend;
        Material cream, ink, mint, gold, coral, blue;
        Renderer radioLamp;
        MaterialPropertyBlock tint;
        float nextRefresh;
        public int MeshCount => meshes.Count;

        void Start()
        {
            tint = new MaterialPropertyBlock();
            frontend = GetComponent<GameFrontend>();
            art = new GameObject("Habitat stories - authored props").transform; art.SetParent(transform, false);
            cream = MakeMaterial("Porcelain", "#E6E0CD"); ink = MakeMaterial("Graphite", "#414553");
            mint = MakeMaterial("Life support mint", "#46BEAC"); gold = MakeMaterial("Solar honey", "#F3C65B");
            coral = MakeMaterial("Mission coral", "#EA8968"); blue = MakeMaterial("Observatory blue", "#8B9DD5");
            TankBank(new Vector3(-8.8f, 0, -46.5f));
            BatteryCabinet(new Vector3(-9.5f, 0, -8.4f));
            PersonalCorner(new Vector3(8.5f, 0, -9));
            SampleBench(new Vector3(-8.3f, 0, 46.6f));
            ShelterSupplies(new Vector3(-51, 0, -5.9f));
            Flush();
        }
        Material MakeMaterial(string label, string hex)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "ARES Story / " + label };
            ColorUtility.TryParseHtmlString(hex, out var c); m.SetColor("_BaseColor", c); m.SetFloat("_Smoothness", .25f);
            materials.Add(m); return m;
        }
        RoverMesh Batch(Material m)
        { if (!batches.TryGetValue(m, out var b)) { b = new RoverMesh(); batches.Add(m, b); } return b; }
        void Box(Material m, Vector3 p, Vector3 size, float bevel = .08f) => Batch(m).Box(p, size, Quaternion.identity, bevel);
        void Pipe(Material m, Vector3 a, Vector3 b, float radius = .06f) => Batch(m).Tube(a, b, radius, 10);
        void Tank(Vector3 p, Material accent)
        {
            Batch(cream).Lathe(p + Vector3.up * 1.35f, Quaternion.FromToRotation(Vector3.right, Vector3.up),
                new[] { new Vector2(-1.2f, .22f), new Vector2(-1f, .49f), new Vector2(.8f, .49f), new Vector2(1.1f, .22f) }, 12);
            foreach (float y in new[] { .55f, 2.05f })
                Batch(accent).Lathe(p + Vector3.up * y, Quaternion.FromToRotation(Vector3.right, Vector3.up),
                    new[] {new Vector2(-.1f,.5f),new Vector2(.1f,.5f)}, 12);
            Pipe(ink, p + Vector3.up * 2.4f, p + Vector3.up * 2.7f, .1f);
            Pipe(accent, p + new Vector3(-.23f,2.65f,0), p + new Vector3(.23f,2.65f,0), .055f);
        }
        void TankBank(Vector3 p)
        {
            Box(ink, p + Vector3.up*.12f, new Vector3(4.4f,.24f,1.6f));
            for (int n = 0; n < 3; n++)
            {
                Vector3 t = p + Vector3.right * (n - 1) * 1.35f; Tank(t, mint);
                Pipe(mint,t+new Vector3(0,2.7f,0),t+new Vector3(0,2.7f,.65f));
            }
            Pipe(mint,p+new Vector3(-1.35f,2.7f,.65f),p+new Vector3(1.35f,2.7f,.65f));
            Gauge(p+new Vector3(0,3.05f,.3f), mint, oxygenCells);
        }
        void BatteryCabinet(Vector3 p)
        {
            Box(ink,p+Vector3.up*1.4f,new Vector3(3.2f,2.8f,1.4f),.18f);
            Box(cream,p+new Vector3(0,1.4f,.72f),new Vector3(2.9f,2.5f,.12f));
            for(int i=0;i<3;i++)
            {
                Box(gold,p+new Vector3((i-1)*.86f,1.3f,.84f),new Vector3(.64f,1.4f,.2f));
                Box(ink,p+new Vector3((i-1)*.86f,2.04f,.84f),new Vector3(.28f,.14f,.2f));
                for(int j=0;j<3;j++) Box(cream,p+new Vector3((i-1)*.86f,.9f+j*.3f,.96f),new Vector3(.42f,.07f,.04f),.01f);
            }
            Gauge(p+new Vector3(0,3.1f,.4f),gold,batteryCells);
            Pipe(gold,p+new Vector3(1.6f,.4f,0),p+new Vector3(2.1f,.4f,0),.09f);
            Pipe(gold,p+new Vector3(2.1f,.4f,0),p+new Vector3(2.1f,.1f,1.4f),.09f);
        }
        void PersonalCorner(Vector3 p)
        {
            // An inhabited base: meal trays, mugs and crew lockers, rather than floating instructions.
            Box(cream,p+Vector3.up*1.15f,new Vector3(3.6f,.18f,1.5f));
            foreach(float x in new[]{-1.5f,1.5f}) Box(ink,p+new Vector3(x,.55f,0),new Vector3(.16f,1.1f,1));
            for(int i=0;i<2;i++)
            {
                Vector3 q=p+new Vector3(i==0?-.9f:.9f,1.3f,0);
                Box(mint,q,new Vector3(1.1f,.08f,.8f));
                for(int j=0;j<3;j++) Box(coral,q+new Vector3(-.3f+j*.3f,.1f,0),new Vector3(.22f,.15f,.3f));
                Pipe(cream,q+new Vector3(.45f,.1f,-.25f),q+new Vector3(.45f,.4f,-.25f),.13f);
            }
            for(int i=0;i<3;i++)
            {
                var q=p+new Vector3(-1.2f+i*1.2f,0,-1.3f);
                Box(cream,q+Vector3.up*1.6f,new Vector3(1.08f,3.2f,.6f));
                Box(i==1?mint:coral,q+new Vector3(0,2.5f,.32f),new Vector3(.8f,.4f,.04f));
                Pipe(ink,q+new Vector3(.3f,1.4f,.38f),q+new Vector3(.3f,1.8f,.38f),.035f);
            }
        }
        void SampleBench(Vector3 p)
        {
            Box(cream,p+Vector3.up*1.1f,new Vector3(3.6f,.2f,1.6f));
            foreach(float x in new[]{-1.5f,1.5f}) Box(ink,p+new Vector3(x,.5f,0),new Vector3(.15f,1,1.3f));
            for(int i=0;i<4;i++)
            {
                var q=p+new Vector3(-1.25f+i*.8f,1.27f,0);
                Box(blue,q,new Vector3(.68f,.12f,.9f));
                Batch(coral).Lathe(q+Vector3.up*.23f,Quaternion.Euler(17,i*23,8),
                    new[]{new Vector2(-.2f,.08f),new Vector2(-.08f,.23f),new Vector2(.16f,.16f),new Vector2(.24f,0)},5);
            }
            Box(ink,p+new Vector3(0,2.1f,-.7f),new Vector3(3.1f,1.3f,.16f));
            for(int i=0;i<7;i++) Pipe(blue,p+new Vector3(-1.3f+i*.43f,1.7f,-.58f),p+new Vector3(-1.3f+i*.43f,2.1f+(i%3)*.25f,-.58f),.04f);
        }
        void ShelterSupplies(Vector3 p)
        {
            for(int row=0;row<2;row++) for(int i=0;i<3;i++)
            {
                Vector3 q=p+new Vector3((i-1)*1.3f,.45f+row*.85f,0);
                Box(cream,q,new Vector3(1.2f,.76f,1.1f),.12f);
                Box(blue,q+new Vector3(0,0,.57f),new Vector3(.22f,.65f,.06f));
                Box(blue,q+new Vector3(0,0,.57f),new Vector3(.7f,.18f,.06f));
            }
            // A status antenna on the supplies cabinet mirrors the actual radio connection.
            Pipe(ink,p+new Vector3(2,0,0),p+new Vector3(2,2.7f,0),.08f);
            var mesh=new RoverMesh();mesh.Box(Vector3.zero,new Vector3(.45f,.28f,.4f),Quaternion.identity,.06f);
            radioLamp=Part("Radio signal lamp",mesh,mint,p+new Vector3(2,2.8f,0)).GetComponent<Renderer>();
        }
        void Gauge(Vector3 p, Material color, List<Transform> cells)
        {
            Box(ink,p,new Vector3(2.6f,.56f,.26f));
            for(int i=0;i<5;i++)
            {
                var mesh=new RoverMesh();mesh.Box(Vector3.zero,new Vector3(.38f,.34f,.12f),Quaternion.identity,.04f);
                mesh.Box(new Vector3(0,0,-.4f),new Vector3(.38f,.34f,.12f),Quaternion.identity,.04f);
                cells.Add(Part("Resource segment",mesh,color,p+new Vector3(-.94f+i*.47f,0,.2f)));
            }
        }
        Transform Part(string label,RoverMesh builder,Material m,Vector3 p)
        {
            var go=new GameObject(label);go.transform.SetParent(art,false);go.transform.localPosition=p;
            var mesh=builder.Bake(label);meshes.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=m;return go.transform;
        }
        void Flush() {foreach(var b in batches)Part(b.Key.name,b.Value,b.Key,Vector3.zero);batches.Clear();}
        void Update()
        {
            if(Time.unscaledTime<nextRefresh||frontend?.Campaign==null)return; nextRefresh=Time.unscaledTime+.25f;
            var c=frontend.Campaign; Fill(oxygenCells,c.OxygenSystem.Percent/100);Fill(batteryCells,c.Solar.ReserveKwh/c.Solar.CapacityKwh);
            if(radioLamp!=null){tint.SetColor("_BaseColor",c.Communications.Connected?new Color(.28f,.8f,.65f):new Color(1,.35f,.15f));radioLamp.SetPropertyBlock(tint);}
        }
        static void Fill(List<Transform> cells,float fraction)
        {for(int i=0;i<cells.Count;i++)cells[i].localScale=new Vector3(1,Mathf.Max(.12f,Mathf.Clamp01(fraction*cells.Count-i)),1);}
        void OnDestroy()
        {if(art!=null)Destroy(art.gameObject);foreach(var m in meshes)Destroy(m);foreach(var m in materials)Destroy(m);}
    }
}
