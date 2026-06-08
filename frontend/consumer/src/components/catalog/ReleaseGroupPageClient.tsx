"use client";

import { CollapsibleFormattedText } from "@/components/catalog/CollapsibleFormattedText";
import { AppShell } from "@/components/ui/AppShell";
import { PageContent } from "@/components/ui/PageContent";
import { Card } from "@/components/ui/Card";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { ReleaseTile } from "@/components/playback/ReleaseTile";
import { getCatalogReleaseGroupBySlugs } from "@/lib/api/catalogClient";
import type { GetReleaseGroupDetailResponse, ReleaseType } from "@/lib/api/types";
import { useServerSyncedDetail } from "@/lib/react/useServerSyncedDetail";
import type { ColorSeed } from "@/theme/types";
import { catalogArtistPath } from "@/lib/catalog/paths";
import { usePageSeed } from "@/theme/ThemeProvider";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import Link from "next/link";
import { useCallback } from "react";

const releaseTypeLabel: Record<ReleaseType, string> = {
  single: "Single",
  ep: "EP",
  album: "Album",
  compilation: "Compilation",
};

type ReleaseGroupPageClientProps = {
  artistKey: string;
  groupSlug: string;
  initialGroup?: GetReleaseGroupDetailResponse;
  initialColorSeed?: ColorSeed | null;
};

export function ReleaseGroupPageClient({
  artistKey,
  groupSlug,
  initialGroup,
  initialColorSeed = null,
}: ReleaseGroupPageClientProps) {
  const loadKey = `${artistKey}/${groupSlug}`;
  const fetchGroup = useCallback(
    () => getCatalogReleaseGroupBySlugs(artistKey, groupSlug),
    [artistKey, groupSlug],
  );
  const { detail: group, pending, error } = useServerSyncedDetail({
    routeKey: loadKey,
    initialDetail: initialGroup,
    fetchDetail: fetchGroup,
  });

  const coverUrl = group?.releases.find((edition) => edition.coverArtUrl)?.coverArtUrl ?? null;
  const seed = useCoverArtSeed(coverUrl, { initialSeed: initialColorSeed });
  usePageSeed(seed);

  return (
    <AppShell title={group?.title ?? "Release group"} activePath="/release">
      <PageContent gap="4">
        {error && (
          <Card>
            <Text variant="title-large">Could not load release group</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        )}
        {(pending || !group) && !error ? <ReleaseGroupSkeleton /> : null}
        {!pending && group && (
          <>
            <Card>
              <div className="flex flex-col gap-2">
                <Text variant="headline-medium">{group.title}</Text>
                <Link href={catalogArtistPath(group.artistSlug)} className="underline">
                  <Text variant="title-medium">{group.artistName}</Text>
                </Link>
                <Text variant="label-medium">
                  {group.releases.length} edition{group.releases.length === 1 ? "" : "s"}
                </Text>
              </div>
            </Card>

            {group.description ? (
              <Card>
                <Text variant="title-large">About</Text>
                <CollapsibleFormattedText
                  text={group.description}
                  className="text-on-surface-variant"
                />
              </Card>
            ) : null}

            <section className="flex flex-col gap-3">
              <Text variant="title-large">Editions</Text>
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
                {group.releases.map((release) => (
                  <ReleaseTile
                    key={release.id}
                    release={{
                      id: release.id,
                      slug: release.slug,
                      title: release.title,
                      artistSlug: group.artistSlug,
                      coverArtUrl: release.coverArtUrl,
                    }}
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

function ReleaseGroupSkeleton() {
  return (
    <>
      <Card>
        <div className="flex flex-col gap-2">
          <Skeleton className="h-8 w-2/3" />
          <Skeleton className="h-5 w-1/3" />
        </div>
      </Card>
      <section className="flex flex-col gap-3">
        <Skeleton className="h-6 w-24" />
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
