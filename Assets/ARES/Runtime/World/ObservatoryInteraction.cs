using Ares.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ares.World
{
    /// <summary>Reuses the imported telescope. A small hit volume adds click and E interaction.</summary>
    public sealed class ObservatoryInteraction : MonoBehaviour
    {
        public static readonly Vector3 TelescopePosition = new Vector3(7, 1.8f, 44);
        public static readonly Vector3 StandPosition = new Vector3(7, 0, 41.8f);
        readonly Bounds hitArea = new Bounds(TelescopePosition, new Vector3(2.8f, 3.6f, 2.8f));
        ReferenceExplorer explorer;
        GameFrontend frontend;
        public bool CanUse => frontend?.Campaign != null && Vector2.Distance(
            new Vector2(explorer.PlayerPosition.x, explorer.PlayerPosition.z), new Vector2(7, 44)) <= 4.5f;
        public bool PointerOnTelescope => CanUse && Camera.main != null && Mouse.current != null &&
            hitArea.IntersectRay(Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue()));
        void Awake() { explorer = GetComponent<ReferenceExplorer>(); frontend = GetComponent<GameFrontend>(); }
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
