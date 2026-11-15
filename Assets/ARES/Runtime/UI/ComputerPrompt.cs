using Ares.World.Computer;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Screen-space, world-anchored interaction cue using the project's authored cartoon alphabet.</summary>
    internal sealed class ComputerPrompt
    {
        readonly VisualElement root,prompt;
        public bool Visible { get; private set; }
        public ComputerPrompt(VisualElement root)
        {
            this.root=root;prompt=Box(root,"computer-prompt");prompt.name="computer-prompt";
            var key=Box(prompt,"computer-key");key.Add(new CartoonText("E",39));
            Text(prompt,"Acessar computador","computer-prompt-text");
            Ignore(prompt);prompt.style.display=DisplayStyle.None;
        }
        static void Ignore(VisualElement node) {node.pickingMode=PickingMode.Ignore;foreach(var child in node.Children())Ignore(child);}
        public void Update(Camera camera,ComputerSettings settings,bool available,bool reducedMotion)
        {
            Visible=false;
            if(!available || camera==null || root.panel==null) {prompt.style.display=DisplayStyle.None;return;}
            Vector3 screen=camera.WorldToScreenPoint(settings.screenCenter+Vector3.up*(settings.screenSize.y*.5f+.45f));
            if(screen.z<camera.nearClipPlane || screen.x<0 || screen.x>Screen.width || screen.y<0 || screen.y>Screen.height) {prompt.style.display=DisplayStyle.None;return;}
            Vector2 position=RuntimePanelUtils.ScreenToPanel(root.panel,new Vector2(screen.x,Screen.height-screen.y));
            float bob=reducedMotion?0:Mathf.Sin(Time.unscaledTime*2.8f)*3;
            prompt.style.left=Mathf.Clamp(position.x-95,12,Mathf.Max(12,root.resolvedStyle.width-202));
            // Keep the key below the clock/navigation strip when the monitor projects near the top edge.
            prompt.style.top=Mathf.Clamp(position.y-100+bob,165,Mathf.Max(165,root.resolvedStyle.height-125));
            prompt.style.display=DisplayStyle.Flex;Visible=true;
        }
    }
}
