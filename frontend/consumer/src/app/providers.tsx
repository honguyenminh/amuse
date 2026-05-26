"use client";

import { AuthProvider } from "@/lib/auth/AuthProvider";
import { PlaybackProvider } from "@/lib/playback/PlaybackContext";
import { ThemeProvider } from "@/theme/ThemeProvider";
import type { ReactNode } from "react";

export function Providers({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider>
      <AuthProvider>
        <PlaybackProvider>{children}</PlaybackProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}
