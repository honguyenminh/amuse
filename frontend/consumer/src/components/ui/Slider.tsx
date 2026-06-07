"use client";

import { cn } from "@/lib/cn";
import { useRef, useState, type InputHTMLAttributes, type PointerEvent } from "react";

type SliderProps = Omit<
  InputHTMLAttributes<HTMLInputElement>,
  "type" | "value" | "onChange" | "size"
> & {
  value: number;
  min?: number;
  max?: number;
  step?: number;
  onChange: (next: number) => void;
  /** Fired when the user starts an interaction (pointer down / touch start). */
  onScrubStart?: () => void;
  /** Fired when the user releases the scrubber, with the final value. */
  onScrubEnd?: (final: number) => void;
  /** Visual size of the track / thumb. */
  size?: "sm" | "md";
  orientation?: "horizontal" | "vertical";
  /** Optional non-visual label for assistive tech. */
  label?: string;
  /** Buffered extent on the same scale as `value` (shown behind the played fill). */
  bufferedValue?: number;
  /** Show a popup label at the pointer position while hovering (e.g. seek time). */
  showHoverTooltip?: boolean;
  /** Format the hover tooltip label; defaults to rounding the raw value. */
  formatHoverValue?: (value: number) => string;
};

const sizeClass: Record<NonNullable<SliderProps["size"]>, string> = {
  sm: "h-1",
  md: "h-1.5",
};

const verticalTrackLengthClass: Record<NonNullable<SliderProps["size"]>, string> = {
  sm: "h-32",
  md: "h-36",
};

const thumbClass =
  "[&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:h-4 [&::-webkit-slider-thumb]:w-4 [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-primary [&::-webkit-slider-thumb]:border-2 [&::-webkit-slider-thumb]:border-on-primary [&::-moz-range-thumb]:h-4 [&::-moz-range-thumb]:w-4 [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:bg-primary [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-on-primary";

/**
 * Tokenised range slider.
 *
 * - The fill width has **no** CSS transition: any animation would visibly lag
 *   the native input thumb during scrubbing. Smoothness during natural
 *   playback is the caller's responsibility (e.g. driving the value off
 *   `usePlaybackPosition()` for a 60 Hz feed).
 * - `onScrubStart` / `onScrubEnd` are surfaced so callers can suspend other
 *   pushes into `value` while the user is dragging (preventing the audio
 *   element's own timeupdate from yanking the thumb back).
 */
