import { stepTelescope } from "@/game/sim/telescope";
import { TELESCOPE } from "@/config/telescope";
import { useEffect, useRef, useState } from "react";
import { Canvas, useFrame, useThree } from "@react-three/fiber";
import { QueryClientProvider, useQueryClient } from "@tanstack/react-query";
import { Environment, Lightformer } from "@react-three/drei";
import { EffectComposer, Bloom, Vignette } from "@react-three/postprocessing";
import * as THREE from "three";
import { BALANCE } from "@/config/balance";
import { SPAWN, MODULES } from "@/config/rooms";
import { PALETTE } from "@/config/palette";
import { input, runtime } from "@/game/runtime";
import { useGame } from "@/game/store";
import { dayFactor, solFraction, sunDirection } from "@/game/sim/solClock";
import { nearestStation, roomAt, stationPosition, stepMover, guideWaypoint, isWalkable, areaAt, interiorAt } from "@/game/sim/world";
import { currentTarget, useCampaign } from "@/game/campaign";
import { useStation } from "@/game/actions";
import { sfx } from "@/audio/sfx";
import { Sky } from "./Sky";
import { Terrain } from "./Terrain";
import { Base } from "./Base";
import { Decor } from "./Decor";
import { Astronaut } from "@/game/characters/Astronaut";
import { Bubble3D } from "@/game/characters/Bubble";
import { CameraControls } from "./CameraControls";
import { cameraPosition, cameraRelativeInput, createCameraRig, recenterCamera, safeCameraHeight, stepCameraRig } from "@/game/sim/camera";
import { Ambience } from "./Ambience";
import { StationDetails, WorldLabel } from "./StationDetails";
import { FIRST_WALK, alertStage, guidedTarget, useEmergencies, usePresentation } from "@/game/presentation";
import { THEME } from "@/config/theme";
import { gardenVisuals, useGarden } from "@/game/greenhouse";
import { DERIVED, SOLS } from "@/data/campaign";
import { effectiveEnergyPool, stormForSol } from "@/engine/consequences";
import { advanceTaskClock } from "@/tasks/rules";

const DAY_FOG = new THREE.Color(THEME.lighting.dayFog), NIGHT_FOG = new THREE.Color(THEME.lighting.nightFog), STORM_FOG = new THREE.Color(THEME.emergency.storm), HULL_LIGHT = new THREE.Color(THEME.hull), ALARM_LIGHT = new THREE.Color(THEME.emergency.critical), FROST_LIGHT = new THREE.Color(THEME.emergency.frost);

