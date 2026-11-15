using UnityEngine;

namespace Ares.World
{
    /// <summary>Small code-built cartoon prop; the same broom moves from its rack to the astronaut.</summary>
    public sealed class SolarBroomView
    {
        public Vector3 PickupPosition { get; }
        // These sockets match the imported glove and the centre of the padded handle.
        public static readonly Vector3 HandSocket = new Vector3(0, -.55f, 0);
        public static readonly Vector3 GripSocket = new Vector3(0, .95f, 0);
        readonly Transform broom;
        public SolarBroomView(Transform parent, Material material)
        {
            PickupPosition = parent.GetComponentInParent<ReferenceExplorer>().definition.layout.SolarPoint(3,0,69);
            var rack = new GameObject("Broom rack").transform; rack.SetParent(parent, false); rack.position = PickupPosition;
            Part(rack, "Foot", PrimitiveType.Cube, new Vector3(0,.1f,0), new Vector3(1.3f,.2f,.8f), new Color(.26f,.32f,.35f), material);
            Part(rack, "Back", PrimitiveType.Cube, new Vector3(0,.6f,.22f), new Vector3(.12f,1.1f,.12f), new Color(.9f,.68f,.25f), material);
            broom = new GameObject("Solar cleaning broom").transform; broom.SetParent(parent, false);
            Part(broom, "Handle", PrimitiveType.Cylinder, new Vector3(0,.95f,0), new Vector3(.07f,.85f,.07f), new Color(.65f,.36f,.18f), material);
            Part(broom, "Grip", PrimitiveType.Cylinder, GripSocket, new Vector3(.105f,.18f,.105f), new Color(.3f,.61f,.58f), material);
            Part(broom, "Brush head", PrimitiveType.Cube, new Vector3(0,.18f,0), new Vector3(.85f,.18f,.24f), new Color(.95f,.66f,.22f), material);
            for (int i=0;i<9;i++) Part(broom, "Bristles", PrimitiveType.Cube, new Vector3((i-4)*.09f,.07f,0), new Vector3(.07f,.17f,.22f), new Color(.87f,.79f,.58f), material);
        }
        static void Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Material material)
        {
            var part = GameObject.CreatePrimitive(type); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            Object.Destroy(part.GetComponent<Collider>());
            var renderer = part.GetComponent<Renderer>(); renderer.sharedMaterial = material;
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", color); renderer.SetPropertyBlock(block);
        }
        public void Update(AstronautAnimator actor, bool owned, bool cleaning, bool reducedMotion, Vector3 target, bool showHeld = true)
        {
            broom.gameObject.SetActive(!owned || showHeld);
            if (!owned || actor == null) { broom.position = PickupPosition + new Vector3(0,.21f,0); broom.rotation = Quaternion.Euler(0,0,-12); return; }
            if (!showHeld) return; // The watering can owns the hand inside the greenhouse.
            var astronaut = actor.transform;
            float sweep = cleaning && !reducedMotion ? Mathf.Sin(Time.time * 9) : 0;
            // Override the idle wave/walk swing, then anchor the tool to the animated glove.
            actor.rightArm.localRotation = ReferenceSpace.Euler(cleaning ? -1.9f : -1f, 0, sweep * .12f);
            Vector3 hand = actor.rightArm.TransformPoint(HandSocket);
            if (cleaning)
            {
                Vector3 up = hand - (target + astronaut.right * (sweep * .3f));
                if (up.sqrMagnitude < .01f) up = Vector3.up;
                up.Normalize();
                Vector3 forward = Vector3.Cross(astronaut.right, up);
                if (forward.sqrMagnitude < .01f) forward = Vector3.Cross(astronaut.forward, up);
                broom.rotation = Quaternion.LookRotation(forward, up);
            }
            else broom.rotation = astronaut.rotation * Quaternion.Euler(-18, 0, 12);
            broom.position = hand - broom.rotation * GripSocket;
        }
    }
}
