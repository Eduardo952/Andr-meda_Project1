import { useState } from "react";
import { useT } from "@/i18n";
import { TaskShell,Choice,type TaskProps } from "./TaskShell";
export function T11({onDone}:TaskProps){const {t}=useT(),[result,setResult]=useState<boolean|null>(null);return <TaskShell id="T11" onDone={onDone} result={result}><Choice onClick={()=>setResult(true)}>{t("tasks.T11.right")}</Choice><Choice onClick={()=>setResult(false)}>{t("tasks.T11.wrong")}</Choice></TaskShell>;}