import { useT } from "@/i18n";
import { Modal } from "./Modal";
export function HowToPlay({ onClose }: { onClose: () => void }) {
  const { t } = useT();
  return (
    <Modal title={t("howTo.title")} onClose={onClose} closeLabel={t("howTo.close")}>
      {["move", "use", "follow", "bubble"].map((k, i) => (
        <p key={k} className="flex gap-3"><span className="grid h-8 w-8 shrink-0 place-items-center rounded-full bg-sun font-display">{i + 1}</span>{t(`howTo.${k}`)}</p>
      ))}
    </Modal>
  );
}
