"use client";

import { TrackRow } from "@/components/discovery/TrackRow";
import { TrackDropIndicator } from "@/components/ui/TrackDropIndicator";
import type { PlaylistItemDto, PlayableTrackDto } from "@/lib/api/types";
import {
  INSERT_AFTER_LAST,
  useTrackDragReorder,
} from "@/lib/discovery/useTrackDragReorder";
import { fromPlayableTrackDto } from "@/lib/playback/toPlaybackTrack";
import { cn } from "@/lib/cn";
import { Fragment, useMemo } from "react";

type PlaylistTrackListProps = {
  items: PlaylistItemDto[];
  playableByTrackId: Map<string, PlayableTrackDto>;
  reorderMode: boolean;
  canEdit: boolean;
  isLikedMode: boolean;
  currentTrackId: string | null;
  isPlaying: boolean;
  onReorder: (draggedItemId: string, insertBeforeId: string) => void;
  onRemove: (item: PlaylistItemDto) => void;
  onPlayTrack: (trackId: string) => void;
  onToggle: () => void;
};

export function PlaylistTrackList({
  items,
  playableByTrackId,
  reorderMode,
  canEdit,
  isLikedMode,
  currentTrackId,
  isPlaying,
  onReorder,
  onRemove,
  onPlayTrack,
  onToggle,
}: PlaylistTrackListProps) {
  const sortedItems = useMemo(
    () => [...items].sort((a, b) => a.position - b.position),
    [items],
  );

  const dragEnabled = reorderMode && canEdit;
  const { activeId, insertBeforeId, getItemProps, isDragging } = useTrackDragReorder(
    dragEnabled,
    onReorder,
  );

  return (
    <ol
      className={cn(
        "flex flex-col divide-y divide-outline/40",
        isDragging && "select-none touch-none",
      )}
    >
      {sortedItems.map((item) => {
        const playable = playableByTrackId.get(item.trackId);
        const playbackTrack = playable
          ? fromPlayableTrackDto(playable)
          : {
              id: item.trackId,
              title: item.title,
              trackNumber: item.position,
              durationMs: item.durationMs,
              artistId: "",
              artistName: item.artistName,
              artistSlug: "",
              releaseId: item.releaseId,
              releaseTitle: item.releaseTitle,
              releaseSlug: "",
              coverArtUrl: item.coverArtUrl,
            };
        const isCurrent = currentTrackId === item.trackId;
        const itemProps = dragEnabled ? getItemProps(item.itemId) : undefined;
        const showDropLine =
          isDragging &&
          insertBeforeId === item.itemId &&
          activeId !== item.itemId;

        return (
          <Fragment key={item.itemId}>
            {showDropLine ? <TrackDropIndicator /> : null}
            <TrackRow
              track={{
                trackId: item.trackId,
                title: item.title,
                artistName: item.artistName,
                durationMs: item.durationMs,
                hasAudio: item.hasAudio,
                position: item.position,
              }}
              removeLabel={isLikedMode ? "Remove from liked" : "Remove from playlist"}
              playbackTrack={playbackTrack}
              isCurrent={isCurrent}
              isPlaying={isPlaying}
              isLiked={isLikedMode}
              showDragHandle={dragEnabled}
              isDragging={activeId === item.itemId}
              itemProps={itemProps}
              canRemove={canEdit}
              onRemove={() => onRemove(item)}
              onPlay={() => onPlayTrack(item.trackId)}
              onToggle={onToggle}
            />
          </Fragment>
        );
      })}
      {isDragging && insertBeforeId === INSERT_AFTER_LAST ? <TrackDropIndicator /> : null}
    </ol>
  );
}
