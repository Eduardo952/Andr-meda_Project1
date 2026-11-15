using System;
using Ares.Domain;
using Ares.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ares.World
{
    /// <summary>World adapter: binds the 32 imported cells by position, owns tool input and presentation.</summary>
    public sealed class SolarMaintenance : MonoBehaviour
    {
        readonly SolarPanelView[] panels = new SolarPanelView[SolarPlant.PanelCount];
        ReferenceExplorer explorer;
        GameFrontend frontend;
        SolarBroomView broom;
        AstronautAnimator astronaut;
        Transform visuals;
        Material material;
        public SolarPlant Plant { get; private set; }
        public SolarCleaningSession Cleaning { get; private set; }
        public int NearbyPanel { get; private set; } = -1;
        public Vector3 BroomPickupPosition => broom!=null?broom.PickupPosition:Vector3.zero;
        public bool NearBroom => explorer != null && broom!=null && Vector3.Distance(explorer.PlayerPosition, BroomPickupPosition) <= 2;
        public bool CanPickUp => Cleaning != null && !Cleaning.HasBroom && NearBroom;
        public bool CanReturn => Cleaning != null && Cleaning.HasBroom && NearBroom;
        public bool CapturesPointer => Cleaning != null && Cleaning.HasBroom && NearbyPanel >= 0;
        public int BoundPanels { get; private set; }
        public Vector3 PanelCenter(int index) => panels[index].Center;

        void Awake()
        {
            explorer = GetComponent<ReferenceExplorer>(); frontend = GetComponent<GameFrontend>();
            astronaut = GetComponentInChildren<AstronautAnimator>();
            var shader = Resources.Load<Shader>("ARES/Effects/SolarDust");
            material = new Material(shader) { name = "Solar maintenance (runtime)" };
            var sources = GetComponentsInChildren<MeshRenderer>();
            visuals = new GameObject("Solar maintenance").transform; visuals.SetParent(transform, false);
            for (int i = 0; i < panels.Length; i++)
            {
                int col = i % 8, row = i / 8;
                var expected = explorer.definition.layout.SolarPoint(-21 + col * 6 + (col < 4 ? -1 : 1), 1.5f, 72 + row * 6);
                foreach (var source in sources)
                {
                    if (Vector3.Distance(source.bounds.center, expected) > .1f || source.sharedMaterial.mainTexture == null) continue;
                    panels[i] = new SolarPanelView(visuals, source, material, i, explorer.definition.layout.SolarRotation); BoundPanels++; break;
                }
            }
            if (BoundPanels != SolarPlant.PanelCount) Debug.LogError($"Solar maintenance needs 32 imported panels; found {BoundPanels}. Recheck the geometry adapter.", this);
            broom = new SolarBroomView(visuals, material);
            foreach(var part in GetComponentsInChildren<ReferenceWorldPart>())
                if(part.role=="solar_dust")part.GetComponent<Renderer>().enabled=false;
        }
        public void Bind(SolarPlant plant)
        {
            Plant = plant; Cleaning = new SolarCleaningSession(plant); NearbyPanel = -1;
        }
        public bool TryPickUp()
        {
            if (frontend.BlocksWorld || explorer.IsDrivingRover || !CanPickUp) return false;
            Cleaning.PickUpBroom(); return true;
        }
        public bool TryInteract()
        {
            if (frontend.BlocksWorld || explorer.IsDrivingRover) return false;
            if (!CanReturn) return TryPickUp();
            Cleaning.ReturnBroom(); return true;
        }
        public void Tick(float seconds)
        {
            if (Plant == null) return;
            bool blocked = frontend.BlocksWorld || explorer.IsDrivingRover || frontend.Campaign == null || frontend.Campaign.Phase != Phase.Tasks;
            NearbyPanel = -1;
            for (int i=0;i<panels.Length;i++) if (panels[i] != null && panels[i].Contains(explorer.PlayerPosition)) { NearbyPanel = i; break; }
            var mouse = Mouse.current;
            Cleaning.Tick(NearbyPanel, mouse != null && mouse.leftButton.isPressed, blocked || frontend.BlocksPointer || !Application.isFocused, Mathf.Min(seconds,.05f));
        }
        void LateUpdate()
        {
            bool active = frontend != null && frontend.Campaign != null && Plant != null;
            visuals.gameObject.SetActive(active);
            if (!active) return;
            bool visible = Vector3.Distance(explorer.PlayerPosition, explorer.definition.layout.Room("solar").Center) < 40;
            for (int i=0;i<panels.Length;i++) panels[i]?.Update(Plant.Dust(i), NearbyPanel == i, visible);
            broom.Update(astronaut, Cleaning.HasBroom, Cleaning.ActivePanel >= 0, explorer.reducedMotion, NearbyPanel >= 0 ? panels[NearbyPanel].Center : Vector3.zero,frontend.GreenhouseCare==null || !frontend.GreenhouseCare.Inside);
        }
        void OnDisable() { Cleaning?.Cancel(); }
        void OnDestroy() { if (material != null) Destroy(material); if (visuals != null) Destroy(visuals.gameObject); }
    }
}
