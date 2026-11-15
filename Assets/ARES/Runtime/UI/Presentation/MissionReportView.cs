using System;
using Ares.Domain;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Resource comparisons are rendered directly from the frozen SolOutcome. No invented telemetry.</summary>
    internal static class MissionReportView
    {
        public static void Build(VisualElement root, Campaign campaign, Localization locale, ReferenceExplorer explorer, Action next)
        {
            root.AddToClassList("mission-report"); root.name = "mission-report";
            var header = Box(root, "mission-report-header");
            var titles = Box(header);
            Text(titles, "DIÁRIO DE BORDO / TELEMETRIA DA BASE", "mission-report-kicker");
            Text(titles, "SOL " + campaign.Current.sol + " · Missão do dia", "title");
            if(campaign.EndedByClock)Text(titles,"Tempo de missão concluído · tarefas registradas","small");
            header.Add(new CartoonText("ARES BASE", 30));
            var scroll=new ScrollView(ScrollViewMode.Vertical);scroll.AddToClassList("mission-report-scroll");root.Add(scroll);
            var body = Box(scroll.contentContainer, "mission-report-body");
            var resources = Box(body, "mission-report-resources");
            Comparison(resources, locale.T("sys.shield"), "shield", campaign.Outcome.shield);
            Comparison(resources, locale.T("sys.life"), "lifeSupport", campaign.Outcome.life);
            Comparison(resources, locale.T("sys.greenhouse"), "greenhouse", campaign.Outcome.greenhouse);
            if (campaign.Demands.ImpactedToday)
                Text(resources, campaign.Demands.Event == DemandEvent.DustStorm ? "Evento do dia: tempestade de poeira" : "Evento do dia: erupção solar · escudo regulado", "heading");
            string eventStatus = MissionEventCopy.Status(campaign);
            if (!string.IsNullOrEmpty(eventStatus)) Text(resources, eventStatus, "event-inline warning");
            var mentor = Box(body, "mission-report-mentor");
            var portrait = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            mentor.Add(portrait); mentor.Add(new CartoonText("ARES", 30));
            string explanation = campaign.Outcome.shield.missing > 0
                ? $"O escudo precisava de {campaign.Outcome.shield.demand} kWh, mas recebeu {campaign.Outcome.shield.allocated}. Faltaram {campaign.Outcome.shield.missing} kWh para a proteção prevista. No próximo Sol, confira a necessidade antes de dividir a energia!"
                : campaign.Outcome.life.missing > 0 ? "O sistema de ar precisava de mais energia. Vamos reservar o necessário para a equipe respirar bem no próximo Sol!"
                : campaign.Outcome.greenhouse.missing > 0 ? "A estufa precisava de mais energia. As plantas também fazem parte da nossa equipe: vamos cuidar delas no próximo Sol!"
                : "Os sistemas receberam a energia necessária! Confira os registros e conte à Terra como foi a nossa aventura de hoje.";
            Text(mentor, explanation).name = "report-mentor-explanation";
            var original = explorer.GetComponentInChildren<BubbleAnimator>();
            AresMentorPortrait actor = null;
            if (original != null)
            {
                actor = new AresMentorPortrait(portrait, original); actor.Show(true);
                var animation = mentor.schedule.Execute(() => actor?.Tick(Time.unscaledDeltaTime, explorer.reducedMotion)).Every(16);
                mentor.RegisterCallback<DetachFromPanelEvent>(_ => { animation.Pause(); actor?.Dispose(); actor = null; });
            }
            var stats = Box(scroll.contentContainer, "mission-report-stats");
            Stat(stats, "BASE", campaign.Integrity + "% de integridade");
            Stat(stats, "AR DA EQUIPE", campaign.Oxygen + "% de oxigênio");
            Stat(stats, "ESTUFA", (campaign.Greenhouse.LastResult?.HealthyShelves ?? 0) + "/4 fileiras saudáveis");
            var events = new Foldout { text = "Ver acontecimentos e registros deste Sol", value = false }; scroll.contentContainer.Add(events);
            foreach (var item in campaign.Events)
                Text(events, locale.T("consequences.events." + item.id + ".title"), "small");
            var footer = Box(root, "mission-report-footer");
            Text(footer, "Antes da próxima aventura, revise e envie o relatório para a Terra.", "small");
            Button(footer, "Próximo Sol  ›", next, "mission-next").name = "summary-communications";
            MissionVisualStyle.Open(root);
        }
        static void Stat(VisualElement parent, string title, string value)
        { var card = Box(parent, "mission-report-stat"); Text(card, title, "small"); Text(card, value, "heading"); }
        static void Comparison(VisualElement parent, string title, string sector, SystemResult result)
        {
            bool warning = result.status == "deficit";
            var card = Box(parent, "mission-comparison" + (warning ? " warning" : "")); card.name = "report-resource-" + sector;
            var row = Box(card, "mission-comparison-head"); row.Add(MissionVisualStyle.Icon(sector, 62));
            var caption = Box(row, "mission-comparison-caption"); Text(caption, title, "heading");
            Text(caption, warning ? "ATENÇÃO · ENERGIA INSUFICIENTE" : result.status == "surplus" ? "PROTEGIDO · ENERGIA EXTRA" : "ÓTIMO · NECESSIDADE ATENDIDA", "small");
            Text(row, result.allocated + " / " + result.demand + " kWh", "mission-comparison-value");
            var track = Box(card, "mission-comparison-track"); var fill = Box(track, "mission-comparison-fill");
            float ratio = result.demand > 0 ? Mathf.Clamp01((float)result.allocated / result.demand) : 1;
            fill.style.width = Length.Percent(MissionVisualStyle.Reduced ? ratio * 100 : 0);
            if (!MissionVisualStyle.Reduced)
            {
                float began = Time.unscaledTime;
                IVisualElementScheduledItem animation = null;
                animation = fill.schedule.Execute(() => {
                    float progress = Mathf.Clamp01((Time.unscaledTime - began) / .9f);
                    fill.style.width = Length.Percent(ratio * 100 * (1 - Mathf.Pow(1 - progress, 3)));
                    if (progress >= 1) animation.Pause();
                }).Every(16);
            }
            Text(card, warning ? $"Faltaram {result.missing} kWh para atender este sistema." : "O sistema recebeu o necessário para este Sol.", "small");
        }
    }
}
