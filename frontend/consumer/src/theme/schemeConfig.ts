import { Variant } from "@material/material-color-utilities";

/**
 * M3 dynamic-color preset — same family as Flutter's `dynamic_color` variants.
 * CONTENT keeps the cover-art hue on primary roles (primaryContainer ≈ source).
 * Use VIBRANT for more saturation, EXPRESSIVE only if you want wild hue shifts.
 */
// export const SCHEME_VARIANT = Variant.VIBRANT;
export const SCHEME_VARIANT = Variant.VIBRANT;

/**
 * Contrast level passed to Material Color Utilities (-1 … 1).
 * 0 = spec default; higher values push primaries darker / further from surfaces.
 */
export const SCHEME_CONTRAST = 0;
