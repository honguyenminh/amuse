"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { IconButton } from "@/components/ui/IconButton";
import { PauseIcon, PlayIcon } from "@/components/ui/PlaybackIcons";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { getCatalogAlbum } from "@/lib/api/catalogClient";
import type { GetAlbumDetailResponse, TrackResponse } from "@/lib/api/types";
import { cn } from "@/lib/cn";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { formatDuration } from "@/lib/playback/formatDuration";
import type { PlaybackTrack } from "@/lib/playback/types";
import { usePageSeed } from "@/theme/ThemeProvider";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import Link from "next/link";
import { use, useCallback, useEffect, useMemo, useState } from "react";

export default function AlbumPage({
  params,
}: {
  params: Promise<{ albumId: string }>;
}) {
  const { albumId } = use(params);
  const [album, setAlbum] = useState<GetAlbumDetailResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { state, currentTrack, playQueue, toggle } = usePlayback();

  useEffect(() => {
    let cancelled = false;
    setError(null);
    setAlbum(null);
    getCatalogAlbum(albumId)
      .then((response) => {
        if (!cancelled) setAlbum(response);
      })
      .catch((err: Error) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, [albumId]);

  const seed = useCoverArtSeed(album?.coverArtUrl);
  usePageSeed(seed);

  const playableTracks = useMemo<PlaybackTrack[]>(
    () =>
      album
        ? album.tracks
            .filter((t) => t.hasAudio)
            .map((t) => toPlaybackTrack(t, album))
        : [],
    [album],
  );

  const playFromTrack = useCallback(
    (trackId: string) => {
      if (!album || playableTracks.length === 0) return;
      const idx = playableTracks.findIndex((t) => t.id === trackId);
      if (idx < 0) return;
      playQueue(playableTracks, idx);
    },
    [album, playableTracks, playQueue],
  );

  const playAll = useCallback(() => {
    if (playableTracks.length > 0) playQueue(playableTracks, 0);
  }, [playableTracks, playQueue]);

  const isPlayingThisAlbum =
    currentTrack !== null && album !== null && currentTrack.albumId === album.id;

  return (
    <AppShell title={album?.title ?? "Album"} activePath="/album">
      <div className="flex flex-col gap-4 p-4">
        {error && (
          <Card>
            <Text variant="title-large">Could not load album</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        )}
        {!album && !error && <AlbumSkeleton />}
        {album && (
          <>
            <Card>
              <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
                {album.coverArtUrl && (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={album.coverArtUrl}
                    alt={album.title}
                    className="aspect-square w-40 rounded-md object-cover"
                  />
                )}
                <div className="flex flex-col gap-1">
                  <Text variant="headline-medium">{album.title}</Text>
                  <Link href={`/artist/${album.artistId}`} className="underline">
                    <Text variant="title-medium">{album.artistName}</Text>
                  </Link>
                  <Text variant="label-medium">
                    {album.releaseType} ·{" "}
                    {new Date(album.releaseDate).getFullYear()}
                  </Text>
                  <div className="mt-2 flex gap-2">
                    <Button
                      type="button"
                      onClick={isPlayingThisAlbum ? toggle : playAll}
                      disabled={playableTracks.length === 0}
                    >
                      {isPlayingThisAlbum && state.isPlaying ? "Pause" : "Play album"}
                    </Button>
                  </div>
                </div>
              </div>
            </Card>

            <Card>
              <Text variant="title-large">Tracks</Text>
              <ol className="mt-2 flex flex-col divide-y divide-outline/40">
                {album.tracks.map((track) => {
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
  album: GetAlbumDetailResponse,
): PlaybackTrack {
  return {
    id: track.id,
    title: track.title,
    trackNumber: track.trackNumber,
    durationMs: track.durationMs,
    artistId: album.artistId,
    artistName: album.artistName,
    albumId: album.id,
    albumTitle: album.title,
    coverArtUrl: album.coverArtUrl,
  };
}

function AlbumSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <Card>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
          <Skeleton className="aspect-square w-40 rounded-md" />
          <div className="flex flex-1 flex-col gap-2">
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
