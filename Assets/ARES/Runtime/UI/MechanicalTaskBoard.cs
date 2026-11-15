using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    internal static class MechanicalTaskBoard
    {
        static readonly string[] Symbols = { "◆", "●", "▲", "✚" };
        static readonly int[] EndOrder = { 2, 0, 3, 1 };
        public static void Build(TaskBoard c)
        {
            if (c.Attempt.Id == "WireShield") Wires(c);
            else if (c.Attempt.Id == "PipeFlow") Pipes(c);
            else Coils(c);
        }
        static void Coils(TaskBoard c)
        {
            var a = c.Attempt; var rules = a.Rules.miniGames; var row = Box(c.Root, "row mini-dials");
            for (int i = 0; i < rules.dialCount; i++)
            {
                int index = i; var cell = Box(row, "mini-dial");
                Text(cell, c.T("mini.dial") + " " + (i + 1), "mini-instruction");
                var art = new CoilArt(a.Values[i], rules.dialTargets[i], rules.dialSteps, a.Readings.CoilMarks,
                    c.ReducedMotion ? 0 : a.Readings.CoilMotionSeconds); cell.Add(art);
                var buttons = Box(cell, "row"); SliderInt slider = null;
                void SetValue(int value) { a.Set(index, (value + rules.dialSteps) % rules.dialSteps); slider.value = a.Values[index]; art.SetValue(a.Values[index]); }
                Button(buttons, "↶", () => SetValue(a.Values[index] - 1)).tooltip = c.Locale.T("mini.rotateLeft", "n", i + 1);
                Button(buttons, "↷", () => SetValue(a.Values[index] + 1)).tooltip = c.Locale.T("mini.rotateRight", "n", i + 1);
                slider = Slider(cell, c.T("mini.position"), a.Values[i], rules.dialSteps - 1, 1, value => { a.Set(index, value); art.SetValue(value); });
                slider.name = "coil-" + i;
            }
            c.Confirm();
        }
        static void Pipes(TaskBoard c)
        {
            var a = c.Attempt; Text(c.Root, c.T("mini.PipeFlow.instruction"), "mini-instruction");
            for (int row = 0; row < 2; row++)
            {
                var circuit = Box(c.Root, "mini-circuit");
                Text(circuit, c.T(row == 0 ? "mini.oxygenTank" : "mini.waterTank"), "mini-instruction");
                var pieces = Box(circuit, "row mini-pipe-row");
                for (int column = 0; column < 3; column++)
                {
                    int index = row * 3 + column, target = a.Rules.miniGames.dialTargets[column];
                    bool connected = a.Values[index] == target;
                    var button = Button(pieces, "", () => { a.Tap(index); c.Refresh(); }, "mini-pipe-piece" + (connected ? " is-connected" : ""));
                    button.name = "pipe-" + index;
                    button.tooltip = c.Locale.T(connected ? "mini.PipeFlow.connectedPipe" : "mini.PipeFlow.rotate", "n", index + 1);
                    button.Add(new PipeArt(a.Values[index], target, a.Rules.miniGames.dialSteps));
                    Text(button, (connected ? "✓ " : "↻ ") + c.T(connected ? "mini.PipeFlow.connected" : "mini.PipeFlow.turn"), "small").pickingMode = PickingMode.Ignore;
                }
            }
            bool ready = a.Values.Select((value, i) => value == a.Rules.miniGames.dialTargets[i % 3]).All(value => value);
            Text(c.Root, c.T(ready ? "mini.PipeFlow.ready" : "mini.PipeFlow.keepGoing"), ready ? "success" : "small");
            c.Confirm("mini.flow");
        }
        static void Wires(TaskBoard c)
        {
            var a = c.Attempt; var row = Box(c.Root, "row mini-wires");
            var left = Box(row, "wire-column");
            var art = new WireArt(a.Done); row.Add(art);
            var right = Box(row, "wire-column");
            var starts = new Button[4]; var ends = new VisualElement[4];
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                starts[i] = Button(left, Symbols[i] + (a.Done[i] ? " ✓" : ""), () => {
                    a.Selected = index;
                    for (int k = 0; k < starts.Length; k++) starts[k].EnableInClassList("selected", k == index);
                }, "wire-tone-" + i);
                starts[i].name = "wire-start-" + i; starts[i].SetEnabled(!a.Done[i]);
                starts[i].EnableInClassList("selected", a.Selected == i);
                starts[i].tooltip = c.Locale.T("mini.wireStart", "symbol", Symbols[i]);
            }
            foreach (int i in EndOrder)
            {
                var button = Button(right, Symbols[i] + (a.Done[i] ? " ✓" : ""), () => { a.Tap(i); c.Refresh(); }, "wire-tone-" + i);
                button.name = "wire-end-" + i; button.SetEnabled(!a.Done[i]);
                button.tooltip = c.Locale.T("mini.wireEnd", "symbol", Symbols[i]); ends[i] = button;
            }
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                starts[i].AddManipulator(new TaskTransferGesture(ends, destination => { a.Selected = index; a.Tap(destination); c.Refresh(); }));
            }
        }
        sealed class WireArt : ChallengeArt
        {
            readonly bool[] done;
            public WireArt(bool[] done) : base(120, 312) { this.done = done; }
            protected override void Draw(Painter2D p)
            {
                Color[] colors = { ChallengePalette.Control, ChallengePalette.Shield, ChallengePalette.Green, ChallengePalette.Solar };
                for (int i = 0; i < done.Length; i++) if (done[i])
                {
                    float y = 39 + i * 78, end = 39 + System.Array.IndexOf(EndOrder, i) * 78;
                    p.strokeColor = colors[i]; p.lineWidth = 4; p.BeginPath(); p.MoveTo(new Vector2(0, y));
                    p.BezierCurveTo(new Vector2(66, y), new Vector2(54, end), new Vector2(120, end)); p.Stroke();
                }
            }
        }
    }
}
