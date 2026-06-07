import type { RepeatMode } from "./types";

/** Cycles: off → repeat queue → repeat one → off. */
export function nextRepeatMode(mode: RepeatMode): RepeatMode {
  if (mode === "off") return "queue";
  if (mode === "queue") return "one";
  return "off";
}

export function repeatModeLabel(mode: RepeatMode): string {
  switch (mode) {
    case "queue":
      return "Repeat queue";
    case "one":
      return "Repeat current song";
    default:
      return "Repeat off";
  }
}

export function repeatButtonVariant(
  mode: RepeatMode,
): "ghost" | "tonal" | "tertiary-tonal" {
  switch (mode) {
    case "one":
      return "tertiary-tonal";
    case "queue":
      return "tonal";
    default:
      return "ghost";
  }
}
