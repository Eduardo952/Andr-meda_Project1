using System.Linq;
using UnityEngine.UIElements;
using static Ares.UI.UiElements;

namespace Ares.UI
{
    internal static class FarmTaskBoard
    {
        static readonly int[] Moisture = { 1, 3, 2 };
        public static void Build(TaskBoard c)
        {
            if (c.Attempt.Id == "IrrigationPlan") Irrigation(c); else Farm(c);
        }
        static void Irrigation(TaskBoard c)
        {
            var a = c.Attempt; int free = 3 - a.Values.Take(3).Sum();
            Text(c.Root, c.T("challenge.waterBudget") + " " + free);
            Text(c.Root, c.T("challenge.moistureTarget") + " 3");
            var row = Box(c.Root, "row mini-dials");
            for (int i = 0; i < Moisture.Length; i++)
            {
                int index = i; var bed = Box(row, "mini-dial");
                bed.Add(new ChallengeIcon("sprout", ChallengePalette.Green));
                Text(bed, c.Locale.T("mini.bed", "n", i + 1), "small");
                Text(bed, c.T("challenge.moisture") + " " + (Moisture[i] + a.Values[i]));
                var buttons = Box(bed, "row");
                var remove = Button(buttons, "−", () => { a.Set(index, a.Values[index] - 1); c.Refresh(); });
                remove.tooltip = c.Locale.T("challenge.resetWater", "n", i + 1); remove.SetEnabled(a.Values[i] > 0);
                var add = Button(buttons, "+", () => { a.Set(index, a.Values[index] + 1); c.Refresh(); });
                add.tooltip = c.Locale.T("challenge.addWater", "n", i + 1); add.SetEnabled(free > 0);
            }
            c.Confirm();
        }
        static void Farm(TaskBoard c)
        {
            var a = c.Attempt;
            if (a.Stage == 0)
            {
                Button water = null;
                water = Button(c.Root, c.T("mini.drop"), () => { a.Selected = 0; water.AddToClassList("selected"); });
                water.Insert(0, new ChallengeIcon("drop", ChallengePalette.Life, 28));
                water.EnableInClassList("selected", a.Selected == 0);
                var row = Box(c.Root, "row mini-beds"); var beds = new VisualElement[a.Rules.miniGames.beds];
                for (int i=0;i<beds.Length;i++)
                {
                    int index=i;
                    var bed=Button(row,"",()=> { a.Tap(index); c.Refresh(); },"mini-bed" + (a.Done[i]?" is-connected":""));
                    bed.name="farm-bed-"+i;
                    bed.Add(new ChallengeIcon("sprout",ChallengePalette.Green,48));
                    Text(bed,c.Locale.T("mini.bed","n",i+1),"small").pickingMode=PickingMode.Ignore;
                    Text(bed,c.T("challenge.moisture")+" "+Moisture[i]+(a.Done[i]?" ✓":""),"small").pickingMode=PickingMode.Ignore;
                    bed.SetEnabled(!a.Done[i]); beds[i]=bed;
                }
                water.AddManipulator(new TaskTransferGesture(beds,index=> { a.Selected=0; a.Tap(index); c.Refresh(); }));
            }
            else if (a.Stage==1)
            {
                c.Root.Add(new ChallengeIcon("sun",ChallengePalette.Green,100));
                Text(c.Root,c.T("mini.lightTarget")+" "+a.Rules.miniGames.lightTarget);
                Slider(c.Root,c.T("mini.light"),a.Values[0],5,1,value=>a.Set(0,value));
            }
            else
            {
                Text(c.Root,c.T("mini.harvest")); var row=Box(c.Root,"row mini-beds");
                for(int i=0;i<a.Rules.miniGames.apples;i++)
                {
                    int index=i; var apple=Button(row,"",()=>{a.Tap(index);c.Refresh();},"mini-bed apple");
                    apple.Add(new ChallengeIcon("apple",ChallengePalette.Green,48));
                    Text(apple,c.Locale.T("mini.apple","n",i+1)+(a.Done[i]?" ✓":""),"small").pickingMode=PickingMode.Ignore;
                    apple.SetEnabled(!a.Done[i]);
                }
            }
        }
    }
}
