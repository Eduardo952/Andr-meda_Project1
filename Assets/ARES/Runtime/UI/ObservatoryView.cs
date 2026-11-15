using System;
using Ares.Domain;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Five-day photo diary, using the shared station design and real APOD assets.</summary>
    internal sealed class ObservatoryView
    {
        readonly Campaign campaign;
        readonly ApodService service;
        readonly VisualElement body, footer;
        readonly Action close;
        int selected;
        bool information;

        public ObservatoryView(VisualElement root, Campaign campaign, ApodService service, Action close)
        {
            this.campaign = campaign; this.service = service; this.close = close;
            selected = Math.Min(campaign.SolIndex, 4);
            root.name = "observatory-view";
            var panel = new ChallengePanel(root, new Localization("pt-BR"), "Diário de descobertas", "observatory",
                "APOD", false, () => { information = !information; Render(); }, close);
            body = panel.Body; footer = panel.Footer;
            service.Changed += Render;
            root.RegisterCallback<DetachFromPanelEvent>(_ => service.Changed -= Render);
            Render(); service.Load(selected);
        }

        void Select(int day)
        {
            if (!campaign.Discoveries.CanView(day, campaign.SolIndex)) return;
            selected = day; Render(); service.Load(day);
        }

        void Render()
        {
            body.Clear(); footer.Clear();
            var content = Box(body, "observatory-content");
            Text(content, "Uma nova descoberta a cada Sol. O que você consegue observar?", "heading");
            var days = Box(content, "row observatory-days");
            for (int i = 0; i < DiscoveryJournal.Days; i++)
            {
                int day = i;
                bool unlocked = campaign.Discoveries.CanView(day, campaign.SolIndex);
                var button = Button(days, "Sol " + (day + 1) + (campaign.Discoveries.Observed(day) ? " ✓" : ""), () => Select(day), day == selected ? "" : "ghost");
                button.name = "observatory-day-" + day;
                button.SetEnabled(unlocked);
                button.tooltip = unlocked ? "Explorar esta descoberta" : "Disponível no Sol " + (day + 1);
            }
            var entry = service.Collection.entries[selected];
            var texture = service.Image(selected);
            var photo = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit, name = "observatory-photo" };
            photo.AddToClassList("observatory-photo"); content.Add(photo);
            photo.tooltip = entry.title;
            if (texture != null) campaign.Discoveries.Record(selected, campaign.SolIndex);
            Text(content, entry.title, "heading").name = "observatory-title";
            Text(content, entry.summary, "observatory-summary").name = "observatory-summary";
            Text(content, entry.question, "mini-hint");
            Text(content, "Sol " + (selected + 1) + " da missão · APOD publicado em " + entry.date, "small");
            Text(content, "Créditos: " + entry.credit, "small");
            var remote = service.Record(selected);
            if (!string.IsNullOrWhiteSpace(remote?.copyright)) Text(content, "Crédito informado pela API: " + Ares.Data.ApodRecord.PlainText(remote.copyright), "small");
            Text(content, service.Status(selected), "small").name = "observatory-status";
            if (information)
            {
                Text(content, "Este telescópio abre imagens reais publicadas no APOD. A coleção usa datas históricas: o dia da missão não é a data de publicação. Os resumos em português foram adaptados das explicações oficiais. As cores podem representar luz que nossos olhos não enxergam.", "mini-hint");
                var record = service.Record(selected);
                if (record != null) Text(content, "Título original: " + Ares.Data.ApodRecord.PlainText(record.title), "small");
                Text(content, entry.sourceUrl, "small");
                if (!string.IsNullOrWhiteSpace(entry.licenseUrl)) Text(content, "Licença da imagem: " + entry.licenseUrl + " · imagem sem alterações", "small");
            }
            var retry = Button(footer, "Consultar NASA", () => service.Load(selected, true), "ghost");
            retry.name = "observatory-retry"; retry.SetEnabled(!service.Loading(selected));
            Button(footer, "Voltar à missão", close).name = "observatory-close";
        }
    }
}
