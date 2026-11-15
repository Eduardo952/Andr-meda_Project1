using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Native title diorama using TitleScreen.tsx transforms and the imported Bubble rig.</summary>
    internal sealed class TitleBackdrop : System.IDisposable
    {
        readonly GameObject stage;
        readonly Transform mars;
        readonly BubbleAnimator bubble;
        readonly Camera camera;
        readonly RenderTexture texture;
        readonly Material coral, rust, stars;
        readonly Mesh starMesh;
        readonly Vector3 offset = new Vector3(0, -1000, 0);
        float time;
        public TitleBackdrop(VisualElement parent, BubbleAnimator original)
        {
            stage = new GameObject("ARES title diorama"); stage.transform.position = offset;
            camera = new GameObject("Title camera").AddComponent<Camera>(); camera.transform.SetParent(stage.transform, false);
            camera.transform.localPosition = new Vector3(0,.5f,-6); camera.transform.LookAt(offset);
            camera.fieldOfView = 50; camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(36,26,61,255);
            texture = new RenderTexture(Mathf.Min(Screen.width,1920), Mathf.Min(Screen.height,1080),24); texture.Create(); camera.targetTexture = texture;
            var image = new Image { image = texture, scaleMode = ScaleMode.StretchToFill, pickingMode = PickingMode.Ignore };
            image.style.position = Position.Absolute; image.style.left = 0; image.style.top = 0; image.style.right = 0; image.style.bottom = 0; parent.Add(image);
            coral = Material(new Color32(219,128,92,255)); rust = Material(new Color32(185,92,58,255)); stars = Material(new Color32(255,246,224,255), true);
            mars = new GameObject("Mars").transform; mars.SetParent(stage.transform,false); mars.localPosition = new Vector3(0,-5.2f,2);
            Sphere(mars, Vector3.zero, Vector3.one * 10, coral);
            Vector3[] craters = { new Vector3(1,4,-2.6f), new Vector3(-2.5f,3.6f,-2.2f), new Vector3(3,2,-3.4f), new Vector3(-1,2.5f,-4.1f) };
            for (int i = 0; i < craters.Length; i++) Sphere(mars, craters[i], new Vector3(1,.3f,1) * (1.2f + i*.2f), rust);
            if (original != null) { bubble = Object.Instantiate(original, stage.transform); bubble.transform.localPosition = new Vector3(1.9f,.4f,0); bubble.transform.localScale = Vector3.one * 1.1f; bubble.transform.localRotation = Quaternion.identity; SetLayer(bubble.transform); }
            var light = new GameObject("Title sunlight").AddComponent<Light>(); light.transform.SetParent(stage.transform,false); light.type = LightType.Directional; light.color = new Color32(255,226,184,255); light.intensity = 2.2f; light.cullingMask = 1 << 31; light.transform.rotation = Quaternion.LookRotation(new Vector3(4,-5,4));
            starMesh = Stars(); var sky = new GameObject("Title stars"); sky.transform.SetParent(stage.transform,false); sky.layer = 31; sky.AddComponent<MeshFilter>().sharedMesh = starMesh; sky.AddComponent<MeshRenderer>().sharedMaterial = stars;
        }
        static Material Material(Color color, bool unlit = false) { var material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); material.SetColor("_BaseColor",color); return material; }
        static void Sphere(Transform parent, Vector3 position, Vector3 scale, Material material)
        { var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); Object.Destroy(sphere.GetComponent<Collider>()); sphere.layer = 31; sphere.transform.SetParent(parent,false); sphere.transform.localPosition = position; sphere.transform.localScale = scale; sphere.GetComponent<Renderer>().sharedMaterial = material; }
        static void SetLayer(Transform parent) { parent.gameObject.layer = 31; foreach(Transform child in parent) SetLayer(child); }
        static Mesh Stars()
        {
            long seed = 7; System.Func<float> random = () => { seed = seed * 16807 % 2147483647; return seed / 2147483647f; };
            var vertices = new Vector3[2400]; var indices = new int[3600];
            for (int i=0;i<600;i++) { var p = new Vector3((random()-.5f)*60,(random()-.3f)*30,15+random()*10); float s=.025f;
                vertices[i*4]=p+new Vector3(-s,-s,0); vertices[i*4+1]=p+new Vector3(-s,s,0); vertices[i*4+2]=p+new Vector3(s,s,0); vertices[i*4+3]=p+new Vector3(s,-s,0);
                int k=i*6,v=i*4; indices[k]=v;indices[k+1]=v+1;indices[k+2]=v+2;indices[k+3]=v;indices[k+4]=v+2;indices[k+5]=v+3; }
            var mesh = new Mesh { vertices=vertices, triangles=indices }; mesh.RecalculateBounds(); return mesh;
        }
        public void Tick(float dt, bool reduced)
        { if (!reduced) time += dt; mars.localRotation = Quaternion.Euler(0,-time*.08f*Mathf.Rad2Deg,0); if (bubble != null) { bubble.transform.localPosition = new Vector3(1.9f,.4f+Mathf.Sin(time*1.5f)*.12f,0); bubble.transform.localRotation = Quaternion.Euler(0,0,-Mathf.Sin(time*2)*.08f*Mathf.Rad2Deg); bubble.Apply(time,"happy",true,true); } }
        public void Dispose() { camera.targetTexture = null; texture.Release(); Object.Destroy(texture); Object.Destroy(stage); Object.Destroy(coral); Object.Destroy(rust); Object.Destroy(stars); Object.Destroy(starMesh); }
    }
}
