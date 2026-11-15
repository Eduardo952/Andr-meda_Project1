using Ares.Domain;
using Ares.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ares.World
{
    /// <summary>Input/proximity adapter. Clicking a target delegates to the pure watering state machine.</summary>
    public sealed class GreenhouseCare : MonoBehaviour
    {
        public Ares.Data.GardenLayoutData Layout => explorer.definition.layout.garden;
        public Vector3 PickupPosition => Layout.pickup.Position;
        public Vector3 RefillPosition => Layout.refill.Position;
        public Vector3[] ShelfPositions => System.Array.ConvertAll(Layout.rows,r=>r.Approach);
        ReferenceExplorer explorer;
        GameFrontend frontend;
        GreenhouseWorldView presentation;
        GreenhouseOcclusion occlusion;
        public GreenhouseWatering Watering => frontend?.Campaign?.Watering;
        public GreenhouseSimulation Simulation => frontend?.Campaign?.Greenhouse;
        public float GripError => presentation == null || Watering == null || !Watering.HasCan ? 0 : Vector3.Distance(presentation.GripPosition, presentation.HandPosition);
        public int NearbyShelf { get; private set; } = -1;
        public bool Inside => explorer != null && explorer.PlayerPosition.x >= 32 && explorer.PlayerPosition.x <= 72 && Mathf.Abs(explorer.PlayerPosition.z) <= 16;
        bool Available => Watering != null && frontend.Campaign.Phase == Phase.Tasks && Simulation.CanCare;
        public bool NearCanRack => Watering != null && Inside && FlatDistance(explorer.PlayerPosition, PickupPosition) < 1.8f;
        public bool CanPickUp => NearCanRack && !Watering.HasCan;
        public bool CanReturn => NearCanRack && Watering.HasCan;
        public bool AtRefill => Available && Watering.HasCan && FlatDistance(explorer.PlayerPosition, RefillPosition) < 1.7f;
        public bool CapturesPointer => Inside && Mouse.current != null && Camera.main != null && Target(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue())) != -1;
        public string InteractionPrompt => CanReturn ? "Clique no suporte ou [E] para devolver" : CanPickUp ? "Clique no regador ou [E] para pegar" : AtRefill ? (Watering.FillableMl > 0 ? "Clique no reservatório ou [E] para encher" : Watering.ReservoirMl == 0 ? "Reservatório vazio" : "Regador cheio: 510 mL") : null;
        static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        void Awake()
        {
            explorer = GetComponent<ReferenceExplorer>(); frontend = GetComponent<GameFrontend>();
            presentation = new GreenhouseWorldView(transform, GetComponentInChildren<AstronautAnimator>()?.transform, Layout);
            occlusion = new GreenhouseOcclusion(transform);
        }
        public bool TryInteract()
        {
            if (frontend.BlocksWorld || explorer.IsDrivingRover) return false;
            if (frontend.TutorialActive)
            {
                int target = frontend.Tutorial.GardenTarget;
                if (target == -2 && CanPickUp) { Watering.PickUp(); return true; }
                if (target == -3 && AtRefill) { Watering.Fill(); return true; }
                if (target >= 0 && Available && NearbyShelf == target)
                { Simulation.Inspect(target); return Watering.StartPour(target); }
                return false;
            }
            if (CanReturn) { Watering.ReturnCan(); return true; }
            if (CanPickUp) { Watering.PickUp(); return true; }
            if (AtRefill) { Watering.Fill(); return true; }
            return false;
        }
        // Targets -2: can rack, -3: reservoir, 0..3: cultivation shelves, -1: no eligible target.
        int Target(Ray ray)
        {
            if (NearCanRack && new Bounds(PickupPosition + Vector3.up, new Vector3(1.8f, 2, 1.6f)).IntersectRay(ray)) return -2;
            if (AtRefill && new Bounds(RefillPosition + Vector3.up*Layout.refill.height*.5f, new Vector3(3, Layout.refill.height, 3)).IntersectRay(ray)) return -3;
            if (!Available) return -1;
            for (int i = 0; i < Layout.rows.Length; i++)
            {
                var row=Layout.rows[i];
                if (NearShelf(i) && new Bounds(row.Center+Vector3.up*1.8f,new Vector3(row.width,3.6f,row.depth+.4f)).IntersectRay(ray)) return i;
            }
            return -1;
        }
        bool NearShelf(int i) => Mathf.Abs(explorer.PlayerPosition.x - Layout.rows[i].x) < Layout.rows[i].width*.5f+.6f && Mathf.Abs(explorer.PlayerPosition.z - Layout.rows[i].Approach.z) < 1.6f;
        public bool TryClick(Ray ray)
        {
            if (Watering == null || frontend.BlocksPointer || explorer.IsDrivingRover) return false;
            int target = Target(ray);
            if (target == -1) return false;
            if (frontend.TutorialActive && !frontend.Tutorial.AllowsGardenTarget(target)) return false;
            if (target == -2) { if (Watering.HasCan) Watering.ReturnCan(); else Watering.PickUp(); }
            else if (target == -3) Watering.Fill();
            else { Simulation.Inspect(target); Watering.StartPour(target); }
            return true;
        }
        public void Tick(float seconds)
        {
            NearbyShelf = -1;
            if (!Inside || Watering == null) { Watering?.Stop(); return; }
            for (int i = 0; i < Layout.rows.Length; i++) if (NearShelf(i)) { NearbyShelf = i; break; }
            bool pointerBlocksPour = frontend.BlocksPointer && !(frontend.TutorialActive && Watering.ActiveShelf >= 0 && !frontend.BlocksWorld);
            bool blocked = !Available || pointerBlocksPour || explorer.IsDrivingRover || !Application.isFocused;
            if (!blocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Camera.main != null)
                TryClick(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()));
            // Pickup/return by click also works during energy planning, just like E.
            else if (!Available && !frontend.BlocksPointer && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Camera.main != null)
                TryClick(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()));
            Watering.Tick(NearbyShelf, blocked, Mathf.Min(seconds, .05f));
        }
        void LateUpdate()
        {
            presentation.Update(Watering, Simulation, Inside, NearbyShelf, explorer.reducedMotion, frontend.TutorialActive ? (int?)frontend.Tutorial.GardenTarget : null);
            occlusion.Update(Camera.main, explorer.PlayerPosition, Inside && Watering != null && !explorer.IsDrivingRover && !frontend.BlocksWorld);
        }
        void OnDisable() { Watering?.Stop(); }
        void OnDestroy() { occlusion?.Dispose(); presentation?.Dispose(); }
    }
}
