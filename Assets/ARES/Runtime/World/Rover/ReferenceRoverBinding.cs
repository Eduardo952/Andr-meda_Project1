using System;
using Ares.Data;
using UnityEngine;

namespace Ares.World.Rover
{
    /// <summary>Reuses the authored rover meshes; retains six pivots for the existing driving model.</summary>
    public static class ReferenceRoverBinding
    {
        public static bool TryBind(Transform rover, LayoutData layout, Transform[] steering,
            Vector3[] centers, out Transform body, out Transform[] wheels)
        {
            body=null; wheels=null;
            var landmark=layout.landmarks==null?null:Array.Find(layout.landmarks,l=>l.id=="rover");
            if(landmark==null)return false;
            var parts=rover.parent.GetComponentsInChildren<ReferenceWorldPart>();
            if(!Array.Exists(parts,p=>p.role=="reference_rover"))return false;
            float yaw=-(Mathf.Atan2(-landmark.x,-landmark.z)+.6f)*Mathf.Rad2Deg;
            var inverse=Quaternion.Inverse(Quaternion.Euler(0,yaw,0));
            body=new GameObject("Reference rover / body").transform; body.SetParent(rover,false);
            wheels=new Transform[6];
            for(int i=0;i<6;i++)
            {
                steering[i]=new GameObject("Steering "+i).transform;steering[i].SetParent(rover,false);
                wheels[i]=new GameObject("Wheel "+i).transform;wheels[i].SetParent(steering[i],false);
            }
            foreach(var part in parts)
            {
                if(part.role!="reference_rover"&&part.role!="rover_wheel")continue;
                Transform parent=body; Vector3 pivot=Vector3.zero;
                if(part.role=="rover_wheel")
                {
                    int i=part.instance;
                    pivot=inverse*(part.GetComponent<Renderer>().bounds.center-landmark.Center);
                    centers[i]=pivot;steering[i].localPosition=pivot;parent=wheels[i];
                }
                part.transform.SetParent(parent,false);
                part.transform.localRotation=inverse;
                part.transform.localPosition=-(inverse*landmark.Center)-pivot;
            }
            return true;
        }
    }
}
