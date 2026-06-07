import { cn } from "@/lib/cn";

type KeyCapProps = {
  children: string;
  className?: string;
};

export function KeyCap({ children, className }: KeyCapProps) {
  return (
    <kbd
      className={cn(
        "inline-flex min-h-8 min-w-8 items-center justify-center rounded-lg",
        "border-2 border-outline/50 bg-surface/90 px-2.5",
        "font-mono text-label-medium text-on-surface shadow-[0_2px_0_0_rgba(0,0,0,0.12)]",
        className,
      )}
    >
      {children}
    </kbd>
  );
}

type KeyComboProps = {
  keys: string[];
};

export function KeyCombo({ keys }: KeyComboProps) {
  return (
    <span className="inline-flex flex-wrap items-center gap-1.5">
      {keys.map((key, index) => (
        <span key={`${key}-${index}`} className="inline-flex items-center gap-1.5">
          {index > 0 ? (
            <span className="text-label-small text-on-surface-variant" aria-hidden>
              +
            </span>
          ) : null}
          <KeyCap>{key}</KeyCap>
        </span>
      ))}
    </span>
  );
}
