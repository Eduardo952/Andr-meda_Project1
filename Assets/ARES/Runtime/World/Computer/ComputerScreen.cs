using Ares.Domain;
using Ares.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.World.Computer
{
    /// <summary>Live UI Toolkit render texture on the existing console's glass. No second simulation or duplicate input.</summary>
    internal sealed class ComputerScreen : System.IDisposable
    {
        readonly GameObject surface,documentObject;
        readonly RenderTexture texture;
        readonly PanelSettings panel;
        readonly Material material;
        readonly Mesh mesh;
        readonly UIDocument document;
        ComputerPanel view;
        public ComputerScreen(Transform parent,ComputerSettings settings)
        {
            // Rounded UI clipping uses stencil; a depth/stencil target is required for the monitor just as for the main panel.
            texture=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32) {name="Computer live interface",filterMode=FilterMode.Bilinear};texture.Create();
            panel=ScriptableObject.CreateInstance<PanelSettings>();panel.name="Computer monitor panel";
            panel.themeStyleSheet=Resources.Load<ThemeStyleSheet>("ARES/UI/RuntimeTheme");panel.targetTexture=texture;
            panel.scaleMode=PanelScaleMode.ConstantPixelSize;
            panel.SetScreenToPanelSpaceFunction(_=>new Vector2(float.NaN,float.NaN));
            // A UIDocument below another UIDocument inherits its parent's panel. The monitor needs an independent target.
            documentObject=new GameObject("Computer / offscreen document");
            document=documentObject.AddComponent<UIDocument>();document.panelSettings=panel;
            var root=document.rootVisualElement;root.AddToClassList("ares-root");root.pickingMode=PickingMode.Ignore;
            foreach(string sheet in new[]{"Game","Oxygen","MissionTheme","Refinement","ReferenceUX"})root.styleSheets.Add(Resources.Load<StyleSheet>("ARES/UI/"+sheet));
            root.AddToClassList("computer-physical-screen");PresentationLocalizer.Attach(root);
            documentObject.AddComponent<PresentationLocalizationLoop>().Bind(root);
            surface=new GameObject("Computer / live screen surface");surface.transform.SetParent(parent,false);
            surface.transform.SetPositionAndRotation(settings.screenCenter,Quaternion.LookRotation(-settings.screenNormal.normalized));
            float x=settings.screenSize.x*.5f,y=settings.screenSize.y*.5f;
            mesh=new Mesh {name="Computer glass quad"};
            mesh.vertices=new[]{new Vector3(-x,-y,0),new Vector3(-x,y,0),new Vector3(x,y,0),new Vector3(x,-y,0)};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};mesh.RecalculateNormals();mesh.RecalculateBounds();
            surface.AddComponent<MeshFilter>().sharedMesh=mesh;
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit")) {name="Computer / display"};material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);
            var renderer=surface.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        public void Bind(Campaign campaign)
        {document.rootVisualElement.Clear();view=new ComputerPanel(document.rootVisualElement,campaign,()=>false,()=>{},null);}
        public void Refresh() {view?.Refresh();PresentationLocalizer.Refresh(document.rootVisualElement);}
        public void Dispose()
        {
            Object.Destroy(surface);Object.Destroy(documentObject);Object.Destroy(mesh);Object.Destroy(material);Object.Destroy(panel);texture.Release();Object.Destroy(texture);
        }
    }
}
