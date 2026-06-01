"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { IconButton } from "@/components/ui/IconButton";
import { PauseIcon, PlayIcon } from "@/components/ui/PlaybackIcons";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { getCatalogRelease } from "@/lib/api/catalogClient";
import type { GetReleaseDetailResponse, ReleaseType, TrackResponse } from "@/lib/api/types";
import { cn } from "@/lib/cn";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { formatDuration } from "@/lib/playback/formatDuration";
import type { PlaybackTrack } from "@/lib/playback/types";
import { usePageSeed } from "@/theme/ThemeProvider";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import Link from "next/link";
import { use, useCallback, useEffect, useMemo, useState } from "react";

const releaseTypeLabel: Record<ReleaseType, string> = {
  single: "Single",
  ep: "EP",
  album: "Album",
  compilation: "Compilation",
};

export default function ReleasePage({
  params,
}: {
  params: Promise<{ releaseId: string }>;
}) {
  const { releaseId } = use(params);
  const [release, setRelease] = useState<GetReleaseDetailResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { state, currentTrack, playQueue, toggle } = usePlayback();

  useEffect(() => {
    let cancelled = false;
    setError(null);
    setRelease(null);
    getCatalogRelease(releaseId)
      .then((response) => {
        if (!cancelled) setRelease(response);
      })
      .catch((err: Error) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, [releaseId]);

  const seed = useCoverArtSeed(release?.coverArtUrl);
  usePageSeed(seed);

  const playableTracks = useMemo<PlaybackTrack[]>(
    () =>
      release
        ? release.tracks
            .filter((t) => t.hasAudio)
            .map((t) => toPlaybackTrack(t, release))
        : [],
    [release],
  );

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

  // Chrome title uses the release_type discriminator so singles say "Single",
  // EPs say "EP", etc., rather than the catch-all "Album".
  const chromeTitle = release
    ? release.title
    : "Release";

  return (
    <AppShell title={chromeTitle} activePath="/release">
      <div className="flex flex-col gap-4 p-4">
        {error && (
          <Card>
            <Text variant="title-large">Could not load release</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        )}
        {!release && !error && <ReleaseSkeleton />}
        {release && (
          <>
            <Card>
              <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
                {release.coverArtUrl && (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={release.coverArtUrl}
                    alt={release.title}
                    className="aspect-square w-40 rounded-md object-cover"
                  />
                )}
                <div className="flex flex-col gap-1">
                  <Text variant="label-medium" className="text-on-surface-variant">
                    {releaseTypeLabel[release.releaseType]}
                  </Text>
                  <Text variant="headline-medium">{release.title}</Text>
                  <Link href={`/artist/${release.artistId}`} className="underline">
                    <Text variant="title-medium">{release.artistName}</Text>
                  </Link>
                  <Text variant="label-medium">
                    {new Date(release.releaseDate).getFullYear()} ·{" "}
                    {release.tracks.length} track{release.tracks.length === 1 ? "" : "s"}
                  </Text>
                  <div className="mt-2 flex gap-2">
                    <Button
                      type="button"
                      onClick={isPlayingThisRelease ? toggle : playAll}
                      disabled={playableTracks.length === 0}
                    >
                      {isPlayingThisRelease && state.isPlaying ? "Pause" : "Play"}
                    </Button>
                  </div>
                </div>
              </div>
            </Card>

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
                        href={`/release/${edition.id}`}
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
                    <li
                      key={track.id}
                      className={cn(
                        "flex items-center justify-between gap-3 py-2",
                        isCurrent && "text-primary",
                      )}
                    >
                      <button
                        type="button"
                        onClick={() => playFromTrack(track.id)}
                        disabled={!track.hasAudio}
                        className="flex flex-1 items-center gap-3 text-left disabled:cursor-not-allowed disabled:opacity-50"
                      >
                        <span className="w-6 text-right tabular-nums opacity-70">
                          {track.trackNumber}
                        </span>
                        <Text variant="body-medium" className="truncate">
                          {track.title}
                        </Text>
                      </button>
                      <div className="flex items-center gap-3">
                        <Text variant="label-medium">
                          {formatDuration(track.durationMs)}
                        </Text>
                        {isCurrent && (
                          <IconButton
                            label={state.isPlaying ? "Pause" : "Play"}
                            variant="tonal"
                            size="sm"
                            onClick={toggle}
                          >
                            {state.isPlaying ? <PauseIcon /> : <PlayIcon />}
                          </IconButton>
                        )}
                      </div>
                    </li>
                  );
                })}
              </ol>
            </Card>
          </>
        )}
      </div>
    </AppShell>
  );
}

function toPlaybackTrack(
  track: TrackResponse,
  release: GetReleaseDetailResponse,
): PlaybackTrack {
  return {
    id: track.id,
    title: track.title,
    trackNumber: track.trackNumber,
    durationMs: track.durationMs,
    artistId: release.artistId,
    artistName: release.artistName,
    releaseId: release.id,
    releaseTitle: release.title,
    coverArtUrl: release.coverArtUrl,
  };
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
