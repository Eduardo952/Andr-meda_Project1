using System;
using Ares.Data;
using UnityEngine;

namespace Ares.World
{
    [Serializable] public sealed class CameraConfiguration
    {
        public float defaultYaw, defaultPitchDegrees, defaultDistance, minPitchDegrees, maxPitchDegrees;
        public float minDistance, maxDistance, isometricYaw, isometricPitchDegrees, isometricDistance;
        public float lookHeight, rotationDamping, zoomDamping, radiansPerPixel, wheelIntensity;
        public float minSensitivity, maxSensitivity, defaultSensitivity, groundClearance, domeClearance;
    }
    [Serializable] public sealed class WorldConfiguration
    {
        public float walkSpeed, walkAccel, useRadius, cameraDamping;
        public CameraConfiguration camera;
        public AmbienceConfiguration ambience;
    }
    [Serializable] public sealed class AmbienceConfiguration
    {
        public float interiorNear, interiorFar, shellOpacity, fadeDamping, jumpSeconds, celebrationSeconds, fallbackWindYaw;
    }
    /// <summary>Pure camera state in reference coordinates. Rendering converts handedness once.</summary>
    public sealed class OrbitCamera
    {
        readonly CameraConfiguration config;
        readonly LayoutData layout;
        public float Yaw, Pitch, Distance, TargetYaw, TargetPitch, TargetDistance;
        public float WheelIntensity => config.wheelIntensity;
        public OrbitCamera(CameraConfiguration config, LayoutData layout)
        {
            this.config = config; this.layout = layout;
            Yaw = TargetYaw = config.defaultYaw;
            Pitch = TargetPitch = config.defaultPitchDegrees * Mathf.Deg2Rad;
            Distance = TargetDistance = config.defaultDistance;
        }
        public void Recenter(bool isometric)
        {
            TargetYaw = isometric ? config.isometricYaw : config.defaultYaw;
            TargetPitch = (isometric ? config.isometricPitchDegrees : config.defaultPitchDegrees) * Mathf.Deg2Rad;
            TargetDistance = isometric ? config.isometricDistance : config.defaultDistance;
        }
        public void Orbit(float dx, float dy, float sensitivity, bool invert, bool isometric)
        {
            if (isometric) return;
            float gain = config.radiansPerPixel * Mathf.Clamp(sensitivity, config.minSensitivity, config.maxSensitivity);
            TargetYaw = WrapYaw(TargetYaw - dx * gain);
            TargetPitch = Mathf.Clamp(TargetPitch + dy * gain * (invert ? -1 : 1), config.minPitchDegrees * Mathf.Deg2Rad, config.maxPitchDegrees * Mathf.Deg2Rad);
        }
        public void Zoom(float factor) { TargetDistance = Mathf.Clamp(TargetDistance * factor, config.minDistance, config.maxDistance); }
        public void Step(float dt, bool isometric)
        {
            dt = Mathf.Min(dt, .05f);
            float rotation = 1 - Mathf.Exp(-config.rotationDamping * dt);
            if (isometric) { TargetYaw = config.isometricYaw; TargetPitch = config.isometricPitchDegrees * Mathf.Deg2Rad; }
            // Keep a full natural orbit without accumulating huge angles over long sessions.
            TargetYaw = WrapYaw(TargetYaw);
            float delta = Mathf.Atan2(Mathf.Sin(TargetYaw - Yaw), Mathf.Cos(TargetYaw - Yaw));
            Yaw = WrapYaw(Yaw + delta * rotation); Pitch += (TargetPitch - Pitch) * rotation;
            Distance += (TargetDistance - Distance) * (1 - Mathf.Exp(-config.zoomDamping * dt));
        }
        static float WrapYaw(float yaw) => Mathf.Repeat(yaw + Mathf.PI, 2 * Mathf.PI) - Mathf.PI;
        public static Vector2 RelativeInput(float x, float y, float yaw) => new Vector2(x * Mathf.Cos(yaw) + y * Mathf.Sin(yaw), -x * Mathf.Sin(yaw) + y * Mathf.Cos(yaw));
        public float SafeHeight(float x, float y, float z, bool occupied)
        {
            float height = Mathf.Max(config.groundClearance, y);
            if (occupied) return height;
            foreach (var room in layout.rooms)
                if (room.shape != "park" && Mathf.Abs(x - room.x) < room.width / 2 + config.domeClearance && Mathf.Abs(z - room.z) < room.depth / 2 + config.domeClearance)
                    height = Mathf.Max(height, room.height + .8f + config.domeClearance);
            foreach (var corridor in layout.corridors)
            {
                var a = new Vector2(corridor.a.x, corridor.a.z); var b = new Vector2(corridor.b.x, corridor.b.z); var point = new Vector2(x, z);
                var d = b - a; float t = d.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - a, d) / d.sqrMagnitude) : 0;
                if (Vector2.Distance(point, a + d * t) < corridor.width / 2 + config.domeClearance) height = Mathf.Max(height, 6);
            }
            return height;
        }
        public Vector3 Position(float x, float z, bool occupied)
        {
            float horizontal = Mathf.Cos(Pitch) * Distance;
            float px = x + Mathf.Sin(Yaw) * horizontal, pz = z + Mathf.Cos(Yaw) * horizontal;
            return new Vector3(px, SafeHeight(px, config.lookHeight + Mathf.Sin(Pitch) * Distance, pz, occupied), -pz);
        }
    }
}
