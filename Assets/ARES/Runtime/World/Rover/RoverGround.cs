using System.Collections.Generic;
using UnityEngine;

namespace Ares.World.Rover
{
    /// <summary>Adapter for the imported height mesh and decorative boulders. Owns only its added terrain collider.</summary>
    public sealed class RoverGround : System.IDisposable
    {
        MeshCollider terrain;
        bool ownsCollider;
        readonly List<Bounds> rocks = new List<Bounds>();
        public RoverGround(Transform world)
        {
            foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.name.StartsWith("landscape_")) continue;
                Bounds b = renderer.bounds;
                if (b.size.x > 300 && b.size.z > 300)
                {
                    terrain = renderer.GetComponent<MeshCollider>(); ownsCollider = terrain == null;
                    if (ownsCollider) { terrain = renderer.gameObject.AddComponent<MeshCollider>(); terrain.sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh; }
                }
                else if (b.size.y > .45f && b.max.y > .4f) rocks.Add(b);
            }
        }
        public float Height(Vector3 p)
        { return terrain != null && terrain.Raycast(new Ray(p + Vector3.up * 15,Vector3.down),out var hit,40) ? hit.point.y : 0; }
        public bool Clear(Vector3 p,float radius)
        {
            foreach(var rock in rocks)
            {
                float x=Mathf.Clamp(p.x,rock.min.x,rock.max.x), z=Mathf.Clamp(p.z,rock.min.z,rock.max.z);
                if((new Vector2(x-p.x,z-p.z)).sqrMagnitude<radius*radius)return false;
            }
            return true;
        }
        public void Place(Transform rover, Vector3 position, float yaw)
        {
            Quaternion heading=Quaternion.Euler(0,yaw,0);
            Vector3 forward=heading*Vector3.forward, right=heading*Vector3.right;
            float front=Height(position+forward*1.3f), rear=Height(position-forward*1.3f);
            float left=Height(position-right*1.38f), r=Height(position+right*1.38f);
            position.y=(front+rear+left+r)*.25f+.015f;
            Vector3 f=forward*2.6f+Vector3.up*(front-rear), side=right*2.76f+Vector3.up*(r-left);
            rover.SetPositionAndRotation(position,Quaternion.LookRotation(f,Vector3.Cross(f,side).normalized));
        }
        public void Dispose() { if(ownsCollider && terrain!=null)Object.Destroy(terrain); }
    }
}
