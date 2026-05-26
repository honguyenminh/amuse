"use client";

import { IconButton } from "@/components/ui/IconButton";
import { NextIcon, PauseIcon, PlayIcon } from "@/components/ui/PlaybackIcons";
import { Text } from "@/components/ui/Text";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { cn } from "@/lib/cn";
import Link from "next/link";

/**
 * Persistent mini player docked above the BottomNav. Hidden when the queue is empty.
 * Tapping the body navigates to the full /playing view; tapping play/next does not.
 */
export function MiniPlayer() {
  const { state, currentTrack, toggle, next } = usePlayback();
  if (!currentTrack) return null;

  const progress = state.durationMs > 0 ? (state.positionMs / state.durationMs) * 100 : 0;

  return (
    <div className="border-t-2 border-outline bg-surface">
      <span
        aria-hidden
        className="block h-0.5 w-full bg-surface-variant"
      >
        <span
          className="block h-full bg-primary transition-[width] duration-200 ease-out"
          style={{ width: `${progress}%` }}
        />
      </span>
      <div className="flex items-center gap-3 px-3 py-2">
        <Link
          href="/playing"
          className="flex flex-1 items-center gap-3 overflow-hidden"
          aria-label={`Open now playing: ${currentTrack.title}`}
        >
          <div
            className={cn(
              "h-12 w-12 shrink-0 overflow-hidden rounded-md border-2 border-outline bg-surface-variant",
            )}
          >
            {currentTrack.coverArtUrl ? (
              // eslint-disable-next-line @next/next/no-img-element -- raw <img> avoids the next/image domain allowlist for arbitrary cover hosts.
              <img
                src={currentTrack.coverArtUrl}
                alt=""
                className="h-full w-full object-cover"
              />
            ) : null}
          </div>
          <div className="flex min-w-0 flex-col">
            <Text variant="title-small" className="truncate">
              {currentTrack.title}
            </Text>
            <Text variant="body-small" className="truncate text-on-surface-variant">
              {currentTrack.artistName}
            </Text>
          </div>
        </Link>
        <IconButton
          label={state.isPlaying ? "Pause" : "Play"}
          variant="filled"
          size="md"
          onClick={toggle}
        >
          {state.isPlaying ? <PauseIcon /> : <PlayIcon />}
        </IconButton>
        <IconButton
          label="Next track"
          variant="ghost"
          size="md"
          onClick={next}
          disabled={state.currentIndex >= state.queue.length - 1 && state.repeat !== "queue"}
        >
          <NextIcon />
        </IconButton>
      </div>
    </div>
  );
}
