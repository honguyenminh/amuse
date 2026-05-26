"use client";

import { cn } from "@/lib/cn";
import type { InputHTMLAttributes } from "react";

type SliderProps = Omit<InputHTMLAttributes<HTMLInputElement>, "type" | "value" | "onChange" | "size"> & {
  value: number;
  min?: number;
  max?: number;
  step?: number;
  onChange: (next: number) => void;
  /** Visual size of the track / thumb. */
  size?: "sm" | "md";
  /** Optional non-visual label for assistive tech. */
  label?: string;
};

const sizeClass: Record<NonNullable<SliderProps["size"]>, string> = {
  sm: "h-1",
  md: "h-1.5",
};

/**
 * Tokenised range slider. The native input drives behaviour (keyboard, focus, accessibility);
 * the visual track is styled via CSS variables so it inherits the active theme.
 */
export function Slider({
  value,
  min = 0,
  max = 100,
  step = 1,
  onChange,
  className,
  size = "md",
  label,
  ...props
}: SliderProps) {
  const span = max - min || 1;
  const clamped = Math.max(min, Math.min(max, value));
  const percent = ((clamped - min) / span) * 100;

  return (
    <span className={cn("group relative block w-full select-none", className)}>
      <span
        className={cn(
          "block w-full overflow-hidden rounded-full bg-surface-variant",
          sizeClass[size],
        )}
        aria-hidden
      >
        <span
          className="block h-full bg-primary transition-[width] duration-100 ease-out"
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
        className="absolute inset-0 h-full w-full cursor-pointer appearance-none bg-transparent [&::-webkit-slider-thumb]:appearance-none [&::-webkit-slider-thumb]:h-4 [&::-webkit-slider-thumb]:w-4 [&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-primary [&::-webkit-slider-thumb]:border-2 [&::-webkit-slider-thumb]:border-on-primary [&::-moz-range-thumb]:h-4 [&::-moz-range-thumb]:w-4 [&::-moz-range-thumb]:rounded-full [&::-moz-range-thumb]:bg-primary [&::-moz-range-thumb]:border-2 [&::-moz-range-thumb]:border-on-primary"
        {...props}
      />
    </span>
  );
}
