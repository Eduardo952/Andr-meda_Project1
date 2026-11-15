import { useEffect } from "react";
import { useThree } from "@react-three/fiber";
import * as THREE from "three";
import { runtime } from "@/game/runtime";
import { useGame } from "@/game/store";
import { orbitCamera, wheelZoomFactor, zoomCamera } from "@/game/sim/camera";

/** Native canvas listeners leave HUD controls and the left joystick untouched. No pointer lock. */
export function CameraControls() {
  const { gl, camera, scene } = useThree();
  useEffect(() => {
    const canvas = gl.domElement;
    const pointers = new Map<number, { x: number; y: number; touch: boolean }>();
    const raycaster = new THREE.Raycaster();
    const pointer = new THREE.Vector2();
    let pinchDistance = 0;
    const touches = () => [...pointers.values()].filter((p) => p.touch);
    const separation = () => {
      const [a, b] = touches();
      return a && b ? Math.hypot(a.x - b.x, a.y - b.y) : 0;
    };
    const overObject = (e: PointerEvent) => {
      const rect = canvas.getBoundingClientRect();
      pointer.set((e.clientX - rect.left) / rect.width * 2 - 1, -(e.clientY - rect.top) / rect.height * 2 + 1);
      raycaster.setFromCamera(pointer, camera);
      return raycaster.intersectObjects(scene.children, true).some((hit) => {
        let object: THREE.Object3D | null = hit.object;
        while (object) {
          if (object.userData['cameraInteractive']) return true;
          object = object.parent;
        }
        return false;
      });
    };
    const down = (e: PointerEvent) => {
      const rect = canvas.getBoundingClientRect();
      const touch = e.pointerType === "touch";
      if (touch) {
        // The second finger may start anywhere on the scene, but never on the joystick/HUD.
        if (!touches().length && e.clientX < rect.left + rect.width / 2) return;
      } else if (e.button !== 2 && (e.button !== 0 || overObject(e))) return;
      e.preventDefault();
      pointers.set(e.pointerId, { x: e.clientX, y: e.clientY, touch });
      canvas.setPointerCapture(e.pointerId);
      pinchDistance = separation();
    };
    const move = (e: PointerEvent) => {
      const previous = pointers.get(e.pointerId);
      if (!previous) return;
      const dx = e.clientX - previous.x, dy = e.clientY - previous.y;
      pointers.set(e.pointerId, { ...previous, x: e.clientX, y: e.clientY });
      e.preventDefault();
      if (previous.touch && touches().length >= 2) {
        const next = separation();
        if (pinchDistance > 0 && next > 0) zoomCamera(runtime.camera, pinchDistance / next);
        pinchDistance = next;
      } else {
        const settings = useGame.getState().settings;
        orbitCamera(runtime.camera, dx, dy, settings.cameraSensitivity, settings.invertCamera, settings.isometric);
      }
    };
    const end = (e: PointerEvent) => {
      pointers.delete(e.pointerId);
      pinchDistance = separation();
      if (canvas.hasPointerCapture(e.pointerId)) canvas.releasePointerCapture(e.pointerId);
    };
    const clear = () => {
      for (const id of pointers.keys()) if (canvas.hasPointerCapture(id)) canvas.releasePointerCapture(id);
      pointers.clear(); pinchDistance = 0;
    };
    const wheel = (e: WheelEvent) => {
      e.preventDefault();
      zoomCamera(runtime.camera, wheelZoomFactor(e.deltaY, e.deltaMode));
    };
    const contextMenu = (e: Event) => e.preventDefault();
    canvas.style.touchAction = "none";
    canvas.addEventListener("pointerdown", down);
    canvas.addEventListener("pointermove", move);
    canvas.addEventListener("pointerup", end);
    canvas.addEventListener("pointercancel", end);
    canvas.addEventListener("lostpointercapture", end);
    canvas.addEventListener("wheel", wheel, { passive: false });
    canvas.addEventListener("contextmenu", contextMenu);
    window.addEventListener("blur", clear);
    return () => {
      clear();
      canvas.removeEventListener("pointerdown", down);
      canvas.removeEventListener("pointermove", move);
      canvas.removeEventListener("pointerup", end);
      canvas.removeEventListener("pointercancel", end);
      canvas.removeEventListener("lostpointercapture", end);
      canvas.removeEventListener("wheel", wheel);
      canvas.removeEventListener("contextmenu", contextMenu);
      window.removeEventListener("blur", clear);
    };
  }, [gl, camera, scene]);
  return null;
}