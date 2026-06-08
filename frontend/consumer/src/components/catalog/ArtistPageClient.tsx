"use client";

import { FormattedCatalogText } from "@amuse/catalog-text";
import { UnverifiedSellerBadge } from "@/components/catalog/UnverifiedSellerBadge";
import { AppShell } from "@/components/ui/AppShell";
import { PageContent } from "@/components/ui/PageContent";
import { Card } from "@/components/ui/Card";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { ReleaseTile } from "@/components/playback/ReleaseTile";
import { getCatalogArtist } from "@/lib/api/catalogClient";
import type { GetArtistDetailResponse, ReleaseType } from "@/lib/api/types";
import { useServerSyncedDetail } from "@/lib/react/useServerSyncedDetail";
import type { ColorSeed } from "@/theme/types";
import { usePageSeed } from "@/theme/ThemeProvider";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import { useCallback } from "react";

const releaseTypeLabel: Record<ReleaseType, string> = {
  single: "Single",
  ep: "EP",
  album: "Album",
  compilation: "Compilation",
};

type ArtistPageClientProps = {
  artistKey: string;
  initialArtist?: GetArtistDetailResponse;
  initialColorSeed?: ColorSeed | null;
};

export function ArtistPageClient({
  artistKey,
  initialArtist,
  initialColorSeed = null,
}: ArtistPageClientProps) {
  const fetchArtist = useCallback(
    () => getCatalogArtist(artistKey),
    [artistKey],
  );
  const { detail: artist, pending, error } = useServerSyncedDetail({
    routeKey: artistKey,
    initialDetail: initialArtist,
    fetchDetail: fetchArtist,
  });

  const seed = useCoverArtSeed(artist?.coverUrl ?? artist?.avatarUrl, {
    initialSeed: initialColorSeed,
  });
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
        {(pending || !artist) && !error && <ArtistSkeleton />}
        {!pending && artist && (
          <>
            {artist.coverUrl ? (
              <div className="overflow-hidden rounded-lg">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={artist.coverUrl}
                  alt=""
                  className="aspect-[3/1] w-full object-cover"
                />
              </div>
            ) : null}
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
                  <div className="flex flex-wrap items-center gap-2">
                    <Text variant="headline-medium">{artist.name}</Text>
                    <UnverifiedSellerBadge trustTier={artist.trustTier} />
                  </div>
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
