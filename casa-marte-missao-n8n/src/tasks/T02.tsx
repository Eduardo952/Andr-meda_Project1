import { Button } from "@/components/ui/button";
import { useMemo, useState } from "react";
import { useT } from "@/i18n";
import { flareFlux, parseFlareClass } from "@/engine/flare";
import type { NormalizedSol } from "@/engine/normalize";
import { TaskShell, type TaskProps } from "./TaskShell";

/** Reference classes are a teaching scale (GAME RULE), today's flare is snapshot data. */
const REFERENCE = ["B1.0", "C3.0", "M1.0", "X1.0", "A5.0"];

export function T02({ sol, onDone }: TaskProps & { sol: NormalizedSol }) {
  const { t } = useT();
  const today = sol.flare.value!;
  const items = useMemo(() => {
    const refs = REFERENCE.filter((r) => r !== today.classType).slice(0, 4).map((c) => ({ c, today: false }));
    const all = [...refs, { c: today.classType, today: true }];
    return [2, 0, 4, 1, 3].map((i) => all[i]!).filter(Boolean);
  }, [today]);
  const correct = useMemo(() => [...items].sort((a, b) => flareFlux(parseFlareClass(a.c)!) - flareFlux(parseFlareClass(b.c)!)).map((i) => i.c), [items]);
  const [picked, setPicked] = useState<string[]>([]);
  const [result, setResult] = useState<null | boolean>(null);
  const tap = (c: string) => {
    if (result !== null || picked.includes(c)) return;
    const next = [...picked, c];
    setPicked(next);
    if (next[next.length - 1] !== correct[next.length - 1]) setResult(false);
    else if (next.length === correct.length) setResult(true);
  };
  return (
    <TaskShell id="T02" onDone={onDone} result={result} sample={sol.sample}>
      <div className="flex flex-wrap gap-2">
        {items.map((i) => {
          const idx = picked.indexOf(i.c);
          return (
            <Button key={i.c} onClick={() => tap(i.c)} className={`relative min-h-14 min-w-20 rounded-2xl border-2 border-ink/20 px-3 font-display text-2xl ${idx >= 0 ? "bg-sun" : "bg-milk"}`}>
              {i.c}
              {i.today && <span className="absolute -top-2 right-1 rounded-full bg-turquoise px-2 text-xs">{t("tasks.T02.today")}</span>}
              {idx >= 0 && <span className="absolute -bottom-2 left-1 rounded-full bg-ink px-2 text-xs text-milk">{idx + 1}</span>}
            </Button>
          );
        })}
      </div>
      {result !== null && <p className="mt-3 font-bold">{correct.join(" < ")}</p>}
      <p className="mt-3 text-sm text-muted-foreground">{t("tasks.T02.fact")}</p>
    </TaskShell>
  );
}
