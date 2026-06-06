import { DynamicScheme, Hct, Variant } from "@material/material-color-utilities";
import { describe, expect, it } from "vitest";
import { argbToOklch, colorSeedToArgb } from "../colorConvert";
import type { ColorSeed } from "../types";

function hueOf(oklch: string): number {
  return Number.parseFloat(oklch.match(/oklch\([\d.]+ [\d.]+ ([\d.]+)/)?.[1] ?? "0");
}

function paletteFor(variant: Variant, seed: ColorSeed, contrast: number) {
  const scheme = new DynamicScheme({
    sourceColorHct: Hct.fromInt(colorSeedToArgb(seed)),
    variant,
    contrastLevel: contrast,
    isDark: false,
  });
  return {
    primary: argbToOklch(scheme.primary),
    primaryContainer: argbToOklch(scheme.primaryContainer),
    surface: argbToOklch(scheme.surface),
  };
}

describe("variantCompare", () => {
  it("content keeps primaryContainer near seed hue", () => {
    const seed: ColorSeed = { l: 0.55, c: 0.28, h: 285 };
    const content = paletteFor(Variant.CONTENT, seed, 0);
    const expressive = paletteFor(Variant.EXPRESSIVE, seed, 0.5);
    const seedHue = seed.h;
    const contentHueDelta = Math.min(
      Math.abs(hueOf(content.primaryContainer) - seedHue),
      360 - Math.abs(hueOf(content.primaryContainer) - seedHue),
    );
    const expressivePrimaryDelta = Math.min(
      Math.abs(hueOf(expressive.primary) - seedHue),
      360 - Math.abs(hueOf(expressive.primary) - seedHue),
    );
    expect(contentHueDelta).toBeLessThan(25);
    expect(expressivePrimaryDelta).toBeGreaterThan(40);
  });

});
