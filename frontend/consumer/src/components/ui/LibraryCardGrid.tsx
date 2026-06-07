"use client";

import { cn } from "@/lib/cn";
import {
  Children,
  useLayoutEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";

const MIN_COLUMN_WIDTH_PX = 176;
const COLUMN_GAP_PX = 12;

type LibraryCardGridProps = {
  children: ReactNode;
  className?: string;
};

function columnCountForWidth(width: number): number {
  return Math.max(
    1,
    Math.floor((width + COLUMN_GAP_PX) / (MIN_COLUMN_WIDTH_PX + COLUMN_GAP_PX)),
  );
}

/**
 * Row-first masonry: items 0..n-1 read left-to-right, top-to-bottom, while each
 * column stacks independently so cards can differ in height without row gaps.
 */
export function LibraryCardGrid({ children, className }: LibraryCardGridProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [columnCount, setColumnCount] = useState(2);
  const items = Children.toArray(children);

  useLayoutEffect(() => {
    const element = containerRef.current;
    if (!element) return;

    const update = () => {
      setColumnCount(columnCountForWidth(element.clientWidth));
    };

    update();
    const observer = new ResizeObserver(update);
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  const columns = useMemo(() => {
    const buckets: ReactNode[][] = Array.from({ length: columnCount }, () => []);
    items.forEach((item, index) => {
      buckets[index % columnCount]!.push(item);
    });
    return buckets;
  }, [items, columnCount]);

  return (
    <div ref={containerRef} className={cn("flex items-start gap-3", className)}>
      {columns.map((column, index) => (
        <div key={index} className="flex min-w-0 flex-1 flex-col gap-3">
          {column}
        </div>
      ))}
    </div>
  );
}

type LibraryCardGridItemProps = {
  children: ReactNode;
  className?: string;
};

export function LibraryCardGridItem({ children, className }: LibraryCardGridItemProps) {
  return <div className={cn("min-w-0", className)}>{children}</div>;
}
