using System;
using UnityEngine;

namespace Ares.World.Rover
{
    /// <summary>Deterministic arcade bicycle model, in Unity coordinates (+Z forward). No input or rendering.</summary>
    public sealed class RoverMotion
    {
        readonly RoverSettings settings;
        readonly Func<Vector3, bool> canOccupy;
        public Vector3 Position { get; private set; }
        public float Yaw { get; private set; }
        public float Speed { get; private set; }
        public float Steering { get; private set; }
        public float Travel { get; private set; }
        public bool Obstructed { get; private set; }
        public RoverMotion(RoverSettings settings, Func<Vector3, bool> canOccupy, Vector3? initialPosition = null)
        { this.settings = settings; this.canOccupy = canOccupy; Position = initialPosition ?? settings.spawn; Yaw = settings.spawnYaw; }
        public void Stop() { Speed = 0; Steering = 0; }
        public void Step(float throttle, float steer, float dt)
        {
            dt = Mathf.Clamp(dt, 0, .1f);
            throttle = Mathf.Clamp(throttle, -1, 1);
            Steering = Mathf.MoveTowards(Steering, Mathf.Clamp(steer, -1, 1), dt * 3);
            float target = throttle * (throttle >= 0 ? settings.maxSpeed : settings.reverseSpeed);
            float rate = Mathf.Abs(throttle) < .01f ? settings.rollingDrag
                : Speed * throttle < 0 ? settings.braking : settings.acceleration;
            Speed = Mathf.MoveTowards(Speed, target, rate * dt);
            Obstructed = false;
            // Small sweeps prevent tunnelling at low frame rates. Collision never adds wheel travel.
            int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(Speed) * dt / .15f));
            float step = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float angle = Steering * settings.steeringDegrees / (1 + Mathf.Abs(Speed) * .035f);
                float yaw = Yaw + Speed / Mathf.Max(.1f, settings.wheelbase) * Mathf.Tan(angle * Mathf.Deg2Rad) * Mathf.Rad2Deg * step;
                Vector3 next = Position + Quaternion.Euler(0, yaw, 0) * Vector3.forward * (Speed * step);
                if (!canOccupy(next)) { Speed = 0; Obstructed = true; break; }
                Position = next; Yaw = Mathf.Repeat(yaw, 360); Travel += Speed * step;
            }
        }
    }
}
