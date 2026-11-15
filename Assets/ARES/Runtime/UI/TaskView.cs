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
        string Source => ChallengeReadings.Source(attempt.Id, locale);
        bool Practice => attempt.Id == "PhotoDetective" || attempt.Id == "CmeCountdown" && attempt.Readings.CmePractice
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
            }
            if (attempt.Solved || attempt.Expired) { Result(); return; }
            var context = Box(body, "mini-context");
            bool compact = attempt.Id == "SortFlares" || attempt.Id == "PipeFlow" || attempt.Id == "CleanPanels";
            if (!compact || !attempt.Begun)
            { Text(context, locale.T("challenge.happening"), "small"); Text(context, locale.T("challenge.briefings." + attempt.Id + ".happening")); }
            Text(context, locale.T("challenge.action"), "small"); Text(context, locale.T("mini." + attempt.Id + ".goal"), "mini-instruction");
            if (Practice) Text(body, locale.T("report.noReading") + " · " + locale.T("mini.practice"), "mini-practice");
            ChallengeReadings.Build(body, attempt, locale);
            if (!attempt.Begun)
            {
                var briefing = Box(body, "mini-briefing");
                Text(briefing, locale.T("challenge.importance"), "small");
                Text(briefing, locale.T("challenge.briefings." + attempt.Id + ".importance"));
            }
            else
            {
                var board = new ChallengeGrid(); body.Add(board);
                new TaskBoard(board, attempt, locale, Render, reducedMotion).Build();
            }
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
            Text(result, locale.T("mini.learned"), "mini-instruction");
            Text(result, locale.T(Practice ? "mini.practiceLearned" : "mini." + attempt.Id + ".learned", "value", value));
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
