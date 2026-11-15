/** Drawn placeholder art (SAMPLE) until real NASA Image and Video Library photos are provided. */
export function Illustration({ kind }: { kind: string }) {
  return (
    <svg viewBox="0 0 200 110" className="w-full rounded-2xl">
      <rect width="200" height="110" className={kind === "sunset" ? "fill-ink" : "fill-sun/60"} />
      {kind === "sunset" && <><circle cx="100" cy="72" r="26" className="fill-bar-oxygen/40" /><circle cx="100" cy="72" r="9" className="fill-milk" /></>}
      {kind === "olympus" && <path d="M10 85 Q100 20 190 85 Z" className="fill-rust" />}
      {kind === "dunes" && [0, 1, 2].map((i) => <path key={i} d={`M0 ${70 + i * 12} Q50 ${55 + i * 12} 100 ${70 + i * 12} T200 ${70 + i * 12} V110 H0 Z`} className={i % 2 ? "fill-coral" : "fill-rust"} />)}
      <rect y="85" width="200" height="25" className="fill-coral" />
      {kind === "dustDevil" && <path d="M120 85 Q110 60 125 45 Q105 30 130 10 L145 10 Q125 30 140 45 Q125 60 130 85 Z" className="fill-milk/70" />}
      {kind === "rover" && (
        <g transform="translate(70 55)">
          <rect width="55" height="16" rx="4" className="fill-milk" />
          <rect x="40" y="-20" width="4" height="20" className="fill-milk" /><rect x="36" y="-26" width="12" height="8" rx="2" className="fill-milk" />
          {[6, 27, 48].map((x) => <circle key={x} cx={x} cy="26" r="7" className="fill-ink" />)}
        </g>
      )}
    </svg>
  );
}
