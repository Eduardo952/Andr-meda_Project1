using System.Collections.Generic;
using Ares.Domain;
using Ares.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ares.World
{
    /// <summary>Cosmetic garden state. Harvest never grants energy, stars, task completion or inventory.</summary>
    public sealed class GardenHarvest
    {
        readonly HashSet<int> collected=new HashSet<int>();
        public int Count=>collected.Count;
        public bool Collected(int id)=>collected.Contains(id);
        public bool Pick(int id,int ripe,float distance)
        {return id>=0&&id<Mathf.Clamp(ripe,0,24)&&distance>=0&&distance<=7&&collected.Add(id);}
    }
    [DefaultExecutionOrder(-10)]
    public sealed class ReferenceGarden : MonoBehaviour
    {
        sealed class Part
        {
            public ReferenceWorldPart source;public MeshRenderer renderer;public Vector3 center;public Color color;
            public MeshSightline sightline;
        }
        readonly List<Part> parts=new List<Part>();
        readonly List<Part> apples=new List<Part>();
        ReferenceExplorer explorer;GameFrontend frontend;Campaign session;
        MaterialPropertyBlock properties;Transform drone;TextMesh label;
        float time;int lastCount=-1;
        public GardenHarvest Harvest {get;private set;}=new GardenHarvest();
        public int Ripe {get;private set;}=24;
        public Vector3 DronePosition=>drone!=null?drone.position:Vector3.zero;
        void Start()
        {
            properties=new MaterialPropertyBlock();explorer=GetComponent<ReferenceExplorer>();frontend=GetComponent<GameFrontend>();
            drone=new GameObject("Original greenhouse drone").transform;drone.SetParent(transform,false);drone.position=new Vector3(64,4.8f,0);
            foreach(var source in GetComponentsInChildren<ReferenceWorldPart>())
            {
                if(!source.role.StartsWith("foliage")&&source.role!="apple"&&source.role!="mist"&&source.role!="drone")continue;
                var renderer=source.GetComponent<MeshRenderer>();var mesh=source.GetComponent<MeshFilter>().sharedMesh;
                var p=new Part{source=source,renderer=renderer,center=mesh.bounds.center,color=renderer.sharedMaterial.GetColor("_BaseColor")};
                if(source.role=="drone"){source.transform.SetParent(drone,true);continue;}
                if(source.role=="apple"){p.sightline=new MeshSightline(mesh);apples.Add(p);}
                parts.Add(p);
            }
            label=new GameObject("Cosmetic harvest hint").AddComponent<TextMesh>();label.transform.SetParent(transform,false);
            label.transform.position=new Vector3(52,6.1f,0);label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;
            label.fontSize=64;label.characterSize=.035f;label.color=new Color(.18f,.2f,.17f);
            var font=Resources.Load<Font>("ARES/UI/Fonts/Nunito");if(font!=null){label.font=font;label.GetComponent<MeshRenderer>().sharedMaterial=font.material;}
        }
        public bool TryHarvest(Ray ray)
        {
            if(session==null||frontend.BlocksPointer||explorer.IsDrivingRover)return false;
            Part nearest=null;float best=float.PositiveInfinity;
            foreach(var part in apples)
            {
                if(!part.renderer.enabled||Harvest.Collected(part.source.instance)||!part.renderer.bounds.IntersectRay(ray,out var hit)||hit>=best)continue;
                if(!part.sightline.Intersects(part.source.transform,ray.origin,ray.GetPoint(200)))continue;
                nearest=part;best=hit;
            }
            if(nearest==null)return false;
            var delta=nearest.center-explorer.PlayerPosition;delta.y=0;
            return Harvest.Pick(nearest.source.instance,Ripe,delta.magnitude);
        }
        void LateUpdate()
        {
            var campaign=frontend.Campaign;
            if(campaign!=session){session=campaign;Harvest=new GardenHarvest();lastCount=-1;}
            if(!frontend.BlocksWorld&&!explorer.reducedMotion)time+=Mathf.Min(Time.deltaTime,.05f);
            float coverage=campaign==null?1:Mathf.Clamp01((float)campaign.Allocation.greenhouse/Mathf.Max(1,campaign.Current.demand.greenhouse));
            Ripe=coverage>=1?24:coverage>=.5f?12:4;
            float speed=coverage>=1?.22f:coverage>=.5f?.12f:.055f;
            float angle=explorer.reducedMotion?.4f:time*speed;
            drone.SetPositionAndRotation(new Vector3(52+Mathf.Cos(angle)*12,4.8f+(explorer.reducedMotion?0:Mathf.Sin(time*.8f)*.12f),-Mathf.Sin(angle)*10),Quaternion.Euler(0,angle*Mathf.Rad2Deg,0));
            foreach(var part in parts)
            {
                string role=part.source.role;int i=part.source.instance;
                if(role=="apple"){part.renderer.enabled=i<Ripe&&!Harvest.Collected(i);continue;}
                if(role=="mist")
                {
                    part.renderer.enabled=coverage>=1&&!explorer.reducedMotion;
                    float phase=Mathf.Repeat(time*.12f+i*.37f,1),initial=Mathf.Repeat(i*.37f,1);
                    float scale=i%3==0?1:(2+phase*2)/(2+initial*2);
                    var p=new Vector3(38+Mathf.Repeat(i*7.3f,29),i%3==0?3+phase*2:7+phase*2,-Mathf.Sin(i*3.7f)*10);
                    part.source.transform.localScale=Vector3.one*scale;part.source.transform.localPosition=p-part.center*scale;continue;
                }
                if(role.StartsWith("foliage"))
                {
                    part.source.transform.localScale=new Vector3(1,.65f+coverage*.35f,1);
                    part.source.transform.localRotation=Quaternion.Euler(0,0,coverage<.5f?.015f*Mathf.Rad2Deg:0);
                    var tint=Color.Lerp(part.color,new Color32(193,182,109,255),1-coverage);
                    part.renderer.GetPropertyBlock(properties);properties.SetColor("_BaseColor",tint);part.renderer.SetPropertyBlock(properties);
                }
            }
            var camera=Camera.main;
            bool near=camera!=null&&session!=null&&!frontend.BlocksWorld&&!explorer.IsDrivingRover&&Vector2.Distance(new Vector2(explorer.PlayerPosition.x,explorer.PlayerPosition.z),new Vector2(52,0))<7;
            label.gameObject.SetActive(near);
            if(near)
            {
                label.transform.rotation=camera.transform.rotation;
                if(lastCount!=Harvest.Count){lastCount=Harvest.Count;bool en=Ares.UI.Localization.CurrentLanguage=="en";label.text=en?$"Click ripe apples\nCollected {Harvest.Count}/24 · just for fun":$"Clique nas maçãs maduras\nColhidas {Harvest.Count}/24 · só por diversão";}
                if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame)TryHarvest(camera.ScreenPointToRay(Mouse.current.position.ReadValue()));
            }
        }
    }
}
