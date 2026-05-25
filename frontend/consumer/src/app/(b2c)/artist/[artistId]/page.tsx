"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { getCatalogArtist } from "@/lib/api/catalogClient";
import type { AlbumSummary, GetArtistDetailResponse } from "@/lib/api/types";
import { usePageSeed } from "@/theme/ThemeProvider";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import Link from "next/link";
import { use, useEffect, useState } from "react";

export default function ArtistPage({
  params,
}: {
  params: Promise<{ artistId: string }>;
}) {
  const { artistId } = use(params);
  const [artist, setArtist] = useState<GetArtistDetailResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setError(null);
    setArtist(null);
    getCatalogArtist(artistId)
      .then((response) => {
        if (!cancelled) setArtist(response);
      })
      .catch((err: Error) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, [artistId]);

  const seed = useCoverArtSeed(artist?.coverUrl ?? artist?.avatarUrl);
  usePageSeed(seed);

  return (
    <AppShell title={artist?.name ?? "Artist"} activePath="/artist">
      <div className="flex flex-col gap-4 p-4">
        {error && (
          <Card>
            <Text variant="title-large">Could not load artist</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        )}
        {!artist && !error && <Text variant="body-medium">Loading…</Text>}
        {artist && (
          <>
            <Card>
              <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
                {artist.avatarUrl && (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={artist.avatarUrl}
                    alt={artist.name}
                    className="aspect-square w-32 rounded-full object-cover"
                  />
                )}
                <div className="flex flex-col gap-1">
                  <Text variant="headline-medium">{artist.name}</Text>
                  {artist.bio && (
                    <Text variant="body-medium">{artist.bio}</Text>
                  )}
                </div>
              </div>
            </Card>

            <section className="flex flex-col gap-3">
              <Text variant="title-large">Discography</Text>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
                {artist.albums.map((album) => (
                  <AlbumTile key={album.id} album={album} />
                ))}
              </div>
            </section>
          </>
        )}
      </div>
    </AppShell>
  );
}

function AlbumTile({ album }: { album: AlbumSummary }) {
  return (
    <Link href={`/album/${album.id}`} className="group block">
      <Card>
        <div className="flex flex-col gap-2">
          {album.coverArtUrl && (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={album.coverArtUrl}
              alt={album.title}
              className="aspect-square w-full rounded-md object-cover"
            />
          )}
          <Text variant="title-medium">{album.title}</Text>
          <Text variant="label-medium">
            {album.releaseType} · {new Date(album.releaseDate).getFullYear()}
          </Text>
        </div>
      </Card>
    </Link>
  );
}
