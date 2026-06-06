"use client";

import { FormattedCatalogText } from "@amuse/catalog-text";
import { AppShell } from "@/components/ui/AppShell";
import { PageContent } from "@/components/ui/PageContent";
import { Card } from "@/components/ui/Card";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { ReleaseTile } from "@/components/playback/ReleaseTile";
import { getCatalogArtist } from "@/lib/api/catalogClient";
import type { GetArtistDetailResponse, ReleaseType } from "@/lib/api/types";
import { usePageSeed } from "@/theme/ThemeProvider";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
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
  params: Promise<{ artistKey: string }>;
}) {
  const { artistKey } = use(params);
  const [artist, setArtist] = useState<GetArtistDetailResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setError(null);
    setArtist(null);
    getCatalogArtist(artistKey)
      .then((response) => {
        if (!cancelled) setArtist(response);
      })
      .catch((err: Error) => {
        if (!cancelled) setError(err.message);
      });
    return () => {
      cancelled = true;
    };
  }, [artistKey]);

  const seed = useCoverArtSeed(artist?.coverUrl ?? artist?.avatarUrl);
  usePageSeed(seed);

  return (
    <AppShell title={artist?.name ?? "Artist"} activePath="/artist">
      <PageContent gap="4">
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
                {artist.avatarUrl ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={artist.avatarUrl}
                    alt={artist.name}
                    className="aspect-square w-32 rounded-full object-cover"
                  />
                ) : (
                  <div
                    className="grid aspect-square w-32 place-items-center rounded-full bg-surface-container-high text-3xl font-semibold text-on-surface-variant"
                    aria-hidden
                  >
                    {artist.name.trim().charAt(0).toUpperCase() || "?"}
                  </div>
                )}
                <div className="flex flex-col gap-1">
                  <Text variant="headline-medium">{artist.name}</Text>
                  {artist.bio && (
                    <FormattedCatalogText
                      text={artist.bio}
                      className="text-on-surface-variant"
                    />
                  )}
                </div>
              </div>
            </Card>

            <section className="flex flex-col gap-3">
              <Text variant="title-large">Discography</Text>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
                {artist.releases.map((release) => (
                  <ReleaseTile
                    key={release.id}
                    release={release}
                    subtitle={`${releaseTypeLabel[release.releaseType]} · ${new Date(release.releaseDate).getFullYear()}`}
                  />
                ))}
              </div>
            </section>
          </>
        )}
      </PageContent>
    </AppShell>
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
