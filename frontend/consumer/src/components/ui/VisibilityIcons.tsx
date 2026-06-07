type IconProps = {
  className?: string;
};

const baseProps = {
  width: 16,
  height: 16,
  viewBox: "0 0 24 24",
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 2,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

/** Public playlist visibility (globe). */
export function GlobeIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <circle cx="12" cy="12" r="10" />
      <path d="M2 12h20" />
      <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" />
    </svg>
  );
}

/** Private playlist visibility (padlock). */
export function PadlockIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <rect x="5" y="11" width="14" height="10" rx="2" />
      <path d="M8 11V8a4 4 0 0 1 8 0v3" />
    </svg>
  );
}

export function PlaylistVisibilityIcon({
  visibility,
  className,
}: {
  visibility: string;
  className?: string;
}) {
  return visibility === "public" ? (
    <GlobeIcon className={className} />
  ) : (
    <PadlockIcon className={className} />
  );
}
