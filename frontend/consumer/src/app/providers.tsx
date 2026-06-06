"use client";

import { AuthProvider } from "@/lib/auth/AuthProvider";
import { PlaybackProvider } from "@/lib/playback/PlaybackContext";
import { PlaybackContextMenuProvider } from "@/lib/playback/PlaybackContextMenuProvider";
import { ThemeProvider } from "@/theme/ThemeProvider";
import type { ReactNode } from "react";

export function Providers({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider>
      <AuthProvider>
        <PlaybackProvider>
          <PlaybackContextMenuProvider>{children}</PlaybackContextMenuProvider>
        </PlaybackProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}
