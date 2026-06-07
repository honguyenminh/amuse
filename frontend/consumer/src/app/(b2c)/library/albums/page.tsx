"use client";

import { ReleaseTile } from "@/components/playback/ReleaseTile";
import { Card } from "@/components/ui/Card";
import { LibraryCardGrid, LibraryCardGridItem } from "@/components/ui/LibraryCardGrid";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { listLibraryReleases } from "@/lib/api/discoveryClient";
import type { SavedReleaseRowDto } from "@/lib/api/types";
import { useEffect, useState } from "react";

function toReleaseTileModel(release: SavedReleaseRowDto) {
  return {
    id: release.releaseId,
    slug: release.releaseSlug,
    title: release.title,
    artistSlug: release.artistSlug,
    coverArtUrl: release.coverArtUrl,
  };
}

export default function LibraryAlbumsPage() {
  const [releases, setReleases] = useState<SavedReleaseRowDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    listLibraryReleases()
      .then((response) => {
        if (!cancelled) setReleases(response.releases);
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
    <section className="flex flex-col gap-4">
      <Text variant="title-large">Saved albums</Text>

      {loading ? (
        <LibraryCardGrid>
          {Array.from({ length: 6 }, (_, i) => (
            <LibraryCardGridItem key={i}>
              <Skeleton className="aspect-square w-full rounded-md" />
            </LibraryCardGridItem>
          ))}
        </LibraryCardGrid>
      ) : null}

      {error ? (
        <Card>
          <Text variant="label-medium">{error}</Text>
        </Card>
      ) : null}

      {!loading && !error && releases.length === 0 ? (
        <Card>
          <Text variant="body-medium" className="text-on-surface-variant">
            Releases you save will appear here.
          </Text>
        </Card>
      ) : null}

      {!loading && releases.length > 0 ? (
        <LibraryCardGrid>
          {releases.map((release) => (
            <LibraryCardGridItem key={release.releaseId}>
              <ReleaseTile
                release={toReleaseTileModel(release)}
                subtitle={release.artistName}
              />
            </LibraryCardGridItem>
          ))}
        </LibraryCardGrid>
      ) : null}
    </section>
  );
}
