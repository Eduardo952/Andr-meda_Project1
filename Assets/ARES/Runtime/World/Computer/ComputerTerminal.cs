using System;
using Ares.Domain;
using Ares.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.World.Computer
{
    /// <summary>Proximity and session coordinator. Rules live in OxygenPlant; camera and views have separate lifetimes.</summary>
    public sealed class ComputerTerminal : MonoBehaviour
    {
        ReferenceExplorer explorer;
        GameFrontend frontend;
        ComputerSettings settings;
        VisualElement root;
        ComputerScreen screen;
        ComputerPrompt prompt;
        ComputerPanel foreground;
        Campaign campaign;
        float refresh;
        public ComputerCamera CameraRig { get; private set; }
        public bool OwnsCamera => CameraRig != null && CameraRig.Active;
        public bool Focused => CameraRig != null && CameraRig.Focused;
        public bool PromptVisible => prompt != null && prompt.Visible;
        public bool Near
        {
            get
            {
                if(explorer==null || explorer.IsDrivingRover || campaign==null || (campaign.Phase!=Phase.Plan && campaign.Phase!=Phase.Tasks))return false;
                var p=explorer.PlayerPosition;var delta=p-settings.standPoint;delta.y=0;
                return delta.sqrMagnitude<=settings.interactionRadius*settings.interactionRadius && Vector3.Dot(p-settings.screenCenter,settings.screenNormal)>.5f;
            }
        }
        public void Initialize(ReferenceExplorer explorer,GameFrontend frontend,VisualElement root)
        {
            this.explorer=explorer;this.frontend=frontend;this.root=root;settings=explorer.computerSettings;
            CameraRig=new ComputerCamera(settings);screen=new ComputerScreen(transform,settings);
        }
        public void Bind(Campaign campaign) { Reset();this.campaign=campaign;screen.Bind(campaign); }
        public void AttachPrompt(VisualElement parent = null) {prompt=new ComputerPrompt(parent ?? root);}
        public bool TryInteract()
        {
            if(!Near || frontend.BlocksWorld || OwnsCamera || Camera.main==null)return false;
            if (frontend.TutorialActive && !frontend.Tutorial.AllowsComputer) return false;
            CameraRig.Begin(Camera.main);
            foreground=new ComputerPanel(root,campaign,()=>Focused && (!frontend.TutorialActive || frontend.Tutorial.AllowsEnergy),Close,frontend.BuildComputerEnergy,()=>explorer.reducedMotion,frontend.TutorialActive);
            foreground.Root.style.opacity=0;return true;
        }
        public void Close() {CameraRig?.Close();}
        public void Reset()
        {
            CameraRig?.Reset();foreground?.Root.RemoveFromHierarchy();foreground=null;
            RestoreHud();
        }
        void RestoreHud() {if(root==null)return;foreach(var child in root.Children())child.style.opacity=1;}
        void LateUpdate()
        {
            if(CameraRig==null)return;
            bool active=OwnsCamera;
            CameraRig.Tick(Mathf.Min(Time.unscaledDeltaTime,.05f),explorer.reducedMotion);
            if(OwnsCamera)
            {
                float hud=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(CameraRig.Progress*3));
                foreach(var child in root.Children())
                    if(child!=foreground.Root && child.name!="tutorial-layer" && child.name!="command-console" && child.name!="mission-event-banner")child.style.opacity=hud;
                foreground.Root.style.opacity=CameraRig.InterfaceOpacity;
                foreground.Root.style.display = frontend.TutorialActive && !frontend.Tutorial.AllowsEnergy ? DisplayStyle.None : DisplayStyle.Flex;
            }
            else if(active)
            {
                foreground?.Root.RemoveFromHierarchy();foreground=null;RestoreHud();
            }
            prompt?.Update(Camera.main,settings,Near&&!frontend.BlocksWorld,explorer.reducedMotion);
            refresh+=Time.unscaledDeltaTime;
            if(refresh>.15f) {refresh=0;foreground?.Refresh();screen.Refresh();}
        }
        void OnDisable() {Reset();}
        void OnDestroy() {Reset();screen?.Dispose();}
    }
}
