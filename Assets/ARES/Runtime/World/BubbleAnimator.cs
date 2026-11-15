using UnityEngine;

namespace Ares.World
{
    /// <summary>Original fish, mouth, blink, fin and air-bubble animation.</summary>
    public sealed class BubbleAnimator : MonoBehaviour
    {
        public Transform fish, tail, mouth, fin, bubbles, eyes;
        float heading;

        public void Follow(PlayerMotion player, float elapsed, float dt)
        {
            float side = player.Heading + Mathf.PI / 2;
            float x = player.X + Mathf.Sin(side) * 1.1f - Mathf.Sin(player.Heading) * .4f;
            float z = player.Z + Mathf.Cos(side) * 1.1f - Mathf.Cos(player.Heading) * .4f;
            float k = 1 - Mathf.Exp(-3 * Mathf.Min(dt, .05f));
            var p = transform.position;
            transform.position = new Vector3(p.x + (x - p.x) * k, 2.3f + Mathf.Sin(elapsed * 1.6f) * .15f, p.z + (-z - p.z) * k);
            heading += (player.Heading - heading) * k;
            transform.rotation = ReferenceSpace.Euler(0, heading, 0);
        }

        public void Apply(float time, string mood, bool talking, bool wave)
        {
            fish.localRotation = ReferenceSpace.Euler(0, Mathf.Sin(time * .8f) * .4f, 0);
            fish.localPosition = Vector3.up * (Mathf.Sin(time * 2) * .04f);
            tail.localRotation = ReferenceSpace.Euler(Mathf.PI / 2, Mathf.Sin(time * 9) * .5f, 0);
            mouth.localScale = new Vector3(mood == "proud" ? 1.5f : 1, talking ? .4f + Mathf.Abs(Mathf.Sin(time * 14)) * 1.2f : mood == "worried" ? 1 : mood == "excited" ? 1.5f : .4f, 1);
            eyes.localScale = new Vector3(1, time % 4.3f > 4.12f ? .12f : 1, 1);
            fin.localRotation = ReferenceSpace.Euler(0, 0, wave ? .6f + Mathf.Sin(time * 8) * .6f : Mathf.Sin(time * 4) * .2f);
            for (int i = 0; i < bubbles.childCount; i++)
            {
                float seed = i == 0 ? 0 : i == 1 ? .33f : .66f;
                float k = (time * .4f + seed) % 1;
                var bubble = bubbles.GetChild(i);
                bubble.localPosition = ReferenceSpace.Position(Mathf.Sin(time + i) * .12f, -.2f + k * .6f, .15f);
                bubble.localScale = Vector3.one * (.03f + k * .03f);
            }
        }
    }
}
