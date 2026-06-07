"use client";

import { VolumeControl } from "@/components/player/VolumeControl";
import { LikeTrackButton } from "@/components/player/LikeTrackButton";
import { PlayingQueuePanel } from "@/components/player/PlayingQueuePanel";
import { QualityPicker } from "@/components/player/QualityPicker";
import { IconButton } from "@/components/ui/IconButton";
import {
  ChevronDownIcon,
  NextIcon,
  PauseIcon,
  PlayIcon,
  PrevIcon,
  ShuffleIcon,
} from "@/components/ui/PlaybackIcons";
import { RepeatModeIcon } from "@/components/ui/RepeatModeIcon";
import { Slider } from "@/components/ui/Slider";
import { Text } from "@/components/ui/Text";
import { catalogArtistPath, catalogReleaseByIdHref, catalogReleaseHref } from "@/lib/catalog/paths";
import { cn } from "@/lib/cn";
import { formatDuration } from "@/lib/playback/formatDuration";
import {
  nextRepeatMode,
  repeatButtonVariant,
  repeatModeLabel,
} from "@/lib/playback/repeatMode";
import { mainScrollPaddingClass, shellContentPaddingClass } from "@/lib/ui/pageLayout";
import {
  usePlayback,
  usePlaybackBufferedEnd,
  usePlaybackPosition,
} from "@/lib/playback/PlaybackContext";
import { useScrubPosition } from "@/lib/playback/useScrubPosition";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import { usePageSeed } from "@/theme/ThemeProvider";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useKeyboardShortcuts } from "@/lib/keyboard/KeyboardShortcutsContext";
import { useEffect, useState } from "react";

