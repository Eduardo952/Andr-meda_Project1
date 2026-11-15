import { useState } from "react";
import { useT } from "@/i18n";
import type { NormalizedSol } from "@/engine/normalize";
import { TaskShell, type TaskProps } from "./TaskShell";

const POINTS = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];

export function T08({ sol, onDone }: TaskProps & { sol: NormalizedSol }) {
  const { t } = useT();
  const wd = sol.windDir.value!;
  const max = Math.max(1, ...Object.values(wd.counts));
  const [pick, setPick] = useState<string | null>(null);
  const result = pick ? pick === wd.mostCommon : null;
  return (
    <TaskShell id="T08" onDone={onDone} result={result} sample={sol.sample}>
      <svg viewBox="-120 -120 240 240" className="mx-auto h-64 w-64">
        <circle r="100" className="fill-muted stroke-ink/20" />
        {POINTS.map((p, i) => {
          const a = (i / 16) * Math.PI * 2 - Math.PI / 2;
          const len = 20 + ((wd.counts[p] ?? 0) / max) * 70;
          const x = Math.cos(a), y = Math.sin(a);
          const state = pick === p ? (result ? "fill-bar-integrity" : "fill-coral") : "fill-turquoise";
          return (
            <g key={p} onClick={() => !pick && setPick(p)} className="cursor-pointer">
              <path d={`M0 0 L${x * len - y * 8} ${y * len + x * 8} L${x * len + y * 8} ${y * len - x * 8} Z`} className={`${state} stroke-milk`} strokeWidth="1.5" />
              <circle cx={x * 108} cy={y * 108} r="11" className={pick === p ? state : "fill-milk"} />
              <text x={x * 108} y={y * 108 + 4} textAnchor="middle" className="fill-ink text-[8px] font-bold">{t(`compass.${p}`)}</text>
            </g>
          );
        })}
      </svg>
    </TaskShell>
  );
}