/** The single stepper: clock -> input -> movement -> proximity -> camera, in that order. */
function Stepper() {
  const { camera, scene, gl } = useThree();
  const ready = useRef(false);
  const frameSample = useRef({ frames: 0, seconds: 0 });
  const camPos = useRef(new THREE.Vector3(0, 8, 12));
  const saidNight = useRef(false);
  const previousIsometric = useRef(useGame.getState().settings.isometric);
  const previousSuccesses = useRef(0);
  const previousPhase = useRef(useCampaign.getState().phase);
  const previousNear = useRef<string | null>(null);
  useFrame((_, delta) => {
    runtime.metrics.calls = gl.info.render.calls; runtime.metrics.triangles = gl.info.render.triangles;
    gl.info.autoReset = false; gl.info.reset();
    // Measure real rendered frame intervals, not the clamped simulation step.
    const sample = frameSample.current;
    if (document.hidden || delta > 1) {
      sample.frames = 0; sample.seconds = 0; runtime.metrics.fps = 0;
    } else if (delta > 0) {
      sample.frames += 1; sample.seconds += delta;
      if (sample.seconds >= 0.5) {
        runtime.metrics.fps = Math.round(sample.frames / sample.seconds);
        sample.frames = 0; sample.seconds = 0;
      }
    }
    const dt = Math.min(delta, 0.05);
    useEmergencies.getState().tick(Math.min(delta, 0.25));
    const camp = useCampaign.getState();
    runtime.ambienceTime += useGame.getState().settings.reducedMotion ? 0 : dt;
    runtime.walkPhase += dt * runtime.speed * 2.2;
    runtime.jump = Math.max(0, runtime.jump - dt);
    runtime.celebration = Math.max(0, runtime.celebration - dt);
    runtime.interaction = Math.max(0, runtime.interaction - dt);
    const successes = Object.values(camp.results).reduce((n, list) => n + list.filter((r) => r.success).length, 0);
    if (successes > previousSuccesses.current) { runtime.jump = BALANCE.ambience.jumpSeconds; runtime.interaction = 1; sfx.use(); }
    previousSuccesses.current = successes;
    if (camp.phase === "summary" && previousPhase.current !== "summary") { runtime.celebration = BALANCE.ambience.celebrationSeconds; sfx.use(); }
    previousPhase.current = camp.phase;
    const activeAlert = useEmergencies.getState().active, activeStage = activeAlert ? alertStage(activeAlert) : null;
    const emergencyKind = camp.alarm ? "solar" : activeAlert?.kind === "solar" || activeAlert?.kind === "lifeSupport" || activeAlert?.kind === "dust" || activeAlert?.kind === "greenhouse" ? activeAlert.kind : "none";
    runtime.emergencyKind = emergencyKind;
    const emergencyTarget = activeStage === "emergency" ? 1 : activeStage === "warning" ? .65 : activeStage === "advisory" ? .3 : 0;
    runtime.emergencyLevel += (emergencyTarget - runtime.emergencyLevel) * (1 - Math.exp(-2 * dt));
    runtime.mood = camp.alarm || emergencyKind !== "none" ? "worried" : camp.phase === "summary" || camp.phase === "victory" ? "proud" : runtime.jump > 0 ? "excited" : "happy";
    // The Sol clock runs only during the task phase, and pauses while a task window is open.
    if (camp.phase === "tasks") {
      runtime.solTime = advanceTaskClock(runtime.solTime, camp.solIdx, dt, !!(camp.openTask || camp.energyOpen || camp.sealOpen || camp.reportOpen));
      camp.tickAlarm(dt);
    }
    runtime.sunDir = sunDirection(solFraction(runtime.solTime));
    runtime.alarm += ((camp.alarm ? 1 : 0) - runtime.alarm) * (1 - Math.exp(-3 * dt));
    runtime.day = dayFactor(runtime.sunDir[1]) * (1 - 0.6 * runtime.alarm);

    const k = input.keys;
    let ix = (k.has("d") || k.has("arrowright") ? 1 : 0) - (k.has("a") || k.has("arrowleft") ? 1 : 0) + input.joyX;
    let iz = (k.has("s") || k.has("arrowdown") ? 1 : 0) - (k.has("w") || k.has("arrowup") ? 1 : 0) + input.joyY;
    const st = useGame.getState();
    if (camp.openTask || camp.energyOpen || camp.sealOpen || camp.reportOpen || camp.phase === "summary" || runtime.observing) { ix = 0; iz = 0; }
    if (st.settings.isometric !== previousIsometric.current) {
      recenterCamera(runtime.camera, st.settings.isometric);
      previousIsometric.current = st.settings.isometric;
    }
    stepCameraRig(runtime.camera, dt, st.settings.isometric);
    // Read the actual rendered heading, including follow damping, not the target orbit angle.
    const viewYaw = Math.atan2(camera.position.x - runtime.x, camera.position.z - runtime.z);
    const movement = cameraRelativeInput(ix, iz, viewYaw);
    if (runtime.observing) {
      if (!runtime.observeArrived) runtime.observeArrived = stepTelescope(runtime, runtime.observePath, dt);
      else { runtime.vx = 0; runtime.vz = 0; runtime.speed = 0; runtime.observeWait += dt; }
    } else stepMover(runtime, movement.x, movement.z, dt, BALANCE.walkSpeed, BALANCE.walkAccel);

    const near = nearestStation(runtime.x, runtime.z, BALANCE.useRadius);
    if (near && near !== previousNear.current) runtime.interaction = 1;
    previousNear.current = near;
    const nearScope = Math.hypot(runtime.x - TELESCOPE.x, runtime.z - TELESCOPE.z) <= TELESCOPE.useRadius;
    if (nearScope !== st.nearTelescope) useGame.setState({ nearTelescope: nearScope });
    if (near) usePresentation.getState().reach(near);
    if (near !== st.nearStation) { st.setNear(near); if (near) sfx.near(); }
    const area = areaAt(runtime.x, runtime.z)?.id ?? null;
    if (area !== st.currentArea) st.setArea(area);
    const room = roomAt(runtime.x, runtime.z);
    if (room !== st.currentRoom) { st.setRoom(room); if (room) sfx.door(); }
    if (room === "shield" && camp.alarm) camp.clearAlarm(true);
    if (runtime.day < 0.2 && !saidNight.current) { saidNight.current = true; st.say("bubble.night"); }
    if (runtime.day > 0.6) saidNight.current = false;

    runtime.interiorId = interiorAt(runtime.x, runtime.z)?.id ?? null;
    const occupied = runtime.interiorId ?? (isWalkable(runtime.x,runtime.z) ? "tunnel" : undefined);
    const position = cameraPosition(runtime.camera, runtime.x, runtime.z, occupied);
    const target = new THREE.Vector3(position.x, position.y, position.z);
    camPos.current.lerp(target, 1 - Math.exp(-BALANCE.cameraDamping * dt));
    camPos.current.y = safeCameraHeight(camPos.current.x, camPos.current.y, camPos.current.z, occupied);
    camera.position.copy(camPos.current);
    camera.lookAt(runtime.x, BALANCE.camera.lookHeight, runtime.z);

    if (scene.fog) {
      const fog = scene.fog as THREE.Fog, storm = stormForSol(camp.solIdx, SOLS[camp.solIdx]?.windSpeed.value?.av ?? null);
      const stormLevel = storm.active && camp.phase === "tasks" ? Math.max(.45, runtime.emergencyLevel) : 0;
      fog.color.copy(NIGHT_FOG).lerp(DAY_FOG, runtime.day).lerp(STORM_FOG, stormLevel * .75);
      fog.near = 65 - stormLevel * 30; fog.far = 230 - stormLevel * 90;
    }
    if (!ready.current || !document.querySelector("[data-game-root][data-scene-ready]")) { ready.current = true; document.querySelector("[data-game-root]")?.setAttribute("data-scene-ready", "1"); }
  });
  return null;
}

