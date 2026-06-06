"use client";

import { IconButton } from "@/components/ui/IconButton";
import { MoreVertIcon } from "@/components/ui/MoreVertIcon";
import { cn } from "@/lib/cn";
import { forwardRef, type ButtonHTMLAttributes } from "react";

type OverflowMenuButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  label: string;
  size?: "sm" | "md";
  /** When true, stays visible (e.g. active reorder mode). */
  active?: boolean;
  className?: string;
};

export const OverflowMenuButton = forwardRef<HTMLButtonElement, OverflowMenuButtonProps>(
  function OverflowMenuButton(
    { label, size = "sm", active = false, className, ...props },
    ref,
  ) {
    return (
      <IconButton
        ref={ref}
        label={label}
        variant="tertiary-tonal"
        size={size}
        className={cn(active && "ring-2 ring-secondary/40", className)}
        {...props}
      >
        <MoreVertIcon />
      </IconButton>
    );
  },
);