export function Slider({
  value,
  min = 0,
  max = 100,
  step = 1,
  onChange,
  onScrubStart,
  onScrubEnd,
  className,
  size = "md",
  orientation = "horizontal",
  label,
  bufferedValue,
  showHoverTooltip = false,
  formatHoverValue = (next) => String(Math.round(next)),
  ...props
}: SliderProps) {
  const span = max - min || 1;
  const clamped = Math.max(min, Math.min(max, value));
  const percent = ((clamped - min) / span) * 100;
  const bufferedPercent =
    bufferedValue === undefined
      ? undefined
      : ((Math.max(min, Math.min(max, bufferedValue)) - min) / span) * 100;
  const scrubbingRef = useRef(false);
  const containerRef = useRef<HTMLSpanElement>(null);
  const [hoverTooltip, setHoverTooltip] = useState<{
    value: number;
    xPercent: number;
  } | null>(null);
  const isVertical = orientation === "vertical";

  const updateHoverTooltip = (event: PointerEvent<HTMLElement>) => {
    if (!showHoverTooltip || scrubbingRef.current || isVertical) {
      setHoverTooltip(null);
      return;
    }
    const rect = containerRef.current?.getBoundingClientRect();
    if (!rect || rect.width <= 0) return;

    const ratio = Math.max(0, Math.min(1, (event.clientX - rect.left) / rect.width));
    const hoverValue = min + ratio * span;
    setHoverTooltip({ value: hoverValue, xPercent: ratio * 100 });
  };

  const clearHoverTooltip = () => setHoverTooltip(null);

  const valueFromClientY = (clientY: number): number => {
    const rect = containerRef.current?.getBoundingClientRect();
    if (!rect || rect.height <= 0) return clamped;
    const ratio = 1 - (clientY - rect.top) / rect.height;
    const raw = min + Math.max(0, Math.min(1, ratio)) * span;
    if (step <= 0) return raw;
    const stepped = Math.round(raw / step) * step;
    return Math.max(min, Math.min(max, stepped));
  };

  const endVerticalScrub = (event: PointerEvent<HTMLSpanElement>, emitFinal = true) => {
    if (!scrubbingRef.current) return;
    scrubbingRef.current = false;
    if (emitFinal) {
      const final = valueFromClientY(event.clientY);
      onChange(final);
      onScrubEnd?.(final);
    }
    try {
      event.currentTarget.releasePointerCapture(event.pointerId);
    } catch {
      // Ignore.
    }
  };

  const rangeInput = (
    <input
      type="range"
      min={min}
      max={max}
      step={step}
      value={clamped}
      aria-label={label}
      aria-orientation={isVertical ? "vertical" : "horizontal"}
      onChange={(event) => onChange(Number(event.target.value))}
      onInput={(event) => onChange(Number(event.currentTarget.value))}
      onPointerDown={(event) => {
        if (isVertical) return;
        scrubbingRef.current = true;
        clearHoverTooltip();
        try {
          event.currentTarget.setPointerCapture(event.pointerId);
        } catch {
          // Ignore browsers that don't support pointer capture here.
        }
        onScrubStart?.();
      }}
      onPointerUp={(event) => {
        if (isVertical) return;
        if (scrubbingRef.current) {
          scrubbingRef.current = false;
          onScrubEnd?.(Number(event.currentTarget.value));
        }
        updateHoverTooltip(event);
        try {
          event.currentTarget.releasePointerCapture(event.pointerId);
        } catch {
          // Ignore.
        }
      }}
      onPointerCancel={(event) => {
        if (isVertical) return;
        if (scrubbingRef.current) {
          scrubbingRef.current = false;
          onScrubEnd?.(Number(event.currentTarget.value));
        }
      }}
      onLostPointerCapture={(event) => {
        if (isVertical) return;
        if (scrubbingRef.current) {
          scrubbingRef.current = false;
          onScrubEnd?.(Number(event.currentTarget.value));
        }
      }}
      className={cn(
        "absolute inset-0 cursor-pointer appearance-none bg-transparent",
        isVertical && "pointer-events-none opacity-0",
        !isVertical && thumbClass,
      )}
      {...props}
    />
  );

  const track = (
    <span
      className={cn(
        "pointer-events-none absolute overflow-hidden rounded-full bg-surface-variant",
        isVertical
          ? cn(
              "left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2",
              verticalTrackLengthClass[size],
              "w-1.5",
            )
          : cn("inset-x-0 top-1/2 w-full -translate-y-1/2", sizeClass[size]),
      )}
      aria-hidden
    >
      {bufferedPercent !== undefined && bufferedPercent > 0 ? (
        <span
          className={cn(
            "absolute bg-primary/35",
            isVertical ? "inset-x-0 bottom-0 w-full" : "inset-y-0 left-0",
          )}
          style={isVertical ? { height: `${bufferedPercent}%` } : { width: `${bufferedPercent}%` }}
        />
      ) : null}
      <span
        className={cn(
          "absolute bg-primary",
          isVertical ? "inset-x-0 bottom-0 w-full" : "inset-y-0 left-0",
        )}
        style={isVertical ? { height: `${percent}%` } : { width: `${percent}%` }}
      />
    </span>
  );

  if (isVertical) {
    return (
      <span
        ref={containerRef}
        className={cn(
          "relative block w-5 min-h-0 cursor-pointer select-none touch-none",
          verticalTrackLengthClass[size],
          className,
        )}
        onPointerDown={(event) => {
          scrubbingRef.current = true;
          onScrubStart?.();
          try {
            event.currentTarget.setPointerCapture(event.pointerId);
          } catch {
            // Ignore.
          }
          onChange(valueFromClientY(event.clientY));
        }}
        onPointerMove={(event) => {
          if (!scrubbingRef.current) return;
          onChange(valueFromClientY(event.clientY));
        }}
        onPointerUp={(event) => endVerticalScrub(event)}
        onPointerCancel={(event) => endVerticalScrub(event, false)}
        onLostPointerCapture={(event) => endVerticalScrub(event, false)}
      >
        {track}
        {rangeInput}
      </span>
    );
  }

  return (
    <span
      ref={containerRef}
      className={cn(
        "group relative block h-4 w-full min-w-0 select-none",
        className,
      )}
      onPointerMove={updateHoverTooltip}
      onPointerLeave={clearHoverTooltip}
    >
      {track}
      {rangeInput}
      {showHoverTooltip && hoverTooltip ? (
        <span
          role="tooltip"
          className="pointer-events-none absolute bottom-full z-10 mb-2 -translate-x-1/2 whitespace-nowrap rounded-md border border-outline bg-surface px-2 py-0.5 text-label-small text-on-surface shadow-sm tabular-nums"
          style={{ left: `${hoverTooltip.xPercent}%` }}
        >
          {formatHoverValue(hoverTooltip.value)}
        </span>
      ) : null}
    </span>
  );
}
