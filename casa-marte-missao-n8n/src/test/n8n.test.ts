import { describe, it, expect, vi } from "vitest";
import { N8nSolCache, cleanMission, fetchN8nSol, mergeN8nSol, n8nHeaders, nasaRequestUrl, parseN8nSol, resolveN8n } from "@/lib/n8n";

const MISSION = "3f0c2a52-8d1e-4a57-9a9e-1b2c3d4e5f60";

const live = {
  sol: 2, earth_date: "2024-05-09", insight_sol: "675",
  facts: {
    flare: { class: "X", magnitude: 2.3, source: "DONKI/FLR" }, cme: { speed_kms: 1600, source: "DONKI/CMEAnalysis" },
    mars_impact: { level: "direct", source: "DONKI/WSAEnlilSimulations" }, weather: { temp_avg_c: -61.2, source: "InSight/AT" },
    wind: { speed_avg_ms: 4.9, source: "InSight/HWS" }, severity: "alert",
  },
  apod: { date: "2024-05-09", title: "Aurora", explanation: "Sky", media_type: "image", url: "https://apod.nasa.gov/a.jpg" },
  photo: { id: "PIA1", title: "Mars", description: "d", credit: "JPL", url: "https://images-assets.nasa.gov/m.jpg" },
};

const v3 = {
  ...live, sol: 3, earth_date: "2016-11-19", insight_sol: null, mission: MISSION,
  mission_window: { earth_dates: ["2016-11-17", "2016-11-18", "2016-11-19", "2016-11-20", "2016-11-21"], curiosity_sols: [1520, 1521, 1522, 1523, 1524], reason: "eventful" },
  data_dates: { sol: 3, earth_date: "2016-11-19", curiosity_sol: 1522, apod: "2003-03-14", weather: "2016-11-19", note: "Same real period." },
  effects: { rules_version: "effects-v2", radiation_alert: true, comm_disruption: "high", shield_load_factor: 1, heating_factor: 1.01, panel_factor: 0.5, dust_storm_risk: "high", mars_season_ls: 263.8, dust_season: true },
  facts: { ...live.facts, weather: { temp_avg_c: -58.5, temp_min_c: -80, temp_max_c: -37, pressure_pa: 780, opacity: "Sunny", source: "Curiosity/REMS" }, wind: null },
};

describe("resolveN8n", () => {
  it("derives both webhooks from N8N_BASE_URL and ignores trailing slashes", () => {
    expect(resolveN8n({ N8N_BASE_URL: "https://n8n.example.com/webhook/", N8N_WEBHOOK_SECRET: " s " })).toEqual({
      narratorUrl: "https://n8n.example.com/webhook/bubble-narrator", nasaUrl: "https://n8n.example.com/webhook/nasa-sol", secret: "s",
    });
  });
  it("keeps the legacy N8N_WEBHOOK_URL and derives the NASA url from it", () => {
    const c = resolveN8n({ N8N_WEBHOOK_URL: "https://n8n.example.com/webhook/bubble-narrator" });
    expect(c.narratorUrl).toBe("https://n8n.example.com/webhook/bubble-narrator");
    expect(c.nasaUrl).toBe("https://n8n.example.com/webhook/nasa-sol");
  });
  it("prefers explicit urls and rejects invalid ones", () => {
    expect(resolveN8n({ N8N_BASE_URL: "https://a/webhook", N8N_NASA_URL: "https://b/x" }).nasaUrl).toBe("https://b/x");
    expect(resolveN8n({ N8N_BASE_URL: "not a url" })).toEqual({ narratorUrl: null, nasaUrl: null, secret: null });
    expect(resolveN8n({})).toEqual({ narratorUrl: null, nasaUrl: null, secret: null });
  });
  it("sends the secret as x-api-key only when set", () => {
    expect(n8nHeaders("k", true)).toMatchObject({ "x-api-key": "k", "Content-Type": "application/json" });
    expect(n8nHeaders(null)).not.toHaveProperty("x-api-key");
    expect(nasaRequestUrl("https://n/webhook/nasa-sol", 3)).toBe("https://n/webhook/nasa-sol?sol=3");
  });
  it("adds the mission id (never a date) and drops invalid ids", () => {
    expect(nasaRequestUrl("https://n/webhook/nasa-sol", 3, MISSION)).toBe(`https://n/webhook/nasa-sol?sol=3&mission=${MISSION}`);
    expect(nasaRequestUrl("https://n/webhook/nasa-sol", 3, "../x")).toBe("https://n/webhook/nasa-sol?sol=3");
    expect(cleanMission("short")).toBeNull();
    expect(cleanMission(MISSION)).toBe(MISSION);
  });
});

