using System;
using Ares.Domain;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Computer navigation shell. The same oxygen view is used on the monitor and in the foreground.</summary>
    internal sealed class ComputerPanel
    {
        readonly Campaign campaign;
        readonly Func<bool> canInteract;
        readonly Action<VisualElement,Action> energy;
        readonly VisualElement content;
        readonly Button oxygenTab,energyTab,seal;
        OxygenView oxygen;
        public VisualElement Root { get; }
        public ComputerPanel(VisualElement parent,Campaign campaign,Func<bool> canInteract,Action close,Action<VisualElement,Action> energy,Action sealAction=null)
        {
            this.campaign=campaign;this.canInteract=canInteract;this.energy=energy;
            Root=Box(parent,"computer-session");Root.name="computer-session";
            var shell=Box(Root,"computer-shell");var header=Box(shell,"computer-header");
            Text(header,"ARES / COMPUTADOR CENTRAL","computer-brand");
            var navigation=Box(header,"computer-navigation");
            oxygenTab=Button(navigation,"Oxigênio",()=>{if(canInteract())ShowOxygen();},"ghost");oxygenTab.name="computer-oxygen";
            energyTab=Button(navigation,"Energia",()=>{if(canInteract())ShowEnergy();},"ghost");energyTab.name="computer-energy";
            if(sealAction!=null)seal=Button(navigation,"Encerrar Sol",()=>{if(canInteract())sealAction();},"alt");
            Button(navigation,"Voltar  [Esc]",()=>{if(canInteract())close();},"ghost").name="computer-close";
            var scroll=new ScrollView(ScrollViewMode.Vertical);scroll.AddToClassList("computer-content");shell.Add(scroll);content=scroll.contentContainer;
            ShowOxygen();
        }
        public void ShowOxygen()
        {
            content.Clear();var body=Box(content);oxygen=new OxygenView(body,campaign,canInteract);
            oxygenTab.AddToClassList("selected");energyTab.RemoveFromClassList("selected");
        }
        void ShowEnergy()
        {
            if(energy==null)return;
            content.Clear();oxygen=null;var body=Box(content);body.style.marginTop=20;
            energy(body,ShowOxygen);energyTab.AddToClassList("selected");oxygenTab.RemoveFromClassList("selected");
        }
        public void Refresh()
        {
            oxygen?.Refresh();
            if(seal!=null)seal.style.display=campaign.Phase==Phase.Tasks&&campaign.AllTasksDone?DisplayStyle.Flex:DisplayStyle.None;
        }
    }
}
