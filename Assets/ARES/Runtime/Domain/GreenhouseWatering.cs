using System;

namespace Ares.Domain
{
    /// <summary>One click starts one complete 510 mL dose. Interruptions cancel the pending dose, never lose water.</summary>
    public sealed class GreenhouseWatering
    {
        public const int CapacityMl = GreenhouseRules.DoseMl;
        public const float SecondsPerShelf = 2.5f;
        readonly GreenhouseSimulation game;
        float pourSeconds;
        int pourDay;
        public bool HasCan { get; private set; }
        public int CanMl { get; private set; }
        public int ActiveShelf { get; private set; } = -1;
        public int ReservoirMl => game.ReservoirMl;
        public int AvailableMl => ReservoirMl + CanMl;
        public int DeliveredMl => game.TodayDeliveredMl;
        public int FillableMl => HasCan && game.CanCare ? Math.Min(ReservoirMl, CapacityMl - CanMl) : 0;
        public int WateredShelves { get { int count = 0; for (int i = 0; i < GreenhouseRules.ShelfCount; i++) if (IsWatered(i)) count++; return count; } }
        public float PourProgress => ActiveShelf < 0 ? 0 : pourSeconds / SecondsPerShelf;
        public GreenhouseWatering(GreenhouseSimulation game) { this.game = game; }
        public void PickUp() => HasCan = true;
        public void ReturnCan() { HasCan = false; Stop(); }
        public bool IsWatered(int shelf) => shelf >= 0 && shelf < GreenhouseRules.ShelfCount && game.Shelves[shelf].CaredFor;
        public float Progress(int shelf)
        {
            var state = game.Shelves[shelf];
            return state.RequiredDoses == 0 ? (state.Inspected ? 1 : 0) : (float)state.DeliveredDoses / state.RequiredDoses;
        }
        public SoilMoisture ShelfSoil(int shelf) => game.Shelves[shelf].Soil;
        public ShelfHealth PreviewHealth(int shelf) => game.Shelves[shelf].Health;
        public bool Fill()
        {
            if (ActiveShelf >= 0 || FillableMl <= 0) return false;
            CanMl += game.Withdraw(FillableMl); return true;
        }
        public bool StartPour(int shelf)
        {
            if (!HasCan || !game.CanCare || ActiveShelf >= 0 || shelf < 0 || shelf >= GreenhouseRules.ShelfCount) return false;
            game.Inspect(shelf);
            if (IsWatered(shelf) || CanMl < CapacityMl) return false;
            ActiveShelf = shelf; pourSeconds = 0; pourDay = game.Day; return true;
        }
        public void Stop() { ActiveShelf = -1; pourSeconds = 0; }
        public bool Tick(int nearbyShelf, bool blocked, float seconds)
        {
            if (ActiveShelf < 0) return false;
            if (blocked || !HasCan || !game.CanCare || game.Day != pourDay || nearbyShelf != ActiveShelf) { Stop(); return false; }
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) return false;
            pourSeconds += seconds;
            if (pourSeconds < SecondsPerShelf) return false;
            bool delivered = game.DeliverDose(ActiveShelf);
            if (delivered) CanMl -= CapacityMl;
            Stop(); return delivered;
        }
    }
}
