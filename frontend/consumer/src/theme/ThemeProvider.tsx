"use client";

import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { applyThemeVariables } from "./applyThemeVariables";
import { DEFAULT_APP_SEED } from "./defaultPalette";
import { resolveEffectiveSeed } from "./resolveEffectiveSeed";
import { seedToPalette } from "./seedToPalette";
import type { ColorSeed } from "./types";

type ThemeContextValue = {
  defaultSeed: ColorSeed;
  playingSeed: ColorSeed | null;
  pageSeed: ColorSeed | null;
  isPaused: boolean;
  setPlayingSeed: (seed: ColorSeed | null) => void;
  setPageSeed: (seed: ColorSeed | null) => void;
  setPaused: (paused: boolean) => void;
  effectiveSeed: ColorSeed;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [playingSeed, setPlayingSeed] = useState<ColorSeed | null>(null);
  const [pageSeed, setPageSeed] = useState<ColorSeed | null>(null);
  const [isPaused, setPaused] = useState(false);

  const effectiveSeed = useMemo(
    () =>
      resolveEffectiveSeed({
        pageSeed,
        playingSeed,
        defaultSeed: DEFAULT_APP_SEED,
      }),
    [pageSeed, playingSeed],
  );

  useEffect(() => {
    applyThemeVariables(seedToPalette(effectiveSeed, { paused: isPaused }));
  }, [effectiveSeed, isPaused]);

  const value = useMemo<ThemeContextValue>(
    () => ({
      defaultSeed: DEFAULT_APP_SEED,
      playingSeed,
      pageSeed,
      isPaused,
      setPlayingSeed,
      setPageSeed,
      setPaused,
      effectiveSeed,
    }),
    [playingSeed, pageSeed, isPaused, effectiveSeed],
  );

  return (
    <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
  );
}

export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) {
    throw new Error("useTheme must be used within ThemeProvider");
  }
  return ctx;
}

export function usePageSeed(seed: ColorSeed | null): void {
  const { setPageSeed } = useTheme();
  useEffect(() => {
    setPageSeed(seed);
    return () => setPageSeed(null);
  }, [seed, setPageSeed]);
}
