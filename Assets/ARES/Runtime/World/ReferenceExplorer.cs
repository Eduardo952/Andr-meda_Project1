using Ares.Data;
using Ares.World.Rover;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ares.World
{
    /// <summary>World movement and camera; campaign UI owns input blocking, not motion rules.</summary>
    public sealed class ReferenceExplorer : MonoBehaviour
    {
        public GameDefinition definition;
        public bool reducedMotion, invertCamera;
        [Range(.25f, 2)] public float cameraSensitivity = 1;
        public SprintSettings sprint = new SprintSettings();
        public MartianWindSettings windSettings = new MartianWindSettings();
        public RoverSettings roverSettings = new RoverSettings();
        public Computer.ComputerSettings computerSettings = new Computer.ComputerSettings();
        public Ares.Domain.OxygenSettings oxygenSettings = new Ares.Domain.OxygenSettings();
        public RoverController Rover { get; private set; }
        public bool IsDrivingRover => Rover != null && Rover.IsDriving;
        public Vector3 ControlledPosition => IsDrivingRover ? Rover.transform.position : PlayerPosition;
        public Ares.Domain.SolarMaintenanceSettings solarSettings = new Ares.Domain.SolarMaintenanceSettings();
        SprintDust sprintDust;
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
        Ares.UI.GameFrontend frontend;
        Vector3 cameraPosition = new Vector3(0, 8, -12);
        public Vector3 PlayerPosition => player?.UnityPosition ?? new Vector3(0, 0, -4);
        public float CameraTargetYaw => IsDrivingRover ? Rover.CameraRig.TargetYaw : rig?.TargetYaw ?? 0;
        public bool Isometric => isometric;
        public bool RoofsVisible => roofs;
        public bool IsRunning { get; private set; }
        public void ToggleCamera() { if (IsDrivingRover) return; isometric = !isometric; rig?.Recenter(isometric); }
        public void RecenterCamera() { overview = false; if (IsDrivingRover) Rover.CameraRig.Recenter(Rover.transform); else rig?.Recenter(isometric); }
        public void ToggleRover()
        {
            if (Rover == null || (frontend != null && frontend.BlocksWorld)) return;
            Rover.SetDriving(!Rover.IsDriving); cameraInput.Clear(); overview = false;
            player.VelocityX = player.VelocityZ = player.Speed = 0; IsRunning = false;
            cameraPosition = view.transform.position;
        }
        public void ToggleRoofs() { roofs = !roofs; }
        void Start() { Initialize(); }
        void Initialize()
        {
            navigation = new WorldNavigation(definition.layout);
            player = new PlayerMotion(navigation);
            rig = new OrbitCamera(definition.world.camera, definition.layout);
            cutaway = new RoomCutaway(transform, definition.layout);
            view = Camera.main;
            frontend = GetComponent<Ares.UI.GameFrontend>();
            astronaut = GetComponentInChildren<AstronautAnimator>();
            bubble = GetComponentInChildren<BubbleAnimator>();
            sprintDust = GetComponent<SprintDust>();
            if (sprintDust == null) sprintDust = gameObject.AddComponent<SprintDust>();
            Rover = GetComponentInChildren<RoverController>();
            // Non-serialized simulation state is lost after a script reload in Play.
            if (Rover != null && Rover.Motion == null) { Destroy(Rover.gameObject); Rover = null; }
            if (Rover == null)
            {
                var roverObject = new GameObject("ARES Nomad / six-wheel rover");
                roverObject.transform.SetParent(transform, false);
                Rover = roverObject.AddComponent<RoverController>();
                Rover.Initialize(definition, roverSettings);
            }
            if (astronaut == null || bubble == null)
                Debug.LogError("ARES character prefabs are missing. Regenerate the reference scene before testing animation.", this);
            if (GetComponent<MartianWind>() == null) gameObject.AddComponent<MartianWind>();
        }
        void Update()
        {
            // Unity can reload scripts during Play without calling Start again.
            if (player == null || rig == null || view == null) Initialize();
            var keys = Keyboard.current;
            bool inputBlocked = frontend != null && frontend.BlocksWorld;
            if (inputBlocked) keys = null;
            if (keys != null && keys.yKey.wasPressedThisFrame) ToggleRover();
            if (!IsDrivingRover && keys != null && keys.tabKey.wasPressedThisFrame) overview = !overview;
            if (keys != null && keys.cKey.wasPressedThisFrame) ToggleCamera();
            if (keys != null && (keys.rKey.wasPressedThisFrame || keys.homeKey.wasPressedThisFrame)) RecenterCamera();
            if (keys != null && keys.hKey.wasPressedThisFrame) ToggleRoofs();
            float dt = Mathf.Min(Time.deltaTime, .05f);
            elapsed += Time.deltaTime;
            if (!reducedMotion) ambienceTime += dt;
            walkPhase += dt * player.Speed * 2.2f;
            float ix = keys == null ? 0 : (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
            float iz = keys == null ? 0 : (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0) - (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0);
            var mouse = Mouse.current;
            bool blocked = frontend != null && frontend.BlocksPointer;
            if (inputBlocked || IsDrivingRover) cameraInput.Clear();
            else cameraInput.Read(rig, view, isometric, cameraSensitivity, invertCamera, blocked, frontend != null && ((frontend.Solar != null && frontend.Solar.CapturesPointer) || (frontend.GreenhouseCare != null && frontend.GreenhouseCare.CapturesPointer) || (frontend.Observatory != null && frontend.Observatory.PointerOnTelescope)));
            rig.Step(dt, isometric);
            float yaw = Mathf.Atan2(view.transform.position.x - player.X, -view.transform.position.z - player.Z);
            Vector2 input = OrbitCamera.RelativeInput(ix, iz, yaw);
            bool sprintRequested = keys != null && (keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed) && input.sqrMagnitude > .01f;
            if (inputBlocked || IsDrivingRover) { player.VelocityX = 0; player.VelocityZ = 0; player.Speed = 0; }
            else player.Step(input.x, input.y, dt, sprint.Speed(definition.world.walkSpeed, sprintRequested), definition.world.walkAccel);
            IsRunning = sprintRequested && player.Speed > definition.world.walkSpeed * 1.05f;
            sprintDust.Tick(player.UnityPosition, player.Heading, IsRunning, reducedMotion);
            Rover.Tick(-iz, ix, dt, inputBlocked, reducedMotion);
            bool occupied = navigation.Walkable(player.UnityPosition);
            bool computerCamera = frontend != null && frontend.Terminal != null && frontend.Terminal.OwnsCamera;
            if (!computerCamera)
            {
                cameraPosition = Vector3.Lerp(cameraPosition, rig.Position(player.X, player.Z, occupied), 1 - Mathf.Exp(-definition.world.cameraDamping * dt));
                cameraPosition.y = rig.SafeHeight(cameraPosition.x, cameraPosition.y, -cameraPosition.z, occupied);
            }
            if (computerCamera) { /* ComputerTerminal owns the camera in LateUpdate until its return finishes. */ }
            else if (IsDrivingRover)
            {
                Rover.CameraRig.Tick(view, Rover.transform, dt, inputBlocked, blocked, cameraSensitivity, invertCamera);
                cameraPosition = view.transform.position;
            }
            else if (!overview) { view.transform.position = cameraPosition; view.transform.LookAt(player.UnityPosition + Vector3.up * definition.world.camera.lookHeight); }
            else { view.transform.position = new Vector3(100, 130, -145); view.transform.LookAt(new Vector3(0, 0, -25)); }
            cutaway.Update(ControlledPosition, view.transform.position, roofs, !IsDrivingRover && (isometric || overview), IsDrivingRover ? Rover.CameraRig.Pitch : rig.Pitch, dt);
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
        void OnApplicationFocus(bool focused) { if (!focused) { cameraInput.Clear(); Rover?.Motion?.Stop(); Rover?.CameraRig?.ClearInput(); } }
        void OnDisable() { cameraInput.Clear(); IsRunning = false; if (Rover != null && Rover.Motion != null) Rover.SetDriving(false); }
        void OnGUI()
        {
            if (frontend != null) return;
            GUI.Box(new Rect(12, 12, 650, 70), "Base ARES — inspeção da migração\nWASD: mover • Arraste o cenário: girar • Roda: zoom • R: centralizar\nC: isométrica • H: tetos • TAB: visão geral • Campanha/interface em migração.");
        }
    }
}
