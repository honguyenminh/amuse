"use client";

import { IconButton } from "@/components/ui/IconButton";
import {
  ChevronDownIcon,
  NextIcon,
  PauseIcon,
  PlayIcon,
  PrevIcon,
  RepeatIcon,
  ShuffleIcon,
} from "@/components/ui/PlaybackIcons";
import { Slider } from "@/components/ui/Slider";
import { Text } from "@/components/ui/Text";
import { formatDuration } from "@/lib/playback/formatDuration";
import { usePlayback, usePlaybackPosition } from "@/lib/playback/PlaybackContext";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import { usePageSeed } from "@/theme/ThemeProvider";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export default function PlayingPage() {
  const router = useRouter();
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
  const [scrubMs, setScrubMs] = useState<number | null>(null);

  // Theme: this view IS the playing seed, so route the cover into pageSeed for
  // identical resolution to the rest of the app (page > playing > default).
  const coverSeed = useCoverArtSeed(currentTrack?.coverArtUrl ?? null);
  usePageSeed(coverSeed);

  useEffect(() => {
    if (!currentTrack) router.replace("/home");
  }, [currentTrack, router]);

  if (!currentTrack) {
    return (
      <div className="flex h-dvh items-center justify-center bg-background p-6">
        <Text variant="body-medium">No track in the queue.</Text>
      </div>
    );
  }

  const nextRepeat: typeof state.repeat =
    state.repeat === "off" ? "queue" : state.repeat === "queue" ? "one" : "off";
  const max = Math.max(state.durationMs, 1);
  const displayMs = scrubMs ?? Math.min(smoothMs, max);
  const canNext = state.currentIndex < state.queue.length - 1 || state.repeat === "queue";

  return (
    <div className="flex h-dvh flex-col bg-background">
      <div className="flex items-center justify-between p-4">
        <IconButton label="Close now playing" variant="ghost" onClick={() => router.back()}>
          <ChevronDownIcon />
        </IconButton>
        <div className="text-center">
          <Text variant="label-small" className="text-on-surface-variant">
            Playing from
          </Text>
          <Link href={`/release/${currentTrack.releaseId}`}>
            <Text variant="title-small">{currentTrack.releaseTitle}</Text>
          </Link>
        </div>
        <div className="h-10 w-10" aria-hidden />
      </div>

      <div className="flex flex-1 min-h-0 flex-col gap-6 overflow-y-auto p-4 md:flex-row md:items-center md:justify-center md:gap-12 md:px-12">
        <div className="mx-auto aspect-square w-full max-w-md shrink-0 overflow-hidden rounded-2xl border-2 border-outline bg-surface-variant md:mx-0 md:max-w-[28rem] lg:max-w-[36rem]">
          {currentTrack.coverArtUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={currentTrack.coverArtUrl}
              alt=""
              className="h-full w-full object-cover"
            />
          ) : null}
        </div>

        <div className="flex w-full max-w-xl flex-col gap-6 md:max-w-md">
          <div className="flex flex-col gap-1 text-center md:text-left">
            <Text variant="headline-small">{currentTrack.title}</Text>
            <Link href={`/artist/${currentTrack.artistId}`}>
              <Text variant="body-medium" className="text-on-surface-variant">
                {currentTrack.artistName}
              </Text>
            </Link>
          </div>

          <div className="flex flex-col gap-2">
            <Slider
              value={displayMs}
              min={0}
              max={max}
              step={1}
              onChange={(next) => setScrubMs(next)}
              onScrubStart={() => {
                beginScrub();
                setScrubMs(displayMs);
              }}
              onScrubEnd={(final) => {
                setScrubMs(null);
                endScrub(final);
              }}
              label="Seek within current track"
            />
            <div className="flex justify-between text-on-surface-variant tabular-nums">
              <Text variant="label-small">{formatDuration(displayMs)}</Text>
              <Text variant="label-small">{formatDuration(state.durationMs)}</Text>
            </div>
          </div>

          <div className="flex items-center justify-around md:justify-start md:gap-2">
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
              label={`Repeat ${state.repeat}`}
              variant={state.repeat !== "off" ? "tonal" : "ghost"}
              onClick={() => setRepeat(nextRepeat)}
            >
              <RepeatIcon />
            </IconButton>
          </div>

          <section className="flex flex-col gap-2">
            <Text variant="title-small" className="text-on-surface-variant">
              Up next
            </Text>
            <ol className="flex flex-col">
              {state.queue.slice(state.currentIndex + 1).map((track) => (
                <li
                  key={track.id}
                  className="flex items-center justify-between border-b border-outline/50 py-2"
                >
                  <div className="flex min-w-0 flex-col">
                    <Text variant="body-medium" className="truncate">
                      {track.title}
                    </Text>
                    <Text variant="label-small" className="truncate text-on-surface-variant">
                      {track.artistName}
                    </Text>
                  </div>
                  <Text variant="label-small" className="text-on-surface-variant">
                    {formatDuration(track.durationMs)}
                  </Text>
                </li>
              ))}
              {state.queue.length - state.currentIndex - 1 === 0 && (
                <Text variant="body-small" className="py-2 text-on-surface-variant">
                  End of queue.
                </Text>
              )}
            </ol>
          </section>
        </div>
      </div>
    </div>
  );
}
