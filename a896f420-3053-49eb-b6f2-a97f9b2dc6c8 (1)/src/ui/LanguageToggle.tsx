import { Button } from "@/components/ui/button";
import { LANGS } from "@/i18n";
import { useT } from "@/i18n";
import { useGame } from "@/game/store";
import { sfx } from "@/audio/sfx";
export function LanguageToggle() {
  const { t, lang } = useT();
  const setLang = useGame((s) => s.setLang);
  return (
    <div className="flex gap-2" role="radiogroup">
      {LANGS.map((l) => (
        <Button key={l} role="radio" aria-checked={lang === l} className={`btn-game ${lang === l ? "alt" : "ghost"} px-4 text-base`} onClick={() => { sfx.click(); setLang(l); }}>
          {t(`lang.${l}`)}
        </Button>
      ))}
    </div>
  );
}
