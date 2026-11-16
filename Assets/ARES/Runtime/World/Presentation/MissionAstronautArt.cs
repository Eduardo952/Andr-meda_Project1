using System.Collections.Generic;
using Ares.UI;
using UnityEngine;
using UnityEngine.Rendering;
using Ares.World.Rover;

namespace Ares.World
{
    /// <summary>Expressive helmet and suit details attached to the existing animation pivots.</summary>
    [DefaultExecutionOrder(20)]
    public sealed class MissionAstronautArt : MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Transform> eyes=new List<Transform>();
        Transform head;Mesh sphere;ReferenceExplorer explorer;float time;
        void Start()
        {
            explorer=GetComponent<ReferenceExplorer>();var animator=GetComponentInChildren<AstronautAnimator>();if(animator==null)return;
            sphere=MissionGeometry.Sphere();meshes.Add(sphere);var body=animator.body;
            var ink=Material("Visor / midnight",MissionVisualStyle.Ink,.64f);
            var skin=Material("Explorer face",MissionVisualStyle.Hex("#dba378"),.15f);
            var white=Material("Suit / warm ceramic",MissionVisualStyle.Cream,.42f);
            var teal=Material("Suit / teal seams",MissionVisualStyle.Teal,.48f);
            var coral=Material("Suit / coral details",MissionVisualStyle.Coral,.28f);
            var gold=Material("Suit / discovery badge",MissionVisualStyle.Gold,.5f);
            var hair=Material("Explorer hair",MissionVisualStyle.Hex("#564342"),.2f);
            var joints=Material("Suit / flexible joints",MissionVisualStyle.Hex("#526370"),.2f);
            head=new GameObject("Expressive helmet pivot").transform;head.SetParent(body,false);head.localPosition=new Vector3(0,1.82f,0);
            // Replace the closed opaque sphere and its overlapping visor/highlights as a unit.
            // The three new surfaces share exact boundary vertices, with no coplanar layers.
            foreach(var renderer in animator.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.name=="astronaut_3"||renderer.name=="astronaut_4"||renderer.name=="astronaut_5"||renderer.name=="astronaut_6")renderer.enabled=false;
                if(renderer.name=="astronaut_1") { renderer.sharedMaterial=white;renderer.transform.localScale=new Vector3(1.1f,1,1.08f); }
                if(renderer.name=="astronaut_11"||renderer.name=="astronaut_15"||renderer.name=="astronaut_19"||renderer.name=="astronaut_22")renderer.sharedMaterial=white;
                if(renderer.name=="astronaut_13"||renderer.name=="astronaut_17")renderer.sharedMaterial=teal;
                if(renderer.name=="astronaut_2") { renderer.sharedMaterial=teal;renderer.transform.localPosition=new Vector3(0,1.05f,-.445f);renderer.GetComponent<MeshFilter>().sharedMesh=RoundedBox(new Vector3(.38f,.24f,.065f),.025f); }
                if(renderer.name=="astronaut_8") { renderer.sharedMaterial=white;renderer.GetComponent<MeshFilter>().sharedMesh=RoundedBox(new Vector3(.62f,.65f,.30f),.065f); }
                if(renderer.name=="astronaut_20"||renderer.name=="astronaut_23") { renderer.sharedMaterial=joints;renderer.GetComponent<MeshFilter>().sharedMesh=RoundedBox(new Vector3(.37f,.22f,.48f),.065f); }
            }
            HelmetSurface("Pressure helmet shell",60,180,white);
            var lining=Material("Helmet / inner pressure lining",MissionVisualStyle.Hex("#45555e"),.15f);
            lining.SetFloat("_Cull",(float)CullMode.Front);
            HelmetSurface("Inner helmet lining",60,180,lining).localScale=Vector3.one*.97f;
            HelmetSurface("Visor perimeter seal",55,60,joints);
            var glass=Material("Helmet / smoke blue translucent glass",new Color(.14f,.24f,.30f,.27f),.94f);
            glass.SetFloat("_Surface",1);glass.SetFloat("_ZWrite",0);
            glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=(int)RenderQueue.Transparent;
            glass.SetShaderPassEnabled("ShadowCaster",false);glass.SetShaderPassEnabled("DepthOnly",false);
            var visor=HelmetSurface("Translucent visor",0,55,glass).GetComponent<MeshRenderer>();
            visor.shadowCastingMode=ShadowCastingMode.Off;visor.receiveShadows=false;
            Detail(head,"Pressure hood",joints,new Vector3(0,0,.015f),new Vector3(.33f,.36f,.29f));
            Detail(head,"Face inside pressure helmet",skin,new Vector3(0,-.01f,-.12f),new Vector3(.27f,.30f,.27f));
            Detail(head,"Soft hair fringe",hair,new Vector3(-.05f,.20f,-.295f),new Vector3(.20f,.08f,.07f));
            Detail(head,"Side hair curl",hair,new Vector3(.18f,.16f,-.27f),new Vector3(.06f,.10f,.05f));
            for(int side=-1;side<=1;side+=2)
            {
                eyes.Add(Detail(head,"Expressive eye",ink,new Vector3(side*.095f,.04f,-.378f),new Vector3(.026f,.043f,.014f)));
                Detail(head,"Eye glint",white,new Vector3(side*.095f-.007f,.053f,-.392f),new Vector3(.007f,.009f,.004f));
                Detail(head,"Warm cheek",coral,new Vector3(side*.17f,-.065f,-.328f),new Vector3(.033f,.019f,.014f));
                Detail(head,"Helmet communication pod",white,new Vector3(side*.62f,0,.025f),new Vector3(.075f,.13f,.14f));
                Detail(head,"Helmet locking dial",teal,new Vector3(side*.70f,0,.025f),new Vector3(.018f,.075f,.08f));
                Detail(body,"Shoulder pressure pad",white,new Vector3(side*.46f,1.24f,0),new Vector3(.16f,.18f,.22f));
                Detail(body,"Harness strap",joints,new Vector3(side*.26f,1.19f,-.31f),new Vector3(.038f,.23f,.035f));
            }
            Detail(head,"Nose",skin,new Vector3(0,-.02f,-.387f),new Vector3(.035f,.046f,.033f));
            Detail(head,"Smile",ink,new Vector3(0,-.11f,-.377f),new Vector3(.059f,.023f,.012f));
            Detail(head,"Smile upper lip",skin,new Vector3(0,-.096f,-.386f),new Vector3(.060f,.017f,.009f));
            Detail(body,"Discovery badge",gold,new Vector3(-.12f,1.10f,-.49f),new Vector3(.045f,.045f,.018f));
            for(int i=0;i<3;i++)Detail(body,"Suit life indicator",i==2?gold:white,new Vector3(.025f+i*.055f,1.07f,-.49f),new Vector3(.014f,.027f,.012f));
            Detail(body,"Collar pressure seal",joints,new Vector3(0,1.39f,0),new Vector3(.36f,.09f,.33f));
            Detail(body,"Collar locking ring",white,new Vector3(0,1.43f,0),new Vector3(.38f,.045f,.35f));
            foreach(var arm in new[]{animator.leftArm,animator.rightArm})
                Detail(arm,"Glove pressure cuff",joints,new Vector3(0,-.44f,0),new Vector3(.195f,.055f,.195f));
            foreach(var leg in new[]{animator.leftLeg,animator.rightLeg})
            {
                Detail(leg,"Rounded knee reinforcement",teal,new Vector3(0,-.22f,-.205f),new Vector3(.13f,.12f,.04f));
                Detail(leg,"Boot upper cuff",white,new Vector3(0,-.39f,0),new Vector3(.215f,.055f,.215f));
            }
            Detail(body,"Backpack access panel",teal,new Vector3(0,1.12f,.607f),new Vector3(.19f,.20f,.028f));
            for(int i=0;i<3;i++)Detail(body,"Backpack vent",joints,new Vector3(0,.85f+i*.055f,.608f),new Vector3(.17f,.012f,.018f));
            // Crew use baked local meshes, so add expressions in each rig's existing coordinate space.
            int crewIndex=0;
            foreach(var rig in GetComponentsInChildren<ReferenceRig>())if(rig.id.StartsWith("crew_"))
            {
                var face=Material("Crew face "+crewIndex,crewIndex++==1?MissionVisualStyle.Hex("#bd815f"):MissionVisualStyle.Hex("#e5b18b"),.2f);
                Detail(rig.transform,"Crew friendly face",face,new Vector3(0,1.80f,-.495f),new Vector3(.245f,.22f,.045f));
                for(int side=-1;side<=1;side+=2)Detail(rig.transform,"Crew eye",ink,new Vector3(side*.085f,1.83f,-.537f),new Vector3(.021f,.035f,.009f));
                Detail(rig.transform,"Crew smile",ink,new Vector3(0,1.73f,-.539f),new Vector3(.046f,.018f,.009f));
            }
        }
        Mesh RoundedBox(Vector3 size,float bevel)
        {var author=new RoverMesh();author.Box(Vector3.zero,size,Quaternion.identity,bevel);var mesh=MissionGeometry.Finish(author,"Suit rounded pressure housing");meshes.Add(mesh);return mesh;}
        Transform HelmetSurface(string name,float startDegrees,float endDegrees,Material material)
        {
            const int sides=48;int rows=Mathf.Max(2,Mathf.CeilToInt((endDegrees-startDegrees)/5));
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            var radius=new Vector3(.55f,.52f,.53f);
            for(int row=0;row<=rows;row++)for(int col=0;col<=sides;col++)
            {
                float theta=Mathf.Lerp(startDegrees,endDegrees,(float)row/rows)*Mathf.Deg2Rad,phi=col*2*Mathf.PI/sides;
                var unit=new Vector3(Mathf.Sin(theta)*Mathf.Cos(phi),Mathf.Sin(theta)*Mathf.Sin(phi),-Mathf.Cos(theta));
                vertices.Add(Vector3.Scale(unit,radius));normals.Add(new Vector3(unit.x/radius.x,unit.y/radius.y,unit.z/radius.z).normalized);uv.Add(new Vector2((float)col/sides,(float)row/rows));
            }
            for(int row=0;row<rows;row++)for(int col=0;col<sides;col++)
            {
                int a=row*(sides+1)+col,b=a+sides+1;
                if(startDegrees>0||row>0){triangles.Add(a);triangles.Add(a+1);triangles.Add(b);}
                if(endDegrees<180||row<rows-1){triangles.Add(a+1);triangles.Add(b+1);triangles.Add(b);}
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();meshes.Add(mesh);
            return MissionGeometry.Render(head,name,mesh,material,Vector3.zero,Vector3.one);
        }
        Transform Detail(Transform parent,string name,Material material,Vector3 position,Vector3 scale)=>MissionGeometry.Render(parent,name,sphere,material,position,scale);
        Material Material(string name,Color color,float smooth)
        {var m=new Material(Shader.Find("ARES/Reference Toon")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);materials.Add(m);return m;}
        void LateUpdate()
        {
            if(head==null)return;time+=Time.deltaTime;
            bool reduced=explorer.reducedMotion;
            float blink=reduced?1:Mathf.Abs(Mathf.Repeat(time+1.5f,5.2f)-.12f)<.10f?.12f:1;
            foreach(var eye in eyes){var scale=eye.localScale;scale.y=.043f*blink;eye.localScale=scale;}
            float look=explorer.GetComponent<GameFrontend>().Terminal.OwnsCamera?-8:reduced?0:Mathf.Sin(time*.55f)*3;
            head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(look,reduced?0:Mathf.Sin(time*.33f)*5,0),1-Mathf.Exp(-Time.deltaTime*3));
        }
        void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)Destroy(material);}
    }
}
