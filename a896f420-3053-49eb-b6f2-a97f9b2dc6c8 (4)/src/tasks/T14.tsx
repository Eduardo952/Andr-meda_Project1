import { Button } from "@/components/ui/button";
import { useEffect, useState } from "react";
import { Droplet, Sprout } from "lucide-react";
import { TaskShell, type TaskProps } from "./TaskShell";
import { sfx } from "@/audio/sfx";
import { useT } from "@/i18n";
import { GreenhouseLearning } from "@/ui/GreenhousePanel";

/** GAME RULE: tap the thirsty plants (no API). */
export function T14({ onDone }: TaskProps) {
  const { t } = useT();
  const [thirsty, setThirsty] = useState<boolean[]>(() => [true, false, true, true, false, true, false, true]);
  const left = thirsty.filter(Boolean).length;
  const [result, setResult] = useState<null | boolean>(null);
  useEffect(() => { if (left === 0 && result === null) setResult(true); }, [left, result]);
  return (
    <TaskShell id="T14" onDone={onDone} result={result}>
      <GreenhouseLearning />
      <div className="grid grid-cols-4 gap-2">
        {thirsty.map((th, i) => (
          <Button key={i} aria-label={t(th ? "garden.thirsty" : "garden.happy")} onClick={() => { if (th) { sfx.blub(); setThirsty((a) => a.map((x, j) => (j === i ? false : x))); } }}
            className={`grid h-16 place-items-center rounded-2xl border-2 border-ink/20 ${th ? "bg-sun/60" : "bg-bar-integrity/60"}`}>
            {th ? <Droplet className="h-7 w-7 text-bar-oxygen" /> : <Sprout className="h-7 w-7 text-ink" />}
          </Button>
        ))}
      </div>
    </TaskShell>
  );
}
