using System;
using UnityEngine;

namespace Ares.World.Computer
{
    [Serializable]
    public sealed class ComputerSettings
    {
        public Vector3 screenCenter = new Vector3(9.0156f, 3.85f, 9.0156f);
        public Vector3 screenNormal = new Vector3(-.7071068f, 0, -.7071068f);
        public Vector2 screenSize = new Vector2(5.14f, 2.38f);
        public Vector3 standPoint = new Vector3(7.3f, 0, 7.3f);
        [Min(.5f)] public float interactionRadius = 3;
        [Min(.2f)] public float transitionSeconds = 1.65f;
        [Range(30,60)] public float focusedFieldOfView = 42;
    }
}