describe("parseN8nSol", () => {
  it("accepts the workflow response", () => expect(parseN8nSol(live, 2)?.facts.flare).toEqual({ class: "X", magnitude: 2.3, source: "DONKI/FLR" }));
  it("keeps working with the old workflow (no mission fields)", () => {
    const r = parseN8nSol(live, 2);
    expect(r).toMatchObject({ mission: null, mission_window: null, data_dates: null, effects: null });
    expect(r?.facts.weather).toEqual({ temp_avg_c: -61.2, source: "InSight/AT" });
  });
  it("parses the mission-aware workflow (v3): window, dates, effects, Curiosity weather, no wind", () => {
    const r = parseN8nSol(v3, 3);
    expect(r?.mission).toBe(MISSION);
    expect(r?.mission_window).toEqual({ earth_dates: ["2016-11-17", "2016-11-18", "2016-11-19", "2016-11-20", "2016-11-21"], curiosity_sols: [1520, 1521, 1522, 1523, 1524], reason: "eventful" });
    expect(r?.data_dates).toMatchObject({ curiosity_sol: 1522, apod: "2003-03-14" });
    expect(r?.effects).toEqual({ radiation_alert: true, comm_disruption: "high", shield_load_factor: 1, heating_factor: 1.01, panel_factor: 0.5, dust_storm_risk: "high", mars_season_ls: 263.8, dust_season: true });
    expect(r?.facts.weather).toEqual({ temp_avg_c: -58.5, source: "Curiosity/REMS", temp_min_c: -80, temp_max_c: -37, pressure_pa: 780, opacity: "Sunny" });
    expect(r?.facts.wind).toBeNull();
  });
  it("clamps bad effects instead of failing", () => {
    const r = parseN8nSol({ ...v3, effects: { panel_factor: 99, dust_storm_risk: "boom", comm_disruption: "x" } }, 3);
    expect(r?.effects).toMatchObject({ panel_factor: 1, dust_storm_risk: "unknown", comm_disruption: "none", radiation_alert: false });
  });
  it("rejects another Sol, bad dates and missing facts", () => {
    expect(parseN8nSol(live, 3)).toBeNull();
    expect(parseN8nSol({ ...live, earth_date: "SAMPLE" }, 2)).toBeNull();
    expect(parseN8nSol({ sol: 2, earth_date: "2024-05-09" }, 2)).toBeNull();
    expect(parseN8nSol("x", 2)).toBeNull();
  });
  it("turns invalid facts into null and drops non-https media", () => {
    const r = parseN8nSol({ ...live, facts: { ...live.facts, flare: { class: "Z", magnitude: 1 }, cme: { speed_kms: 0 }, weather: { temp_avg_c: "x" } },
      apod: { ...live.apod, url: "http://insecure/a.jpg" }, photo: null }, 2);
    expect(r?.facts).toMatchObject({ flare: null, cme: null, weather: null });
    expect(r?.apod).toBeNull();
    expect(r?.photo).toBeNull();
  });
});

