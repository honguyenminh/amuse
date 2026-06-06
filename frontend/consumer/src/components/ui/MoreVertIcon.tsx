type MoreVertIconProps = {
  className?: string;
};

export function MoreVertIcon({ className }: MoreVertIconProps) {
  return (
    <svg
      width={20}
      height={20}
      viewBox="0 0 24 24"
      className={className}
      aria-hidden
    >
      <circle cx="12" cy="5" r="2.5" fill="currentColor" />
      <circle cx="12" cy="12" r="2.5" fill="currentColor" />
      <circle cx="12" cy="19" r="2.5" fill="currentColor" />
    </svg>
  );
}
