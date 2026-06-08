"use client";

import { cn } from "@/lib/utils";
import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";

type PlatformSortHeaderProps<T extends string> = {
  label: string;
  column: T;
  sortBy: T;
  sortDirection: "asc" | "desc";
  onSort: (column: T) => void;
  align?: "left" | "right";
};

export function PlatformSortHeader<T extends string>({
  label,
  column,
  sortBy,
  sortDirection,
  onSort,
  align = "left",
}: PlatformSortHeaderProps<T>) {
  const active = sortBy === column;
  const Icon = !active ? ArrowUpDown : sortDirection === "asc" ? ArrowUp : ArrowDown;

  return (
    <button
      type="button"
      className={cn(
        "inline-flex items-center gap-1 font-medium hover:text-foreground",
        align === "right" && "ml-auto",
      )}
      onClick={() => onSort(column)}
    >
      {label}
      <Icon className={cn("size-3.5", active ? "text-foreground" : "text-muted-foreground")} />
    </button>
  );
}
