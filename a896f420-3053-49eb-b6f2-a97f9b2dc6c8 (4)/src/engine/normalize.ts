import type { Apod } from "@/lib/observatory-data";
import { flareFlux, parseFlareClass, type FlareClass } from "./flare";
import { greenhouseDemand, shieldDemand, thermalDemand, type Allocation } from "./demand";
import { BALANCE } from "@/config/balance";

// ---------- Raw layer (shape of the local snapshot files) ----------
interface Sensor { av: number | null; mn?: number | null; mx?: number | null; ct?: number }
export interface RawSnapshot {
  _notice?: string; sample: boolean; snapshot: boolean; sol: number; earth_date: string; captured_at: string;
  donki: {
    FLR: { flrID: string; classType: string | null; peakTime?: string }[];
    CMEAnalysis: { associatedCMEID?: string; speed: number | null; halfAngle?: number | null; time21_5?: string }[];
    SEP: { sepID: string; eventTime?: string }[];
    WSAEnlilSimulations: { simulationID: string; impactList?: { location: string; arrivalTime?: string; isGlancingBlow?: boolean }[] }[];
    notifications: { messageType: string; messageID: string; messageBody: string }[];
  };
  insight: {
    AT: Sensor | null; HWS: Sensor | null; PRE: Sensor | null;
    WD: (Record<string, { compass_point: string; ct: number }> & { most_common?: { compass_point: string; ct: number } }) | null;
  };
  apod?: Apod | null;
  image_library: { nasa_id: string; illustration: string; credit: string; url?: string; title?: string; description?: string };
}

// ---------- Normalized layer (values + provenance) ----------
export interface Provenance { api: string; endpoint: string; field: string; capturedAt: string; snapshot: true; sample: boolean }
export interface Datum<T> { value: T | null; prov: Provenance }

export interface NormalizedSol {
  sol: number; earthDate: string; sample: boolean;
  flare: Datum<FlareClass>;
  cme: Datum<{ speed: number; halfAngle: number | null }>;
  sep: Datum<{ eventTime: string | null }>;
  marsImpact: Datum<{ impact: boolean; glancing: boolean; arrival: string | null }>;
  airTemp: Datum<{ av: number; mn: number | null; mx: number | null }>;
  windSpeed: Datum<{ av: number }>;
  windDir: Datum<{ mostCommon: string; counts: Record<string, number> }>;
  pressure: Datum<{ av: number }>;
  notifications: Datum<{ type: string; id: string; body: string }[]>;
  apod?: Datum<Apod>;
  photo: Datum<{ id: string; illustration: string; credit: string }>;
}

const DONKI = "DONKI (api.nasa.gov)";
const INSIGHT = "InSight: Mars Weather (api.nasa.gov)";

function sensor(s: Sensor | null) {
  return s && typeof s.av === "number" && Number.isFinite(s.av) ? s : null;
}

export function normalizeSnapshot(r: RawSnapshot): NormalizedSol {
  const p = (api: string, endpoint: string, field: string): Provenance => ({ api, endpoint, field, capturedAt: r.captured_at, snapshot: true, sample: r.sample });
  // Strongest flare of the day (by real X-ray flux, so M9.9 < X1.0).
  const flares = r.donki.FLR.map((f) => parseFlareClass(f.classType)).filter((f): f is FlareClass => !!f);
  const flare = flares.sort((a, b) => flareFlux(b) - flareFlux(a))[0] ?? null;
  // A CME without speed means "no CME".
  const cmeRaw = r.donki.CMEAnalysis.filter((c) => typeof c.speed === "number" && c.speed > 0).sort((a, b) => (b.speed ?? 0) - (a.speed ?? 0))[0];
  const sepRaw = r.donki.SEP[0];
  const sim = r.donki.WSAEnlilSimulations[0];
  const marsHit = sim?.impactList?.find((i) => i.location === "Mars");
  const at = sensor(r.insight.AT), hws = sensor(r.insight.HWS), pre = sensor(r.insight.PRE);
  let windDir: NormalizedSol["windDir"]["value"] = null;
  if (r.insight.WD?.most_common) {
    const counts: Record<string, number> = {};
    for (const [k, v] of Object.entries(r.insight.WD)) if (k !== "most_common") counts[v.compass_point] = v.ct;
    windDir = { mostCommon: r.insight.WD.most_common.compass_point, counts };
  }
  return {
    sol: r.sol, earthDate: r.earth_date, sample: r.sample,
    flare: { value: flare, prov: p(DONKI, "/DONKI/FLR", "classType") },
    cme: { value: cmeRaw ? { speed: cmeRaw.speed as number, halfAngle: cmeRaw.halfAngle ?? null } : null, prov: p(DONKI, "/DONKI/CMEAnalysis", "speed") },
    sep: { value: sepRaw ? { eventTime: sepRaw.eventTime ?? null } : null, prov: p(DONKI, "/DONKI/SEP", "sepID") },
    marsImpact: {
      value: sim ? { impact: !!marsHit, glancing: !!marsHit?.isGlancingBlow, arrival: marsHit?.arrivalTime ?? null } : null,
      prov: p(DONKI, "/DONKI/WSAEnlilSimulations", "impactList[location=Mars] (field names to confirm)"),
    },
    airTemp: { value: at ? { av: at.av as number, mn: at.mn ?? null, mx: at.mx ?? null } : null, prov: p(INSIGHT, "/insight_weather/", "AT.av") },
    windSpeed: { value: hws ? { av: hws.av as number } : null, prov: p(INSIGHT, "/insight_weather/", "HWS.av") },
    windDir: { value: windDir, prov: p(INSIGHT, "/insight_weather/", "WD.most_common.compass_point") },
    pressure: { value: pre ? { av: pre.av as number } : null, prov: p(INSIGHT, "/insight_weather/", "PRE.av") },
    notifications: {
      value: r.donki.notifications.length ? r.donki.notifications.map((n) => ({ type: n.messageType, id: n.messageID, body: n.messageBody })) : null,
      prov: p(DONKI, "/DONKI/notifications", "messageType, messageBody"),
    },
    apod: {value:r.apod??null,prov:p("NASA APOD","/planetary/apod","apod")},
    photo: { value: { id: r.image_library.nasa_id, illustration: r.image_library.illustration, credit: r.image_library.credit }, prov: p("NASA Image and Video Library", "images.nasa.gov", "nasa_id") },
  };
}

// ---------- Derived layer (game results) ----------
export interface DerivedSol {
  demand: Allocation;
  thermalMissing: boolean;
  alarm: null | "sep" | "cme" | "flare";
}

export function deriveSol(n: NormalizedSol): DerivedSol {
  const thermal = thermalDemand(n.airTemp.value?.av ?? null);
  const flareAlarm = n.flare.value && BALANCE.alarmFlareLetters.includes(n.flare.value.letter);
  return {
    demand: {
      shield: shieldDemand(n.flare.value, n.cme.value?.speed ?? null),
      life: thermal ?? BALANCE.thermal.missingFallback,
      greenhouse: greenhouseDemand(),
    },
    thermalMissing: thermal === null,
    alarm: n.sep.value ? "sep" : n.cme.value && n.marsImpact.value?.impact ? "cme" : flareAlarm ? "flare" : null,
  };
}
