import { useT } from "@/i18n";
import { useGame, type Settings } from "@/game/store";
import { sfx } from "@/audio/sfx";
import { Modal } from "./Modal";
import { LanguageToggle } from "./LanguageToggle";
import { BALANCE } from "@/config/balance";
import { Button } from "@/components/ui/button";

export function SettingsDialog({ onClose, showHome }: { onClose: () => void; showHome?: boolean }) {
  const { t } = useT();
  const settings = useGame((s) => s.settings);
  const setSetting = useGame((s) => s.setSetting);
  const setScreen = useGame((s) => s.setScreen);
  const row = (k: Exclude<keyof Settings, "cameraSensitivity" | "quality" | "roofs">, label: string) => (
    <div className="flex items-center justify-between gap-3">
      <span>{label}</span>
      <Button variant="ghost" role="switch" aria-label={label} aria-checked={settings[k]} className={`btn-game ${settings[k] ? "alt" : "ghost"} min-w-24 text-base`} onClick={() => { setSetting(k, !settings[k]); sfx.click(); }}>
        {settings[k] ? t("settings.on") : t("settings.off")}
      </Button>
    </div>
  );
  return (
    <Modal title={t("settings.title")} onClose={onClose} closeLabel={t("settings.close")}>
      {row("sound", t("settings.sound"))}
      {row("bubbleToasts", t("refine.bubbleToasts"))}
      {row("largeText", t("settings.largeText"))}
      {row("reducedMotion", t("settings.reducedMotion"))}
      {row("invertCamera", t("settings.invertCamera"))}
      <label className="flex flex-col gap-2">
        <span>{t("settings.cameraSensitivity")}</span>
        <input type="range" aria-label={t("settings.cameraSensitivity")} min={BALANCE.camera.minSensitivity} max={BALANCE.camera.maxSensitivity} step={BALANCE.camera.sensitivityStep} value={settings.cameraSensitivity} onChange={(e) => setSetting("cameraSensitivity", Number(e.target.value))} className="h-12 w-full accent-tech" />
      </label>
      <div className="flex items-center justify-between gap-2">
        <span>{t("settings.quality")}</span>
        <div role="radiogroup" aria-label={t("settings.quality")} className="flex gap-2">
          {(["low", "high"] as const).map((q) => <Button key={q} variant="ghost" role="radio" aria-checked={settings.quality === q} className={`btn-game ${settings.quality === q ? "alt" : "ghost"} text-base`} onClick={() => setSetting("quality", q)}>{t(`settings.${q}`)}</Button>)}
        </div>
      </div>
      <div className="flex items-center justify-between gap-2"><span>{t("settings.roofs")}</span><div role="radiogroup" aria-label={t("settings.roofs")} className="flex gap-2">{(["auto","off"] as const).map(r=><Button key={r} variant="ghost" role="radio" aria-checked={settings.roofs===r} className={`btn-game ${settings.roofs===r?"alt":"ghost"}`} onClick={()=>setSetting("roofs",r)}>{t(`settings.roof${r}`)}</Button>)}</div></div>
      <div className="space-y-2 pt-2"><span>{t("settings.language")}</span><LanguageToggle /></div>
      {showHome && <Button className="btn-game ghost mt-2 text-base" onClick={() => { onClose(); setScreen("title"); }}>{t("settings.home")}</Button>}
    </Modal>
  );
}
