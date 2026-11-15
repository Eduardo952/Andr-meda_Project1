using UnityEngine;

namespace Ares.World.Computer
{
    /// <summary>Exclusive camera session. Reversible eased dolly; Escape may reverse at any point.</summary>
    public sealed class ComputerCamera
    {
        readonly ComputerSettings settings;
        Camera camera;
        Vector3 position, destination, control, look;
        Quaternion rotation;
        float fieldOfView;
        bool closing;
        public bool Active { get; private set; }
        public bool Focused => Active && !closing && Progress >= 1;
        public float Progress { get; private set; }
        public float InterfaceOpacity => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.64f, 1, Progress));
        public ComputerCamera(ComputerSettings settings) { this.settings = settings; }
        public void Begin(Camera view)
        {
            camera = view; position = view.transform.position; rotation = view.transform.rotation; fieldOfView = view.fieldOfView;
            look = position + view.transform.forward * Vector3.Distance(position, settings.screenCenter);
            float distance = settings.screenSize.y * .48f / Mathf.Tan(settings.focusedFieldOfView * .5f * Mathf.Deg2Rad);
            destination = settings.screenCenter + settings.screenNormal.normalized * distance;
            control = settings.screenCenter + settings.screenNormal.normalized * Mathf.Max(5, distance + 2) + Vector3.up * .4f;
            Progress = 0; closing = false; Active = true;
        }
        public void Close() { if (Active) closing = true; }
        public void Tick(float dt, bool reducedMotion)
        {
            if (!Active || camera == null) return;
            float duration = reducedMotion ? .22f : Mathf.Max(.2f, settings.transitionSeconds);
            Progress = Mathf.Clamp01(Progress + (closing ? -1 : 1) * dt / duration);
            float t = Progress * Progress * Progress * (Progress * (Progress * 6 - 15) + 10);
            if (reducedMotion)
            {
                // Accessibility setting uses a short dissolve without spatial flight.
                camera.transform.SetPositionAndRotation(position, rotation); camera.fieldOfView = fieldOfView;
            }
            else
            {
                Vector3 p = (1-t)*(1-t)*position + 2*(1-t)*t*control + t*t*destination;
                Vector3 target = Vector3.Lerp(look, settings.screenCenter, Mathf.SmoothStep(0,1,Mathf.Clamp01(t*1.5f)));
                camera.transform.SetPositionAndRotation(p, Quaternion.Slerp(rotation, Quaternion.LookRotation(target-p), Mathf.SmoothStep(0,1,Mathf.Clamp01(t*3))));
                camera.fieldOfView = Mathf.Lerp(fieldOfView, settings.focusedFieldOfView, t);
            }
            if (closing && Progress <= 0) Reset();
        }
        public void Reset()
        {
            if (Active && camera != null) { camera.transform.SetPositionAndRotation(position, rotation); camera.fieldOfView = fieldOfView; }
            Active = false; Progress = 0; closing = false;
        }
    }
}
