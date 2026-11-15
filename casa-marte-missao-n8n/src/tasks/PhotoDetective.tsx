import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { useT } from "@/i18n";
import { getObservatory } from "@/lib/observatory.functions";
import { useCampaign } from "@/game/campaign";
import { Illustration } from "./Illustration";
import { MiniFrame, type MiniProps, type PlayState } from "./MiniFrame";
import type { LibraryPicture } from "@/lib/observatory-data";
function Detective({ state, photo, kind, onImageFailure }: { state: PlayState; photo: LibraryPicture | null; kind: string; onImageFailure: () => void }) {
  const { t } = useT(); const [captionMatched, setCaptionMatched] = useState(false); const [picked, setPicked] = useState<string | null>(null);
  const options = photo ? [t("mini.PhotoDetective.decoyLab"), photo.title, t("mini.PhotoDetective.decoyMap")] : [t("mini.PhotoDetective.decoyLab"), t(`photo.${kind}`), t("mini.PhotoDetective.decoyMap")];
  const place = (index: string | null) => { if (index === null) return; if (index === "1") setCaptionMatched(true); else state.mistake(); };
  return <>{captionMatched?<><p>{t("challenge.impactQuestion")}</p><div className="mini-cards"><Button onClick={()=>state.mistake()}>{t("challenge.yes")}</Button><Button onClick={()=>state.win()}>{t("challenge.no")}</Button></div><p>{t("challenge.impactAnswer")}</p></>:<><div className="mini-photo">{photo ? <img src={photo.url} alt={photo.title} onError={onImageFailure} /> : <Illustration kind={kind} />}</div><small>{photo ? t("observatory.credit", { credit: photo.credit }) : t("tasks.T10.sampleArt")}</small>
    <div className="mini-caption-cards">{options.map((caption, i) => <Button key={i} draggable aria-pressed={picked === String(i)} onDragStart={e => { e.dataTransfer.setData("text/plain", String(i)); setPicked(String(i)); }} onClick={() => setPicked(String(i))}>{caption}</Button>)}</div><Button className="mini-dropzone" onDragOver={e => e.preventDefault()} onDrop={e => { e.preventDefault(); place(e.dataTransfer.getData("text/plain")); }} onClick={() => place(picked)}>{t("mini.placeCaption")}</Button></>}</>;
}
export default function PhotoDetective(props: MiniProps) {
  const { t } = useT(); const [failed, setFailed] = useState(false);
  const missionId = useCampaign(s => s.missionId);
  const { data, isPending } = useQuery({ queryKey: ["observatory", missionId, props.sol.sol], queryFn: () => getObservatory({ data: { sol: props.sol.sol, mission: missionId || undefined } }), staleTime: 86400000, retry: false });
  const photo = failed ? null : data?.photo ?? null;
  const kind = props.sol.photo.value?.illustration ?? "rover";
  return <MiniFrame {...props} id="PhotoDetective" ready={!isPending} source={photo ? "NASA Image Library" : undefined} practice={!photo && !isPending} learned={photo ? t("mini.PhotoDetective.learned", { value: photo.title }) : t("mini.practiceLearned")}>
    {state => isPending ? <div className="mini-photo-loading" role="status">{t("observatory.loading")}</div> : <Detective state={state} photo={photo} kind={kind} onImageFailure={() => setFailed(true)} />}
  </MiniFrame>;
}