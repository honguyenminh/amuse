type IconProps = {
  className?: string;
};

const baseProps = {
  width: 20,
  height: 20,
  viewBox: "0 0 24 24",
  fill: "none",
  stroke: "currentColor",
  strokeWidth: 2,
  strokeLinecap: "round" as const,
  strokeLinejoin: "round" as const,
};

export function PlayIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <polygon points="6 4 20 12 6 20 6 4" fill="currentColor" stroke="none" />
    </svg>
  );
}

export function PauseIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <rect x="6" y="4" width="4" height="16" fill="currentColor" stroke="none" />
      <rect x="14" y="4" width="4" height="16" fill="currentColor" stroke="none" />
    </svg>
  );
}

export function PrevIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <polygon points="19 4 19 20 7 12 19 4" fill="currentColor" stroke="none" />
      <line x1="5" y1="4" x2="5" y2="20" />
    </svg>
  );
}

export function NextIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <polygon points="5 4 5 20 17 12 5 4" fill="currentColor" stroke="none" />
      <line x1="19" y1="4" x2="19" y2="20" />
    </svg>
  );
}

export function ChevronDownIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <polyline points="6 9 12 15 18 9" />
    </svg>
  );
}

export function ShuffleIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <polyline points="16 3 21 3 21 8" />
      <line x1="4" y1="20" x2="21" y2="3" />
      <polyline points="21 16 21 21 16 21" />
      <line x1="15" y1="15" x2="21" y2="21" />
      <line x1="4" y1="4" x2="9" y2="9" />
    </svg>
  );
}

/** Equalizer bars — indicates active playback (Lucide `audio-lines`). */
export function NowPlayingIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <path d="M2 10v3" />
      <path d="M6 6v11" />
      <path d="M10 3v18" />
      <path d="M14 8v7" />
      <path d="M18 5v13" />
      <path d="M22 10v3" />
    </svg>
  );
}

/** Repeat entire queue when a track ends. */
export function RepeatQueueIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <polyline points="17 1 21 5 17 9" />
      <path d="M3 11V9a4 4 0 0 1 4-4h14" />
      <polyline points="7 23 3 19 7 15" />
      <path d="M21 13v2a4 4 0 0 1-4 4H3" />
    </svg>
  );
}

/** Repeat the current track when it ends. */
export function RepeatOneIcon({ className }: IconProps) {
  return (
    <svg {...baseProps} className={className} aria-hidden>
      <polyline points="17 1 21 5 17 9" />
      <path d="M3 11V9a4 4 0 0 1 4-4h14" />
      <polyline points="7 23 3 19 7 15" />
      <path d="M21 13v2a4 4 0 0 1-4 4H3" />
      <path
        d="M12 8.5v8"
        fill="none"
        stroke="currentColor"
        strokeWidth="2.2"
        strokeLinecap="round"
      />
    </svg>
  );
}

/** @deprecated Use RepeatQueueIcon */
export const RepeatIcon = RepeatQueueIcon;
