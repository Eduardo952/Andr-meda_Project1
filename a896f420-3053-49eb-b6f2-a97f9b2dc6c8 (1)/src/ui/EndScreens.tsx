import { Button } from "@/components/ui/button";
import { useMemo, useState } from "react";
import { Award } from "lucide-react";
import { useT } from "@/i18n";
import { useCampaign } from "@/game/campaign";
import { useGame } from "@/game/store";
import { seeded } from "@/game/scene/materials";

export function GameOver() {
  const { t } = useT();
  const start = useCampaign((s) => s.start);
  const setScreen = useGame((s) => s.setScreen);
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-ink/60 p-4" role="dialog" aria-modal="true">
      <div className="panel animate-pop max-w-md p-6 text-center">
        <h2 className="text-4xl font-bold">{t("gameover.title")}</h2>
        <p className="my-4 text-lg font-bold">{t("gameover.body")}</p>
        <div className="flex justify-center gap-2">
          <Button className="btn-game text-xl" onClick={start}>{t("gameover.retry")}</Button>
          <Button className="btn-game ghost" onClick={() => setScreen("title")}>{t("gameover.home")}</Button>
        </div>
      </div>
    </div>
  );
}

function Confetti() {
  const pieces = useMemo(() => { const r = seeded(3); return Array.from({ length: 60 }, (_, i) => ({ left: r() * 100, delay: r() * 2, dur: 2.5 + r() * 2, tone: ["bg-sun", "bg-coral", "bg-turquoise", "bg-tech", "bg-milk"][i % 5] })); }, []);
  return (
    <div className="pointer-events-none fixed inset-0 z-50 overflow-hidden" aria-hidden>
      {pieces.map((p, i) => <span key={i} className={`confetti ${p.tone}`} style={{ left: `${p.left}%`, animationDelay: `${p.delay}s`, animationDuration: `${p.dur}s` }} />)}
    </div>
  );
}

export function Certificate() {
  const { t } = useT();
  const nickname = useGame((s) => s.nickname);
  const setNickname = useGame((s) => s.setNickname);
  const setScreen = useGame((s) => s.setScreen);
  const { integrity, oxygen, start } = useCampaign();
  const [name, setName] = useState(nickname);
  const [shown, setShown] = useState(!!nickname);
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-ink/60 p-4" role="dialog" aria-modal="true">
      {shown && <Confetti />}
      <div className="panel animate-pop relative z-50 w-full max-w-lg border-sun p-6 text-center">
        <Award className="mx-auto h-16 w-16 text-rust" />
        <h2 className="title-3d text-3xl sm:text-4xl">{t("cert.title")}</h2>
        {!shown ? (
          <form className="mt-4 space-y-3" onSubmit={(e) => { e.preventDefault(); setNickname(name.trim() || "Astronaut"); setShown(true); }}>
            <label className="block font-extrabold" htmlFor="nick">{t("cert.nickname")}</label>
            <input id="nick" value={name} maxLength={20} onChange={(e) => setName(e.target.value)} placeholder={t("cert.placeholder")} className="h-12 w-full rounded-2xl border-2 border-ink/20 bg-milk px-4 text-center font-display text-xl" />
            <p className="text-xs text-muted-foreground">{t("cert.local")}</p>
            <Button className="btn-game text-xl" type="submit">{t("cert.show")}</Button>
          </form>
        ) : (
          <>
            <p className="my-4 font-display text-2xl">{t("cert.body", { name: nickname || name })}</p>
            <p className="font-bold">{t("cert.stats", { i: integrity, o: oxygen })}</p>
            <div className="mt-4 flex justify-center gap-2">
              <Button className="btn-game text-xl" onClick={start}>{t("cert.again")}</Button>
              <Button className="btn-game ghost" onClick={() => setScreen("title")}>{t("gameover.home")}</Button>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
