using System;
using Ares.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>ChallengePanel/MiniFrame presentation. Board renderers do not own rewards or clocks.</summary>
    internal sealed class TaskView
    {
        readonly VisualElement body, footer;
        readonly TaskAttempt attempt;
        readonly Localization locale;
        readonly Action retry, accept;
        readonly bool reducedMotion;
        Label timer;
        bool hint, details;
        int previousStage, previousMistakes;
        bool previousSolved, previousExpired;

        public TaskView(VisualElement root, TaskAttempt attempt, Localization locale, Action retry, Action accept, Action close, bool reducedMotion)
        {
            this.attempt = attempt; this.locale = locale; this.retry = retry; this.accept = accept;
            this.reducedMotion = reducedMotion;
            var panel = new ChallengePanel(root, locale, locale.T("mini." + attempt.Id + ".name"), Sector, Source,
                attempt.Sample, () => { details = !details; Render(); }, close);
            body = panel.Body; footer = panel.Footer; Render();
        }

        string Sector => ChallengeReadings.Sector(attempt.Id);
        string Source => attempt.Id == "PhotoDetective" && attempt.Readings.ArchivedPhoto != null ? "Foto real · NASA/APOD" : ChallengeReadings.Source(attempt.Id, locale);
        bool Practice => attempt.Id == "PhotoDetective" && attempt.Readings.ArchivedPhoto == null || attempt.Id == "CmeCountdown" && attempt.Readings.CmePractice
            || attempt.Id == "ThermostatDial" && attempt.Readings.TempPractice || attempt.Id == "SortFlares" && attempt.Readings.FlarePractice
            || attempt.Id == "WindReading" && attempt.Readings.WindPractice;

        public void Update()
        {
            if (previousStage != attempt.Stage || previousMistakes != attempt.Mistakes || previousSolved != attempt.Solved || previousExpired != attempt.Expired) Render();
            if (timer != null) timer.text = locale.T("mini.timer") + " " + Mathf.CeilToInt(attempt.Rules.taskSeconds - attempt.Seconds)
                + "   ·   " + locale.T("mini.mistakes") + " " + attempt.Mistakes;
        }
        public void Render()
        {
            body.Clear(); footer.Clear(); timer = null;
            previousStage = attempt.Stage; previousMistakes = attempt.Mistakes; previousSolved = attempt.Solved; previousExpired = attempt.Expired;
            if (details)
            {
                var info = Box(body, "mini-hint");
                Text(info, Source ?? locale.T("tasks.badgeRule"), "mini-instruction");
                if (attempt.Sample) Text(info, locale.T("tasks.sampleNote"));
                Text(info, locale.T("mini.rulesNote")); Text(info, locale.T("info.snapshot"));
                Text(info, locale.T("challenge.briefings." + attempt.Id + ".importance"));
                Text(info, locale.T("challenge.briefings." + attempt.Id + ".happening"));
                var record = attempt.Id == "SortFlares" ? attempt.Readings.ArchivedFlare : attempt.Id == "CmeCountdown" ? attempt.Readings.ArchivedCme : null;
                if (record != null)
                {
                    Text(info, "Arquivo NASA/DONKI · " + record.date + (record.preliminary ? " · análise inicial" : ""));
                    Text(info, "Não é um alerta de agora. Este registro sozinho não diz se uma tempestade chega a Marte.");
                    Button(info, "Ver registro oficial", () => Application.OpenURL(record.url), "ghost");
                }
                if (attempt.Id == "PhotoDetective" && attempt.Readings.ArchivedPhoto != null)
                    Button(info, "Fonte da foto · NASA/APOD", () => Application.OpenURL(attempt.Readings.ArchivedPhoto.sourceUrl), "ghost");
            }
            if (attempt.Solved || attempt.Expired) { Result(); return; }
            var context = Box(body, "mini-context");
            Text(context, locale.T("mini." + attempt.Id + ".goal"), "mini-instruction");
            if (Practice) Text(body, locale.T("report.noReading") + " · " + locale.T("mini.practice"), "mini-practice");
            string formula = attempt.Id == "CmeCountdown" ? "t (h) = 228 000 000 km ÷ v (km/s) ÷ 3 600" :
                attempt.Id == "ThermostatDial" ? "E = arredondar para 5 [10 + 0,15 × máx(0, 20 − T)] kWh" :
                attempt.Id == "PowerBalance" ? "Saldo = energia disponível − (escudo + suporte + estufa)" : null;
            if(formula!=null) Text(body,formula,"formula-strip");
            ChallengeReadings.Build(body, attempt, locale);
            // Show the actual equipment first. The clock and input start only after Begin.
            var board = new ChallengeGrid(); body.Add(board);
            new TaskBoard(board, attempt, locale, Render, reducedMotion).Build();
            board.SetEnabled(attempt.Begun);
            board.EnableInClassList("mini-equipment-preview", !attempt.Begun);
            if (!attempt.Begun) board.Query<VisualElement>().ForEach(element => element.style.opacity = 1);
            if (attempt.Mistakes > 0)
                Text(body, locale.T("challenge.briefings." + attempt.Id + ".error"), "mini-feedback");
            if (hint)
            {
                var hintBox = Box(body, "mini-hint"); Text(hintBox, locale.T("bubble.name"), "mini-instruction");
                Text(hintBox, locale.T("challenge.briefings." + attempt.Id + ".hint"));
            }
            timer = Text(footer, "", "mini-status");
            var actions = Box(footer, "row mini-footer-actions");
            Button(actions, locale.T("challenge.hint"), () => { hint = !hint; Render(); }, "ghost");
            if (!attempt.Begun) Button(actions, locale.T("challenge.begin"), () => { attempt.Begin(); Render(); });
            else Button(actions, locale.T("mini.retry"), retry);
            Update();
        }
        void Result()
        {
            var result = Box(body, "mini-result" + (attempt.Solved ? " result-success" : " result-retry"));
            Text(result, attempt.Solved ? "✓" : "↻", "title");
            Text(result, locale.T(attempt.Solved ? "tasks.success" : "mini.retryTitle"), "heading");
            if (!attempt.Solved)
            { Text(result, locale.T("mini.retryBody")); Button(result, locale.T("mini.retry"), retry); return; }
            Text(result, new string('★', Ares.Domain.Rules.Score(attempt.Seconds, attempt.Mistakes)), "title stars");
            string value = attempt.Id == "SortFlares" ? attempt.Readings.Flare : attempt.Id == "CmeCountdown" ? attempt.Readings.Speed.ToString() : attempt.Readings.Temperature.ToString();
            if (attempt.Id == "PhotoDetective") value = attempt.Readings.ArchivedPhoto?.title ?? locale.T("photo." + attempt.Readings.PictureKind);
            var learning = new Foldout { text = "O que mudou?", value = false };
            result.Add(learning);
            Text(learning, locale.T(Practice ? "mini.practiceLearned" : "mini." + attempt.Id + ".learned", "value", value));
            var rewards = Box(result, "row mini-consequence");
            Reward(rewards, "integrity", attempt.IntegrityBefore, attempt.Rules.taskReward.integrity);
            Reward(rewards, "oxygen", attempt.OxygenBefore, attempt.Rules.taskReward.oxygen);
            Text(result, locale.T("challenge.rewardNote"), "small");
            Text(result, locale.T("mini.timer") + " " + attempt.Seconds.ToString("0.#") + " · " + locale.T("mini.mistakes") + " " + attempt.Mistakes);
            Button(result, locale.T("tasks.continue"), accept);
        }
        void Reward(VisualElement parent, string key, int before, int delta)
        {
            int after = Ares.Domain.Rules.ApplyDelta(before, delta); var card = Box(parent, "mini-reward");
            Text(card, locale.T("hud." + key), "mini-instruction");
            card.Add(new ProgressBar { lowValue = 0, highValue = 100, value = after, title = "" });
            Text(card, before + " → " + after);
            Text(card, locale.T(before == after ? "challenge.maintained" : "challenge.preview"), "small");
        }
    }
}
