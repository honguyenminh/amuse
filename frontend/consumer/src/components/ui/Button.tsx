import { cn } from "@/lib/cn";
import type { ButtonHTMLAttributes, ReactNode } from "react";

type ButtonVariant = "filled" | "primary" | "error" | "outlined" | "text" | "tertiary-tonal";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
  children: ReactNode;
};

const variantClass: Record<ButtonVariant, string> = {
  filled:
    "bg-primary-container text-on-primary-container border-2 border-outline hover:opacity-90",
  primary:
    "bg-primary text-on-primary border-2 border-on-background hover:opacity-90",
  error:
    "bg-error text-on-error border-2 border-outline hover:opacity-90",
  outlined:
    "bg-transparent text-primary border-2 border-outline hover:bg-surface-variant",
  "tertiary-tonal":
    "bg-tertiary-container text-on-tertiary-container border-2 border-outline hover:opacity-90",
  text: "bg-transparent text-primary border-2 border-transparent hover:bg-surface-variant",
};

export function Button({
  variant = "filled",
  className,
  children,
  type = "button",
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      className={cn(
        "inline-flex items-center justify-center px-4 py-2 text-label-large transition-colors disabled:opacity-50",
        variantClass[variant],
        className,
      )}
      {...props}
    >
      {children}
    </button>
  );
}
