/** Zero-height list marker showing where a dragged track will land. */
export function TrackDropIndicator() {
  return (
    <li
      aria-hidden
      className="pointer-events-none relative z-10 -my-px h-0 list-none overflow-visible"
    >
      <div className="absolute inset-x-3 top-0 h-1 -translate-y-1/2 bg-tertiary" />
    </li>
  );
}
