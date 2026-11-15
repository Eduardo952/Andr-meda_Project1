using System;

namespace Ares.Domain
{
    /// <summary>Filling transfers water; only delivery to all four shelves commits the cultivation cycle.</summary>
    public sealed class GreenhouseWatering
    {
        public const int CapacityMl = 2320;
        public const float SecondsPerShelf = 2.5f;
        readonly GreenhouseSimulation game;
        readonly int[] delivered = new int[4];
        readonly double[] fractions = new double[4];
        IrrigationLevel chosen;
        GreenhouseAdjustment adjustment;
        public bool HasCan { get; private set; }
        public bool HasPlan { get; private set; }
        public int CanMl { get; private set; }
        public int PlannedMl { get; private set; }
        public int ActiveShelf { get; private set; } = -1;
        public int CompletedManualCycles { get; private set; }
        public int DeliveredMl { get { int total=0; foreach(int value in delivered)total+=value; return total; } }
        public int ReservoirMl => game.WaterMl - CanMl - DeliveredMl;
        public int AvailableMl => game.WaterMl - DeliveredMl;
        public int FillableMl
        {
            get
            {
                if(!HasCan || game.Finished)return 0;
                int needed=HasPlan ? Math.Max(0,PlannedMl-DeliveredMl-CanMl) : CapacityMl-CanMl;
                return Math.Min(ReservoirMl,Math.Min(CapacityMl-CanMl,needed));
            }
        }
        public int WateredShelves { get { int count=0; for(int i=0;i<4;i++) if(IsWatered(i))count++; return count; } }
        public GreenhouseWatering(GreenhouseSimulation game) { this.game=game; }
        public void PickUp() { HasCan=true; }
        // Water stays in the can on the rack; returning never refunds or discards it.
        public void ReturnCan() { HasCan=false; Stop(); }
        public int ShelfDeliveredMl(int shelf) => delivered[shelf];
        public float Progress(int shelf) => HasPlan ? (float)delivered[shelf] / (PlannedMl/4) : 0;
        public bool IsWatered(int shelf) => HasPlan && shelf>=0 && shelf<4 && delivered[shelf]>=PlannedMl/4;
        public GreenhouseDecisionError Plan(IrrigationLevel level, GreenhouseAdjustment change)
        {
            if(game.Finished)return GreenhouseDecisionError.Finished;
            if(HasPlan || !Enum.IsDefined(typeof(IrrigationLevel),level))return GreenhouseDecisionError.InvalidChoice;
            if(!game.CanAdjust(level,change))return GreenhouseDecisionError.InvalidAdjustment;
            int cost=GreenhouseSimulation.ConsumptionMl(GreenhouseSimulation.FinalLevel(level,change));
            if(game.WaterMl<cost)return GreenhouseDecisionError.InsufficientWater;
            chosen=level; adjustment=change; PlannedMl=cost; HasPlan=true; return GreenhouseDecisionError.None;
        }
        public bool Fill()
        {
            int amount=FillableMl;
            if(amount<=0)return false;
            CanMl+=amount; return true;
        }
        public void Stop() { ActiveShelf=-1; }
        public bool Tick(int shelf,bool held,bool blocked,float seconds)
        {
            ActiveShelf=-1;
            if(blocked || !held || !HasPlan || !HasCan || CanMl<=0 || shelf<0 || shelf>=4 || IsWatered(shelf)
                || float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds<=0)return false;
            ActiveShelf=shelf;
            int target=PlannedMl/4;
            double amount=target/SecondsPerShelf*seconds+fractions[shelf];
            int whole=(int)Math.Min(amount,target-delivered[shelf]);
            int poured=Math.Min(CanMl,whole);
            fractions[shelf]=amount-Math.Floor(amount);
            CanMl-=poured; delivered[shelf]+=poured;
            if(WateredShelves!=4)return false;
            // The simulation's total includes reserved/poured water until this single atomic commit.
            var result=game.Decide(chosen,adjustment);
            if(result!=GreenhouseDecisionError.None)throw new InvalidOperationException("Greenhouse changed during a locked watering plan: "+result);
            Array.Clear(delivered,0,4); Array.Clear(fractions,0,4);
            HasPlan=false; PlannedMl=0; ActiveShelf=-1; CompletedManualCycles++; return true;
        }
        public SoilMoisture ShelfSoil(int shelf) => IsWatered(shelf) ? GreenhouseSimulation.SoilAfter(game.Soil,GreenhouseSimulation.FinalLevel(chosen,adjustment)) : game.Soil;
        public ShelfHealth PreviewHealth(int shelf)
        {
            if(!IsWatered(shelf))return game.Shelves[shelf].Health;
            var light=adjustment==GreenhouseAdjustment.MoreLight || adjustment==GreenhouseAdjustment.LessLight ? GrowLight.Moderate : game.Light;
            int factors=(ShelfSoil(shelf)==SoilMoisture.Moderate?0:1)+(light==GrowLight.Moderate?0:1);
            return factors==0?ShelfHealth.Healthy:factors==2 || game.Shelves[shelf].ProblemCycles>=1?ShelfHealth.AtRisk:ShelfHealth.Problem;
        }
    }
}
