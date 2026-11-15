using UnityEngine;

namespace Ares.World
{
    /// <summary>Source-coordinate simulation: X/Z remain identical to game/sim/world.ts.</summary>
    public sealed class PlayerMotion
    {
        readonly WorldNavigation navigation;
        public float X, Z = 4, VelocityX, VelocityZ, Heading = Mathf.PI, Speed;
        public Vector3 UnityPosition => new Vector3(X, 0, -Z);
        public PlayerMotion(WorldNavigation navigation) { this.navigation = navigation; }
        public void Step(float ix, float iz, float dt, float maxSpeed, float acceleration)
        {
            float length = Mathf.Sqrt(ix * ix + iz * iz);
            if (length > 1) { ix /= length; iz /= length; }
            float damping = 1 - Mathf.Exp(-acceleration * dt);
            VelocityX += (ix * maxSpeed - VelocityX) * damping;
            VelocityZ += (iz * maxSpeed - VelocityZ) * damping;
            float nx = X + VelocityX * dt;
            if (navigation.Walkable(new Vector3(nx, 0, -Z))) X = nx; else VelocityX = 0;
            float nz = Z + VelocityZ * dt;
            if (navigation.Walkable(new Vector3(X, 0, -nz))) Z = nz; else VelocityZ = 0;
            Speed = Mathf.Sqrt(VelocityX * VelocityX + VelocityZ * VelocityZ);
            if (length > .1f)
            {
                float target = Mathf.Atan2(ix, iz);
                float delta = Mathf.Atan2(Mathf.Sin(target - Heading), Mathf.Cos(target - Heading));
                Heading += delta * Mathf.Min(1, dt * 12);
            }
        }
    }
}
