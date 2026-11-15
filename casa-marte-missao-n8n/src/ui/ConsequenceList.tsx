import { AlertTriangle, CheckCircle2, Info, Sparkles } from "lucide-react";
import { useT } from "@/i18n";
import type { ConsequenceEvent } from "@/engine/consequences";

export function consequenceKind(event: ConsequenceEvent) {
  if (event.resource === "shield") return "solar" as const;
  if (event.resource === "lifeSupport") return "lifeSupport" as const;
  if (event.resource === "solar") return "dust" as const;
  if (event.resource === "greenhouse") return "greenhouse" as const;
  return event.severity === "positive" ? "perfect" as const : "info" as const;
}

export function ConsequenceList({ events, compact = false }: { events: readonly ConsequenceEvent[]; compact?: boolean }) {
  const { t, n } = useT();
  if (!events.length) return null;
  return <div className={`consequence-list ${compact ? "compact" : ""}`}>
    {events.map(event => {
      const c = event.cause, e = event.effects;
      const values = {
        allocated: c.allocated ?? 0, demand: c.demand ?? 0, missing: c.missing ?? 0,
        flare: c.flareClass ?? t("report.noFlare"), speed: n(c.cmeSpeed ?? 0), temp: n(c.airTempMean ?? 0),
        wind: c.windSpeed === undefined ? t("consequences.trainingWind") : t("report.windValue", { v: n(c.windSpeed) }),
        dust: c.dust ?? 0, loss: e.nextEnergyLossPct ?? c.energyLossPct ?? 0,
        penalty: c.alarmPenalty ?? 0, dose: e.dose ?? 0, integrity: e.integrity ?? 0, oxygen: e.oxygen ?? 0,
      };
      const causeKey = event.id === "life-deficit" && c.airTempMean === undefined ? `consequences.events.${event.id}.causeNoTemp` : `consequences.events.${event.id}.cause`;
      const Icon = event.severity === "positive" ? event.id === "perfect-sol" ? Sparkles : CheckCircle2 : event.severity === "info" ? Info : AlertTriangle;
      return <article key={event.id} className={`consequence-card severity-${event.severity}`} data-event={event.id}>
        <Icon aria-hidden="true" />
        <div>
          <div className="consequence-heading"><h3>{t(`consequences.events.${event.id}.title`)}</h3>{event.simulated && <span>{t("consequences.simulated")}</span>}</div>
          <p>{t(causeKey, values)}</p>
          {!compact && <p className="consequence-effect">{t(`consequences.events.${event.id}.effect`, values)}</p>}
        </div>
      </article>;
    })}
  </div>;
}
