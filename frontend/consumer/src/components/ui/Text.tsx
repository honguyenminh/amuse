import { cn } from "@/lib/cn";
import type { ElementType, ReactNode } from "react";

const variantClass = {
  "display-large": "text-display-large",
  "headline-large": "text-headline-large",
  "headline-medium": "text-headline-medium",
  "title-large": "text-title-large",
  "title-medium": "text-title-medium",
  "body-large": "text-body-large",
  "body-medium": "text-body-medium",
  "label-large": "text-label-large",
  "label-medium": "text-label-medium",
} as const;

export type TextVariant = keyof typeof variantClass;

type TextProps<T extends ElementType> = {
  as?: T;
  variant?: TextVariant;
  className?: string;
  children: ReactNode;
};

export function Text<T extends ElementType = "p">({
  as,
  variant = "body-large",
  className,
  children,
}: TextProps<T>) {
  const Component = as ?? "p";
  return (
    <Component className={cn(variantClass[variant], "text-on-surface", className)}>
      {children}
    </Component>
  );
}
