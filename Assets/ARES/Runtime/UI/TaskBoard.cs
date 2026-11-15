using System;
using Ares.Tasks;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    /// <summary>Presentation context shared by focused board builders; all answers go through TaskAttempt.</summary>
    internal sealed class TaskBoard
    {
        public readonly TaskAttempt Attempt;
        public readonly Localization Locale;
        public readonly VisualElement Root;
        public readonly Action Refresh;
        public readonly bool ReducedMotion;
        public TaskBoard(VisualElement root, TaskAttempt attempt, Localization locale, Action refresh, bool reducedMotion)
        { Root = root; Attempt = attempt; Locale = locale; Refresh = refresh; ReducedMotion = reducedMotion; }
        public string T(string key) => Locale.T(key);
        public void Confirm(string key = "tasks.check") => Button(Root, T(key), () => { Attempt.Confirm(); Refresh(); }, "mini-confirm");
        public void Choices(params string[] choices) => Choices(Root, choices, x => T("challenge." + x));
        public void Choices(VisualElement parent, string[] choices, Func<string,string> label)
        {
            var row = Box(parent, "row mini-choices");
            foreach (string choice in choices) Button(row, label(choice), () => { Attempt.Choose(choice); Refresh(); });
        }
        public void Build()
        {
            switch (Attempt.Id)
            {
                case "TiltSun": case "CleanPanels": case "PowerBalance": case "WindReading": SolarTaskBoard.Build(this); break;
                case "AlignCoils": case "PipeFlow": case "WireShield": MechanicalTaskBoard.Build(this); break;
                case "SmartFarmCare": case "IrrigationPlan": FarmTaskBoard.Build(this); break;
                case "SortFlares": case "ThermostatDial": case "CmeCountdown": case "ResourcePriority": case "PhotoDetective": ReadingTaskBoard.Build(this); break;
                default: throw new InvalidOperationException("Unregistered puzzle: " + Attempt.Id);
            }
        }
    }
}
