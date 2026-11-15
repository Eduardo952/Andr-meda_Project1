using UnityEngine;

namespace Ares.World
{
    /// <summary>Per-panel dust veil preserves the imported cell texture and never edits shared materials.</summary>
    public sealed class SolarPanelView
    {
        readonly Renderer dust;
        readonly LineRenderer boundary;
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        readonly Vector3 center;
        public Vector3 Center => center;
        public SolarPanelView(Transform parent, MeshRenderer source, Material material, int index)
        {
            center = source.bounds.center; center.y = 0;
            var root = new GameObject($"Solar panel {index + 1:00} maintenance").transform; root.SetParent(parent, false);
            var veil = new GameObject("Sand veil"); veil.transform.SetParent(root, false);
            veil.transform.SetPositionAndRotation(source.transform.position + Vector3.up * .014f, source.transform.rotation);
            veil.transform.localScale = source.transform.lossyScale;
            veil.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            dust = veil.AddComponent<MeshRenderer>(); dust.sharedMaterial = material;
            dust.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var area = new GameObject("Cleaning boundary"); area.transform.SetParent(root, false);
            boundary = area.AddComponent<LineRenderer>(); boundary.sharedMaterial = material;
            boundary.useWorldSpace = true; boundary.loop = true; boundary.positionCount = 4;
            boundary.widthMultiplier = .055f; boundary.numCornerVertices = 2;
            boundary.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            boundary.SetPositions(new[] { center + new Vector3(-2.7f,.12f,-2.7f), center + new Vector3(2.7f,.12f,-2.7f), center + new Vector3(2.7f,.12f,2.7f), center + new Vector3(-2.7f,.12f,2.7f) });
        }
        public bool Contains(Vector3 player) => Mathf.Abs(player.x - center.x) <= 2.7f && Mathf.Abs(player.z - center.z) <= 2.7f;
        public void Update(float level, bool selected, bool parkVisible)
        {
            properties.SetColor("_BaseColor", new Color(.88f, .79f, .65f, level * .14f)); dust.SetPropertyBlock(properties);
            dust.enabled = level > .001f;
            boundary.enabled = parkVisible;
            properties.SetColor("_BaseColor", selected ? new Color(.35f,.92f,.75f,1) : level >= .05f ? new Color(.92f,.73f,.39f,.7f) : new Color(.82f,.83f,.73f,.3f));
            boundary.SetPropertyBlock(properties); boundary.widthMultiplier = selected ? .09f : .045f;
        }
    }
}
