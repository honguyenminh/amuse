import { cn } from "@/lib/cn";
import type { HTMLAttributes, ReactNode } from "react";

type CardProps = HTMLAttributes<HTMLDivElement> & {
  children: ReactNode;
};

export function Card({ className, children, ...props }: CardProps) {
  return (
    <div
      className={cn(
        "border-2 border-outline bg-surface p-4 text-on-surface",
        className,
      )}
      {...props}
    >
      {children}
    </div>
  );
}
