using System;
using System.Collections.Generic;
using Ares.Data;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Moves the actual imported can between its stand and the astronaut's hand.</summary>
    public sealed class GreenhouseCanView : IDisposable
    {
        readonly Transform root;
        readonly Vector3 stand,spout;
        readonly List<(Transform part,Transform parent,Vector3 position,Quaternion rotation)> originals=new List<(Transform,Transform,Vector3,Quaternion)>();
        public Vector3 GripPosition=>root.position;
        public Vector3 SpoutPosition=>root.TransformPoint(spout);
        public GreenhouseCanView(Transform world,Transform parent,GardenPoint pickup)
        {
            stand=pickup.Position+Vector3.up*pickup.height;
            root=new GameObject("Imported watering can / grip").transform;root.SetParent(parent,false);root.position=stand;
            Bounds bounds=new Bounds(stand,Vector3.zero);
            foreach(var part in world.GetComponentsInChildren<ReferenceWorldPart>(true))
            {
                if(part.role!="watering_can")continue;
                var t=part.transform;originals.Add((t,t.parent,t.localPosition,t.localRotation));
                bounds.Encapsulate(part.GetComponent<Renderer>().bounds);t.SetParent(root,true);
            }
            if(originals.Count==0)throw new InvalidOperationException("Imported watering can missing. Reimport the reference scenery.");
            spout=new Vector3(bounds.max.x-stand.x,-.4f,0);
        }
        public void Update(AstronautAnimator actor,bool owned,bool inside,bool pouring,Vector3 facing)
        {
            root.gameObject.SetActive(!owned||inside);
            if(!owned) { root.SetPositionAndRotation(stand,Quaternion.identity);return; }
            if(actor==null||!inside)return;
            var rotation=Quaternion.LookRotation(facing.sqrMagnitude>.001f?facing.normalized:Vector3.forward)*Quaternion.Euler(0,-90,pouring?-24:0);
            root.SetPositionAndRotation(actor.rightArm.TransformPoint(new Vector3(0,-.55f,0)),rotation);
        }
        public void Dispose()
        {
            foreach(var original in originals)
                if(original.part!=null&&original.parent!=null)
                {original.part.SetParent(original.parent,false);original.part.localPosition=original.position;original.part.localRotation=original.rotation;}
            if(root!=null)UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}
