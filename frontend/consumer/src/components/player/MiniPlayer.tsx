"use client";

import { VolumeControl } from "@/components/player/VolumeControl";
import { LikeTrackButton } from "@/components/player/LikeTrackButton";
import { IconButton } from "@/components/ui/IconButton";
import {
  NextIcon,
  PauseIcon,
  PlayIcon,
  PrevIcon,
  ShuffleIcon,
} from "@/components/ui/PlaybackIcons";
import { RepeatModeIcon } from "@/components/ui/RepeatModeIcon";
import { Slider } from "@/components/ui/Slider";
import { Text } from "@/components/ui/Text";
import { cn } from "@/lib/cn";
import { formatDuration } from "@/lib/playback/formatDuration";
import {
  nextRepeatMode,
  repeatButtonVariant,
  repeatModeLabel,
} from "@/lib/playback/repeatMode";
import { shellContentPaddingClass } from "@/lib/ui/pageLayout";
import {
  usePlayback,
  usePlaybackBufferedEnd,
  usePlaybackPosition,
} from "@/lib/playback/PlaybackContext";
import { useScrubPosition } from "@/lib/playback/useScrubPosition";
import Link from "next/link";

/**
 * Persistent mini player docked to the bottom of the viewport (full-width on
 * every breakpoint, sitting under the app shell). Hidden when the queue is
 * empty so anonymous browsing has zero chrome cost.
 *
 * The seek slider is absolutely positioned on the top edge of the control bar
 * so it does not add a separate opaque band above the controls.
 */
export function MiniPlayer() {
  const {
    state,
    currentTrack,
    toggle,
    next,
    previous,
    beginScrub,
    endScrub,
    setRepeat,
    toggleShuffle,
  } = usePlayback();
  const smoothMs = usePlaybackPosition();
  const bufferedMs = usePlaybackBufferedEnd();
  const max = Math.max(state.durationMs, 1);
  const { displayMs: value, sliderProps } = useScrubPosition(smoothMs, max, {
    beginScrub,
    endScrub,
  });

  if (!currentTrack) return null;

  const canNext =
    state.playOrderIndex < state.playOrder.length - 1 || state.repeat === "queue";
  const nextRepeat = nextRepeatMode(state.repeat);

  return (
    <div
      className="relative z-20 w-full shrink-0"
      onDragStart={(event) => event.preventDefault()}
    >
      <div
        className={cn(
          "bg-surface/95 backdrop-blur",
          "supports-[backdrop-filter]:bg-surface/90",
        )}
      >
        <div className={cn("flex items-center gap-3 py-2", shellContentPaddingClass)}>
          <Link
            href="/playing"
            className="flex min-w-0 flex-1 items-center gap-3 overflow-hidden"
            aria-label={`Open now playing: ${currentTrack.title}`}
            draggable={false}
            onDragStart={(event) => event.preventDefault()}
          >
            <div className="h-12 w-12 shrink-0 overflow-hidden rounded-md border-2 border-outline bg-surface-variant">
              {currentTrack.coverArtUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={currentTrack.coverArtUrl}
                  alt=""
                  draggable={false}
                  className="h-full w-full object-cover [-webkit-user-drag:none]"
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

          <div className="hidden items-center gap-2 sm:flex">
            <LikeTrackButton trackId={currentTrack.id} size="sm" />
          </div>

          <VolumeControl variant="compact" portalPopup className="hidden md:flex" />

          <div className="hidden items-center gap-0.5 sm:flex">
            <IconButton
              label={`Shuffle ${state.shuffle ? "on" : "off"}`}
              variant={state.shuffle ? "tonal" : "ghost"}
              size="sm"
              onClick={toggleShuffle}
            >
              <ShuffleIcon />
            </IconButton>
            <IconButton
              label={repeatModeLabel(state.repeat)}
              variant={repeatButtonVariant(state.repeat)}
              size="sm"
              onClick={() => setRepeat(nextRepeat)}
            >
              <RepeatModeIcon mode={state.repeat} />
            </IconButton>
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
      <div className="pointer-events-none absolute inset-x-0 top-0 z-30 -translate-y-1/2">
        <Slider
          value={value}
          bufferedValue={bufferedMs}
          min={0}
          max={max}
          step={1}
          {...sliderProps}
          label="Seek within current track"
          size="sm"
          className="pointer-events-auto h-4 bg-transparent px-0"
          showHoverTooltip
          formatHoverValue={formatDuration}
        />
      </div>
    </div>
  );
}