export default function PlayingPage() {
  const router = useRouter();
  const {
    state,
    isQueueHydrated,
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
  const { helpOpen } = useKeyboardShortcuts();
  const [queueExpanded, setQueueExpanded] = useState(false);

  const coverSeed = useCoverArtSeed(currentTrack?.coverArtUrl ?? null);
  usePageSeed(coverSeed);

  useEffect(() => {
    if (!isQueueHydrated || currentTrack) return;
    router.replace("/home");
  }, [isQueueHydrated, currentTrack, router]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== "Escape" || helpOpen) return;
      event.preventDefault();
      router.back();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [helpOpen, router]);

  if (!isQueueHydrated || !currentTrack) {
    return (
      <div className="flex h-dvh items-center justify-center bg-background p-6">
        <Text variant="body-medium">
          {!isQueueHydrated ? "Restoring queue…" : "No track in the queue."}
        </Text>
      </div>
    );
  }

  const nextRepeat = nextRepeatMode(state.repeat);
  const max = Math.max(state.durationMs, 1);
  const { displayMs, sliderProps } = useScrubPosition(smoothMs, max, {
    beginScrub,
    endScrub,
  });
  const canNext =
    state.playOrderIndex < state.playOrder.length - 1 || state.repeat === "queue";

  const controlsBlock = (
    <div className="relative flex w-full max-w-md items-center justify-center px-10">
      <div className="absolute left-0 top-1/2 -translate-y-1/2">
        <VolumeControl variant="full" />
      </div>
      <div className="flex items-center justify-center gap-2">
        <IconButton
          label={`Shuffle ${state.shuffle ? "on" : "off"}`}
          variant={state.shuffle ? "tonal" : "ghost"}
          onClick={toggleShuffle}
        >
          <ShuffleIcon />
        </IconButton>
        <IconButton label="Previous track" variant="ghost" size="lg" onClick={previous}>
          <PrevIcon />
        </IconButton>
        <IconButton
          label={state.isPlaying ? "Pause" : "Play"}
          variant="filled"
          size="lg"
          onClick={toggle}
        >
          {state.isPlaying ? <PauseIcon /> : <PlayIcon />}
        </IconButton>
        <IconButton
          label="Next track"
          variant="ghost"
          size="lg"
          onClick={next}
          disabled={!canNext}
        >
          <NextIcon />
        </IconButton>
        <IconButton
          label={repeatModeLabel(state.repeat)}
          variant={repeatButtonVariant(state.repeat)}
          onClick={() => setRepeat(nextRepeat)}
        >
          <RepeatModeIcon mode={state.repeat} />
        </IconButton>
      </div>
      <div className="absolute right-0 top-1/2 -translate-y-1/2">
        <LikeTrackButton trackId={currentTrack.id} />
      </div>
    </div>
  );

  const seekBlock = (
    <div className="flex w-full max-w-md flex-col gap-2">
      <Slider
        value={displayMs}
        bufferedValue={bufferedMs}
        min={0}
        max={max}
        step={1}
        {...sliderProps}
        label="Seek within current track"
        showHoverTooltip
        formatHoverValue={formatDuration}
      />
      <div className="flex justify-between text-on-surface-variant tabular-nums">
        <Text variant="label-small">{formatDuration(displayMs)}</Text>
        <Text variant="label-small">{formatDuration(state.durationMs)}</Text>
      </div>
    </div>
  );

  const metadataBlock = (
    <div className="flex w-full max-w-md flex-col items-center gap-1 text-center">
      <Text variant="headline-small">{currentTrack.title}</Text>
      <Link
        href={
          currentTrack.artistSlug
            ? catalogArtistPath(currentTrack.artistSlug)
            : `/artist/${currentTrack.artistId}`
        }
      >
        <Text variant="body-medium" className="text-on-surface-variant">
          {currentTrack.artistName}
        </Text>
      </Link>
      <QualityPicker />
    </div>
  );

  const coverArt = (
    <button
      type="button"
      aria-label={state.isPlaying ? "Pause" : "Play"}
      onClick={toggle}
      className={cn(
        "block shrink-0 overflow-hidden rounded-2xl border-2 border-outline bg-surface-variant",
        "aspect-square w-full max-w-sm sm:max-w-md md:max-w-[24rem] lg:max-w-[28rem] xl:max-w-[32rem]",
        queueExpanded && "md:max-w-[min(100%,24rem)]",
        "transition-opacity hover:opacity-95",
      )}
    >
      {currentTrack.coverArtUrl ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          src={currentTrack.coverArtUrl}
          alt=""
          className="size-full object-cover"
          draggable={false}
        />
      ) : null}
    </button>
  );

  return (
    <div className="flex h-dvh flex-col bg-background">
      <div className={cn("flex items-center justify-between py-4", shellContentPaddingClass)}>
        <IconButton label="Close now playing" variant="ghost" onClick={() => router.back()}>
          <ChevronDownIcon />
        </IconButton>
        <div className="text-center">
          <Text variant="label-small" className="text-on-surface-variant">
            Playing from
          </Text>
          <Link
            href={
              currentTrack.artistSlug && currentTrack.releaseSlug
                ? catalogReleaseHref(currentTrack.artistSlug, currentTrack.releaseSlug)
                : catalogReleaseByIdHref(currentTrack.releaseId)
            }
          >
            <Text variant="title-small">{currentTrack.releaseTitle}</Text>
          </Link>
        </div>
        <div className="h-10 w-10" aria-hidden />
      </div>

      <div
        className={cn(
          "flex flex-1 min-h-0 flex-col gap-6 overflow-y-auto",
          mainScrollPaddingClass,
          queueExpanded
            ? "md:flex-row md:items-stretch md:overflow-hidden"
            : "md:flex-row md:items-center md:justify-center",
        )}
      >
        <div
          className={cn(
            "flex w-full flex-col items-center justify-center gap-6",
            queueExpanded ? "md:min-w-0 md:flex-1 md:self-stretch" : "md:flex-1",
          )}
        >
          {coverArt}

          {queueExpanded ? (
            <div className="flex w-full max-w-md flex-col items-center gap-4">
              {seekBlock}
              {controlsBlock}
            </div>
          ) : null}
        </div>

        <div
          className={cn(
            "flex w-full min-h-0 flex-col items-center gap-6",
            queueExpanded
              ? "md:w-full md:max-w-md md:shrink-0 md:items-stretch md:self-stretch lg:max-w-lg xl:max-w-xl"
              : "md:flex-1 md:justify-center",
          )}
        >
          {!queueExpanded ? (
            <div className="flex w-full max-w-md flex-col items-center gap-6">
              {metadataBlock}
              {seekBlock}
              {controlsBlock}
            </div>
          ) : null}

          <PlayingQueuePanel
            expanded={queueExpanded}
            onExpandedChange={setQueueExpanded}
            className={cn("w-full max-w-md", queueExpanded && "min-h-0 flex-1")}
          />
        </div>
      </div>
    </div>
  );
}
