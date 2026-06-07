import "server-only";

import { cache } from "react";
import { deterministicSeedFromString } from "@/theme/extractSeedFromImage";
import { isAllowedCoverArtUrl } from "@/theme/allowedCoverArtUrl";
import { colorSeedFromWeightedRgba } from "@/theme/sampleColorSeed";
import type { ColorSeed } from "@/theme/types";

async function extractSeedFromImageServer(coverUrl: string): Promise<ColorSeed> {
  if (!isAllowedCoverArtUrl(coverUrl)) {
    return deterministicSeedFromString(coverUrl);
  }

  try {
    const response = await fetch(coverUrl, { signal: AbortSignal.timeout(5000) });
    if (!response.ok) {
      throw new Error("cover fetch failed");
    }

    const buffer = Buffer.from(await response.arrayBuffer());
    const sharp = (await import("sharp")).default;
    const { data } = await sharp(buffer)
      .resize(16, 16, { fit: "fill" })
      .ensureAlpha()
      .raw()
      .toBuffer({ resolveWithObject: true });

    return colorSeedFromWeightedRgba(data, 4);
  } catch {
    return deterministicSeedFromString(coverUrl);
  }
}

export const getCachedCoverArtColorSeed = cache(
  async (coverUrl: string): Promise<ColorSeed> => extractSeedFromImageServer(coverUrl),
);
