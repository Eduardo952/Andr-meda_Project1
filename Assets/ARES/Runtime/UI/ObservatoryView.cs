using System;
using Ares.Domain;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Reference-inspired eyepiece. Presentation stays separate from the APOD service and journal.</summary>
    internal sealed class ObservatoryView
    {
        readonly Campaign campaign;
        readonly ApodService service;
        readonly VisualElement root, details;
        readonly ObservatoryLens lens;
        readonly Label title, credit;
        int selected;

        public ObservatoryView(VisualElement root, Campaign campaign, ApodService service, Action close)
        {
            this.root = root; this.campaign = campaign; this.service = service;
            selected = Math.Min(campaign.SolIndex, DiscoveryJournal.Days - 1);
            root.name = "observatory-view"; root.AddToClassList("telescope-view");
            root.parent.AddToClassList("telescope-backdrop");
            var exit = Button(root, "×", close, "telescope-exit");
            exit.name = "observatory-close"; exit.tooltip = "Voltar à missão (Esc)";
            var content = Box(root, "telescope-content");
            lens = new ObservatoryLens(content);
            title = Text(content, "", "telescope-title"); title.name = "observatory-title";
            credit = Text(content, "", "telescope-credit");
            var controls = Box(content, "telescope-controls");
            lens.BuildControls(controls);
            Button(controls, "Observar", () => ShowDetails(true), "telescope-observe").name = "observatory-observe";
            var sources = Box(content, "telescope-sources");
            Button(sources, "Imagem real: APOD", () => OpenSource(service.Collection.entries[selected].sourceUrl));
            Button(sources, "Biblioteca NASA", () => OpenSource("https://images.nasa.gov/"));
            details = Box(root, "telescope-details"); details.name = "observatory-details";
            ShowDetails(false);
            root.RegisterCallback<GeometryChangedEvent>(_ => Resize());
            service.Changed += Refresh;
            root.RegisterCallback<DetachFromPanelEvent>(_ => service.Changed -= Refresh);
            Refresh(); service.Load(selected);
        }

        void Resize()
        {
            // Keep the lens circular and reserve room for controls, including on short windows.
            float diameter = Mathf.Max(100, Mathf.Min(root.contentRect.width - 40, root.contentRect.height - 228));
            lens.Resize(diameter);
            root.EnableInClassList("telescope-compact", root.contentRect.width < 600);
        }

        void Select(int day)
        {
            if (!campaign.Discoveries.CanView(day, campaign.SolIndex)) return;
            selected = day; lens.ResetZoom(); Refresh(); service.Load(day);
        }

        void Refresh()
        {
            var entry = service.Collection.entries[selected];
            var texture = service.Image(selected);
            lens.SetImage(texture); title.text = entry.title;
            string author = service.Record(selected)?.copyright;
            credit.text = "Créditos: " + Ares.Data.ApodRecord.PlainText(string.IsNullOrWhiteSpace(author) ? entry.credit : author);
            if (texture != null) campaign.Discoveries.Record(selected, campaign.SolIndex);
            details.Clear();
            Button(details, "Voltar à lente", () => ShowDetails(false)).name = "observatory-details-close";
            var scroll = new ScrollView(); details.Add(scroll);
            Text(scroll, entry.title, "telescope-details-title");
            Text(scroll, entry.summary).name = "observatory-summary";
            Text(scroll, entry.question, "telescope-question");
            Text(scroll, "Uma imagem sorteada a cada Sol, sem repetir. Reveja suas descobertas:");
            var days = Box(scroll, "telescope-days");
            for (int i = 0; i < DiscoveryJournal.Days; i++)
            {
                int day = i;
                var button = Button(days, "Sol " + (i + 1), () => Select(day));
                button.name = "observatory-day-" + i;
                button.EnableInClassList("selected", i == selected);
                button.SetEnabled(campaign.Discoveries.CanView(i, campaign.SolIndex));
            }
            Text(scroll, "Estas são fotos reais do arquivo APOD, não uma transmissão ao vivo de Marte. As cores podem mostrar luz que nossos olhos não enxergam.");
            Text(scroll, service.Status(selected), "telescope-detail-credit").name = "observatory-status";
            if (!string.IsNullOrWhiteSpace(entry.licenseUrl))
                Button(scroll, "Licença da imagem", () => OpenSource(entry.licenseUrl));
            var retry = Button(scroll, "Consultar NASA", () => service.Load(selected, true));
            retry.name = "observatory-retry"; retry.SetEnabled(!service.Loading(selected));
        }

        void ShowDetails(bool visible) => details.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        static void OpenSource(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https") Application.OpenURL(uri.AbsoluteUri);
        }
    }
}
