using UnityEngine;

namespace Ares.World
{
    /// <summary>Applies the reference pose to imported pivots, without changing geometry.</summary>
    public sealed class AstronautAnimator : MonoBehaviour
    {
        public Transform body, leftArm, rightArm, leftLeg, rightLeg;

        public void Apply(AstronautPose pose)
        {
            body.localPosition = Vector3.up * pose.bodyHeight;
            body.localRotation = ReferenceSpace.Euler(pose.bodyPitch, 0, 0);
            leftArm.localRotation = ReferenceSpace.Euler(pose.leftArm, 0, 0);
            rightArm.localRotation = ReferenceSpace.Euler(pose.rightArm, 0, pose.rightArmWave);
            leftLeg.localRotation = ReferenceSpace.Euler(pose.leftLeg, 0, 0);
            rightLeg.localRotation = ReferenceSpace.Euler(pose.rightLeg, 0, 0);
        }
    }
}
