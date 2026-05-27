"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Card } from "@/components/ui/Card";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { getCatalogArtist } from "@/lib/api/catalogClient";
import type {
  GetArtistDetailResponse,
  ReleaseSummary,
  ReleaseType,
} from "@/lib/api/types";
import { usePageSeed } from "@/theme/ThemeProvider";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import Link from "next/link";
import { use, useEffect, useState } from "react";

const releaseTypeLabel: Record<ReleaseType, string> = {
  single: "Single",
  ep: "EP",
  album: "Album",
  compilation: "Compilation",
};

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
      <div className="mx-auto flex w-full max-w-7xl flex-col gap-4 p-4 md:p-6">
        {error && (
          <Card>
            <Text variant="title-large">Could not load artist</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        )}
        {!artist && !error && <ArtistSkeleton />}
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
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
                {artist.releases.map((release) => (
                  <ReleaseTile key={release.id} release={release} />
                ))}
              </div>
            </section>
          </>
        )}
      </div>
    </AppShell>
  );
}

function ReleaseTile({ release }: { release: ReleaseSummary }) {
  return (
    <Link href={`/release/${release.id}`} className="group block">
      <Card>
        <div className="flex flex-col gap-2">
          {release.coverArtUrl && (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={release.coverArtUrl}
              alt={release.title}
              className="aspect-square w-full rounded-md object-cover"
            />
          )}
          <Text variant="title-medium" className="truncate">
            {release.title}
          </Text>
          <Text variant="label-medium" className="truncate text-on-surface-variant">
            {releaseTypeLabel[release.releaseType]} ·{" "}
            {new Date(release.releaseDate).getFullYear()}
          </Text>
        </div>
      </Card>
    </Link>
  );
}

function ArtistSkeleton() {
  return (
    <>
      <Card>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
          <Skeleton className="aspect-square w-32 rounded-full" />
          <div className="flex flex-1 flex-col gap-2">
            <Skeleton className="h-8 w-2/3" />
            <Skeleton className="h-4 w-full" />
          </div>
        </div>
      </Card>
      <section className="flex flex-col gap-3">
        <Skeleton className="h-6 w-32" />
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {Array.from({ length: 4 }, (_, i) => (
            <Card key={i}>
              <Skeleton className="aspect-square w-full rounded-md" />
              <Skeleton className="mt-2 h-5 w-3/4" />
            </Card>
          ))}
        </div>
      </section>
    </>
  );
}
