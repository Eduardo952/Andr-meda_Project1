import { useEffect, useState } from "react";
import { useT } from "@/i18n";
import { Modal } from "./Modal";
import { SOL_SECONDS, marsClock, solFraction } from "@/game/sim/solClock";
import { runtime } from "@/game/runtime";

export function DataDialog({ onClose }: { onClose: () => void }) {
  const { t, n } = useT();
  const [, tick] = useState(0);
  useEffect(() => { const i = setInterval(() => tick((x) => x + 1), 500); return () => clearInterval(i); }, []);
  const c = marsClock(solFraction(runtime.solTime));
  return (
    <Modal title={t("data.title")} onClose={onClose} closeLabel={t("data.close")}>
      <div><div className="text-sm text-muted-foreground">{t("data.solLength")}</div>
        <div>{t("data.solLengthValue", { s: n(35.244, { minimumFractionDigits: 3 }) })}</div>
        <div className="text-sm text-muted-foreground">{t("data.solSeconds", { n: n(SOL_SECONDS, { minimumFractionDigits: 3 }) })}</div></div>
      <div><div className="text-sm text-muted-foreground">{t("data.localTime")}</div>
        <div className="font-display text-2xl">{String(c.h).padStart(2, "0")}:{String(c.m).padStart(2, "0")}</div></div>
      <p className="text-sm text-muted-foreground">{t("data.barsNote")}</p>
    </Modal>
  );
}