function Lights({ shadows }: { shadows: boolean }) {
  const sun = useRef<THREE.DirectionalLight>(null);
  const amb = useRef<THREE.AmbientLight>(null);
  const hemi = useRef<THREE.HemisphereLight>(null);
  useFrame(() => {
    const l = sun.current; if (!l) return;
    const [x, y, z] = runtime.sunDir;
    const up = Math.max(y, 0.05);
    l.position.set(runtime.x + x * 40, up * 40, runtime.z + z * 40);
    l.target.position.set(runtime.x, 0, runtime.z); l.target.updateMatrixWorld();
    l.intensity = 0.2 + runtime.day * 1.5;
    if (amb.current) { amb.current.intensity = 0.4 + runtime.day * 0.22; amb.current.color.copy(HULL_LIGHT).lerp(runtime.emergencyKind === "lifeSupport" ? FROST_LIGHT : ALARM_LIGHT, runtime.emergencyLevel * .7); }
    if (hemi.current) { hemi.current.intensity = 0.4 + runtime.day * 0.45; hemi.current.color.copy(HULL_LIGHT).lerp(ALARM_LIGHT, runtime.emergencyLevel * .45); }
  });
  return (
    <>
      <directionalLight ref={sun} color={THEME.lighting.warmFill} castShadow={shadows} shadow-mapSize={[1024, 1024]} shadow-camera-left={-25} shadow-camera-right={25} shadow-camera-top={25} shadow-camera-bottom={-25} shadow-normalBias={.035} shadow-radius={3} shadow-bias={-0.0005} />
      <ambientLight ref={amb} color={THEME.hull} />
      <hemisphereLight ref={hemi} color={THEME.face} groundColor={THEME.terrain} />
      <directionalLight position={[12, 8, -18]} intensity={0.4} color={THEME.lighting.coolFill} />
    </>
  );
}

