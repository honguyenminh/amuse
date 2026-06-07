"use client";

import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import { cn } from "@/lib/cn";
import { catalogReleasePath } from "@/lib/catalog/paths";
import { useReleasePlayableClick } from "@/lib/playback/useAltClickAddToQueue";
import { useReleaseContextMenu } from "@/lib/playback/usePlaybackContextMenuHandlers";
import Link from "next/link";

export type ReleaseTileModel = {
  id: string;
  slug: string;
  title: string;
  artistSlug: string;
  coverArtUrl: string | null;
};

type ReleaseTileProps = {
  release: ReleaseTileModel;
  subtitle: string;
};

export function ReleaseTile({ release, subtitle }: ReleaseTileProps) {
  const onContextMenu = useReleaseContextMenu(release.id);
  const { onClick, queueAddPulsing } = useReleasePlayableClick({
    releaseId: release.id,
    releaseTitle: release.title,
  });

  return (
    <Link
      href={catalogReleasePath(release.artistSlug, release.slug)}
      className="group block"
      onClick={onClick}
      onContextMenu={onContextMenu}
    >
      <Card className={cn("relative", queueAddPulsing && "queue-add-pulse")}>
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
            {subtitle}
          </Text>
        </div>
      </Card>
    </Link>
  );
}
