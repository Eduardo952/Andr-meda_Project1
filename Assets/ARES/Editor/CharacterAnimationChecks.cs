using System;
using System.Collections.Generic;
using System.IO;
using Ares.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ares.Editor
{
    /// <summary>Golden poses/vertices produced by executing the original TSX callbacks.</summary>
    public static class CharacterAnimationChecks
    {
        [Serializable] sealed class Fixtures { public BindGeometry[] bindGeometry; public AstronautFrame[] astronaut; public BubbleFrame[] bubble; }
        [Serializable] sealed class BindGeometry { public string id; public float[] vertices; }
        [Serializable] sealed class Pose { public string binding; public float[] position, rotation, scale; }
        [Serializable] sealed class AstronautFrame
        {
            public bool reduced, observing, observeArrived;
            public float elapsed, dt, phase, speed, x, z, heading, ambience, jump, observe;
            public Pose[] poses;
        }
        [Serializable] sealed class BubbleFrame { public float time; public string mood; public bool talking, wave; public Pose[] poses; }
        [Serializable] sealed class Telescope { public float x, z; }

        public static string Run()
        {
            var data = JsonUtility.FromJson<Fixtures>(File.ReadAllText("Assets/ARES/Import/character-parity.json"));
            var config = JsonUtility.FromJson<WorldConfiguration>(File.ReadAllText("Assets/ARES/Import/balance.json"));
            var telescope = JsonUtility.FromJson<Telescope>(File.ReadAllText("Assets/ARES/Import/telescope.json"));
            int vertices = 0;
            var scene = EditorSceneManager.NewPreviewScene();
            var astronautObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterImporter.Root + "/astronaut.prefab"), scene);
            var bubbleObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterImporter.Root + "/bubble.prefab"), scene);
            try
            {
                // Compare world vertices to the original flattened meshes before any animation.
                foreach (var item in new[] { (id: "astronaut", root: astronautObject), (id: "bubble", root: bubbleObject) })
                {
                    item.root.transform.localScale = Vector3.one;
                    var meshes = item.root.GetComponentsInChildren<MeshFilter>();
                    int mesh = 0;
                    foreach (var source in data.bindGeometry)
                    {
                        if (source.id != item.id) continue;
                        Require(mesh < meshes.Length, "Missing " + item.id + " mesh");
                        var filter = meshes[mesh++];
                        var local = filter.sharedMesh.vertices;
                        Require(local.Length * 3 == source.vertices.Length, "Changed vertex count");
                        for (int v = 0; v < local.Length; v++)
                        {
                            var expected = ReferenceSpace.Position(source.vertices[v * 3], source.vertices[v * 3 + 1], source.vertices[v * 3 + 2]);
                            Require(Vector3.Distance(filter.transform.TransformPoint(local[v]), expected) < .00002f, "Bind geometry mismatch: " + filter.name + " vertex " + v);
                            vertices++;
                        }
                    }
                    Require(mesh == meshes.Length, "Extra character mesh");
                }
                var astronaut = astronautObject.GetComponent<AstronautAnimator>();
                var bindings = new Dictionary<string, Transform>
                {
                    ["root"] = astronaut.transform, ["bodyRef"] = astronaut.body, ["lArm"] = astronaut.leftArm,
                    ["rArm"] = astronaut.rightArm, ["lLeg"] = astronaut.leftLeg, ["rLeg"] = astronaut.rightLeg,
                };
                float observe = 0; bool previousReduced = false;
                foreach (var frame in data.astronaut)
                {
                    if (frame.reduced != previousReduced) observe = 0;
                    previousReduced = frame.reduced;
                    observe = CharacterAnimation.Observe(observe, frame.observing && frame.observeArrived, frame.dt);
                    Require(Mathf.Abs(observe - frame.observe) < .00001f, "Observation damping mismatch");
                    float ob = frame.reduced ? (frame.observing && frame.observeArrived ? 1 : 0) : observe;
                    astronaut.transform.position = ReferenceSpace.Position(frame.x, 0, frame.z);
                    astronaut.transform.rotation = ReferenceSpace.Euler(0, CharacterAnimation.Facing(frame.x, frame.z, frame.heading, ob, telescope.x, telescope.z), 0);
                    astronaut.Apply(CharacterAnimation.Astronaut(frame.speed, frame.phase, frame.elapsed, frame.ambience, frame.jump, ob, frame.reduced, config.ambience.jumpSeconds));
                    foreach (var pose in frame.poses) Check(bindings[pose.binding], pose);
                }
                var bubble = bubbleObject.GetComponent<BubbleAnimator>();
                bindings = new Dictionary<string, Transform>
                {
                    ["fish"] = bubble.fish, ["tail"] = bubble.tail, ["mouth"] = bubble.mouth,
                    ["fin"] = bubble.fin, ["bubbles"] = bubble.bubbles, ["eyes"] = bubble.eyes,
                };
                for (int i = 0; i < bubble.bubbles.childCount; i++) bindings.Add("air" + i, bubble.bubbles.GetChild(i));
                foreach (var frame in data.bubble)
                {
                    bubble.Apply(frame.time, frame.mood, frame.talking, frame.wave);
                    foreach (var pose in frame.poses) Check(bindings[pose.binding], pose);
                }
                return $"{vertices} character bind vertices, {data.astronaut.Length} astronaut frames, {data.bubble.Length} Bubble frames";
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        static void Check(Transform actual, Pose expected)
        {
            var p = expected.position; var q = expected.rotation; var s = expected.scale;
            Require(Vector3.Distance(actual.localPosition, ReferenceSpace.Position(p[0], p[1], p[2])) < .0001f, expected.binding + " position mismatch");
            Require(Mathf.Abs(Quaternion.Dot(actual.localRotation, ReferenceSpace.Rotation(q[0], q[1], q[2], q[3]))) > .999999f, expected.binding + " rotation mismatch");
            Require(Vector3.Distance(actual.localScale, new Vector3(s[0], s[1], s[2])) < .0001f, expected.binding + " scale mismatch");
        }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
