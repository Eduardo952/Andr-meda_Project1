import type { ReactNode } from "react";
import * as Dialog from "@radix-ui/react-dialog";
import { X, Info, Shield, HeartPulse, Sprout, Sun, Telescope, Gauge } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";

export function ChallengePanel({ title, sector, source, sample, details, descriptionId, onClose, children }: {
  title: string; sector: string; source?: string | undefined; sample?: boolean;
  details?: ReactNode; descriptionId?: string; onClose: () => void; children: ReactNode;
}) {
  const { t } = useT();
  const Icon = sector === "shield" ? Shield : sector === "lifeSupport" ? HeartPulse : sector === "greenhouse" ? Sprout : sector === "solar" ? Sun : sector === "observatory" ? Telescope : Gauge;
  return <Dialog.Root open onOpenChange={open => { if (!open) onClose(); }}><Dialog.Portal>
    <Dialog.Overlay className="mini-backdrop" />
    <Dialog.Content className={`mini-overlay region-${sector}`} aria-describedby={descriptionId}
      onCloseAutoFocus={event => { event.preventDefault(); document.querySelector<HTMLElement>("[data-station-use]")?.focus(); }}>
      <header className="mini-header">
        <div><span className="mini-sector"><Icon aria-hidden="true" />{t(`rooms.${sector}`)}</span><Dialog.Title>{title}</Dialog.Title>
          <div className="mini-badges"><span className={`mini-source ${source ? "mini-source-real" : "mini-source-rule"}`}>{t(source ? "report.realData" : "report.gameRule")}</span>{sample && <span className="mini-source mini-source-sample">{t("report.sample")}</span>}</div>
        </div>
        <div className="mini-header-actions">
          <details className="mini-details"><summary aria-label={t("info.show")}><Info aria-hidden="true" /></summary><div><strong>{source ? t("tasks.badgeReal", { src: source }) : t("tasks.badgeRule")}</strong>{details}</div></details>
          <Dialog.Close asChild><Button variant="ghost" size="icon" title={t("settings.close")} aria-label={t("settings.close")}><X /></Button></Dialog.Close>
        </div>
      </header>
      {children}
    </Dialog.Content>
  </Dialog.Portal></Dialog.Root>;
}

export function ChallengeDataCard({ icon, label, children, explanation }: { icon: ReactNode; label: string; children: ReactNode; explanation: string }) {
  const { t } = useT();
  return <div className="mini-data-card"><span className="mini-data-icon" aria-hidden="true">{icon}</span><div className="mini-data-content"><small>{label}</small><strong>{children}</strong></div>
    <details className="mini-details mini-data-info"><summary aria-label={`${t("info.show")}: ${label}`}><Info aria-hidden="true" /></summary><div>{explanation}</div></details>
  </div>;
}