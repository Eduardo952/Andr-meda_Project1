using Ares.Domain;
using Ares.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ares.World
{
    /// <summary>World input adapter for pickup, refill and four existing cultivation racks.</summary>
    public sealed class GreenhouseCare : MonoBehaviour
    {
        public static readonly Vector3 PickupPosition=new Vector3(35,0,-2);
        public static readonly Vector3 RefillPosition=new Vector3(48,0,-2.8f);
        public static readonly Vector3[] ShelfPositions={new Vector3(43,0,-4.5f),new Vector3(61,0,-4.5f),new Vector3(43,0,4.5f),new Vector3(61,0,4.5f)};
        ReferenceExplorer explorer;
        GameFrontend frontend;
        GreenhouseWorldView presentation;
        GreenhouseOcclusion occlusion;
        public GreenhouseWatering Watering => frontend?.Campaign?.Watering;
        public GreenhouseSimulation Simulation => frontend?.Campaign?.Greenhouse;
        public float GripError => presentation==null || Watering==null || !Watering.HasCan?0:Vector3.Distance(presentation.GripPosition,presentation.HandPosition);
        public int NearbyShelf { get; private set; }=-1;
        public bool Inside => explorer!=null && explorer.PlayerPosition.x>=32 && explorer.PlayerPosition.x<=72 && Mathf.Abs(explorer.PlayerPosition.z)<=16;
        bool Available => Watering!=null && frontend.Campaign.Phase==Phase.Tasks && !Simulation.Finished;
        public bool NearCanRack => Watering!=null && Inside && FlatDistance(explorer.PlayerPosition,PickupPosition)<1.8f;
        public bool CanPickUp => NearCanRack && !Watering.HasCan;
        public bool CanReturn => NearCanRack && Watering.HasCan;
        public bool AtRefill => Available && Watering.HasCan && FlatDistance(explorer.PlayerPosition,RefillPosition)<1.7f;
        public bool CapturesPointer => Available && Watering.HasCan && NearbyShelf>=0;
        public string InteractionPrompt => CanReturn?"[E] Devolver regador":CanPickUp?"[E] Pegar regador":AtRefill?(Watering.FillableMl>0?"[E] Abastecer regador":"Regador abastecido ou sem água disponível"):null;
        static float FlatDistance(Vector3 a,Vector3 b) => Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
        void Awake()
        {
            explorer=GetComponent<ReferenceExplorer>(); frontend=GetComponent<GameFrontend>();
            occlusion=new GreenhouseOcclusion(transform);
            presentation=new GreenhouseWorldView(transform,GetComponentInChildren<AstronautAnimator>()?.transform);
        }
        public bool TryInteract()
        {
            if(frontend.BlocksWorld || explorer.IsDrivingRover)return false;
            if(CanReturn) { Watering.ReturnCan(); return true; }
            if(CanPickUp) { Watering.PickUp(); return true; }
            // Consume E at the tap even when full, so it cannot open the station behind it.
            if(AtRefill) { Watering.Fill(); return true; }
            return false;
        }
        public void Tick(float seconds)
        {
            NearbyShelf=-1;
            if(!Inside || Watering==null) { Watering?.Stop(); return; }
            for(int i=0;i<4;i++)
                if(Mathf.Abs(explorer.PlayerPosition.x-ShelfPositions[i].x)<2.1f && Mathf.Abs(explorer.PlayerPosition.z-ShelfPositions[i].z)<.9f) { NearbyShelf=i; break; }
            Watering.Tick(NearbyShelf,Mouse.current!=null && Mouse.current.leftButton.isPressed,
                !Available || frontend.BlocksPointer || explorer.IsDrivingRover || !Application.isFocused,Mathf.Min(seconds,.05f));
        }
        void LateUpdate() { presentation.Update(Watering,Simulation,Inside,NearbyShelf,explorer.reducedMotion);occlusion.Update(Camera.main,explorer.PlayerPosition,Inside && Watering!=null && !explorer.IsDrivingRover && !frontend.BlocksWorld); }
        void OnDisable() { Watering?.Stop(); }
        void OnDestroy() { occlusion?.Dispose();presentation?.Dispose(); }
    }
}
