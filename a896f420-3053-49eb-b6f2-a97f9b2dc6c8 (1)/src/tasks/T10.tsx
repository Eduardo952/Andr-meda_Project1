import { useMemo, useState } from "react";
import { useT } from "@/i18n";
import type { NormalizedSol } from "@/engine/normalize";
import { TaskShell, Choice, type TaskProps } from "./TaskShell";
import { Illustration } from "./Illustration";

const KINDS = ["rover", "sunset", "dunes", "dustDevil", "olympus"];

export function T10({ sol, onDone }: TaskProps & { sol: NormalizedSol }) {
  const { t } = useT();
  const photo = sol.photo.value;
  const options = useMemo(() => {
    const others = KINDS.filter((k) => k !== photo?.illustration);
    const o = [photo?.illustration??"rover", others[sol.sol % others.length]??"sunset", others[(sol.sol + 1) % others.length]??"dunes"];
    return sol.sol % 2 ? o : [o[1]??"sunset", o[0]??"rover", o[2]??"dunes"];
  }, [photo, sol.sol]);
  const [pick, setPick] = useState<string | null>(null);
  if (!photo) return null;
  const result = pick ? pick === photo.illustration : null;
  return (
    <TaskShell id="T10" onDone={onDone} result={result} sample={sol.sample}>
      <Illustration kind={photo.illustration} />
      <p className="text-xs text-muted-foreground">{t("tasks.T10.sampleArt")} {t("tasks.T10.credit", { c: photo.credit })}</p>
      <div className="mt-3 grid gap-2">
        {options.map((k) => <Choice key={k} onClick={() => !pick && setPick(k)} state={pick === k ? (result ? "right" : "wrong") : undefined}>{t(`photo.${k}`)}</Choice>)}
      </div>
    </TaskShell>
  );
}
