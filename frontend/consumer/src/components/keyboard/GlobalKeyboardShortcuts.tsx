"use client";

import { KeyboardShortcutsDialog } from "@/components/keyboard/KeyboardShortcutsDialog";
import { libraryPlaylistsPath } from "@/lib/discovery/paths";
import { hasModKey } from "@/lib/keyboard/isEditableTarget";
import { useKeyboardShortcuts } from "@/lib/keyboard/KeyboardShortcutsContext";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

/**
 * App-wide keyboard shortcuts. Mounted once in the provider tree so they work
 * on every route, including /playing and auth screens.
 */
export function GlobalKeyboardShortcuts() {
  const router = useRouter();
  const {
    toggle,
    next,
    previous,
    stop,
    toggleMute,
    nudgeVolume,
    currentTrack,
  } = usePlayback();
  const { helpOpen, closeHelp, toggleHelp, focusSearch } = useKeyboardShortcuts();

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (helpOpen) {
        if (event.key === "Escape") {
          event.preventDefault();
          closeHelp();
          return;
        }
        if (hasModKey(event) && event.key === "/") {
          event.preventDefault();
          closeHelp();
        }
        return;
      }

      const mod = hasModKey(event);
      if (!mod) return;

      if (event.key === "/") {
        event.preventDefault();
        toggleHelp();
        return;
      }

      if (event.code === "Space") {
        event.preventDefault();
        toggle();
        return;
      }

      if (event.key === "s" || event.key === "S") {
        event.preventDefault();
        stop();
        return;
      }

      if (event.key === "ArrowRight") {
        event.preventDefault();
        next();
        return;
      }

      if (event.key === "ArrowLeft") {
        event.preventDefault();
        previous();
        return;
      }

      if (event.key === "ArrowUp") {
        event.preventDefault();
        nudgeVolume(0.05);
        return;
      }

      if (event.key === "ArrowDown") {
        event.preventDefault();
        nudgeVolume(-0.05);
        return;
      }

      if (event.key === "m" || event.key === "M") {
        event.preventDefault();
        toggleMute();
        return;
      }

      if (event.key === "k" || event.key === "K") {
        event.preventDefault();
        focusSearch();
        return;
      }

      if (event.key === "i" || event.key === "I") {
        event.preventDefault();
        if (currentTrack) router.push("/playing");
        return;
      }

      if (event.key === "h" || event.key === "H") {
        event.preventDefault();
        router.push("/home");
        return;
      }

      if (event.key === "l" || event.key === "L") {
        event.preventDefault();
        router.push(libraryPlaylistsPath);
        return;
      }

      if (event.key === ",") {
        event.preventDefault();
        router.push("/settings");
        return;
      }

    };

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [
    helpOpen,
    closeHelp,
    toggleHelp,
    toggle,
    stop,
    next,
    previous,
    toggleMute,
    nudgeVolume,
    focusSearch,
    router,
    currentTrack,
  ]);

  return <KeyboardShortcutsDialog />;
}
