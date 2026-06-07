"use client";

import { GlobalKeyboardShortcuts } from "@/components/keyboard/GlobalKeyboardShortcuts";
import { AuthProvider } from "@/lib/auth/AuthProvider";
import { KeyboardShortcutsProvider } from "@/lib/keyboard/KeyboardShortcutsContext";
import { PlaybackProvider } from "@/lib/playback/PlaybackContext";
import { PlaybackContextMenuProvider } from "@/lib/playback/PlaybackContextMenuProvider";
import { ThemeProvider } from "@/theme/ThemeProvider";
import type { ReactNode } from "react";

export function Providers({ children }: { children: ReactNode }) {
  return (
    <ThemeProvider>
      <AuthProvider>
        <PlaybackProvider>
          <KeyboardShortcutsProvider>
            <PlaybackContextMenuProvider>
              <GlobalKeyboardShortcuts />
              {children}
            </PlaybackContextMenuProvider>
          </KeyboardShortcutsProvider>
        </PlaybackProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}
