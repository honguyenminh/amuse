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
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import { usePageSeed } from "@/theme/ThemeProvider";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

export default function PlayingPage() {
  const router = useRouter();
  const {
    state,
    currentTrack,
    toggle,
    next,
    previous,
    seek,
    setRepeat,
    toggleShuffle,
  } = usePlayback();

  // Theme: this view IS the playing seed, so route the cover into pageSeed for
  // identical resolution to the rest of the app (page > playing > default).
  const coverSeed = useCoverArtSeed(currentTrack?.coverArtUrl ?? null);
  usePageSeed(coverSeed);

  // Empty queue → push back to home so the screen doesn't dead-end.
  useEffect(() => {
    if (!currentTrack) router.replace("/home");
  }, [currentTrack, router]);

  if (!currentTrack) {
    return (
      <div className="flex flex-1 items-center justify-center p-6">
        <Text variant="body-medium">No track in the queue.</Text>
      </div>
    );
  }

  const nextRepeat = state.repeat === "off" ? "queue" : state.repeat === "queue" ? "one" : "off";

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">
      <div className="flex items-center justify-between">
        <IconButton label="Collapse" variant="ghost" onClick={() => router.back()}>
          <ChevronDownIcon />
        </IconButton>
        <div className="text-center">
          <Text variant="label-small" className="text-on-surface-variant">
            Playing from
          </Text>
          <Link href={`/album/${currentTrack.albumId}`} className="block">
            <Text variant="title-small">{currentTrack.albumTitle}</Text>
          </Link>
        </div>
        <div className="h-10 w-10" aria-hidden />
      </div>

      <div className="mx-auto aspect-square w-full max-w-md overflow-hidden rounded-2xl border-2 border-outline bg-surface-variant">
        {currentTrack.coverArtUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={currentTrack.coverArtUrl}
            alt=""
            className="h-full w-full object-cover"
          />
        ) : null}
      </div>

      <div className="flex flex-col gap-1 text-center">
        <Text variant="headline-small">{currentTrack.title}</Text>
        <Link href={`/artist/${currentTrack.artistId}`}>
          <Text variant="body-medium" className="text-on-surface-variant">
            {currentTrack.artistName}
          </Text>
        </Link>
      </div>

      <div className="flex flex-col gap-2">
        <Slider
          value={state.positionMs}
          min={0}
          max={Math.max(state.durationMs, 1)}
          step={1000}
          onChange={seek}
          label="Seek"
        />
        <div className="flex justify-between text-on-surface-variant">
          <Text variant="label-small">{formatDuration(state.positionMs)}</Text>
          <Text variant="label-small">{formatDuration(state.durationMs)}</Text>
        </div>
      </div>

      <div className="flex items-center justify-around">
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
          disabled={state.currentIndex >= state.queue.length - 1 && state.repeat !== "queue"}
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
  );
}
