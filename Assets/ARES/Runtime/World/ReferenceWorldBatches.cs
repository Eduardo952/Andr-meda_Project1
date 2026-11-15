using System.Collections.Generic;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Combine immutable opaque hardware without changing object identities or cutaway groups.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class ReferenceWorldBatches : MonoBehaviour
    {
        public int BatchedRenderers { get; private set; }
        void Start()
        {
            var candidates = new List<GameObject>();
            foreach (Transform group in transform)
            {
                // Keep shell renderers separate for transparent sorting/cutaway.
                if (group.name.EndsWith("_walls") || group.name.EndsWith("_roof")) continue;
                if (!group.name.EndsWith("_floor") && !group.name.EndsWith("_interior")) continue;
                foreach (var renderer in group.GetComponentsInChildren<MeshRenderer>())
                {
                    // Solar equipment is independently addressed by maintenance presentation.
                    if (renderer.name.StartsWith("decor_") || renderer.name.StartsWith("station_knobs_")) continue;
                    if(renderer.GetComponent<ReferenceWorldPart>()!=null)continue;
                    var material = renderer.sharedMaterial;
                    if (!renderer.enabled || material == null || material.renderQueue >= 3000 || !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
                    candidates.Add(renderer.gameObject);
                }
            }
            BatchedRenderers = candidates.Count;
            if (candidates.Count > 0) StaticBatchingUtility.Combine(candidates.ToArray(), gameObject);
        }
    }
}