describe("fetchN8nSol / N8nSolCache", () => {
  const cfg = { narratorUrl: null, nasaUrl: "https://n/webhook/nasa-sol", secret: "k" };
  it("calls GET with sol and x-api-key", async () => {
    const fetcher = vi.fn().mockResolvedValue(Response.json(live));
    expect((await fetchN8nSol(cfg, 2, { fetcher }))?.earth_date).toBe("2024-05-09");
    expect(fetcher.mock.calls[0]?.[0]).toBe("https://n/webhook/nasa-sol?sol=2");
    expect((fetcher.mock.calls[0]?.[1] as RequestInit).headers).toMatchObject({ "x-api-key": "k" });
  });
  it("sends the mission id and no date", async () => {
    const fetcher = vi.fn().mockResolvedValue(Response.json(v3));
    expect((await fetchN8nSol(cfg, 3, { fetcher, mission: MISSION }))?.mission).toBe(MISSION);
    const url = String(fetcher.mock.calls[0]?.[0]);
    expect(url).toBe(`https://n/webhook/nasa-sol?sol=3&mission=${MISSION}`);
    expect(url).not.toMatch(/\d{4}-\d{2}-\d{2}/);
  });
  it("returns null on HTTP errors, network errors and when unconfigured", async () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    expect(await fetchN8nSol(cfg, 2, { fetcher: vi.fn().mockResolvedValue(new Response("", { status: 500 })) })).toBeNull();
    expect(await fetchN8nSol(cfg, 2, { fetcher: vi.fn().mockRejectedValue(new Error("offline")) })).toBeNull();
    expect(await fetchN8nSol({ ...cfg, nasaUrl: null }, 2, { fetcher: vi.fn() })).toBeNull();
  });
  it("caches successes, dedupes concurrent calls and cools down after a failure", async () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    const ok = new N8nSolCache(), f = vi.fn().mockImplementation(async () => Response.json(live));
    await Promise.all([ok.get(cfg, 2, { fetcher: f }), ok.get(cfg, 2, { fetcher: f })]);
    await ok.get(cfg, 2, { fetcher: f });
    expect(f).toHaveBeenCalledTimes(1);
    const bad = new N8nSolCache(), g = vi.fn().mockRejectedValue(new Error("down"));
    await bad.get(cfg, 2, { fetcher: g, now: 1000 }); await bad.get(cfg, 2, { fetcher: g, now: 1000 + 30_000 });
    expect(g).toHaveBeenCalledTimes(1);
    await bad.get(cfg, 2, { fetcher: g, now: 1000 + 61_000 });
    expect(g).toHaveBeenCalledTimes(2);
  });
  it("keeps one cache entry per mission AND sol, so two games never share data", async () => {
    const other = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
    const cache = new N8nSolCache();
    const f = vi.fn().mockImplementation(async (url: string) => Response.json({ ...v3, mission: new URL(url).searchParams.get("mission"), sol: Number(new URL(url).searchParams.get("sol")) }));
    const a1 = await cache.get(cfg, 3, { fetcher: f, mission: MISSION });
    const b1 = await cache.get(cfg, 3, { fetcher: f, mission: other });
    await cache.get(cfg, 3, { fetcher: f, mission: MISSION });   // cached
    await cache.get(cfg, 4, { fetcher: f, mission: MISSION });   // another sol -> new call
    expect(f).toHaveBeenCalledTimes(3);
    expect(a1?.mission).toBe(MISSION);
    expect(b1?.mission).toBe(other);
  });
});

describe("mergeN8nSol", () => {
  const parsed = parseN8nSol(live, 2)!;
  const snap = { date: "SAMPLE-DATE-2", apod: null, photo: { id: "S", title: "Snap", description: "", credit: "NASA", url: "https://x/s.jpg" }, source: "unavailable" as const, photoSource: "snapshot" as const, sample: true };
  it("is fully live when n8n returns both pictures", () => expect(mergeN8nSol(parsed, null)).toMatchObject({ date: "2024-05-09", source: "live", photoSource: "live", sample: false }));
  it("fills only what is missing and keeps honest provenance", () => {
    const r = mergeN8nSol({ ...parsed, photo: null }, snap);
    expect(r).toMatchObject({ source: "live", photoSource: "snapshot", sample: true });
    expect(r.photo?.title).toBe("Snap");
  });
});
