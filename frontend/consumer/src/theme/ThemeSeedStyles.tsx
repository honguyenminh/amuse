import { seedToRootCss } from "./paletteCss";
import type { ColorSeed } from "./types";

export function ThemeSeedStyles({ seed }: { seed: ColorSeed }) {
  return (
    <style
      data-amuse-page-seed=""
      dangerouslySetInnerHTML={{ __html: seedToRootCss(seed) }}
    />
  );
}
