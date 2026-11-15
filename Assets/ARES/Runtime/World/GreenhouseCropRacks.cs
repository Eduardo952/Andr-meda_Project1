using System;
using System.Collections.Generic;
using Ares.Domain;
using Ares.World.Rover;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Care tools beside the reference garden. Never replaces its original crop geometry.</summary>
    public sealed class GreenhouseCropRacks : IDisposable
    {
        readonly Transform root;
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Material> materials = new List<Material>();
        readonly Dictionary<Material, RoverMesh> batches = new Dictionary<Material, RoverMesh>();
        readonly Material ivory, metal, teal;
        public GreenhouseCropRacks(Transform parent)
        {
            root = new GameObject("Greenhouse care equipment").transform; root.SetParent(parent, false);
            ivory = Material("Ceramic ivory", "#E4E4CA"); metal = Material("Frame graphite", "#455563");
            teal = Material("Garden mint", "#4AAB98");
            Vector3 p = parent.GetComponent<GreenhouseCare>().RefillPosition;
            Box(metal, p + Vector3.up * .12f, new Vector3(1.5f, .24f, 1.4f));
            Box(ivory, p + Vector3.up, new Vector3(1.25f, 1.65f, 1.1f), .15f);
            Box(teal, p + new Vector3(0, 1.05f, -.57f), new Vector3(.7f, 1.1f, .06f));
            Batch(metal).Tube(p + new Vector3(0, .8f, -.55f), p + new Vector3(0, .8f, -.95f), .07f);
            Box(teal, p + new Vector3(0, .95f, -.85f), new Vector3(.28f, .06f, .09f));
            Vector3 rack = parent.GetComponent<GreenhouseCare>().PickupPosition;
            Box(ivory, rack + Vector3.up * .35f, new Vector3(1.45f, .6f, .95f));
            Box(teal, rack + Vector3.up * .68f, new Vector3(1.55f, .1f, 1.05f));
            foreach (var pair in batches)
            {
                var go = new GameObject(pair.Key.name); go.transform.SetParent(root, false);
                var mesh = pair.Value.Bake(pair.Key.name); meshes.Add(mesh);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = pair.Key;
            }
            batches.Clear();
        }
        Material Material(string name, string hex)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            ColorUtility.TryParseHtmlString(hex, out var color); material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", .3f); materials.Add(material); return material;
        }
        RoverMesh Batch(Material material)
        { if (!batches.TryGetValue(material, out var mesh)) { mesh = new RoverMesh(); batches.Add(material, mesh); } return mesh; }
        void Box(Material material, Vector3 position, Vector3 size, float bevel = .04f)
            => Batch(material).Box(position, size, Quaternion.identity, bevel);
        // Plant health is applied to the original foliage by GreenhouseWorldView.
        public void Update(GreenhouseSimulation game) { }
        public void Dispose()
        {
            foreach (var mesh in meshes) UnityEngine.Object.Destroy(mesh);
            foreach (var material in materials) UnityEngine.Object.Destroy(material);
            UnityEngine.Object.Destroy(root.gameObject);
        }
    }
}