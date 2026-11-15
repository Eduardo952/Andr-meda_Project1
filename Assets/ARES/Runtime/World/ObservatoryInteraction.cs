using Ares.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ares.World
{
    /// <summary>Reuses the imported telescope. A small hit volume adds click and E interaction.</summary>
    public sealed class ObservatoryInteraction : MonoBehaviour
    {
        public Vector3 TelescopePosition { get; private set; }
        public Vector3 StandPosition { get; private set; }
        Bounds hitArea;
        ReferenceExplorer explorer;
        GameFrontend frontend;
        public bool CanUse => frontend?.Campaign != null && Vector2.Distance(
            new Vector2(explorer.PlayerPosition.x, explorer.PlayerPosition.z), new Vector2(TelescopePosition.x, TelescopePosition.z)) <= 4.5f;
        public bool PointerOnTelescope => CanUse && Camera.main != null && Mouse.current != null &&
            hitArea.IntersectRay(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()));
        void Awake()
        {
            explorer = GetComponent<ReferenceExplorer>(); frontend = GetComponent<GameFrontend>();
            TelescopePosition=explorer.definition.layout.RoomPoint("observatory",7,1.8f,-2);
            StandPosition=explorer.definition.layout.RoomPoint("observatory",7,0,.2f);
            hitArea=new Bounds(TelescopePosition,new Vector3(2.8f,3.6f,2.8f));
        }
        public bool TryInteract()
        {
            if (!CanUse || frontend.BlocksWorld || explorer.IsDrivingRover) return false;
            frontend.ShowObservatory();
            return true;
        }
        public void Tick()
        {
            if (!frontend.BlocksPointer && PointerOnTelescope && Mouse.current.leftButton.wasPressedThisFrame)
                TryInteract();
        }
    }
}
