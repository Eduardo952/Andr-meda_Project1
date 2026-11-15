using System;
using Ares.Domain;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Three actual field settings; Campaign alone validates and records the optional mission.</summary>
    internal sealed class ShieldRegulationView
    {
        readonly Campaign campaign;
        readonly ShieldRegulation attempt;
        readonly ProgressBar stability;
        readonly Label feedback, warning;
        readonly Button apply;
        public ShieldRegulationView(VisualElement root, Campaign campaign, Localization locale, Action close)
        {
            this.campaign = campaign; attempt = campaign.ShieldCalibration;
            root.name = "shield-regulation";
            Action information = () => feedback.text = PresentationLocalizer.Text("Alinhe os três controles com as marcas de referência. A regulagem protege somente o dia atual.");
            var panel = new ChallengePanel(root, locale, PresentationLocalizer.Text(MissionEventCopy.ShieldTitle), "shield", null, false, information, close);
            panel.Body.style.paddingLeft = 18; panel.Body.style.paddingRight = 18; panel.Body.style.paddingTop = 10;
            panel.Body.style.width = Length.Percent(100);
            root.Q<ScrollView>().horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            Text(panel.Body, "MISSÃO OPCIONAL · DIAS 2 E 5", "heading");
            Text(panel.Body, "Alinhe os três controles com as marcas de referência. A regulagem protege somente o dia atual.");
            Text(panel.Body, "Você pode encerrar o dia sem esta tarefa, mas uma erupção solar sem o escudo regulado causa morte por radiação.", "small");
            warning = Text(panel.Body, "", "event-inline warning"); warning.name = "shield-event-warning";
            warning.style.display = DisplayStyle.None;
            string[] names = { "Intensidade do campo", "Fase das bobinas", "Frequência de proteção" };
            var controls = Box(panel.Body, "shield-controls");
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                var card = Box(controls, "shield-control");
                Text(card, names[i], "heading");
                Text(card, $"Referência: {attempt.Target(i)}%", "small");
                var slider = new SliderInt(0, 100) { value = attempt.Value(i), showInputField = true, name = "shield-dial-" + i };
                slider.tooltip = PresentationLocalizer.Text(names[i]); card.Add(slider);
                slider.RegisterValueChangedCallback(evt => {
                    int value = UnityEngine.Mathf.RoundToInt(evt.newValue / 10f) * 10;
                    slider.SetValueWithoutNotify(value); attempt.Adjust(index, value); Refresh();
                });
            }
            stability = new ProgressBar { lowValue = 0, highValue = 3, title = "", name = "shield-alignment" }; panel.Body.Add(stability);
            feedback = Text(panel.Body, "", "heading"); feedback.name = "shield-feedback";
            apply = Button(panel.Footer, "Ativar proteção do dia", () => { if (campaign.FinishShieldRegulation()) close(); }); apply.name = "shield-activate";
            Button(panel.Footer, "Voltar à base", close, "ghost");
            root.schedule.Execute(() => {
                string text = PresentationLocalizer.Text(MissionEventCopy.Status(campaign));
                if (warning.text != text) warning.text = text;
                warning.style.display = text.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }).Every(100);
            Refresh();
            root.schedule.Execute(() => root.Q<SliderInt>("shield-dial-0")?.Focus());
        }
        void Refresh()
        {
            int aligned = 0; for (int i = 0; i < 3; i++) if (attempt.Value(i) == attempt.Target(i)) aligned++;
            stability.value = aligned;
            feedback.text = PresentationLocalizer.Text(aligned == 3 ? "Campo alinhado! Ative a proteção." : $"Controles alinhados: {aligned}/3");
            apply.SetEnabled(attempt.Aligned && campaign.Phase == Phase.Tasks);
        }
    }
}
