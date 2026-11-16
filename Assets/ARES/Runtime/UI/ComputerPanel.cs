using System;
using Ares.Domain;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Oxygen and energy control pages share a clipped viewport; simulation state stays in Campaign.</summary>
    internal sealed class ComputerPanel
    {
        readonly Campaign campaign;
        readonly Func<bool> canInteract, reducedMotion;
        readonly Action<VisualElement, Action> energy;
        readonly VisualElement track, energyBody;
        readonly ScrollView oxygenPage, energyPage;
        readonly Button previous, next;
        readonly Button seal;
        readonly Button repair;
        readonly Label pageTitle;
        readonly OxygenView oxygen;
        readonly bool energyOnly;
        readonly Action close;
        int page;
        bool sliding;
        public VisualElement Root { get; }
        public ComputerPanel(VisualElement parent, Campaign campaign, Func<bool> canInteract, Action close,
            Action<VisualElement, Action> energy, Func<bool> reducedMotion = null, bool energyOnly = false)
        {
            this.campaign = campaign; this.canInteract = canInteract; this.energy = energy; this.reducedMotion = reducedMotion;
            this.energyOnly = energyOnly; this.close = close;
            Root = Box(parent, "computer-session"); Root.name = "computer-session";
            var shell = Box(Root, "computer-shell"); var header = Box(shell, "computer-header");
            Text(header, "ARES / COMPUTADOR CENTRAL", "computer-brand");
            pageTitle = Text(header, "", "computer-page-title");
            var navigation = Box(header, "computer-navigation");
            Button(navigation, "Voltar  [Esc]", () => { if (canInteract()) close(); }, "ghost").name = "computer-close";
            var locale=new Localization(Ares.UI.Localization.CurrentLanguage);
            seal=Button(navigation,locale.T("hud.endSol"),()=>{if(canInteract() && campaign.Seal("control",campaign.Allocation))close();},"ghost");seal.name="computer-seal-sol";
            repair=Button(navigation,locale.T("hazard.repair"),()=>{if(canInteract())campaign.RepairGrid();},"ghost");repair.name="computer-repair-grid";
            var viewport = Box(shell, "computer-viewport"); viewport.name = "computer-viewport";
            track = Box(viewport, "computer-track"); track.name = "computer-track";
            oxygenPage = Page(track); energyPage = Page(track);
            track.style.width = new UnityEngine.UIElements.Length(200, LengthUnit.Percent);
            oxygenPage.style.width = energyPage.style.width = new UnityEngine.UIElements.Length(50, LengthUnit.Percent);
            oxygen = new OxygenView(Box(oxygenPage.contentContainer), campaign, () => canInteract() && !sliding && page == 0);
            energyBody = Box(energyPage.contentContainer, "computer-energy-body");
            previous = Button(viewport, "‹", () => { if (canInteract() && !sliding) ShowPage(page - 1); }, "computer-arrow computer-arrow-left");
            previous.name = "computer-oxygen"; previous.tooltip = "Voltar ao oxigênio";
            next = Button(viewport, "›", () => { if (canInteract() && !sliding) ShowPage(page + 1); }, "computer-arrow computer-arrow-right");
            next.name = "computer-energy"; next.tooltip = "Ir para o gerenciamento de energia";
            ApplyPage(); Refresh();
        }
        static ScrollView Page(VisualElement parent)
        { var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("computer-page"); parent.Add(scroll); return scroll; }
        public void ShowOxygen() => ShowPage(0);
        void ShowPage(int target)
        {
            if (energyOnly && target != 1) { if (target == 0) close(); return; }
            if (target < 0 || target > 1 || target == page || sliding || (target == 1 && energy == null)) return;
            if (target == 1) { energyBody.Clear(); energy(energyBody, energyOnly ? close : ShowOxygen); }
            page = target;
            bool instant = reducedMotion != null && reducedMotion();
            Root.EnableInClassList("computer-reduced-motion", instant);
            sliding = !instant; ApplyPage();
            track.style.translate = new Translate(new Length(-page * (100f / 2), LengthUnit.Percent), 0, 0);
            if (sliding) Root.schedule.Execute(() => { sliding = false; Refresh(); }).StartingIn(420);
        }
        void ApplyPage()
        {
            pageTitle.text = page == 0 ? "01 / 02   OXIGÊNIO" : "02 / 02   ENERGIA";
            previous.style.display = page > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            previous.tooltip = "Voltar ao oxigênio";
            next.name = "computer-energy";
            next.tooltip = "Ir para energia";
            next.style.display = page < 1 && energy != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (energyOnly)
            {
                pageTitle.text = "TUTORIAL / DIVIDIR ENERGIA";
                previous.style.display = next.style.display = DisplayStyle.None;
            }
            Refresh();
        }
        public void Refresh()
        {
            if (energyOnly && page != 1 && canInteract()) { ShowPage(1); return; }
            oxygen.Refresh();
            oxygenPage.SetEnabled(canInteract() && !sliding && page == 0);
            energyPage.SetEnabled(canInteract() && !sliding && page == 1);
            previous.SetEnabled(!sliding); next.SetEnabled(!sliding);
            seal.style.display=campaign.NativeHazards && !energyOnly && campaign.Phase==Phase.Tasks?DisplayStyle.Flex:DisplayStyle.None;
            seal.SetEnabled(canInteract() && campaign.AllTasksDone && campaign.SolRemainingSeconds<=0 && !campaign.RadioBlackout);
            repair.style.display=campaign.NativeHazards && !energyOnly && campaign.Grid?.OfflineSystem!=null?DisplayStyle.Flex:DisplayStyle.None;repair.SetEnabled(canInteract());
        }
    }
}
