using Ares.Data;
using UnityEngine;

namespace Ares.World.Rover
{
    /// <summary>Manual orbit remains an offset from vehicle heading, so turns carry the chosen viewing angle.</summary>
    public sealed class RoverCamera
    {
        readonly RoverSettings settings;
        readonly OrbitCamera orbit;
        readonly CameraInput input = new CameraInput();
        float previousHeading;
        Vector3 focus;
        public float TargetYaw => orbit.TargetYaw;
        public float Pitch => orbit.Pitch;
        public RoverCamera(RoverSettings settings, LayoutData layout)
        {
            this.settings = settings;
            orbit = new OrbitCamera(new CameraConfiguration {
                defaultPitchDegrees = settings.cameraPitch, defaultDistance = settings.cameraDistance,
                minPitchDegrees = 10, maxPitchDegrees = 65, minDistance = 4.5f, maxDistance = 14,
                rotationDamping = 10, zoomDamping = 9, radiansPerPixel = .004f,
                wheelIntensity = .001f, minSensitivity = .25f, maxSensitivity = 2,
                groundClearance = .6f, domeClearance = .7f, lookHeight = 1.2f
            }, layout);
        }
        public void Enter(Transform rover)
        {
            previousHeading = rover.eulerAngles.y; focus = rover.position + Vector3.up * 1.25f;
            orbit.Yaw = orbit.TargetYaw = -previousHeading * Mathf.Deg2Rad;
            orbit.Pitch = orbit.TargetPitch = settings.cameraPitch * Mathf.Deg2Rad;
            orbit.Distance = orbit.TargetDistance = settings.cameraDistance; input.Clear();
        }
        public void ClearInput() => input.Clear();
        public void Recenter(Transform rover) { orbit.TargetYaw = -rover.eulerAngles.y * Mathf.Deg2Rad; orbit.TargetPitch = settings.cameraPitch * Mathf.Deg2Rad; orbit.TargetDistance = settings.cameraDistance; }
        public void Tick(Camera camera, Transform rover, float dt, bool blocked, bool pointerBlocked, float sensitivity, bool invert)
        {
            float heading = rover.eulerAngles.y;
            orbit.TargetYaw -= Mathf.DeltaAngle(previousHeading, heading) * Mathf.Deg2Rad;
            previousHeading = heading;
            if (blocked) input.Clear();
            else input.Read(orbit, camera, false, sensitivity, invert, pointerBlocked);
            orbit.Step(dt, false);
            float blend = 1 - Mathf.Exp(-settings.cameraDamping * dt);
            focus = Vector3.Lerp(focus, rover.position + Vector3.up * 1.25f, blend);
            float horizontal = Mathf.Cos(orbit.Pitch) * orbit.Distance;
            Vector3 destination = focus + new Vector3(Mathf.Sin(orbit.Yaw) * horizontal, Mathf.Sin(orbit.Pitch) * orbit.Distance, -Mathf.Cos(orbit.Yaw) * horizontal);
            // Pull the camera in before entering a building volume (imported scenery has no colliders).
            for (int i = 1; i <= 24; i++)
            {
                var sample = Vector3.Lerp(focus, destination, i / 24f);
                if (orbit.SafeHeight(sample.x, sample.y, -sample.z, false) <= sample.y + .01f) continue;
                destination = Vector3.Lerp(focus, destination, Mathf.Max(.12f, (i - 1) / 24f)); break;
            }
            camera.transform.position = Vector3.Lerp(camera.transform.position, destination, blend);
            camera.transform.rotation = Quaternion.Slerp(camera.transform.rotation,
                Quaternion.LookRotation(focus - camera.transform.position, Vector3.up), blend);
        }
    }
}
