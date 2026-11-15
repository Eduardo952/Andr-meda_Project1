using UnityEngine;

namespace Ares.World
{
    public struct AstronautPose
    {
        public float bodyHeight, bodyPitch, leftArm, rightArm, rightArmWave, leftLeg, rightLeg;
    }

    /// <summary>Presentation formulas from characters/Astronaut.tsx. No mission state or rewards.</summary>
    public static class CharacterAnimation
    {
        public static AstronautPose Astronaut(float speed, float phase, float elapsed, float ambience,
            float jump, float observe, bool reducedMotion, float jumpSeconds)
        {
            float walk = Mathf.Min(1, speed / 4);
            float swing = Mathf.Sin(phase) * .58f * walk;
            float leap = reducedMotion ? 0 : Mathf.Sin(Mathf.PI * Mathf.Max(0, 1 - jump / jumpSeconds)) * (jump > 0 ? .9f : 0);
            return new AstronautPose
            {
                bodyHeight = (reducedMotion ? .4f : 1) * (1-Mathf.Cos(phase*2)) * .042f * walk + (reducedMotion?0:Mathf.Sin(elapsed * 1.7f) * .018f * (1 - walk)) + leap,
                bodyPitch = .28f * observe,
                leftArm = swing,
                rightArm = observe > .01f ? -1.3f * observe - swing * (1 - observe) : -swing,
                rightArmWave = !reducedMotion && ambience < 3 ? -1.9f + Mathf.Sin(ambience * 8) * .2f : 0,
                leftLeg = -swing,
                rightLeg = swing,
            };
        }

        public static float Observe(float current, bool observing, float dt) => current + ((observing ? 1 : 0) - current) * Mathf.Min(1, Mathf.Min(dt, .05f) * 4);

        public static float Facing(float x, float z, float heading, float observe, float telescopeX, float telescopeZ)
        {
            float target = Mathf.Atan2(telescopeX - x, telescopeZ - z);
            float difference = (target - heading + Mathf.PI * 3) % (Mathf.PI * 2) - Mathf.PI;
            return heading + difference * observe;
        }
    }
}
