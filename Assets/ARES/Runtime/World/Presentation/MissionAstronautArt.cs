using System.Collections.Generic;
using Ares.UI;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Expressive helmet and suit details attached to the existing animation pivots.</summary>
    [DefaultExecutionOrder(20)]
    public sealed class MissionAstronautArt : MonoBehaviour
    {
        readonly List<Material> materials=new List<Material>();
        readonly List<Transform> eyes=new List<Transform>();
        Transform head;Mesh sphere;ReferenceExplorer explorer;float time;
        void Start()
        {
            explorer=GetComponent<ReferenceExplorer>();var animator=GetComponentInChildren<AstronautAnimator>();if(animator==null)return;
            sphere=MissionGeometry.Sphere();var body=animator.body;
            var ink=Material("Visor / midnight",MissionVisualStyle.Ink,.64f);
            var skin=Material("Explorer face",MissionVisualStyle.Hex("#dba378"),.15f);
            var white=Material("Suit / warm ceramic",MissionVisualStyle.Cream,.42f);
            var teal=Material("Suit / teal seams",MissionVisualStyle.Teal,.48f);
            var coral=Material("Suit / coral details",MissionVisualStyle.Coral,.28f);
            var gold=Material("Suit / discovery badge",MissionVisualStyle.Gold,.5f);
            var hair=Material("Explorer hair",MissionVisualStyle.Hex("#564342"),.2f);
            head=new GameObject("Expressive helmet pivot").transform;head.SetParent(body,false);head.localPosition=new Vector3(0,1.85f,0);
            var originals=new List<Transform>();foreach(Transform child in body)if(child!=head&&child.localPosition.y>1.6f)originals.Add(child);
            foreach(var child in originals)child.SetParent(head,true);
            foreach(var renderer in animator.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.name=="astronaut_3")renderer.sharedMaterial=white;
                if(renderer.name=="astronaut_4")renderer.sharedMaterial=ink;
                if(renderer.name=="astronaut_5"||renderer.name=="astronaut_6")renderer.enabled=false;
                if(renderer.name=="astronaut_2")renderer.sharedMaterial=teal;
                if(renderer.name=="astronaut_20"||renderer.name=="astronaut_23")renderer.sharedMaterial=ink;
            }
            Detail(head,"Rounded face behind visor",skin,new Vector3(0,.015f,-.625f),new Vector3(.325f,.28f,.075f));
            Detail(head,"Soft hair fringe",hair,new Vector3(-.07f,.218f,-.661f),new Vector3(.22f,.075f,.05f));
            Detail(head,"Side hair curl",hair,new Vector3(.18f,.19f,-.663f),new Vector3(.075f,.10f,.035f));
            for(int side=-1;side<=1;side+=2)
            {
                eyes.Add(Detail(head,"Expressive eye",ink,new Vector3(side*.115f,.065f,-.699f),new Vector3(.032f,.051f,.012f)));
                Detail(head,"Eye glint",white,new Vector3(side*.115f-.009f,.083f,-.71f),new Vector3(.009f,.012f,.005f));
                Detail(head,"Warm cheek",coral,new Vector3(side*.22f,-.045f,-.682f),new Vector3(.043f,.022f,.012f));
                Detail(head,"Helmet communication pod",teal,new Vector3(side*.535f,0,0),new Vector3(.12f,.19f,.20f));
            }
            Detail(head,"Smile",ink,new Vector3(0,-.092f,-.703f),new Vector3(.073f,.027f,.012f));
            Detail(head,"Smile upper lip",skin,new Vector3(0,-.076f,-.714f),new Vector3(.075f,.021f,.009f));
            Detail(body,"Discovery badge",gold,new Vector3(-.15f,1.14f,-.425f),new Vector3(.065f,.065f,.024f));
            for(int i=0;i<3;i++)Detail(body,"Suit life indicator",i==2?gold:white,new Vector3(.12f+i*.055f,1.10f,-.425f),new Vector3(.018f,.035f,.018f));
            Detail(body,"Soft collar",teal,new Vector3(0,1.48f,0),new Vector3(.37f,.075f,.35f));
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
        Transform Detail(Transform parent,string name,Material material,Vector3 position,Vector3 scale)=>MissionGeometry.Render(parent,name,sphere,material,position,scale);
        Material Material(string name,Color color,float smooth)
        {var m=new Material(Shader.Find("ARES/Reference Toon")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);materials.Add(m);return m;}
        void LateUpdate()
        {
            if(head==null)return;time+=Time.deltaTime;
            bool reduced=explorer.reducedMotion;
            float blink=reduced?1:Mathf.Abs(Mathf.Repeat(time+1.5f,5.2f)-.12f)<.10f?.12f:1;
            foreach(var eye in eyes){var scale=eye.localScale;scale.y=.051f*blink;eye.localScale=scale;}
            float look=explorer.GetComponent<GameFrontend>().Terminal.OwnsCamera?-8:reduced?0:Mathf.Sin(time*.55f)*3;
            head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(look,reduced?0:Mathf.Sin(time*.33f)*5,0),1-Mathf.Exp(-Time.deltaTime*3));
        }
        void OnDestroy(){if(sphere!=null)Destroy(sphere);foreach(var material in materials)Destroy(material);}
    }
}
