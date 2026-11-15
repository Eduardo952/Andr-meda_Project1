import { useState } from "react";
import { useT } from "@/i18n";
import { TaskShell, Choice, type TaskProps } from "./TaskShell";

/** GAME RULE: sort items to the water recycler or the air system (no API). */
const ITEMS: { key: string; bin: "water" | "air" }[] = [
  { key: "wash", bin: "water" }, { key: "co2", bin: "air" }, { key: "vapor", bin: "water" }, { key: "fog", bin: "water" },
];

export function T15({ onDone }: TaskProps) {
  const { t } = useT();
  const [i, setI] = useState(0);
  const [wrong, setWrong] = useState(0);
  const [result, setResult] = useState<null | boolean>(null);
  const item = ITEMS[i];
  const send = (bin: "water" | "air") => {
    if (!item || result !== null) return;
    const w = wrong + (item.bin === bin ? 0 : 1);
    setWrong(w);
    if (i + 1 >= ITEMS.length) setResult(w === 0); else setI(i + 1);
  };
  return (
    <TaskShell id="T15" onDone={onDone} result={result}>
      {item && result === null && <p className="mb-3 rounded-2xl bg-muted p-4 text-center font-display text-2xl">{t(`tasks.T15.${item.key}`)}</p>}
      <div className="grid grid-cols-2 gap-2">
        <Choice onClick={() => send("water")}>💧 {t("tasks.T15.water")}</Choice>
        <Choice onClick={() => send("air")}>🌬️ {t("tasks.T15.air")}</Choice>
      </div>
    </TaskShell>
  );
}