function TaskArrow() {
  const mission = useCampaign(() => currentTarget());
  const urgent = useCampaign(s=>!!s.alarm);
  const manual = usePresentation(s=>s.manual), step = usePresentation(s=>s.tourStep), dismissed=usePresentation(s=>s.tourDismissed);
  const target = guidedTarget(mission,manual,dismissed?null:FIRST_WALK[step]??null,urgent);
  const arrow = useRef<THREE.Group>(null);
  const beacon = useRef<THREE.Group>(null);
  const trail = useRef<THREE.Group>(null);
  useFrame(({ clock }) => {
    if (!target) return;
    const [tx, tz] = stationPosition(target);
    const waypoint = guideWaypoint(runtime.x, runtime.z, target);
    const dx = waypoint.x - runtime.x, dz = waypoint.z - runtime.z;
    const d = Math.max(0.001, Math.hypot(dx, dz));
    const a = arrow.current;
    if (a) {
      a.visible = d > 3;
      a.position.set(runtime.x + (dx / d) * 1.6, 0.25 + Math.sin(clock.elapsedTime * 4) * 0.08, runtime.z + (dz / d) * 1.6);
      a.rotation.y = Math.atan2(dx, dz);
    }
    beacon.current?.position.set(tx, 0, tz);
    if (beacon.current) beacon.current.rotation.y += 0.02;
    trail.current?.children.forEach((c, i) => {
      const f = ((i + 1) / 6 + clock.elapsedTime * 0.25) % 1;
      const s = Math.min(1, 2.4 / d) + f * (1 - Math.min(1, 2.4 / d));
      c.position.set(runtime.x + dx * s, 0.18, runtime.z + dz * s);
      c.visible = d > 3;
    });
  });
  if (!target) return null;
  return (
    <>
      <group ref={arrow}>
        <mesh rotation={[Math.PI / 2, 0, 0]}><coneGeometry args={[0.35, 0.8, 4]} /><meshBasicMaterial color={PALETTE.sunYellow} /></mesh>
      </group>
      <group ref={trail}>
        {Array.from({ length: 6 }, (_, i) => <mesh key={i}><sphereGeometry args={[0.1, 6, 4]} /><meshBasicMaterial color={PALETTE.sunYellow} transparent opacity={0.7} /></mesh>)}
      </group>
      <group ref={beacon}>
        <mesh position={[0, 3.2, 0]}><octahedronGeometry args={[0.35, 0]} /><meshBasicMaterial color={PALETTE.sunYellow} /></mesh>
        <mesh position={[0, 1.6, 0]}><cylinderGeometry args={[0.05, 0.05, 3.2]} /><meshBasicMaterial color={PALETTE.sunYellow} transparent opacity={0.4} /></mesh>
      </group>
    </>
  );
}

function AlarmLight() {
  const ref = useRef<THREE.PointLight>(null);
  useFrame(({ clock }) => {
    const l = ref.current; if (!l) return;
    l.position.set(runtime.x, 6, runtime.z);
    l.intensity = Math.max(runtime.alarm, runtime.emergencyLevel) * (20 + Math.sin(clock.elapsedTime * 6) * 8);
  });
  return <pointLight ref={ref} color={THEME.emergency.critical} distance={30} decay={1.5} intensity={0} />;
}

