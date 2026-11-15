import { Button } from "@/components/ui/button";
import type { ReactNode } from "react";
export function Modal({ title, onClose, children, closeLabel, wide }: { title: string; onClose: () => void; children: ReactNode; closeLabel: string; wide?: boolean }) {
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-ink/50 p-4" role="dialog" aria-modal="true" aria-label={title} onClick={onClose}>
      <div className={`panel animate-pop w-full ${wide?"max-w-3xl":"max-w-md"} max-h-[94dvh] overflow-y-auto p-6`} onClick={(e) => e.stopPropagation()}>
        <h2 className="mb-4 text-3xl font-bold text-ink">{title}</h2>
        <div className="space-y-3 text-lg font-bold">{children}</div>
        <div className="mt-6 flex justify-end"><Button className="btn-game" onClick={onClose}>{closeLabel}</Button></div>
      </div>
    </div>
  );
}
