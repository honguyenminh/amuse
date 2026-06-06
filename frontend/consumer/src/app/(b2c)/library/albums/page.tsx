"use client";

import { Card } from "@/components/ui/Card";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { listLibraryReleases } from "@/lib/api/discoveryClient";
import type { SavedReleaseRowDto } from "@/lib/api/types";
import { catalogReleasePath } from "@/lib/catalog/paths";
import Link from "next/link";
import { useEffect, useState } from "react";

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
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {Array.from({ length: 6 }, (_, i) => (
            <Skeleton key={i} className="aspect-square w-full rounded-md" />
          ))}
        </div>
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
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {releases.map((release) => (
            <Link
              key={release.releaseId}
              href={catalogReleasePath(release.artistSlug, release.releaseSlug)}
              className="group block"
            >
              <Card>
                <div className="flex flex-col gap-2">
                  {release.coverArtUrl ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img
                      src={release.coverArtUrl}
                      alt={release.title}
                      className="aspect-square w-full rounded-md object-cover"
                    />
                  ) : (
                    <div className="aspect-square w-full rounded-md bg-surface-container-high" />
                  )}
                  <Text variant="title-medium" className="truncate">
                    {release.title}
                  </Text>
                  <Text variant="label-medium" className="truncate text-on-surface-variant">
                    {release.artistName}
                  </Text>
                </div>
              </Card>
            </Link>
          ))}
        </div>
      ) : null}
    </section>
  );
}
