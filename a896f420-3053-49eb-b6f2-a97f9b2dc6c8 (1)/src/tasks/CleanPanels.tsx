import { useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { BALANCE } from "@/config/balance";
import { MiniFrame, NumberValue, type MiniProps, type PlayState } from "./MiniFrame";
import { taskData } from "./rules";
function Panels({ state, count }: { state: PlayState; count: number }) {
  const { t } = useT(); const [clean, setClean] = useState<number[]>([]); const cleared = useRef(new Set<number>());
  const wipe = (i: number) => { if (cleared.current.has(i)) return; cleared.current.add(i); setClean([...cleared.current]); if (cleared.current.size === count) state.win(); };
  return <><div className="mini-panel-grid" onPointerDown={e => { e.preventDefault(); e.currentTarget.setPointerCapture(e.pointerId); const el = document.elementFromPoint(e.clientX, e.clientY)?.closest<HTMLElement>("[data-dust]"); if (el?.dataset['dust']) wipe(Number(el.dataset['dust'])); }} onPointerMove={e => { if (!e.buttons && e.pointerType !== "touch") return; if (!e.currentTarget.hasPointerCapture(e.pointerId)) return; const el = document.elementFromPoint(e.clientX, e.clientY)?.closest<HTMLElement>("[data-dust]"); if (el?.dataset['dust']) wipe(Number(el.dataset['dust'])); }}>{Array.from({ length: count }, (_, i) => <Button key={i} data-dust={i} aria-label={t("mini.patch", { n: i + 1 })} className={clean.includes(i) ? "mini-panel-clean" : "mini-panel-dust"} disabled={clean.includes(i)} onClick={() => wipe(i)}><span aria-hidden="true">{clean.includes(i) ? "✦" : "∴"}</span></Button>)}</div><p><NumberValue value={clean.length} origin="progress" /> / <NumberValue value={count} origin={BALANCE.marsWeatherModifier ? "weather" : "count"} /></p></>;
}
export default function CleanPanels(props: MiniProps) { const { t } = useT(); const data = taskData(props.sol); return <MiniFrame {...props} id="CleanPanels" learned={t("mini.CleanPanels.learned")}>{state => <>{BALANCE.marsWeatherModifier && <p>{t("mini.weatherInspired")}</p>}<Panels state={state} count={data.dust.value} /></>}</MiniFrame>; }