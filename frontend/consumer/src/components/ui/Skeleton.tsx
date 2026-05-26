import { cn } from "@/lib/cn";

type SkeletonProps = {
  className?: string;
  /** Provide a non-visual hint for screen readers if the skeleton replaces specific content. */
  ariaLabel?: string;
};

/**
 * Neutral loading placeholder. Animates with a subtle pulse on the surface-variant token so
 * it inherits the active theme. Used in place of "Loading..." text everywhere.
 */
export function Skeleton({ className, ariaLabel = "Loading" }: SkeletonProps) {
  return (
    <span
      role="status"
      aria-label={ariaLabel}
      className={cn(
        "block rounded-md bg-surface-variant/70 animate-pulse",
        className,
      )}
    />
  );
}
