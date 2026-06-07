import { cn } from "@/lib/cn";
import type { ButtonHTMLAttributes, ReactNode } from "react";

type FilterChipProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  selected?: boolean;
  children: ReactNode;
};

export function FilterChip({
  selected = false,
  className,
  children,
  type = "button",
  ...props
}: FilterChipProps) {
  return (
    <button
      type={type}
      aria-pressed={selected}
      className={cn(
        "rounded-full border-2 px-4 py-1.5 text-label-medium transition-colors",
        selected
          ? "border-primary bg-primary-container text-on-primary-container"
          : "border-outline bg-transparent text-on-surface hover:bg-surface-variant",
        className,
      )}
      {...props}
    >
      {children}
    </button>
  );
}
