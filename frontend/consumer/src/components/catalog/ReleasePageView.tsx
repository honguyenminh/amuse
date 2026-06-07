"use client";

import { CollapsibleFormattedText } from "@/components/catalog/CollapsibleFormattedText";
import { AddToPlaylistButton } from "@/components/discovery/AddToPlaylistButton";
import { SaveToLibraryButton } from "@/components/discovery/SaveToLibraryButton";
import { AppShell } from "@/components/ui/AppShell";
import { PageContent } from "@/components/ui/PageContent";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { IconButton } from "@/components/ui/IconButton";
import { OverflowMenuButton } from "@/components/ui/OverflowMenuButton";
import { PauseIcon, PlayIcon } from "@/components/ui/PlaybackIcons";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import type { GetReleaseDetailResponse, ReleaseType, TrackResponse } from "@/lib/api/types";
import {
  catalogArtistPath,
  catalogReleasePathFromEdition,
} from "@/lib/catalog/paths";
import { cn } from "@/lib/cn";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { formatDuration } from "@/lib/playback/formatDuration";
import { toPlaybackTrack, playableTracksFromRelease } from "@/lib/playback/toPlaybackTrack";
import {
  usePlayableClick,
  useReleasePlayableClick,
} from "@/lib/playback/useAltClickAddToQueue";
import {
  useReleaseContextMenu,
  useTrackContextMenu,
} from "@/lib/playback/usePlaybackContextMenuHandlers";
import { usePageSeed } from "@/theme/ThemeProvider";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";

const releaseTypeLabel: Record<ReleaseType, string> = {
  single: "Single",
  ep: "EP",
  album: "Album",
  compilation: "Compilation",
};

type ReleasePageViewProps = {
  loadKey: string;
  load: () => Promise<GetReleaseDetailResponse>;
};

