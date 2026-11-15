using UnityEngine;

namespace Ares.World
{
    /// <summary>Stable imported identity; prefab filenames and display names may change independently.</summary>
    [DisallowMultipleComponent]
    public sealed class ReferenceRig : MonoBehaviour
    {
        public string id;
        public static Transform Find(Transform world,string id)
        {
            foreach(var rig in world.GetComponentsInChildren<ReferenceRig>(true))
                if(rig.id==id)return rig.transform;
            return null;
        }
    }
}
