using System;
using Ares.Domain;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Two persistent pages slide inside a clipped viewport; simulation state stays in Campaign.</summary>
    internal sealed class ComputerPanel
    {
        readonly Campaign campaign;
        readonly Func<bool> canInteract, reducedMotion;
        readonly Action<VisualElement, Action> energy;
        readonly VisualElement track, energyBody;
        readonly ScrollView oxygenPage, energyPage;
        readonly Button previous, next;
        readonly Label pageTitle;
        readonly OxygenView oxygen;
        int page;
        bool sliding;
        public VisualElement Root { get; }
        public ComputerPanel(VisualElement parent, Campaign campaign, Func<bool> canInteract, Action close,
            Action<VisualElement, Action> energy, Func<bool> reducedMotion = null)
        {
            this.campaign = campaign; this.canInteract = canInteract; this.energy = energy; this.reducedMotion = reducedMotion;
            Root = Box(parent, "computer-session"); Root.name = "computer-session";
            var shell = Box(Root, "computer-shell"); var header = Box(shell, "computer-header");
            Text(header, "ARES / COMPUTADOR CENTRAL", "computer-brand");
            pageTitle = Text(header, "", "computer-page-title");
            var navigation = Box(header, "computer-navigation");
            Button(navigation, "Voltar  [Esc]", () => { if (canInteract()) close(); }, "ghost").name = "computer-close";
            var viewport = Box(shell, "computer-viewport"); viewport.name = "computer-viewport";
            track = Box(viewport, "computer-track"); track.name = "computer-track";
            oxygenPage = Page(track); energyPage = Page(track);
            oxygen = new OxygenView(Box(oxygenPage.contentContainer), campaign, () => canInteract() && !sliding && page == 0);
            energyBody = Box(energyPage.contentContainer, "computer-energy-body");
            previous = Button(viewport, "‹", () => { if (canInteract() && !sliding) ShowOxygen(); }, "computer-arrow computer-arrow-left");
            previous.name = "computer-oxygen"; previous.tooltip = "Voltar ao oxigênio";
            next = Button(viewport, "›", () => { if (canInteract() && !sliding) ShowPage(1); }, "computer-arrow computer-arrow-right");
            next.name = "computer-energy"; next.tooltip = "Ir para o gerenciamento de energia";
            ApplyPage(); Refresh();
        }
        static ScrollView Page(VisualElement parent)
        { var scroll = new ScrollView(ScrollViewMode.Vertical); scroll.AddToClassList("computer-page"); parent.Add(scroll); return scroll; }
        public void ShowOxygen() => ShowPage(0);
        void ShowPage(int target)
        {
            if (target == page || sliding || (target == 1 && energy == null)) return;
            if (target == 1) { energyBody.Clear(); energy(energyBody, ShowOxygen); }
            page = target;
            bool instant = reducedMotion != null && reducedMotion();
            Root.EnableInClassList("computer-reduced-motion", instant);
            sliding = !instant; ApplyPage();
            track.style.translate = new Translate(new Length(page == 1 ? -50 : 0, LengthUnit.Percent), 0, 0);
            if (sliding) Root.schedule.Execute(() => { sliding = false; Refresh(); }).StartingIn(420);
        }
        void ApplyPage()
        {
            pageTitle.text = page == 0 ? "01 / 02   OXIGÊNIO" : "02 / 02   ENERGIA";
            previous.style.display = page == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            next.style.display = page == 0 && energy != null ? DisplayStyle.Flex : DisplayStyle.None;
            Refresh();
        }
        public void Refresh()
        {
            oxygen.Refresh();
            oxygenPage.SetEnabled(canInteract() && !sliding && page == 0);
            energyPage.SetEnabled(canInteract() && !sliding && page == 1);
            previous.SetEnabled(!sliding); next.SetEnabled(!sliding);
        }
    }
}
