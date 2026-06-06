"use client";

import { FormattedCatalogText, type FormattedCatalogTextProps } from "@amuse/catalog-text";
import { Button } from "@/components/ui/Button";
import { cn } from "@/lib/cn";
import { useEffect, useLayoutEffect, useRef, useState } from "react";

const DEFAULT_COLLAPSED_LINES = 4;
const EXPAND_TRANSITION_MS = 300;

function getLineHeightPx(element: HTMLElement): number {
  const styles = getComputedStyle(element);
  const lineHeight = Number.parseFloat(styles.lineHeight);
  if (Number.isFinite(lineHeight)) return lineHeight;
  const fontSize = Number.parseFloat(styles.fontSize);
  return Number.isFinite(fontSize) ? fontSize * 1.5 : 24;
}

type CollapsibleFormattedTextProps = FormattedCatalogTextProps & {
  collapsedLines?: number;
};

export function CollapsibleFormattedText({
  text,
  className,
  collapsedLines = DEFAULT_COLLAPSED_LINES,
  ...formattedProps
}: CollapsibleFormattedTextProps) {
  const contentRef = useRef<HTMLDivElement>(null);
  const [expanded, setExpanded] = useState(false);
  const [canExpand, setCanExpand] = useState(false);
  const [collapsedHeight, setCollapsedHeight] = useState<number | null>(null);
  const [fullHeight, setFullHeight] = useState<number | null>(null);
  const [reduceMotion, setReduceMotion] = useState(false);

  useEffect(() => {
    const media = window.matchMedia("(prefers-reduced-motion: reduce)");
    const sync = () => setReduceMotion(media.matches);
    sync();
    media.addEventListener("change", sync);
    return () => media.removeEventListener("change", sync);
  }, []);

  useEffect(() => {
    setExpanded(false);
  }, [text]);

  useLayoutEffect(() => {
    const element = contentRef.current;
    if (!element) return;

    const measure = () => {
      const lineHeight = getLineHeightPx(element);
      const collapsed = lineHeight * collapsedLines;
      const natural = element.scrollHeight;
      setCollapsedHeight(collapsed);
      setFullHeight(natural);
      setCanExpand(natural > collapsed + 1);
    };

    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(element);
    return () => observer.disconnect();
  }, [text, collapsedLines]);

  const isCollapsed = canExpand && !expanded;
  const heightsReady = collapsedHeight !== null && fullHeight !== null;
  const clipHeight =
    canExpand && heightsReady ? (isCollapsed ? collapsedHeight : fullHeight) : undefined;
  const motionTransition = reduceMotion
    ? undefined
    : `height ${EXPAND_TRANSITION_MS}ms ease-in-out`;
  const fadeTransition = reduceMotion
    ? undefined
    : `opacity ${EXPAND_TRANSITION_MS}ms ease-in-out`;

  return (
    <div className={cn("mt-2 flex flex-col gap-2", className)}>
      <div className="relative">
        <div
          data-no-theme-transition
          className="overflow-hidden"
          style={
            clipHeight !== undefined
              ? {
                  height: clipHeight,
                  transition: motionTransition,
                }
              : undefined
          }
        >
          <div ref={contentRef}>
            <FormattedCatalogText text={text} {...formattedProps} />
          </div>
        </div>
        {canExpand ? (
          <div
            aria-hidden
            data-no-theme-transition
            className={cn(
              "pointer-events-none absolute inset-x-0 bottom-0 h-12 bg-gradient-to-t from-surface via-surface/90 to-transparent",
              isCollapsed ? "opacity-100" : "opacity-0",
            )}
            style={{
              transition: fadeTransition,
            }}
          />
        ) : null}
      </div>
      {canExpand ? (
        <Button
          type="button"
          variant="text"
          className="self-start px-0 py-0 text-body-medium underline"
          aria-expanded={expanded}
          onClick={() => setExpanded((value) => !value)}
        >
          {expanded ? "Show less" : "View more"}
        </Button>
      ) : null}
    </div>
  );
}