function BubbleFollower() {
  const id = useGame((s) => s.bubbleLine.id);
  const [talking, setTalking] = useState(true);
  useEffect(() => {
    setTalking(true);
    const t = setTimeout(() => setTalking(false), 3500);
    return () => clearTimeout(t);
  }, [id]);
  return <Bubble3D talking={talking} />;
}

export function GameCanvas() {
  const queryClient = useQueryClient();
  const isTouch = useGame((s) => s.isTouch);
  const reduced = useGame((s) => s.settings.reducedMotion);
  const quality = useGame((s) => s.settings.quality);
  const shadows = quality === "high" && !isTouch;
  const taskOverlay = useCampaign(s => !!(s.openTask || s.energyOpen || s.sealOpen));
  const emergencyActive = useEmergencies(s => !!s.active);

  useEffect(() => {
    const down = (e: KeyboardEvent) => {
      const el = e.target as HTMLElement | null;
      if (el && (el.isContentEditable || el.closest("input, textarea, select, [role=dialog]"))) return;
      const k = e.key.toLowerCase();
      if (k === "r") { e.preventDefault(); recenterCamera(runtime.camera, useGame.getState().settings.isometric); return; }
      if (k === "e" || k === " " || k === "enter") { if (useGame.getState().nearStation) { e.preventDefault(); useStation(); } return; }
      if (["w", "a", "s", "d", "arrowup", "arrowdown", "arrowleft", "arrowright"].includes(k)) { e.preventDefault(); input.keys.add(k); }
    };
    const up = (e: KeyboardEvent) => input.keys.delete(e.key.toLowerCase());
    const blur = () => input.keys.clear();
    window.addEventListener("keydown", down); window.addEventListener("keyup", up); window.addEventListener("blur", blur);
    // Reset player to a clean start
    runtime.x = SPAWN.x; runtime.z = SPAWN.z; runtime.vx = runtime.vz = 0;
    runtime.observing = false; runtime.observe = 0; runtime.observeArrived = false; runtime.observeWait = 0; runtime.observePath = []; useGame.setState({ nearTelescope: false });
    runtime.camera = createCameraRig();
    runtime.cleaning = 0; runtime.cleaningSol = -1; runtime.cleaningFrom = [0, 0, 0, 0];
    useGarden.getState().reset();
    recenterCamera(runtime.camera, useGame.getState().settings.isometric);
    // Dev-only observations expose the live rig, not a second module instance.
    if (import.meta.env.DEV) Object.assign(window, { __game: {
      get state() { const c = useCampaign.getState(); return { phase: c.phase, solIdx: c.solIdx, results: c.results, openTask: c.openTask, alarmElapsed: c.alarmElapsed, dose: c.dose, bonusStars: c.bonusStars, panelDust: [...c.panelDust], energyPool: effectiveEnergyPool(c.panelDust), cleanedThisSol: c.cleanedThisSol, consequences: c.consequences, failureCause: c.failureCause, emergency: { active: useEmergencies.getState().active, queue: useEmergencies.getState().queue }, x: runtime.x, z: runtime.z, interiorId: runtime.interiorId, mood: runtime.mood, jump: runtime.jump, celebration: runtime.celebration, cleaning: runtime.cleaning, solTime: runtime.solTime, observing: runtime.observing, observeArrived: runtime.observeArrived, observeWait: runtime.observeWait, nearTelescope: useGame.getState().nearTelescope, telescope: { ...TELESCOPE }, camera: { ...runtime.camera }, settings: { ...useGame.getState().settings }, alloc: { ...c.alloc }, metrics: { ...runtime.metrics }, navigation: { ...usePresentation.getState(), setManual: undefined, setCameraUp: undefined, reach: undefined, dismissTour: undefined } }; },
      events: { force: (kind: "solar" | "lifeSupport" | "dust" | "greenhouse") => { if (kind === "solar") useCampaign.setState({ phase: "tasks", reportOpen: false, alarm: "flare", alarmElapsed: 0 }); else { if (kind === "dust") useCampaign.setState({ panelDust: [80, 60, 40, 20], cleanedThisSol: false }); useEmergencies.getState().offer({ id: `forced-${kind}`, kind }); } }, clear: () => { useCampaign.setState({ alarm: null }); useEmergencies.getState().reset(); sfx.stopDanger(); } },
      tasks: { preview: (id: string, solIdx = 0) => { useCampaign.setState({ solIdx, phase: "tasks", openTask: id as import("@/tasks/registry").TaskId, reportOpen: false, energyOpen: false, sealOpen: false }); } },
      setTime: (fraction: number) => { if (Number.isFinite(fraction)) runtime.solTime = useCampaign.getState().solIdx + Math.max(0, Math.min(0.999, fraction)); },
      recenter: () => recenterCamera(runtime.camera, useGame.getState().settings.isometric),
      stationPosition,
      setQuality: (q: "low" | "high") => useGame.getState().setSetting("quality",q),
      setRoofs: (r: "auto" | "off") => useGame.getState().setSetting("roofs",r),
      simulate: (seconds:number, x:number,z:number) => { for(let i=0;i<Math.ceil(seconds/0.02);i++)stepMover(runtime,x,z,0.02,BALANCE.walkSpeed,BALANCE.walkAccel); },
      teleport: (x: number, z: number) => { if (isWalkable(x, z)) { runtime.x = x; runtime.z = z; runtime.vx = runtime.vz = 0; } },
      modules: MODULES,
      overview: () => { runtime.camera.targetDistance = 150; runtime.camera.targetPitch = 1.2; },
      greenhouse: { get state() { const c=useCampaign.getState(); return { ...gardenVisuals(c.alloc.greenhouse,DERIVED[c.solIdx]?.demand.greenhouse??20), collected: [...useGarden.getState().collected] }; } },
      view: (yaw: number, pitch: number, distance: number) => { if ([yaw,pitch,distance].every(Number.isFinite)) { runtime.camera.targetYaw=yaw; runtime.camera.targetPitch=pitch; runtime.camera.targetDistance=distance; } },
    } });
    return () => { window.removeEventListener("keydown", down); window.removeEventListener("keyup", up); window.removeEventListener("blur", blur); };
  }, []);

  return (
    <Canvas frameloop={taskOverlay && !emergencyActive ? "demand" : "always"} className="world-canvas" shadows={shadows} dpr={[1, BALANCE.quality[quality].pixelRatio]} camera={{ fov: 50, near: 0.1, far: 500, position: [0, 8, 12] }} gl={{ antialias: !isTouch, powerPreference: "high-performance" }}>
      <fog attach="fog" args={[THEME.lighting.dayFog, 65, 230]} />
      <QueryClientProvider client={queryClient}>
      <Stepper />
      <CameraControls />
      <Environment resolution={64}>
        <Lightformer intensity={1.5} position={[0, 6, 0]} scale={[10, 10, 1]} rotation-x={Math.PI / 2} />
        <Lightformer intensity={1} color={THEME.lighting.coolFill} position={[-6, 2, 0]} rotation-y={Math.PI / 2} scale={[20, 2, 1]} />
        <Lightformer intensity={0.8} color={THEME.lighting.warmFill} position={[6, 2, 0]} rotation-y={-Math.PI / 2} scale={[20, 2, 1]} />
      </Environment>
      <Sky />
      <Lights shadows={shadows} />
      <Terrain />
      <Base />
      <StationDetails />
      <WorldLabel />
      <Decor />
      <Ambience />
      <group userData={{ cameraInteractive: true }}><Astronaut /></group>
      <BubbleFollower />
      <TaskArrow />
      {quality === "high" && <AlarmLight />}
      {quality === "high" && !isTouch && !reduced && (
        <EffectComposer>
          <Bloom luminanceThreshold={1.1} intensity={0.25} mipmapBlur />
          <Vignette offset={0.35} darkness={0.18} />
        </EffectComposer>
      )}
      </QueryClientProvider>
    </Canvas>
  );
}
