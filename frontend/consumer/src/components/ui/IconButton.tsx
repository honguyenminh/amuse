import { cn } from "@/lib/cn";
import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from "react";

type IconButtonVariant = "filled" | "tonal" | "tertiary-tonal" | "outlined" | "ghost";
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
  "tertiary-tonal":
    "bg-tertiary-container text-on-tertiary-container hover:opacity-90",
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
export const IconButton = forwardRef<HTMLButtonElement, IconButtonProps>(
  function IconButton(
    {
      variant = "ghost",
      size = "md",
      label,
      className,
      type = "button",
      children,
      ...props
    },
    ref,
  ) {
    return (
      <button
        ref={ref}
        type={type}
        aria-label={label}
        className={cn(
          "inline-flex shrink-0 items-center justify-center rounded-full transition-colors disabled:cursor-not-allowed disabled:opacity-40",
          variantClass[variant],
          sizeClass[size],
          className,
        )}
        {...props}
      >
        {children}
      </button>
    );
  },
);
