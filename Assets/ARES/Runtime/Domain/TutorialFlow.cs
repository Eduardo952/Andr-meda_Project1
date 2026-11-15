using System;

namespace Ares.Domain
{
    public enum TutorialStage
    {
        Offer, EnergyIntro, FindComputer, EnergyExplain, AllocateEnergy,
        PlantsIntro, FindCan, FillIntro, FillCan, WaterIntro, WaterPlants, Complete, Inactive
    }

    /// <summary>Onboarding progression only. Observes real actions; never grants water, energy or rewards.</summary>
    public sealed class TutorialFlow
    {
        public TutorialStage Stage { get; private set; } = TutorialStage.Offer;
        public bool Active => Stage != TutorialStage.Inactive;
        public bool Dialogue => Stage == TutorialStage.Offer || Stage == TutorialStage.EnergyIntro ||
            Stage == TutorialStage.EnergyExplain || Stage == TutorialStage.PlantsIntro ||
            Stage == TutorialStage.FillIntro || Stage == TutorialStage.WaterIntro || Stage == TutorialStage.Complete;
        public event Action Changed;
        bool explainedWater;

        public void Choose(bool accept) { if (Stage == TutorialStage.Offer) Set(accept ? TutorialStage.EnergyIntro : TutorialStage.Inactive); }
        public void Exit() => Set(TutorialStage.Inactive);
        public void Continue()
        {
            switch (Stage)
            {
                case TutorialStage.EnergyIntro: Set(TutorialStage.FindComputer); break;
                case TutorialStage.EnergyExplain: Set(TutorialStage.AllocateEnergy); break;
                case TutorialStage.PlantsIntro: Set(TutorialStage.FindCan); break;
                case TutorialStage.FillIntro: Set(TutorialStage.FillCan); break;
                case TutorialStage.WaterIntro: explainedWater = true; Set(TutorialStage.WaterPlants); break;
                case TutorialStage.Complete: Set(TutorialStage.Inactive); break;
            }
        }

        public void Observe(bool computerFocused, bool computerOpen, Phase phase, GreenhouseWatering water)
        {
            if (Stage == TutorialStage.FindComputer && computerFocused) Set(TutorialStage.EnergyExplain);
            else if (Stage == TutorialStage.AllocateEnergy && phase == Phase.Tasks && !computerOpen) Set(TutorialStage.PlantsIntro);
            else if (Stage == TutorialStage.AllocateEnergy && phase == Phase.Plan && !computerOpen) Set(TutorialStage.FindComputer);
            else if (Stage == TutorialStage.FindCan && water.HasCan) Set(TutorialStage.FillIntro);
            else if (Stage == TutorialStage.FillCan && water.CanMl >= GreenhouseWatering.CapacityMl)
                Set(explainedWater ? TutorialStage.WaterPlants : TutorialStage.WaterIntro);
            else if (Stage == TutorialStage.WaterPlants)
            {
                if (water.WateredShelves == GreenhouseRules.ShelfCount) Set(TutorialStage.Complete);
                else if (!water.HasCan) Set(TutorialStage.FindCan);
                else if (water.ActiveShelf < 0 && water.CanMl < GreenhouseWatering.CapacityMl) Set(TutorialStage.FillCan);
            }
        }
        void Set(TutorialStage next) { if (Stage == next) return; Stage = next; Changed?.Invoke(); }
    }
}