export function ReleasePageView({ loadKey, load }: ReleasePageViewProps) {
  const searchParams = useSearchParams();
  const titleHint = searchParams.get("title") ?? undefined;
  const [release, setRelease] = useState<GetReleaseDetailResponse | null>(null);
  const [resolvedKey, setResolvedKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setError(null);
    load()
      .then((response) => {
        if (!cancelled) {
          setRelease(response);
          setResolvedKey(loadKey);
        }
      })
      .catch((err: Error) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, [loadKey, load]);

  const pending = resolvedKey !== loadKey;
  const { state, currentTrack, playQueue, toggle } = usePlayback();

  const seed = useCoverArtSeed(release?.coverArtUrl);
  usePageSeed(seed);

  const playableTracks = useMemo(
    () => (release ? playableTracksFromRelease(release) : []),
    [release],
  );

  const onReleaseContextMenu = useReleaseContextMenu(release?.id ?? "");

  const playFromTrack = useCallback(
    (trackId: string) => {
      if (!release || playableTracks.length === 0) return;
      const idx = playableTracks.findIndex((t) => t.id === trackId);
      if (idx < 0) return;
      playQueue(playableTracks, idx);
    },
    [release, playableTracks, playQueue],
  );

  const playAll = useCallback(() => {
    if (playableTracks.length > 0) playQueue(playableTracks, 0);
  }, [playableTracks, playQueue]);

  const isPlayingThisRelease =
    currentTrack !== null && release !== null && currentTrack.releaseId === release.id;

  const { onClick: onReleaseAltClick, queueAddPulsing: releaseQueuePulsing } =
    useReleasePlayableClick({
      releaseId: release?.id ?? "",
      releaseTitle: release?.title ?? "",
      tracks: playableTracks,
    });
  const { onClick: onPlayAllClick } = usePlayableClick({
    tracks: playableTracks,
    hasAudio: playableTracks.length > 0,
    releaseTitle: release?.title,
    onDefaultClick: () => {
      if (isPlayingThisRelease) toggle();
      else playAll();
    },
  });

  const chromeTitle =
    !pending && release ? release.title : (titleHint ?? undefined);

  return (
    <AppShell title={chromeTitle} activePath="/release">
      <PageContent gap="4">
        {error && (
          <Card>
            <Text variant="title-large">Could not load release</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        )}
        {(pending || !release) && !error ? <ReleaseSkeleton /> : null}
        {!pending && release && (
          <>
            <Card onContextMenu={onReleaseContextMenu}>
              <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
                {release.coverArtUrl ? (
                  <div
                    onClick={onReleaseAltClick}
                    className={cn(
                      "relative shrink-0 rounded-md",
                      releaseQueuePulsing && "queue-add-pulse",
                    )}
                  >
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={release.coverArtUrl}
                      alt={release.title}
                      className="aspect-square w-40 rounded-md object-cover"
                    />
                  </div>
                ) : null}
                <div className="flex flex-col gap-1">
                  <Text variant="label-medium" className="text-on-surface-variant">
                    {releaseTypeLabel[release.releaseType]}
                  </Text>
                  <Text variant="headline-medium">{release.title}</Text>
                  <Link href={catalogArtistPath(release.artistSlug)} className="underline">
                    <Text variant="title-medium">{release.artistName}</Text>
                  </Link>
                  <Text variant="label-medium">
                    {new Date(release.releaseDate).getFullYear()} ·{" "}
                    {release.tracks.length} track{release.tracks.length === 1 ? "" : "s"}
                  </Text>
                  <div className="mt-2 flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="primary"
                      onClick={onPlayAllClick}
                      disabled={playableTracks.length === 0}
                    >
                      {isPlayingThisRelease && state.isPlaying ? "Pause" : "Play"}
                    </Button>
                    <AddToPlaylistButton
                      trackIds={playableTracks.map((track) => track.id)}
                      disabled={playableTracks.length === 0}
                    />
                    <SaveToLibraryButton releaseId={release.id} />
                  </div>
                </div>
              </div>
            </Card>

            {release.description ? (
              <Card>
                <Text variant="title-large">About</Text>
                <CollapsibleFormattedText
                  text={release.description}
                  className="text-on-surface-variant"
                />
              </Card>
            ) : null}

            {release.otherEditions.length > 0 ? (
              <Card>
                <Text variant="title-large">
                  {release.releaseGroupTitle
                    ? `Other editions of ${release.releaseGroupTitle}`
                    : "Other editions"}
                </Text>
                <ul className="mt-2 flex flex-col divide-y divide-outline/40">
                  {release.otherEditions.map((edition) => (
                    <li key={edition.id}>
                      <Link
                        href={catalogReleasePathFromEdition(release.artistSlug, edition)}
                        className="flex items-center gap-3 py-3 transition-colors hover:text-primary"
                      >
                        {edition.coverArtUrl ? (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img
                            src={edition.coverArtUrl}
                            alt={edition.title}
                            className="size-12 rounded object-cover"
                          />
                        ) : (
                          <div className="size-12 rounded bg-surface-container-high" />
                        )}
                        <div className="min-w-0 flex-1">
                          <Text variant="body-medium" className="truncate">
                            {edition.title}
                          </Text>
                          <Text variant="label-medium" className="text-on-surface-variant">
                            {releaseTypeLabel[edition.releaseType]} ·{" "}
                            {new Date(edition.releaseDate).getFullYear()}
                          </Text>
                        </div>
                      </Link>
                    </li>
                  ))}
                </ul>
              </Card>
            ) : null}

            <Card>
              <Text variant="title-large">Tracks</Text>
              <ol className="mt-2 flex flex-col divide-y divide-outline/40">
                {release.tracks.map((track) => {
                  const isCurrent = currentTrack?.id === track.id;
                  return (
                    <TrackRow
                      key={track.id}
                      track={track}
                      release={release}
                      isCurrent={isCurrent}
                      isPlaying={state.isPlaying}
                      onPlay={() => playFromTrack(track.id)}
                      onToggle={toggle}
                    />
                  );
                })}
              </ol>
            </Card>
          </>
        )}
      </PageContent>
    </AppShell>
  );
}

function TrackRow({
  track,
  release,
  isCurrent,
  isPlaying,
  onPlay,
  onToggle,
}: {
  track: TrackResponse;
  release: GetReleaseDetailResponse;
  isCurrent: boolean;
  isPlaying: boolean;
  onPlay: () => void;
  onToggle: () => void;
}) {
  const playbackTrack = toPlaybackTrack(track, release);
  const { onContextMenu, openMenuAt } = useTrackContextMenu(playbackTrack, track.hasAudio);
  const { onClick, queueAddPulsing } = usePlayableClick({
    tracks: [playbackTrack],
    hasAudio: track.hasAudio,
    onDefaultClick: onPlay,
  });

  return (
    <li
      className={cn(
        "group relative flex items-center justify-between gap-3 py-2",
        isCurrent && "text-primary",
        queueAddPulsing && "queue-add-pulse",
      )}
      onContextMenu={onContextMenu}
    >
      <div className="flex w-8 shrink-0 items-center justify-center">
        {isCurrent ? (
          <IconButton
            label={isPlaying ? "Pause" : "Play"}
            variant="tonal"
            size="sm"
            onClick={onToggle}
          >
            {isPlaying ? <PauseIcon /> : <PlayIcon />}
          </IconButton>
        ) : (
          <span className="w-full text-center tabular-nums opacity-70">
            {track.trackNumber}
          </span>
        )}
      </div>
      <button
        type="button"
        onClick={onClick}
        disabled={!track.hasAudio}
        className="flex min-w-0 flex-1 flex-col text-left disabled:cursor-not-allowed disabled:opacity-50"
      >
        <Text variant="body-medium" className="truncate">
          {track.title}
        </Text>
        <Text variant="label-medium" className="truncate text-on-surface-variant">
          {release.artistName}
        </Text>
      </button>
      <div className="flex items-center gap-2">
        <OverflowMenuButton
          label="Track options"
          className="opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100"
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();
            openMenuAt(event.clientX, event.clientY);
          }}
        />
        <Text variant="label-medium">{formatDuration(track.durationMs)}</Text>
      </div>
    </li>
  );
}

function ReleaseSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <Card>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
          <Skeleton className="aspect-square w-40 rounded-md" />
          <div className="flex flex-1 flex-col gap-2">
            <Skeleton className="h-4 w-16" />
            <Skeleton className="h-8 w-2/3" />
            <Skeleton className="h-5 w-1/2" />
            <Skeleton className="h-4 w-1/3" />
          </div>
        </div>
      </Card>
      <Card>
        <Skeleton className="mb-3 h-6 w-24" />
        <div className="flex flex-col gap-2">
          {Array.from({ length: 5 }, (_, i) => (
            <Skeleton key={i} className="h-6 w-full" />
          ))}
        </div>
      </Card>
    </div>
  );
}
