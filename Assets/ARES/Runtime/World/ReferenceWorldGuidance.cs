using System.Collections.Generic;
using Ares.Data;
using Ares.Domain;
using Ares.UI;
using Ares.World.Rover;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Routes over the same floor paths and collision predicate as player movement.</summary>
    public sealed class ReferenceRoute
    {
        readonly List<Vector3> nodes=new List<Vector3>();
        readonly List<List<int>> edges=new List<List<int>>();
        readonly WorldNavigation navigation;
        public ReferenceRoute(LayoutData layout)
        {
            navigation=new WorldNavigation(layout);var keys=new HashSet<Vector2Int>();
            foreach(var path in layout.paths)
            {
                var a=new Vector3(path.a.x,0,-path.a.z);var b=new Vector3(path.b.x,0,-path.b.z);int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)));
                for(int i=0;i<=count;i++){var p=Vector3.Lerp(a,b,(float)i/count);var key=new Vector2Int(Mathf.RoundToInt(p.x*1000),Mathf.RoundToInt(p.z*1000));if(keys.Add(key)&&navigation.Walkable(p)){nodes.Add(p);edges.Add(new List<int>());}}
            }
            for(int i=0;i<nodes.Count;i++)for(int j=i+1;j<nodes.Count;j++)
                if(Vector3.Distance(nodes[i],nodes[j])<=1.6f&&Clear(nodes[i],nodes[j])){edges[i].Add(j);edges[j].Add(i);}
        }
        bool Clear(Vector3 a,Vector3 b)
        {int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)*4));for(int i=0;i<=steps;i++)if(!navigation.Walkable(Vector3.Lerp(a,b,(float)i/steps)))return false;return true;}
        int Closest(Vector3 p)
        {int result=-1;float best=float.PositiveInfinity;for(int i=0;i<nodes.Count;i++){float d=(p-nodes[i]).sqrMagnitude;if(d<best&&Clear(p,nodes[i])){result=i;best=d;}}return result;}
        public List<Vector3> Between(Vector3 from,Vector3 to)
        {
            var result=new List<Vector3>();int start=Closest(from),end=Closest(to);if(start<0||end<0)return result;
            var distance=new float[nodes.Count];var previous=new int[nodes.Count];var open=new HashSet<int>{start};
            for(int i=0;i<nodes.Count;i++){distance[i]=float.PositiveInfinity;previous[i]=-1;}distance[start]=0;
            while(open.Count>0)
            {
                int current=-1;float best=float.PositiveInfinity;foreach(int index in open)if(distance[index]<best){best=distance[index];current=index;}
                if(current==end)break;open.Remove(current);
                foreach(int next in edges[current]){float d=best+Vector3.Distance(nodes[current],nodes[next]);if(d>=distance[next])continue;distance[next]=d;previous[next]=current;open.Add(next);}
            }
            if(float.IsInfinity(distance[end]))return result;
            result.Add(to);for(int current=end;current>=0;current=previous[current]){result.Add(nodes[current]);if(current==start)break;}
            result.Reverse();return result;
        }
    }
    public sealed class ReferenceWorldGuidance : MonoBehaviour
    {
        ReferenceExplorer explorer;GameFrontend frontend;ReferenceRoute graph;
        readonly Transform[] dots=new Transform[6];Transform arrow,beacon,visuals;Mesh arrowMesh,dotMesh;Material material;
        List<Vector3> route=new List<Vector3>();Vector2Int lastCell=new Vector2Int(int.MinValue,int.MinValue);string lastTarget;
        public string Target {get;private set;}
        public Vector3 Waypoint {get;private set;}
        void Start()
        {
            explorer=GetComponent<ReferenceExplorer>();frontend=GetComponent<GameFrontend>();graph=new ReferenceRoute(explorer.definition.layout);
            visuals=new GameObject("Reference route markers").transform;visuals.SetParent(transform,false);
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",new Color32(244,202,84,255));
            var mesh=new RoverMesh();mesh.Quad(new Vector3(-.3f,0,-.25f),new Vector3(0,0,.55f),new Vector3(.3f,0,-.25f),Vector3.zero);arrowMesh=mesh.Bake("Direction arrow");
            var dot=new RoverMesh();dot.Box(Vector3.zero,Vector3.one*.12f,Quaternion.identity,.03f);dotMesh=dot.Bake("Route dot");
            arrow=Make("Next waypoint",arrowMesh);beacon=Make("Destination beacon",dotMesh);beacon.localScale=Vector3.one*4;
            for(int i=0;i<dots.Length;i++)dots[i]=Make("Path "+i,dotMesh);
        }
        Transform Make(string name,Mesh mesh){var go=new GameObject(name);go.transform.SetParent(visuals,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return go.transform;}
        void LateUpdate()
        {
            var c=frontend.Campaign;Target=null;
            if(c!=null)
            {
                if(c.HazardTarget != null)Target=c.HazardTarget;
                else if(c.Phase==Phase.Plan||c.Phase==Phase.Tasks)
                {Target="control";if(c.Phase==Phase.Tasks)foreach(var task in c.Current.tasks)if(!c.Completed(task.id)){Target=task.room;break;}}
            }
            Vector3 destinationPoint;
            if (frontend.TutorialActive)
            {
                var target = frontend.Tutorial.Target;
                Target = target.HasValue ? "tutorial-" + frontend.TutorialStage + "-" + frontend.Tutorial.Shelf : null;
                destinationPoint = target ?? Vector3.zero;
            }
            else
            {
                RoomData destination=null;foreach(var room in explorer.definition.layout.rooms)if(room.id==Target){destination=room;break;}
                destinationPoint = destination?.Station ?? Vector3.zero;
            }
            bool visible=Target!=null&&!frontend.BlocksWorld&&!explorer.IsDrivingRover;visuals.gameObject.SetActive(visible);if(!visible)return;
            var player=explorer.PlayerPosition;var cell=new Vector2Int(Mathf.RoundToInt(player.x),Mathf.RoundToInt(player.z));
            if(lastTarget!=Target||cell!=lastCell){route=graph.Between(player,destinationPoint);lastCell=cell;lastTarget=Target;}
            if(route.Count==0){visuals.gameObject.SetActive(false);return;}
            Waypoint=destinationPoint;foreach(var point in route)if(Vector3.Distance(point,player)>2){Waypoint=point;break;}
            Vector3 direction=Waypoint-player;float distance=direction.magnitude;direction.Normalize();
            arrow.gameObject.SetActive(distance>3);arrow.position=player+direction*1.6f+Vector3.up*(.25f+(explorer.reducedMotion?0:Mathf.Sin(Time.time*4)*.08f));
            if(direction.sqrMagnitude>.1f)arrow.rotation=Quaternion.LookRotation(direction);
            beacon.position=destinationPoint+Vector3.up*3.2f;beacon.Rotate(0,explorer.reducedMotion?0:Time.deltaTime*30,0);
            for(int i=0;i<dots.Length;i++){float f=explorer.reducedMotion?(i+1)/7f:Mathf.Repeat((i+1)/7f+Time.time*.25f,1);dots[i].position=Vector3.Lerp(player+direction*Mathf.Min(2.4f,distance),Waypoint,f)+Vector3.up*.14f;dots[i].gameObject.SetActive(distance>3);}
        }
        void OnDestroy(){if(material!=null)Destroy(material);if(arrowMesh!=null)Destroy(arrowMesh);if(dotMesh!=null)Destroy(dotMesh);}
    }
}
