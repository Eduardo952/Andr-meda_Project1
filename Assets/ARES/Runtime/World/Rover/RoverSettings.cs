using System;
using UnityEngine;

namespace Ares.World.Rover
{
    [Serializable]
    public sealed class RoverSettings
    {
        [Header("Drive (metres / seconds)")]
        [Min(.1f)] public float maxSpeed = 12;
        [Min(.1f)] public float reverseSpeed = 4;
        [Min(.1f)] public float acceleration = 2.4f;
        [Min(.1f)] public float braking = 7;
        [Min(.1f)] public float rollingDrag = 1.8f;
        [Range(10, 90)] public float steeringDegrees = 34;
        [Min(.1f)] public float wheelbase = 2.6f;
        [Min(.1f)] public float wheelRadius = .56f;
        [Min(.1f)] public float footprintRadius = 2.35f;
        [Min(20)] public float worldLimit = 125;
        public Vector3 spawn = new Vector3(25, 0, -22);
        public float spawnYaw = 145;
        [Header("Third-person camera")]
        [Min(3)] public float cameraDistance = 7.8f;
        [Range(10, 60)] public float cameraPitch = 23;
        [Min(.1f)] public float cameraDamping = 7;
        [Header("Dust")]
        [Min(.1f)] public float dustBuildSeconds = 5;
        [Range(1, 30)] public float dustRatePerWheel = 13;
    }
}
