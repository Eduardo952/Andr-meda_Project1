using System;
using UnityEngine;

namespace Ares.World
{
    [Serializable]
    public sealed class MartianWindSettings
    {
        public bool enabled = true;
        [Range(6, 40)] public float radius = 25;
        [Range(1, 12)] public float height = 7;
        [Range(1, 50)] public float particlesPerSecond = 22;
        [Range(.3f, 8)] public float speed = 3.2f;
        [Range(0, .5f)] public float opacity = .32f;
        public Vector3 direction = new Vector3(1, .035f, -.4f);
    }
}
