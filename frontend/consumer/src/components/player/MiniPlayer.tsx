"use client";

import { IconButton } from "@/components/ui/IconButton";
import { NextIcon, PauseIcon, PlayIcon, PrevIcon } from "@/components/ui/PlaybackIcons";
import { Slider } from "@/components/ui/Slider";
import { Text } from "@/components/ui/Text";
import { cn } from "@/lib/cn";
import { formatDuration } from "@/lib/playback/formatDuration";
import { usePlayback, usePlaybackPosition } from "@/lib/playback/PlaybackContext";
import { useScrubPosition } from "@/lib/playback/useScrubPosition";
import Link from "next/link";

/**
 * Persistent mini player docked to the bottom of the viewport (full-width on
 * every breakpoint, sitting under the app shell). Hidden when the queue is
 * empty so anonymous browsing has zero chrome cost.
 *
 * Interactions:
 * - Tap the cover/title strip to open the full /playing view.
 * - The progress bar is a real `Slider` — drag to scrub, click to jump.
 * - Prev / play-pause / next inline; full transport controls live in /playing.
 *
 * Visual smoothness: the slider value is driven by `usePlaybackPosition()` so
 * it advances at the animation-frame cadence instead of the audio element's
 * ~4 Hz `timeupdate`. During scrubbing we override that with a local value
 * so the audio element's pushback can't yank the thumb around.
 */
export function MiniPlayer() {
  const { state, currentTrack, toggle, next, previous, beginScrub, endScrub } = usePlayback();
  const smoothMs = usePlaybackPosition();
  const max = Math.max(state.durationMs, 1);
  const { displayMs: value, sliderProps } = useScrubPosition(smoothMs, max, {
    beginScrub,
    endScrub,
  });

  if (!currentTrack) return null;

  const canNext = state.currentIndex < state.queue.length - 1 || state.repeat === "queue";

  return (
    <div
      className={cn(
        "sticky bottom-0 z-20 w-full border-t-2 border-outline bg-surface/95 backdrop-blur",
        "supports-[backdrop-filter]:bg-surface/90",
      )}
    >
      <Slider
        value={value}
        min={0}
        max={max}
        step={1}
        {...sliderProps}
        label="Seek within current track"
        size="sm"
        className="px-0"
      />
      <div className="flex items-center gap-3 px-3 py-2">
        <Link
          href="/playing"
          className="flex min-w-0 flex-1 items-center gap-3 overflow-hidden"
          aria-label={`Open now playing: ${currentTrack.title}`}
        >
          <div className="h-12 w-12 shrink-0 overflow-hidden rounded-md border-2 border-outline bg-surface-variant">
            {currentTrack.coverArtUrl ? (
              // eslint-disable-next-line @next/next/no-img-element
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

        <div className="hidden items-center gap-3 text-on-surface-variant tabular-nums sm:flex">
          <Text variant="label-small">{formatDuration(value)}</Text>
          <span aria-hidden>/</span>
          <Text variant="label-small">{formatDuration(state.durationMs)}</Text>
        </div>

        <div className="flex items-center gap-1">
          <IconButton
            label="Previous track"
            variant="ghost"
            size="md"
            onClick={previous}
          >
            <PrevIcon />
          </IconButton>
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
            disabled={!canNext}
          >
            <NextIcon />
          </IconButton>
        </div>
      </div>
    </div>
  );
}
