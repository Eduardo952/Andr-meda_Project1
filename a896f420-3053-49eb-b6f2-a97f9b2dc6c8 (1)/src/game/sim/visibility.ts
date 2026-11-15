export function roofOpacity(mode: "auto" | "off", occupied: boolean, isometric: boolean, pitch: number) {
  if (mode === "off") return 0;
  return occupied || isometric || pitch > Math.PI / 3 ? 0.025 : 0.3;
}