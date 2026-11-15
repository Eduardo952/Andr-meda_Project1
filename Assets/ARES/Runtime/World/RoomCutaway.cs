using System.Collections.Generic;
using Ares.Data;
using UnityEngine;

namespace Ares.World
{
    /// <summary>Presentation-only visibility. Never modifies collision or the walkable floor.</summary>
    public sealed class RoomCutaway
    {
        sealed class Group
        {
            public Transform root;
            public Renderer[] renderers;
            public Color[] colors;
            public RoomData room;
            public bool roof;
            public float alpha = 1;
        }
        readonly List<Group> groups = new List<Group>();
        readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        public RoomCutaway(Transform world, LayoutData layout)
        {
            foreach (Transform child in world)
            {
                bool roof = child.name.EndsWith("_roof");
                if (!roof && !child.name.EndsWith("_walls")) continue;
                RoomData room = null;
                foreach (var candidate in layout.rooms) if (child.name.StartsWith(candidate.id + "_")) { room = candidate; break; }
                var renderers = child.GetComponentsInChildren<Renderer>();
                var colors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i].sharedMaterial.GetColor(BaseColor);
                groups.Add(new Group { root = child, room = room, roof = roof, renderers = renderers, colors = colors });
            }
        }
        public void Update(Vector3 player, Vector3 camera, bool roofs, bool isometric, float pitch, float dt)
        {
            foreach (var group in groups)
            {
                bool occupied = group.room != null && WorldNavigation.Inside(player.x, -player.z, group.room, -.6f);
                float target;
                if (group.roof)
                {
                    if (group.room == null) target = !roofs ? 0 : isometric ? .025f : .1f;
                    else if (group.room.id == "greenhouse") target = !roofs || isometric || pitch > Mathf.PI / 3 ? 0 : occupied ? .025f : .22f;
                    else target = !roofs ? 0 : occupied || isometric || pitch > Mathf.PI / 3 ? .025f : .3f;
                    group.alpha = target;
                }
                else
                {
                    var center = group.room.Center;
                    float cameraDistance = Vector2.Distance(new Vector2(camera.x, camera.z), new Vector2(center.x, center.z));
                    float playerDistance = Vector2.Distance(new Vector2(player.x, player.z), new Vector2(center.x, center.z));
                    bool obstruction = occupied || isometric || cameraDistance < Mathf.Max(group.room.width, group.room.depth) / 2 + 3 && playerDistance < 30;
                    target = obstruction ? .09f : 1;
                    group.alpha += (target - group.alpha) * (1 - Mathf.Exp(-7 * Mathf.Min(dt, .05f)));
                }
                group.root.gameObject.SetActive(group.alpha > 0);
                for (int i = 0; i < group.renderers.Length; i++)
                {
                    var color = group.colors[i]; color.a = group.alpha;
                    properties.SetColor(BaseColor, color); group.renderers[i].SetPropertyBlock(properties);
                }
            }
        }
    }
}
