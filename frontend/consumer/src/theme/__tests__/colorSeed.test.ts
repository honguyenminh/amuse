import { describe, expect, it } from "vitest";
import { colorSeedFromWeightedRgba } from "../sampleColorSeed";
import { seedToRootCss } from "../paletteCss";
import { pageSeedAfterOwnerUnmount, resolveThemeSeed } from "../pageSeedState";
import { DEFAULT_APP_SEED } from "../defaultPalette";
import { seedToPalette } from "../seedToPalette";

describe("colorSeedFromWeightedRgba", () => {
  it("biases toward higher-chroma pixels", () => {
    const data = new Uint8ClampedArray(16 * 16 * 4);
    for (let i = 0; i < data.length; i += 4) {
      data[i] = 240;
      data[i + 1] = 240;
      data[i + 2] = 240;
      data[i + 3] = 255;
    }
    data[0] = 220;
    data[1] = 40;
    data[2] = 40;

    const seed = colorSeedFromWeightedRgba(data, 4);
    expect(seed.l).toBeGreaterThanOrEqual(0.42);
    expect(seed.l).toBeLessThanOrEqual(0.68);
    expect(seed.c).toBeGreaterThanOrEqual(0.14);
    expect(seed.h).toBeGreaterThanOrEqual(0);
    expect(seed.h).toBeLessThan(360);
  });
});

describe("seedToRootCss", () => {
  it("emits css variables for the palette", () => {
    const css = seedToRootCss(DEFAULT_APP_SEED);
    const palette = seedToPalette(DEFAULT_APP_SEED);
    expect(css).toContain(`--amuse-primary:${palette.primary}`);
    expect(css.startsWith(":root{")).toBe(true);
    expect(css.endsWith("}")).toBe(true);
  });
});

describe("pageSeedAfterOwnerUnmount", () => {
  it("clears page seed only when the unmounting owner still owns it", () => {
    const owner = { l: 0.5, c: 0.2, h: 120 };
    const nextOwner = { l: 0.6, c: 0.25, h: 200 };

    expect(pageSeedAfterOwnerUnmount(owner, owner)).toBeNull();
    expect(pageSeedAfterOwnerUnmount(nextOwner, owner)).toBe(nextOwner);
    expect(pageSeedAfterOwnerUnmount(null, owner)).toBeNull();
  });
});

describe("resolveThemeSeed", () => {
  it("prefers the render-time page seed ref over stale pageSeed state", () => {
    const refSeed = { l: 0.55, c: 0.3, h: 180 };
    const pageSeedRef = { current: refSeed };

    expect(
      resolveThemeSeed({
        pageSeed: null,
        pageSeedRef,
        playingSeed: null,
        defaultSeed: DEFAULT_APP_SEED,
      }),
    ).toEqual(refSeed);
  });
});
