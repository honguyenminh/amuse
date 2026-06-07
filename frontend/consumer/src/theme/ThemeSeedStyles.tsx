import { seedToRootCss } from "./paletteCss";
import type { ColorSeed } from "./types";

export function ThemeSeedStyles({ seed }: { seed: ColorSeed }) {
  return <style dangerouslySetInnerHTML={{ __html: seedToRootCss(seed) }} />;
}
