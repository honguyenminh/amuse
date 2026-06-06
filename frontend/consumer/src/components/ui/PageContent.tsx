import { cn } from "@/lib/cn";
import {
  pageContentBaseClass,
  pageContentWidthClass,
  type PageContentWidth,
} from "@/lib/ui/pageLayout";
import type { ReactNode } from "react";

type PageContentProps = {
  children: ReactNode;
  width?: PageContentWidth;
  gap?: "4" | "6" | "8";
  className?: string;
};

const gapClass = {
  "4": "gap-4",
  "6": "gap-6",
  "8": "gap-8",
} as const;

/**
 * Width-constrained page body inside AppShell. Outer padding comes from AppShell `<main>`.
 */
export function PageContent({
  children,
  width = "catalog",
  gap = "6",
  className,
}: PageContentProps) {
  return (
    <div
      className={cn(
        pageContentBaseClass,
        pageContentWidthClass[width],
        gapClass[gap],
        className,
      )}
    >
      {children}
    </div>
  );
}
