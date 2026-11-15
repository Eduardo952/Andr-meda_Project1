import { useState } from "react";
import { useT } from "@/i18n";
import type { NormalizedSol } from "@/engine/normalize";
import type { DerivedSol } from "@/engine/normalize";
import { TaskShell, Choice, type TaskProps } from "./TaskShell";

const DISTRACTORS = [{ type: "IPS", body: "IPS" }, { type: "GST", body: "GST" }];

export function mainEventType(sol: NormalizedSol, d: DerivedSol): string {
  if (d.alarm === "sep") return "SEP";
  if (sol.cme.value) return "CME";
  return "FLR";
}

export function T06({ sol, derived, onDone }: TaskProps & { sol: NormalizedSol; derived: DerivedSol }) {
  const { t } = useT();
  const target = mainEventType(sol, derived);
  const msgs = sol.notifications.value ?? [];
  const options = msgs.length >= 2 ? msgs : [...msgs, ...DISTRACTORS.map((d, i) => ({ ...d, id: `d${i}`, body: `[${d.type}] —` }))];
  const [pick, setPick] = useState<string | null>(null);
  const picked = options.find((o) => o.id === pick);
  const result = picked ? picked.type === target : null;
  return (
    <TaskShell id="T06" onDone={onDone} result={result} sample={sol.sample}>
      <p className="mb-3 font-bold">{t("tasks.T06.event", { e: t(`event.${target}`) })}</p>
      <div className="grid gap-2">
        {options.map((o) => (
          <Choice key={o.id} onClick={() => !pick && setPick(o.id)} state={pick === o.id ? (result ? "right" : "wrong") : undefined}>
            <span className="mr-2 rounded bg-ink px-2 text-xs text-milk">{o.type}</span>
            <span className="font-mono text-sm">{o.body}</span>
          </Choice>
        ))}
      </div>
    </TaskShell>
  );
}
