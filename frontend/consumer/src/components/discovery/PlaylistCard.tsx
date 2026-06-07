"use client";

import { PlaylistCoverArt } from "@/components/discovery/PlaylistCoverArt";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import type { PlaylistSummaryDto } from "@/lib/api/types";
import { cn } from "@/lib/cn";
import { playlistPath } from "@/lib/discovery/paths";
import { usePlaylistPlayableClick } from "@/lib/playback/useAltClickAddToQueue";
import Link from "next/link";

type PlaylistCardProps = {
  playlist: PlaylistSummaryDto;
};

export function PlaylistCard({ playlist }: PlaylistCardProps) {
  const ownerLabel = playlist.owner?.displayName ?? "Unknown listener";
  const trackLabel = `${playlist.trackCount} track${playlist.trackCount === 1 ? "" : "s"}`;
  const relationLabel = playlist.isOwned
    ? "Yours"
    : playlist.isFollowed
      ? `Following · ${ownerLabel}`
      : playlist.isSaved
        ? `Saved · ${ownerLabel}`
        : ownerLabel;
  const subtitle = `${trackLabel} · ${relationLabel}`;
  const { onClick, queueAddPulsing } = usePlaylistPlayableClick({
    playlistId: playlist.id,
    playlistTitle: playlist.title,
  });

  return (
    <Link
      href={playlistPath(playlist.id)}
      className="group block"
      onClick={onClick}
    >
      <Card className={cn("relative", queueAddPulsing && "queue-add-pulse")}>
        <div className="flex flex-col gap-2">
          <PlaylistCoverArt coverArtUrls={playlist.coverArtUrls} variant="tile" />
          <Text variant="title-medium" className="truncate">
            {playlist.title}
          </Text>
          {playlist.kind !== "liked" && playlist.description ? (
            <Text
              variant="body-small"
              className="line-clamp-2 text-on-surface-variant"
            >
              {playlist.description}
            </Text>
          ) : null}
          <Text variant="label-medium" className="truncate text-on-surface-variant">
            {subtitle}
          </Text>
        </div>
      </Card>
    </Link>
  );
}
