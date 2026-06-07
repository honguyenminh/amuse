"use client";

import { GlobalKeyboardShortcuts } from "@/components/keyboard/GlobalKeyboardShortcuts";
import { QueueAddBurstProvider } from "@/components/ui/QueueAddBurstProvider";
import { SnackbarProvider } from "@/components/ui/SnackbarProvider";
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
          <SnackbarProvider>
            <QueueAddBurstProvider>
            <KeyboardShortcutsProvider>
              <PlaybackContextMenuProvider>
                <GlobalKeyboardShortcuts />
                {children}
              </PlaybackContextMenuProvider>
            </KeyboardShortcutsProvider>
            </QueueAddBurstProvider>
          </SnackbarProvider>
        </PlaybackProvider>
      </AuthProvider>
    </ThemeProvider>
  );
}
