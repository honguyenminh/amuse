"use client";

import { AppShell } from "@/components/ui/AppShell";
import { Card } from "@/components/ui/Card";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { browseCatalogHome } from "@/lib/api/catalogClient";
import type {
  ArtistSummary,
  BrowseHomeResponse,
  ReleaseSummary,
  ReleaseType,
} from "@/lib/api/types";
import Link from "next/link";
import { useEffect, useState } from "react";

const releaseTypeLabel: Record<ReleaseType, string> = {
  single: "Single",
  ep: "EP",
  album: "Album",
  compilation: "Compilation",
};

export default function HomePage() {
  const [data, setData] = useState<BrowseHomeResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    browseCatalogHome()
      .then((response) => {
        if (!cancelled) setData(response);
      })
      .catch((err: Error) => {
        if (!cancelled) setError(err.message);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <AppShell title="Home" activePath="/home">
      <div className="mx-auto flex w-full max-w-7xl flex-col gap-8 p-4 md:p-6">
        {loading && (
          <section className="flex flex-col gap-3">
            <Skeleton className="h-7 w-48" />
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
              {Array.from({ length: 8 }, (_, i) => (
                <Card key={i}>
                  <Skeleton className="aspect-square w-full rounded-md" />
                  <Skeleton className="mt-2 h-5 w-3/4" />
                  <Skeleton className="mt-1 h-4 w-1/2" />
                </Card>
              ))}
            </div>
          </section>
        )}
        {error && (
          <Card>
            <Text variant="title-large">Could not load catalog</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        )}

        {data && (
          <>
            <section className="flex flex-col gap-3">
              <Text variant="title-large">Recent releases</Text>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
                {data.recentReleases.map((release) => (
                  <ReleaseTile key={release.id} release={release} />
                ))}
              </div>
            </section>

            <section className="flex flex-col gap-3">
              <Text variant="title-large">Featured artists</Text>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
                {data.featuredArtists.map((artist) => (
                  <ArtistTile key={artist.id} artist={artist} />
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
            {release.artistName} · {releaseTypeLabel[release.releaseType]}
          </Text>
        </div>
      </Card>
    </Link>
  );
}

function ArtistTile({ artist }: { artist: ArtistSummary }) {
  return (
    <Link href={`/artist/${artist.id}`} className="group block">
      <Card>
        <div className="flex flex-col gap-2">
          {artist.avatarUrl && (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={artist.avatarUrl}
              alt={artist.name}
              className="aspect-square w-full rounded-full object-cover"
            />
          )}
          <Text variant="title-medium" className="truncate text-center">
            {artist.name}
          </Text>
        </div>
      </Card>
    </Link>
  );
}
