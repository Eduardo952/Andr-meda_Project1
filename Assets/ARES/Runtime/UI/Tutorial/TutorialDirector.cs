using System;
using Ares.Domain;
using Ares.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ares.UI
{
    /// <summary>Connects the tutorial to existing terminal, watering and route adapters.</summary>
    internal sealed class TutorialDirector : IDisposable
    {
        readonly ReferenceExplorer explorer;
        readonly GameFrontend frontend;
        readonly VisualElement normalHud;
        readonly TutorialView view;
        public TutorialFlow Flow { get; }
        public bool Active => Flow.Active;
        public bool BlocksWorld => Active && Flow.Dialogue;
        public bool AllowsComputer => Flow.Stage == TutorialStage.FindComputer || Flow.Stage == TutorialStage.AllocateEnergy;
        public bool AllowsEnergy => Flow.Stage == TutorialStage.AllocateEnergy;
        public Campaign Campaign => frontend.Campaign;
        public int GardenTarget => Flow.Stage == TutorialStage.FindCan ? -2 : Flow.Stage == TutorialStage.FillCan ? -3 : Flow.Stage == TutorialStage.WaterPlants ? Shelf : -1;
        public VisualElement Root => view.Root;
        public int Shelf
        {
            get { for (int i = 0; i < GreenhouseRules.ShelfCount; i++) if (!frontend.Campaign.Watering.IsWatered(i)) return i; return -1; }
        }
        public Vector3? Target
        {
            get
            {
                switch (Flow.Stage)
                {
                    case TutorialStage.FindComputer: return explorer.computerSettings.standPoint;
                    case TutorialStage.FindCan: return frontend.GreenhouseCare.PickupPosition;
                    case TutorialStage.FillCan: return frontend.GreenhouseCare.RefillPosition;
                    case TutorialStage.WaterPlants: return Shelf < 0 ? (Vector3?)null : frontend.GreenhouseCare.Layout.rows[Shelf].Approach;
                    default: return null;
                }
            }
        }
        public TutorialDirector(VisualElement parent, VisualElement normalHud, ReferenceExplorer explorer, GameFrontend frontend, TutorialFlow flow = null)
        {
            Flow=flow ?? new TutorialFlow();
            this.explorer = explorer; this.frontend = frontend; this.normalHud = normalHud;
            view = new TutorialView(parent, explorer, this);
            Flow.Changed += Changed;
            Changed();
        }
        public void Choose(bool accept) => Flow.Choose(accept);
        public void Continue() => Flow.Continue();
        public void Exit() { frontend.Terminal.Close(); Flow.Exit(); }
        public bool AllowsGardenTarget(int target)
        {
            if (!Active) return true;
            return Flow.Stage == TutorialStage.FindCan && target == -2 ||
                Flow.Stage == TutorialStage.FillCan && target == -3 ||
                Flow.Stage == TutorialStage.WaterPlants && target == Shelf;
        }
        public bool CanInteract => Flow.Stage == TutorialStage.FindComputer ? frontend.Terminal.Near :
            Flow.Stage == TutorialStage.FindCan ? frontend.GreenhouseCare.CanPickUp :
            Flow.Stage == TutorialStage.FillCan ? frontend.GreenhouseCare.AtRefill :
            Flow.Stage == TutorialStage.WaterPlants && frontend.GreenhouseCare.NearbyShelf == Shelf && frontend.Campaign.Watering.ActiveShelf < 0;
        public void Interact()
        {
            if (!CanInteract || BlocksWorld) return;
            if (Flow.Stage == TutorialStage.FindComputer) frontend.Terminal.TryInteract();
            else frontend.GreenhouseCare.TryInteract();
        }
        public void Tick()
        {
            if (!Active) return;
            var terminal = frontend.Terminal;
            if (Flow.Stage == TutorialStage.AllocateEnergy && frontend.Campaign.Phase == Phase.Tasks && terminal.Focused) terminal.Close();
            Flow.Observe(terminal.Focused, terminal.OwnsCamera, frontend.Campaign.Phase, frontend.Campaign.Watering);
            view.Tick(frontend, Shelf, CanInteract, Time.unscaledDeltaTime, explorer.reducedMotion);
        }
        void Changed()
        {
            normalHud.style.display = Active ? DisplayStyle.None : DisplayStyle.Flex;
            normalHud.SetEnabled(!Active);
            if (Flow.Stage == TutorialStage.Complete && frontend.GreenhouseCare.Inside && frontend.Campaign.Greenhouse.CareReady)
            {
                foreach (var task in frontend.Campaign.Current.tasks)
                    if (GreenhouseSimulation.IsTask(task.id) && !frontend.Campaign.Completed(task.id))
                    {
                        if (frontend.Campaign.BeginTask(task.id, "greenhouse")) frontend.Campaign.FinishGreenhouseTask();
                        break;
                    }
            }
            view.Show(Flow.Stage);
        }
        public void Dispose()
        {
            Flow.Changed -= Changed; view.Dispose();
            normalHud.style.display = DisplayStyle.Flex; normalHud.SetEnabled(true);
        }
    }
}
