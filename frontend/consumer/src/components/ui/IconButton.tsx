import { cn } from "@/lib/cn";
import type { ButtonHTMLAttributes, ReactNode } from "react";

type IconButtonVariant = "filled" | "tonal" | "outlined" | "ghost";
type IconButtonSize = "sm" | "md" | "lg";

type IconButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: IconButtonVariant;
  size?: IconButtonSize;
  label: string;
  children: ReactNode;
};

const variantClass: Record<IconButtonVariant, string> = {
  filled: "bg-primary text-on-primary hover:opacity-90",
  tonal: "bg-primary-container text-on-primary-container hover:opacity-90",
  outlined: "bg-transparent text-on-surface border-2 border-outline hover:bg-surface-variant",
  ghost: "bg-transparent text-on-surface hover:bg-surface-variant",
};

const sizeClass: Record<IconButtonSize, string> = {
  sm: "h-8 w-8",
  md: "h-10 w-10",
  lg: "h-14 w-14",
};

/**
 * Square button optimised for a single icon. `label` is required for assistive tech because
 * the visible content is purely glyphic.
 */
export function IconButton({
  variant = "ghost",
  size = "md",
  label,
  className,
  type = "button",
  children,
  ...props
}: IconButtonProps) {
  return (
    <button
      type={type}
      aria-label={label}
      className={cn(
        "inline-flex shrink-0 items-center justify-center rounded-full transition-colors disabled:opacity-40 disabled:cursor-not-allowed",
        variantClass[variant],
        sizeClass[size],
        className,
      )}
      {...props}
    >
      {children}
    </button>
  );
}
