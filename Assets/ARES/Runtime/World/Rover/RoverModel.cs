using System.Collections.Generic;
using UnityEngine;

namespace Ares.World.Rover
{
    /// <summary>ARES Nomad: authored chamfered hull, six profiled tyres, rocker arms and science deck.
    /// Static details are batched by material; only suspension, wheel steering/spin and body lean move.</summary>
    public sealed class RoverModel : MonoBehaviour
    {
        public Transform[] Wheels { get; private set; }
        public Transform Body { get; private set; }
        readonly List<Object> owned = new List<Object>();
        readonly Dictionary<Material, RoverMesh> batches = new Dictionary<Material, RoverMesh>();
        readonly Transform[] steering = new Transform[6];
        readonly Vector3[] wheelCenters = new Vector3[6];
        Material ivory, orange, dark, metal, rubber, solar, light, red;
        float wheelRadius;
        public void Build(RoverSettings settings)
        {
            wheelRadius = settings.wheelRadius;
            var explorer=GetComponentInParent<ReferenceExplorer>();
            if(explorer!=null) foreach(var part in explorer.GetComponentsInChildren<ReferenceWorldPart>())
                if(part.role=="reference_rover" || part.role=="rover_wheel") part.gameObject.SetActive(false);
            ivory = Material("Ceramic / warm ivory", new Color(.84f,.84f,.72f), .28f, .42f);
            orange = Material("Safety / burnt orange", new Color(.92f,.27f,.075f), .3f, .38f);
            dark = Material("Frame / graphite", new Color(.085f,.13f,.16f), .55f, .38f);
            metal = Material("Machined titanium", new Color(.4f,.53f,.56f), .8f, .65f);
            rubber = Material("Tyres / basalt", new Color(.055f,.067f,.075f), .05f, .16f);
            solar = Material("Photovoltaic / indigo", new Color(.045f,.15f,.28f), .6f, .72f);
            light = Material("Optics / ice", new Color(.42f,.94f,1), .25f, .8f, true);
            red = Material("Tail / amber", new Color(1,.19f,.035f), .2f, .6f, true);
            Body = new GameObject("Sprung chassis").transform; Body.SetParent(transform, false);

            Box(dark, new Vector3(0,.83f,0), new Vector3(1.85f,.34f,3.45f), .14f);
            Box(ivory, new Vector3(0,1.15f,.05f), new Vector3(2.04f,.58f,3.25f), .2f);
            Box(orange, new Vector3(0,1.43f,.05f), new Vector3(2.06f,.12f,2.95f), .04f);
            // Sloped instrument nose and two inset sensor windows.
            Box(ivory, new Vector3(0,1.47f,1.18f), new Vector3(1.8f,.42f,.75f), .12f, Quaternion.Euler(16,0,0));
            Box(dark, new Vector3(0,1.48f,1.56f), new Vector3(1.5f,.23f,.09f), .025f, Quaternion.Euler(16,0,0));
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * 1.04f;
                Box(dark, new Vector3(x,1.1f,-.13f), new Vector3(.07f,.3f,1.72f), .025f);
                for (int v = 0; v < 9; v++) Box(metal, new Vector3(x*1.04f,1.1f,-.83f+v*.17f), new Vector3(.04f,.21f,.035f), .008f, Quaternion.Euler(0,0,side*20));
                Box(orange, new Vector3(x,1.16f,1.01f), new Vector3(.09f,.29f,.45f), .035f);
                Tube(dark, new Vector3(side*.95f,.74f,1.77f), new Vector3(side*.95f,1.02f,1.77f), .07f);
                Tube(metal, new Vector3(side*.78f,1.56f,-1.28f), new Vector3(side*.78f,1.56f,.18f), .035f);
                Tube(metal, new Vector3(side*.78f,1.4f,-1.28f), new Vector3(side*.78f,1.56f,-1.28f), .035f);
                Box(light, new Vector3(side*.63f,1.48f,1.62f), new Vector3(.32f,.105f,.06f), .02f);
                Box(red, new Vector3(side*.76f,1.15f,-1.64f), new Vector3(.27f,.12f,.07f), .02f);
                // Fender eyebrows and exposed double wishbones.
                for (int axle = 0; axle < 3; axle++)
                {
                    float z = 1.3f - axle*1.3f;
                    Box(ivory, new Vector3(side*1.32f,1.23f,z), new Vector3(.61f,.12f,.83f), .05f);
                    Box(orange, new Vector3(side*1.64f,1.19f,z), new Vector3(.06f,.13f,.7f), .02f);
                    Tube(dark, new Vector3(side*.74f,.78f,z-.3f), new Vector3(side*1.37f,.56f,z), .07f);
                    Tube(metal, new Vector3(side*.74f,.94f,z+.25f), new Vector3(side*1.37f,.56f,z), .045f);
                    Vector3 a = new Vector3(side*.96f,1.09f,z-.22f), b = new Vector3(side*1.3f,.62f,z);
                    Tube(metal,a,b,.045f);
                    for (int ring = 0; ring < 7; ring++)
                    {
                        var p = Vector3.Lerp(a,b,.15f+ring*.1f);
                        batches.TryAdd(orange, new RoverMesh());
                        batches[orange].Lathe(p,Quaternion.FromToRotation(Vector3.right,b-a),new[] {new Vector2(-.014f,.07f),new Vector2(.014f,.07f)},12);
                    }
                }
                Tube(dark,new Vector3(side*1.1f,.8f,-1.3f),new Vector3(side*1.1f,.8f,1.3f),.075f);
            }
            Tube(dark, new Vector3(-1.05f,.91f,1.82f),new Vector3(1.05f,.91f,1.82f),.09f);
            Tube(dark, new Vector3(-1.05f,.88f,-1.82f),new Vector3(1.05f,.88f,-1.82f),.075f);
            // Two framed cell arrays; individual cells and conducting lines catch highlights.
            for (int side = -1; side <= 1; side += 2)
            {
                Box(metal,new Vector3(side*.48f,1.53f,-.52f),new Vector3(.88f,.08f,1.49f),.035f);
                for(int row=0;row<5;row++) for(int col=0;col<3;col++)
                {
                    Vector3 p = new Vector3(side*.48f+(col-1)*.265f,1.58f,-1.1f+row*.29f);
                    Box(solar,p,new Vector3(.25f,.025f,.272f),.005f);
                    Box(metal,p+Vector3.up*.015f,new Vector3(.007f,.005f,.255f),.001f);
                }
            }
            Box(ivory,new Vector3(0,1.75f,.56f),new Vector3(.64f,.4f,.65f),.08f);
            Tube(dark,new Vector3(0,1.84f,.56f),new Vector3(0,2.56f,.56f),.085f);
            Tube(orange,new Vector3(0,1.86f,.56f),new Vector3(0,2.05f,.56f),.12f);
            Box(ivory,new Vector3(0,2.56f,.57f),new Vector3(.88f,.34f,.36f),.08f);
            Box(dark,new Vector3(0,2.56f,.765f),new Vector3(.72f,.23f,.04f),.025f);
            foreach(float x in new[]{-.24f,.24f})
            {
                Tube(metal,new Vector3(x,2.56f,.77f),new Vector3(x,2.56f,.85f),.105f,24);
                Tube(light,new Vector3(x,2.56f,.851f),new Vector3(x,2.56f,.866f),.073f,24);
            }
            // Radio mast and rear cargo canisters.
            Tube(metal,new Vector3(-.81f,1.5f,-1.28f),new Vector3(-.81f,2.78f,-1.28f),.018f);
            Tube(orange,new Vector3(-.81f,2.6f,-1.28f),new Vector3(-.81f,2.82f,-1.28f),.035f);
            for(int i=0;i<3;i++)
            {
                float x=(i-1)*.42f;
                Box(dark,new Vector3(x,1.61f,-1.49f),new Vector3(.32f,.27f,.28f),.04f);
                Box(orange,new Vector3(x,1.76f,-1.49f),new Vector3(.33f,.045f,.29f),.01f);
            }
            Flush(Body, "Chassis");
            Label("ARES",new Vector3(0,1.16f,1.683f),Quaternion.Euler(0,180,0),.036f);
            Label("NOMAD / 06",new Vector3(0,1.17f,-1.683f),Quaternion.identity,.021f);

