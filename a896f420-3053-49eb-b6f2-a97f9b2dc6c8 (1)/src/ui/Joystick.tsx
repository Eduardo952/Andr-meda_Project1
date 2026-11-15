import { useRef, useState } from "react";
import { input } from "@/game/runtime";

const R = 52;
export function Joystick() {
  const base = useRef<HTMLDivElement>(null);
  const activePointer = useRef<number | null>(null);
  const [knob, setKnob] = useState({ x: 0, y: 0 });
  const move = (e: React.PointerEvent) => {
    if (activePointer.current !== e.pointerId || !base.current) return;
    const b = base.current.getBoundingClientRect();
    let x = e.clientX - (b.left + b.width / 2), y = e.clientY - (b.top + b.height / 2);
    const d = Math.hypot(x, y);
    if (d > R) { x = (x / d) * R; y = (y / d) * R; }
    setKnob({ x, y }); input.joyX = x / R; input.joyY = y / R;
  };
  const end = (e: React.PointerEvent) => { if (activePointer.current !== e.pointerId) return; activePointer.current = null; setKnob({ x: 0, y: 0 }); input.joyX = 0; input.joyY = 0; };
  return (
    <div ref={base} className="relative h-36 w-36 touch-none rounded-full border-4 border-milk/70 bg-ink/30"
      onPointerDown={(e) => { if (activePointer.current !== null) return; activePointer.current = e.pointerId; e.currentTarget.setPointerCapture(e.pointerId); move(e); }} onPointerMove={move} onPointerUp={end} onPointerCancel={end} onLostPointerCapture={end}>
      <div className="absolute left-1/2 top-1/2 h-16 w-16 rounded-full bg-sun shadow-lg" style={{ transform: `translate(calc(-50% + ${knob.x}px), calc(-50% + ${knob.y}px))` }} />
    </div>
  );
}
