using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    internal static class SolarTaskBoard
    {
        public static void Build(TaskBoard c)
        {
            var a = c.Attempt; var root = c.Root;
            switch (a.Id)
            {
                case "TiltSun":
                    root.Add(new ChallengeIcon("sun", ChallengePalette.Solar));
                    var art = new SolarTiltArt(a.Values[0], a.Readings.Tilt); root.Add(art);
                    Text(root, c.T("mini.targetAngle") + " " + a.Readings.Tilt + "°");
                    var slider = Slider(root, c.T("mini.angle"), a.Values[0], 90, a.Rules.miniGames.tiltStepDegrees, value => { a.Set(0, value); art.SetAngle(a.Values[0]); });
                    slider.name = "solar-angle"; c.Confirm(); break;
                case "CleanPanels": Clean(c); break;
                case "PowerBalance":
                    int maximum = Math.Max(a.Rules.energyPool, a.Demand.Total);
                    Meter(root, c.T("challenge.available"), a.Rules.energyPool, maximum);
                    Meter(root, c.T("challenge.demand"), a.Demand.Total, maximum);
                    if (a.Stage == 0) c.Choices("matches", "shortfall");
                    else
                    {
                        string[] systems = { "shield", "life", "greenhouse" };
                        int[] demand = { a.Demand.shield, a.Demand.life, a.Demand.greenhouse };
                        for (int i = 0; i < 3; i++)
                        {
                            int index = i; var row = Box(root, "mini-energy-row");
                            Text(row, c.T("sys." + systems[i]) + " · " + demand[i]);
                            Slider(row, c.T("mini.position"), a.Values[i], 100, 5, value => a.Set(index, value));
                        }
                        c.Confirm();
                    }
                    break;
                case "WindReading":
                    root.Add(new ChallengeIcon("compass", ChallengePalette.Solar, 100));
                    Text(root, c.T("challenge.wind") + " " + c.T("compass." + a.Readings.WindDirection));
                    c.Choices(root, new[] { "N", "E", "S", "W", a.Readings.WindDirection }.Distinct().ToArray(), x => c.T("compass." + x)); break;
            }
        }
        internal static void Meter(VisualElement root, string label, float value, float max)
        {
            Text(root, label + " · " + value.ToString("0.#"));
            root.Add(new ProgressBar { lowValue = 0, highValue = max, value = value, title = "", tooltip = label, name = "reading-meter" });
        }
        static void Clean(TaskBoard c)
        {
            var a = c.Attempt; var root = c.Root;
            if (a.ActiveArray < 0) { root.Add(new ChallengeIcon("sun", ChallengePalette.Solar)); Text(root, c.T("mini.CleanPanels.alreadyClean")); c.Confirm("mini.CleanPanels.verify"); return; }
            Text(root, c.T("mini.CleanPanels.instruction"), "mini-instruction");
            var grid = Box(root, "row solar-array-list");
            foreach (int i in Enumerable.Range(0, 4).OrderByDescending(i => a.Dust[i]))
            {
                float dust = a.Dust[i]; int need = a.Passes[i], done = a.Values[i];
                bool active = i == a.ActiveArray;
                var card = Box(grid, "solar-array-card" + (active ? " active" : ""));
                Text(card, c.Locale.T("mini.CleanPanels.array", "n", i + 1), "heading");
                string level = dust <= 10 ? "clear" : dust <= 35 ? "light" : dust <= 70 ? "heavy" : "buried";
                Text(card, c.T("mini.CleanPanels.level." + level), "small");
                int output = Mathf.FloorToInt(100 - dust * .2f * (1 - done / (float)Math.Max(1, need)) + .5f);
                Text(card, c.Locale.T("mini.CleanPanels.dust", "n", dust), "small");
                Meter(card, c.Locale.T("mini.CleanPanels.output", "n", output), output, 100);
                var button = Button(card, c.Locale.T("mini.CleanPanels.pass", "pass", Math.Min(done + 1, need), "total", need), () => { a.Tap(i); c.Refresh(); });
                button.name = "clean-array-" + i;
                button.tooltip = c.Locale.T("mini.CleanPanels.wipe", "n", i + 1, "pass", done + 1, "total", need);
                button.SetEnabled(active);
            }
            Text(root, "100% · " + c.T("mini.CleanPanels.restore"), "mini-instruction");
        }
    }
}
