using Ares.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ares.World
{
    /// <summary>Inspection controller for the migration scene. Not a substitute for the original game flow.</summary>
    public sealed class ReferenceExplorer : MonoBehaviour
    {
        public GameDefinition definition;
        public bool reducedMotion, invertCamera;
        [Range(.25f, 2)] public float cameraSensitivity = 1;
        WorldNavigation navigation;
        PlayerMotion player;
        OrbitCamera rig;
        RoomCutaway cutaway;
        AstronautAnimator astronaut;
        BubbleAnimator bubble;
        readonly CameraInput cameraInput = new CameraInput();
        float walkPhase, elapsed, ambienceTime;
        Camera view;
        bool overview, roofs = true, isometric;
        Vector3 cameraPosition = new Vector3(0, 8, -12);
        public Vector3 PlayerPosition => player?.UnityPosition ?? new Vector3(0, 0, -4);
        public float CameraTargetYaw => rig?.TargetYaw ?? 0;
        void Start() { Initialize(); }
        void Initialize()
        {
            navigation = new WorldNavigation(definition.layout);
            player = new PlayerMotion(navigation);
            rig = new OrbitCamera(definition.world.camera, definition.layout);
            cutaway = new RoomCutaway(transform, definition.layout);
            view = Camera.main;
            astronaut = GetComponentInChildren<AstronautAnimator>();
            bubble = GetComponentInChildren<BubbleAnimator>();
            if (astronaut == null || bubble == null)
                Debug.LogError("ARES character prefabs are missing. Regenerate the reference scene before testing animation.", this);
        }
        void Update()
        {
            // Unity can reload scripts during Play without calling Start again.
            if (player == null || rig == null || view == null) Initialize();
            var keys = Keyboard.current;
            if (keys != null && keys.tabKey.wasPressedThisFrame) overview = !overview;
            if (keys != null && keys.cKey.wasPressedThisFrame) { isometric = !isometric; rig.Recenter(isometric); }
            if (keys != null && (keys.rKey.wasPressedThisFrame || keys.homeKey.wasPressedThisFrame)) { overview = false; rig.Recenter(isometric); }
            if (keys != null && keys.hKey.wasPressedThisFrame) roofs = !roofs;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            elapsed += Time.deltaTime;
            if (!reducedMotion) ambienceTime += dt;
            walkPhase += dt * player.Speed * 2.2f;
            float ix = keys == null ? 0 : (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
            float iz = keys == null ? 0 : (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0) - (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0);
            var mouse = Mouse.current;
            bool blocked = mouse != null && new Rect(12, Screen.height - 82, 650, 70).Contains(mouse.position.ReadValue());
            cameraInput.Read(rig, view, isometric, cameraSensitivity, invertCamera, blocked);
            rig.Step(dt, isometric);
            float yaw = Mathf.Atan2(view.transform.position.x - player.X, -view.transform.position.z - player.Z);
            Vector2 input = OrbitCamera.RelativeInput(ix, iz, yaw);
            player.Step(input.x, input.y, dt, definition.world.walkSpeed, definition.world.walkAccel);
            bool occupied = navigation.Walkable(player.UnityPosition);
            cameraPosition = Vector3.Lerp(cameraPosition, rig.Position(player.X, player.Z, occupied), 1 - Mathf.Exp(-definition.world.cameraDamping * dt));
            cameraPosition.y = rig.SafeHeight(cameraPosition.x, cameraPosition.y, -cameraPosition.z, occupied);
            if (!overview) { view.transform.position = cameraPosition; view.transform.LookAt(player.UnityPosition + Vector3.up * definition.world.camera.lookHeight); }
            else { view.transform.position = new Vector3(100, 130, -145); view.transform.LookAt(new Vector3(0, 0, -25)); }
            cutaway.Update(player.UnityPosition, view.transform.position, roofs, isometric || overview, rig.Pitch, dt);
            if (astronaut != null)
            {
                astronaut.transform.position = player.UnityPosition;
                astronaut.transform.rotation = ReferenceSpace.Euler(0, player.Heading, 0);
                astronaut.Apply(CharacterAnimation.Astronaut(player.Speed, walkPhase, elapsed, ambienceTime, 0, 0, reducedMotion, definition.world.ambience.jumpSeconds));
            }
            if (bubble != null)
            {
                bubble.Follow(player, elapsed, dt);
                bubble.Apply(ambienceTime, "happy", false, false);
            }
        }
        void OnApplicationFocus(bool focused) { if (!focused) cameraInput.Clear(); }
        void OnDisable() { cameraInput.Clear(); }
        void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 650, 70), "Base ARES — inspeção da migração\nWASD: mover • Arraste o cenário: girar • Roda: zoom • R: centralizar\nC: isométrica • H: tetos • TAB: visão geral • Campanha/interface em migração.");
        }
    }
}