            Wheels = new Transform[6]; int index=0;
            for(int side=-1;side<=1;side+=2) for(int axle=0;axle<3;axle++)
            {
                Vector3 center = new Vector3(side*1.38f,wheelRadius,1.3f-axle*1.3f); wheelCenters[index]=center;
                var pivot = new GameObject("Steering " + index).transform; pivot.SetParent(transform,false); pivot.localPosition=center; steering[index]=pivot;
                var spin = new GameObject("Wheel " + index).transform; spin.SetParent(pivot,false); Wheels[index]=spin;
                var tyre = new RoverMesh();
                tyre.Lathe(Vector3.zero,Quaternion.identity,new[]{new Vector2(-.25f,wheelRadius*.53f),new Vector2(-.25f,wheelRadius*.8f),new Vector2(-.17f,wheelRadius*.97f),new Vector2(.17f,wheelRadius*.97f),new Vector2(.25f,wheelRadius*.8f),new Vector2(.25f,wheelRadius*.53f)},40);
                for(int tread=0;tread<28;tread++) for(int half=-1;half<=1;half+=2)
                {
                    float a=tread*360f/28;
                    Quaternion q=Quaternion.Euler(a,0,0);
                    tyre.Box(q*new Vector3(half*.105f,wheelRadius*.97f,0),new Vector3(.235f,.055f,.085f),q*Quaternion.Euler(0,half*25,0),.012f);
                }
                Attach(spin,"Chevron tyre",tyre,rubber);
                var hub=new RoverMesh();
                hub.Lathe(Vector3.zero,Quaternion.identity,new[]{new Vector2(-.252f,0),new Vector2(-.252f,wheelRadius*.58f),new Vector2(.252f,wheelRadius*.58f),new Vector2(.252f,0)},32);
                Attach(spin,"Machined hub",hub,metal);
                var inset=new RoverMesh();
                inset.Tube(new Vector3(-.265f,0,0),new Vector3(.265f,0,0),.2f,24);
                for(int bolt=0;bolt<8;bolt++)
                {
                    float a=bolt*Mathf.PI/4; Vector3 p=new Vector3(side*.261f,Mathf.Cos(a)*.257f,Mathf.Sin(a)*.257f);
                    inset.Tube(p,p+Vector3.right*side*.018f,.032f,6);
                }
                Attach(spin,"Hub bolts and motor",inset,dark);
                var cap=new RoverMesh(); cap.Tube(new Vector3(-.28f,0,0),new Vector3(.28f,0,0),.113f,16); Attach(spin,"Orange axle cap",cap,orange);
                index++;
            }
        }
        public void Animate(RoverMotion motion, float dt, bool reducedMotion)
        {
            float spin = motion.Travel / Mathf.Max(.1f,wheelRadius) * Mathf.Rad2Deg;
            for(int i=0;i<6;i++)
            {
                int axle=i%3;
                steering[i].localRotation=Quaternion.Euler(0,motion.Steering*(axle==0?28:axle==2?-14:0),0);
                Wheels[i].localRotation=Quaternion.Euler(spin,0,0);
                float travel = reducedMotion ? 0 : Mathf.Sin(motion.Travel*4+i*1.7f)*.018f*Mathf.Clamp01(Mathf.Abs(motion.Speed));
                steering[i].localPosition=wheelCenters[i]+Vector3.up*travel;
            }
            var lean = reducedMotion ? Quaternion.identity : Quaternion.Euler(Mathf.Clamp(-motion.Speed*.15f,-1.8f,1.8f),0,-motion.Steering*Mathf.Abs(motion.Speed)*.25f);
            Body.localRotation=Quaternion.Slerp(Body.localRotation,lean,1-Mathf.Exp(-8*dt));
        }
        Material Material(string label, Color color, float metallic, float smoothness, bool emission=false)
        {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")) {name="Nomad / "+label};
            material.SetColor("_BaseColor",color); material.SetFloat("_Metallic",metallic); material.SetFloat("_Smoothness",smoothness);
            if(emission) { material.SetColor("_EmissionColor",color*1.5f); material.EnableKeyword("_EMISSION"); }
            owned.Add(material); return material;
        }
        void Box(Material material,Vector3 p,Vector3 size,float bevel,Quaternion? rotation=null)
        { if(!batches.ContainsKey(material))batches.Add(material,new RoverMesh()); batches[material].Box(p,size,rotation??Quaternion.identity,bevel); }
        void Tube(Material material,Vector3 a,Vector3 b,float radius,int sides=12)
        { if(!batches.ContainsKey(material))batches.Add(material,new RoverMesh()); batches[material].Tube(a,b,radius,sides); }
        void Flush(Transform parent,string name)
        { foreach(var pair in batches)Attach(parent,name+" / "+pair.Key.name,pair.Value,pair.Key); batches.Clear(); }
        void Attach(Transform parent,string label,RoverMesh source,Material material)
        {
            var go=new GameObject(label); go.transform.SetParent(parent,false);
            var mesh=source.Bake(label); owned.Add(mesh); go.AddComponent<MeshFilter>().sharedMesh=mesh; go.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        void Label(string text,Vector3 p,Quaternion rotation,float size)
        {
            var go=new GameObject("Hull stencil / "+text); go.transform.SetParent(Body,false); go.transform.localPosition=p; go.transform.localRotation=rotation;
            var label=go.AddComponent<TextMesh>(); label.text=text; label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center;
            label.fontSize=64; label.characterSize=size; label.color=new Color(.09f,.14f,.17f);
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.font.RequestCharactersInTexture(text,64);
            var stencil=new Material(Resources.Load<Shader>("ARES/Effects/RoverStencil")) {name="Nomad / hull stencil"};
            stencil.mainTexture=label.font.material.mainTexture; owned.Add(stencil); go.GetComponent<MeshRenderer>().sharedMaterial=stencil;
        }
        void OnDestroy() { foreach(var resource in owned)if(resource!=null)Destroy(resource); }
    }
}
