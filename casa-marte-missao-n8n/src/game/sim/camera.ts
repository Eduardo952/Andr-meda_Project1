import { BALANCE } from "@/config/balance";
import { MODULES, CORRIDORS } from "@/config/rooms";

const config = BALANCE.camera;
const radians = (degrees: number) => degrees * Math.PI / 180;
const clamp = (value: number, min: number, max: number) => Math.max(min, Math.min(max, value));

export interface CameraRig {
  yaw: number; pitch: number; distance: number;
  targetYaw: number; targetPitch: number; targetDistance: number;
}

export function createCameraRig(): CameraRig {
  const pitch = radians(config.defaultPitchDegrees);
  return { yaw: config.defaultYaw, pitch, distance: config.defaultDistance,
    targetYaw: config.defaultYaw, targetPitch: pitch, targetDistance: config.defaultDistance };
}

export function recenterCamera(rig: CameraRig, isometric = false) {
  rig.targetYaw = isometric ? config.isometricYaw : config.defaultYaw;
  rig.targetPitch = radians(isometric ? config.isometricPitchDegrees : config.defaultPitchDegrees);
  rig.targetDistance = isometric ? config.isometricDistance : config.defaultDistance;
}

export function orbitCamera(rig: CameraRig, dx: number, dy: number, sensitivity: number, invert: boolean, isometric: boolean) {
  if (isometric) return;
  const gain = config.radiansPerPixel * clamp(sensitivity, config.minSensitivity, config.maxSensitivity);
  rig.targetYaw -= dx * gain;
  rig.targetPitch = clamp(rig.targetPitch + dy * gain * (invert ? -1 : 1),
    radians(config.minPitchDegrees), radians(config.maxPitchDegrees));
}

export function zoomCamera(rig: CameraRig, factor: number) {
  rig.targetDistance = clamp(rig.targetDistance * factor, config.minDistance, config.maxDistance);
}

export function wheelZoomFactor(deltaY: number, deltaMode: number) {
  const pixels = deltaY * (deltaMode === 1 ? 16 : deltaMode === 2 ? 100 : 1);
  return Math.exp(pixels * config.wheelIntensity);
}

export function stepCameraRig(rig: CameraRig, dt: number, isometric: boolean) {
  const rotation = 1 - Math.exp(-config.rotationDamping * Math.min(dt, 0.05));
  if (isometric) {
    rig.targetYaw = config.isometricYaw;
    rig.targetPitch = radians(config.isometricPitchDegrees);
  }
  const delta = Math.atan2(Math.sin(rig.targetYaw - rig.yaw), Math.cos(rig.targetYaw - rig.yaw));
  rig.yaw += delta * rotation;
  rig.pitch += (rig.targetPitch - rig.pitch) * rotation;
  rig.distance += (rig.targetDistance - rig.distance) * (1 - Math.exp(-config.zoomDamping * Math.min(dt, 0.05)));
}

/** Input y is screen-down for both keyboard and joystick; yaw points from player to camera. */
export function cameraRelativeInput(x: number, y: number, yaw: number) {
  return { x: x * Math.cos(yaw) + y * Math.sin(yaw), z: -x * Math.sin(yaw) + y * Math.cos(yaw) };
}

/** Conservative clearance for solid hulls and reinforced overhead connectors. */
export function safeCameraHeight(x: number, y: number, z: number, occupiedId?: string) {
  let height = Math.max(config.groundClearance, y);
  for (const m of MODULES) {
    if (occupiedId || m.shape === "park") continue;
    if (Math.abs(x - m.x) < m.width / 2 + config.domeClearance && Math.abs(z - m.z) < m.depth / 2 + config.domeClearance) {
      height = Math.max(height, m.height + 0.8 + config.domeClearance);
    }
  }
  for (const c of CORRIDORS) {
    if (occupiedId) continue;
    const dx = c.b.x - c.a.x, dz = c.b.z - c.a.z;
    const length = dx * dx + dz * dz;
    const t = length ? Math.max(0, Math.min(1, ((x - c.a.x) * dx + (z - c.a.z) * dz) / length)) : 0;
    if (Math.hypot(x - c.a.x - t * dx, z - c.a.z - t * dz) < c.width / 2 + config.domeClearance) height = Math.max(height, 6);
  }
  return height;
}

export function cameraPosition(rig: CameraRig, x: number, z: number, occupiedId?: string) {
  const horizontal = Math.cos(rig.pitch) * rig.distance;
  const px = x + Math.sin(rig.yaw) * horizontal;
  const pz = z + Math.cos(rig.yaw) * horizontal;
  return { x: px, y: safeCameraHeight(px, config.lookHeight + Math.sin(rig.pitch) * rig.distance, pz, occupiedId), z: pz };
}