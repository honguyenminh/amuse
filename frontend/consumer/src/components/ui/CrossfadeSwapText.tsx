"use client";

import { cn } from "@/lib/cn";
import { useLayoutEffect, useRef, useState } from "react";

type CrossfadeSwapTextProps = {
  showSecondary: boolean;
  primary: string;
  secondary: string;
  className?: string;
};

/**
 * Crossfades between two labels while smoothly animating width to match the active text.
 */
export function CrossfadeSwapText({
  showSecondary,
  primary,
  secondary,
  className,
}: CrossfadeSwapTextProps) {
  const measureRef = useRef<HTMLSpanElement>(null);
  const [width, setWidth] = useState<number>();

  useLayoutEffect(() => {
    const measure = measureRef.current;
    if (!measure) {
      return;
    }
    setWidth(measure.offsetWidth);
  }, [showSecondary, primary, secondary]);

  return (
    <span
      className={cn(
        "relative inline-block overflow-hidden transition-[width] duration-200 ease-out motion-reduce:transition-none",
        className,
      )}
      style={width === undefined ? undefined : { width }}
    >
      <span
        ref={measureRef}
        className="pointer-events-none invisible absolute left-0 top-0 whitespace-nowrap"
        aria-hidden
      >
        {showSecondary ? secondary : primary}
      </span>
      <span
        aria-hidden={showSecondary}
        className={cn(
          "block whitespace-nowrap transition-opacity duration-200 ease-out motion-reduce:transition-none",
          showSecondary ? "opacity-0" : "opacity-100",
        )}
      >
        {primary}
      </span>
      <span
        aria-hidden={!showSecondary}
        className={cn(
          "absolute left-0 top-0 block whitespace-nowrap transition-opacity duration-200 ease-out motion-reduce:transition-none",
          showSecondary ? "opacity-100" : "opacity-0",
        )}
      >
        {secondary}
      </span>
    </span>
  );
}
