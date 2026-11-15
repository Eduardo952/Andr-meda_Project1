import { describe, expect, it } from "vitest";
import { BALANCE } from "@/config/balance";
import { ROOMS } from "@/config/rooms";
import { cameraPosition, cameraRelativeInput, createCameraRig, orbitCamera, recenterCamera, safeCameraHeight, stepCameraRig, wheelZoomFactor, zoomCamera } from "@/game/sim/camera";

describe("camera controls", () => {
  it("clamps pitch at 15 and 70 degrees", () => {
    const rig = createCameraRig();
    orbitCamera(rig, 0, -10000, 1, false, false);
    expect(rig.targetPitch).toBeCloseTo(15 * Math.PI / 180);
    orbitCamera(rig, 0, 10000, 1, false, false);
    expect(rig.targetPitch).toBeCloseTo(70 * Math.PI / 180);
  });
  it("keeps zoom within the configured minimum and maximum", () => {
    const rig = createCameraRig();
    zoomCamera(rig, 0.001); expect(rig.targetDistance).toBe(7);
    zoomCamera(rig, 10000); expect(rig.targetDistance).toBe(24);
  });
  it("scales wheel zoom by delta magnitude and normalizes line events", () => {
    expect(wheelZoomFactor(16, 0)).toBeCloseTo(wheelZoomFactor(1, 1));
    expect(wheelZoomFactor(120, 0)).toBeCloseTo(wheelZoomFactor(12, 0) ** 10);
  });
  it("maps W and strafing to the camera at four headings", () => {
    for (const yaw of [0, Math.PI / 2, Math.PI, -Math.PI / 2]) {
      const forward = cameraRelativeInput(0, -1, yaw);
      expect(forward.x).toBeCloseTo(-Math.sin(yaw));
      expect(forward.z).toBeCloseTo(-Math.cos(yaw));
      const right = cameraRelativeInput(1, 0, yaw);
      expect(right.x).toBeCloseTo(Math.cos(yaw));
      expect(right.z).toBeCloseTo(-Math.sin(yaw));
    }
  });
  it("maps the joystick up vector identically to keyboard W", () => {
    const keyboard = cameraRelativeInput(0, -1, 1.7);
    const joystick = cameraRelativeInput(0, -0.5, 1.7);
    expect(joystick.x).toBeCloseTo(keyboard.x * 0.5);
    expect(joystick.z).toBeCloseTo(keyboard.z * 0.5);
  });
  it("inverts vertical rotation and applies sensitivity", () => {
    const normal = createCameraRig(), inverted = createCameraRig();
    orbitCamera(normal, 10, 10, 1, false, false);
    orbitCamera(inverted, 10, 10, 2, true, false);
    expect(inverted.targetYaw).toBeCloseTo(normal.targetYaw * 2);
    expect(inverted.targetPitch - inverted.pitch).toBeCloseTo(-2 * (normal.targetPitch - normal.pitch));
  });
  it("retains the new orbit after release and damps toward it", () => {
    const rig = createCameraRig();
    orbitCamera(rig, 90, 30, 1, false, false);
    const target = rig.targetYaw;
    stepCameraRig(rig, 0.05, false);
    expect(rig.yaw).not.toBe(0);
    expect(Math.abs(rig.yaw)).toBeLessThan(Math.abs(target));
    for (let i = 0; i < 100; i++) stepCameraRig(rig, 0.05, false);
    expect(rig.targetYaw).toBe(target);
    expect(rig.yaw).toBeCloseTo(target);
  });
  it("recenters angle, pitch and zoom", () => {
    const rig = createCameraRig();
    orbitCamera(rig, 150, 20, 1, false, false); zoomCamera(rig, 2);
    recenterCamera(rig);
    expect(rig.targetYaw).toBe(0);
    expect(rig.targetPitch).toBeCloseTo(31 * Math.PI / 180);
    expect(rig.targetDistance).toBe(12.25);
  });
  it("keeps isometric yaw fixed while allowing zoom", () => {
    const rig = createCameraRig(); recenterCamera(rig, true);
    orbitCamera(rig, 100, 100, 1, false, true);
    zoomCamera(rig, 0.8); stepCameraRig(rig, 0.05, true);
    expect(rig.targetYaw).toBe(Math.PI / 4);
    expect(rig.targetPitch).toBeCloseTo(37 * Math.PI / 180);
    expect(rig.targetDistance).toBeCloseTo(23 * 0.8);
  });
  it("clamps above ground and all solid hull roofs", () => {
    expect(safeCameraHeight(60, -5, 60)).toBe(0.8);
    for (const room of ROOMS.filter(r=>r.shape!=="park")) {
      expect(safeCameraHeight(room.x, 0, room.z)).toBeGreaterThanOrEqual(room.height + 1.3 - 0.001);
      for (let angle = 0; angle < 6.3; angle += 0.3) {
        const rig = createCameraRig(); rig.yaw = angle; rig.pitch = 15 * Math.PI / 180; rig.distance = BALANCE.camera.minDistance;
        const p = cameraPosition(rig, room.x, room.z);
        expect(p.y).toBe(safeCameraHeight(p.x, p.y, p.z));
      }
    }
  });
});