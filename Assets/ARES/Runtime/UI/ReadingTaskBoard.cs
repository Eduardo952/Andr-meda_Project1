using System;
using System.Globalization;
using Ares.Tasks;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    internal static class ReadingTaskBoard
    {
        public static void Build(TaskBoard c)
        {
            var a = c.Attempt;
            switch (a.Id)
            {
                case "SortFlares": Flares(c); break;
                case "CmeCountdown": Cme(c); break;
                case "ThermostatDial":
                    c.Root.Add(new ChallengeIcon("thermometer", ChallengePalette.Life, 100));
                    Text(c.Root, c.T("mini.outside") + " " + a.Readings.Temperature + " °C");
                    Text(c.Root, c.T("mini.heating") + " " + a.Readings.Heating);
                    Slider(c.Root, c.T("mini.heating"), a.Values[0], a.Rules.energyPool, a.Rules.sliderStep, value => a.Set(0, value));
                    c.Confirm(); break;
                case "ResourcePriority":
                    SolarTaskBoard.Meter(c.Root, c.T("challenge.oxygen"), a.OxygenBefore, 100);
                    Text(c.Root, c.T("sys.life") + " " + a.Demand.life + " · " + c.T("sys.greenhouse") + " " + a.Demand.greenhouse);
                    c.Choices("air", "water", "heat"); break;
                case "PhotoDetective": Photo(c); break;
            }
        }
        static void Flares(TaskBoard c)
        {
            var a = c.Attempt;
            var reading = Box(c.Root, "mini-flare-reading");
            Text(reading, c.Locale.T("mini.SortFlares.reading", "value", a.Readings.Flare), "heading");
            Text(reading, c.T("mini.SortFlares.readingHint"), "small");
            Text(c.Root, c.T("mini.SortFlares.instruction"), "mini-instruction");
            Text(c.Root, c.T("mini.SortFlares.weakest") + "  →  " + c.T("mini.SortFlares.strongest"), "small");
            var slots = Box(c.Root, "row mini-flare-order");
            for (int i = 0; i < 5; i++)
            {
                bool filled = i < a.Stage;
                var slot = Box(slots, "mini-flare-slot" + (filled ? " is-connected" : ""));
                Text(slot, (i + 1).ToString(), "small"); Text(slot, filled ? "ABCMX"[i] + " ✓" : "·", "heading");
            }
            var cards = Box(c.Root, "row mini-flare-cards");
            foreach (string value in new[] { "M", "A", "X", "B", "C" })
            {
                var card = Button(cards, value, () => { a.Choose(value); c.Refresh(); }, "mini-flare-card");
                card.name = "flare-" + value; card.SetEnabled("ABCMX".IndexOf(value, StringComparison.Ordinal) >= a.Stage);
                card.tooltip = c.Locale.T("mini.SortFlares.choose", "value", value);
            }
            Text(c.Root, c.Locale.T("mini.SortFlares.next", "value", "ABCMX"[Math.Min(4, a.Stage)]), "mini-instruction");
        }
        static void Cme(TaskBoard c)
        {
            var a = c.Attempt; var r = a.Readings;
            var orbit = Box(c.Root, "row mini-orbit");
            orbit.Add(new ChallengeIcon("sun", ChallengePalette.Solar)); Box(orbit, "orbit-path");
            Text(orbit, "◉", "title");
            Text(c.Root, c.T("mini.CmeCountdown.reading") + " " + r.Speed + " km/s");
            Label hours = null;
            string Estimate() => c.T("mini.hours") + " " + TaskReadings.TravelHoursExact(a.Values[0]).ToString("0.#", CultureInfo.GetCultureInfo(c.Locale.Language));
            var slider = Slider(c.Root, c.T("mini.speed"), a.Values[0], Math.Max(3000, (int)Math.Ceiling(r.Speed / 100) * 100), a.Rules.miniGames.speedStep,
                value => { a.Set(0, value); if (hours != null) hours.text = Estimate(); });
            slider.name = "cme-speed";
            hours = Text(c.Root, Estimate()); hours.name = "cme-travel-hours";
            Text(c.Root, c.T("mini.constantSpeed"), "small");
            if (a.Stage == 0) c.Confirm();
            else
            {
                int arrival = TaskReadings.TravelHours(r.Speed);
                Text(c.Root, c.T("mini.CmeCountdown.arrival"));
                c.Choices(c.Root, new[] { (arrival + 12).ToString(), arrival.ToString(), (arrival + 24).ToString() }, x => x + " h");
            }
        }
        static void Photo(TaskBoard c)
        {
            var a = c.Attempt;
            if (a.Stage != 0)
            { Text(c.Root, c.T("challenge.impactQuestion")); c.Choices("yes", "no"); Text(c.Root, c.T("challenge.impactAnswer"), "small"); return; }
            c.Root.Add(new SnapshotArt(a.Readings.PictureKind)); Text(c.Root, c.T("tasks.T10.sampleArt"), "small");
            var cards = Box(c.Root, "row mini-caption-cards"); var buttons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                string caption = i == 1 ? c.T("photo." + a.Readings.PictureKind) : c.T(i == 0 ? "mini.PhotoDetective.decoyLab" : "mini.PhotoDetective.decoyMap");
                buttons[i] = Button(cards, caption, () => { a.Selected = index; for (int k = 0; k < 3; k++) buttons[k].EnableInClassList("selected", k == index); });
                buttons[i].EnableInClassList("selected", a.Selected == i);
            }
            var drop = Button(c.Root, c.T("mini.placeCaption"), () => { if (a.Selected >= 0) { a.Choose(a.Selected.ToString()); c.Refresh(); } }, "mini-dropzone");
            for (int i = 0; i < buttons.Length; i++)
            { int index = i; buttons[i].AddManipulator(new TaskTransferGesture(new VisualElement[] { drop }, _ => { a.Choose(index.ToString()); c.Refresh(); })); }
        }
    }
}
