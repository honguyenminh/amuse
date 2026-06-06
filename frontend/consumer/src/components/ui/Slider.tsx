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

  const updateHoverTooltip = (event: PointerEvent<HTMLElement>) => {
    if (!showHoverTooltip || scrubbingRef.current) {
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
      <span
        className={cn(
          "pointer-events-none absolute inset-x-0 top-1/2 w-full -translate-y-1/2 overflow-hidden rounded-full bg-surface-variant",
          sizeClass[size],
        )}
        aria-hidden
      >
        {bufferedPercent !== undefined && bufferedPercent > 0 ? (
          <span
            className="absolute inset-y-0 left-0 bg-primary/35"
            style={{ width: `${bufferedPercent}%` }}
          />
        ) : null}
        <span
          className="absolute inset-y-0 left-0 bg-primary"
          style={{ width: `${percent}%` }}
        />
      </span>
      <input
        type="range"
        min={min}
        max={max}
        step={step}
        value={clamped}
        aria-label={label}
        onChange={(event) => onChange(Number(event.target.value))}
        onInput={(event) => onChange(Number(event.currentTarget.value))}
        onPointerDown={(event) => {
          // Keep receiving pointer events even if the pointer leaves the input,
          // so we always end scrubbing and re-enable progress updates.
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
          if (scrubbingRef.current) {
            scrubbingRef.current = false;
            onScrubEnd?.(Number(event.currentTarget.value));
          }
        }}
        onLostPointerCapture={(event) => {
          if (scrubbingRef.current) {
            scrubbingRef.current = false;
            onScrubEnd?.(Number(event.currentTarget.value));
          }
        }}
        className="absolute inset-0 h-full w-full cursor-pointer appearance-none bg-transparent [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:h-4 [&::-webkit-slider-thumb]:w-4 [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-primary [&::-webkit-slider-thumb]:border-2 [&::-webkit-slider-thumb]:border-on-primary [&::-moz-range-thumb]:h-4 [&::-moz-range-thumb]:w-4 [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:bg-primary [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-on-primary"
        {...props}
      />
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
