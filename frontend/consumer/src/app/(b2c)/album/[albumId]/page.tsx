"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { getCatalogAlbum } from "@/lib/api/catalogClient";
import type { GetAlbumDetailResponse } from "@/lib/api/types";
import { usePageSeed } from "@/theme/ThemeProvider";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import Link from "next/link";
import { use, useEffect, useState } from "react";

export default function AlbumPage({
  params,
}: {
  params: Promise<{ albumId: string }>;
}) {
  const { albumId } = use(params);
  const [album, setAlbum] = useState<GetAlbumDetailResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

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

  return (
    <AppShell title={album?.title ?? "Album"} activePath="/album">
      <div className="flex flex-col gap-4 p-4">
        {error && (
          <Card>
            <Text variant="title-large">Could not load album</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        )}
        {!album && !error && <Text variant="body-medium">Loading…</Text>}
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
                </div>
              </div>
            </Card>

            <Card>
              <Text variant="title-large">Tracks</Text>
              <ol className="mt-2 flex flex-col divide-y divide-outline/40">
                {album.tracks.map((track) => (
                  <li
                    key={track.id}
                    className="flex items-center justify-between py-2"
                  >
                    <div className="flex items-center gap-3">
                      <span className="w-6 text-right tabular-nums opacity-70">
                        {track.trackNumber}
                      </span>
                      <Text variant="body-medium">{track.title}</Text>
                    </div>
                    <Text variant="label-medium">
                      {formatDuration(track.durationMs)}
                    </Text>
                  </li>
                ))}
              </ol>
            </Card>
          </>
        )}
      </div>
    </AppShell>
  );
}

function formatDuration(ms: number): string {
  const total = Math.floor(ms / 1000);
  const minutes = Math.floor(total / 60);
  const seconds = total % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}
