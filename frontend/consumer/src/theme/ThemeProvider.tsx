"use client";

import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type Dispatch,
  type MutableRefObject,
  type ReactNode,
  type SetStateAction,
} from "react";
import { applyThemeVariables } from "./applyThemeVariables";
import { DEFAULT_APP_SEED } from "./defaultPalette";
import { pageSeedAfterOwnerUnmount, resolveThemeSeed } from "./pageSeedState";
import { seedToPalette } from "./seedToPalette";
import type { ColorSeed } from "./types";

type ThemeContextValue = {
  defaultSeed: ColorSeed;
  playingSeed: ColorSeed | null;
  pageSeed: ColorSeed | null;
  pageSeedRef: MutableRefObject<ColorSeed | null>;
  isPaused: boolean;
  setPlayingSeed: (seed: ColorSeed | null) => void;
  setPageSeed: Dispatch<SetStateAction<ColorSeed | null>>;
  setPaused: (paused: boolean) => void;
  effectiveSeed: ColorSeed;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const pageSeedRef = useRef<ColorSeed | null>(null);
  const [playingSeed, setPlayingSeed] = useState<ColorSeed | null>(null);
  const [pageSeed, setPageSeed] = useState<ColorSeed | null>(null);
  const [isPaused, setPaused] = useState(false);

  const effectiveSeed = useMemo(
    () =>
      resolveThemeSeed({
        pageSeed,
        pageSeedRef,
        playingSeed,
        defaultSeed: DEFAULT_APP_SEED,
      }),
    [pageSeed, playingSeed],
  );

  useEffect(() => {
    applyThemeVariables(
      seedToPalette(
        resolveThemeSeed({
          pageSeed,
          pageSeedRef,
          playingSeed,
          defaultSeed: DEFAULT_APP_SEED,
        }),
        { paused: isPaused },
      ),
    );
  }, [pageSeed, playingSeed, isPaused]);

  const value = useMemo<ThemeContextValue>(
    () => ({
      defaultSeed: DEFAULT_APP_SEED,
      playingSeed,
      pageSeed,
      pageSeedRef,
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
  const { pageSeedRef, setPageSeed } = useTheme();

  // Ref updates during render are safe and visible to ThemeProvider's effect in
  // the same commit, avoiding a default-palette flash over SSR ThemeSeedStyles.
  pageSeedRef.current = seed;

  useEffect(() => {
    setPageSeed(seed);
    return () => {
      if (pageSeedRef.current === seed) {
        pageSeedRef.current = null;
      }
      setPageSeed((current) => pageSeedAfterOwnerUnmount(current, seed));
    };
  }, [pageSeedRef, seed, setPageSeed]);
}
