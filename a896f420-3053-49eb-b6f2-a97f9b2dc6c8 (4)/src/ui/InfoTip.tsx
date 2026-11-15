import { Button } from "@/components/ui/button";
import { useState } from "react";
import { Info } from "lucide-react";
import { useT } from "@/i18n";
import type { Provenance } from "@/engine/normalize";

/** "i" button showing where a number comes from. */
export function InfoTip({ prov }: { prov: Provenance }) {
  const { t } = useT();
  const [open, setOpen] = useState(false);
  return (
    <span className="relative inline-block">
      <Button aria-label={t("info.show")} aria-expanded={open} onClick={() => setOpen((o) => !o)} className="grid h-8 w-8 place-items-center rounded-full bg-turquoise text-ink"><Info className="h-4 w-4" /></Button>
      {open && (
        <span className="absolute right-0 top-9 z-10 w-64 rounded-2xl bg-ink p-3 text-left text-xs font-bold text-milk shadow-lg">
          <span className="block">{t("info.api")}: {prov.api}</span>
          <span className="block">{t("info.endpoint")}: {prov.endpoint}</span>
          <span className="block">{t("info.field")}: {prov.field}</span>
          <span className="block">{t("info.captured")}: {prov.capturedAt}</span>
          <span className="block">{t("info.snapshot")}: {t("info.yes")}</span>
          {prov.sample && <span className="mt-1 block text-sun">{t("report.sample")}</span>}
        </span>
      )}
    </span>
  );
}
