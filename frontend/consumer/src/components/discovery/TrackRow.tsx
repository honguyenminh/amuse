"use client";

import { IconButton } from "@/components/ui/IconButton";
import { OverflowMenuButton } from "@/components/ui/OverflowMenuButton";
import { PauseIcon, PlayIcon } from "@/components/ui/PlaybackIcons";
import { Text } from "@/components/ui/Text";
import { cn } from "@/lib/cn";
import { formatTrackSubtitle } from "@/lib/discovery/formatTrackSubtitle";
import { formatDuration } from "@/lib/playback/formatDuration";
import { usePlayableClick } from "@/lib/playback/useAltClickAddToQueue";
import { useTrackContextMenu } from "@/lib/playback/usePlaybackContextMenuHandlers";
import type { PlaybackTrack } from "@/lib/playback/types";
import type { HTMLAttributes } from "react";

export type TrackRowData = {
  trackId: string;
  title: string;
  artistName?: string;
  durationMs: number;
  hasAudio: boolean;
  position?: number;
};

type TrackRowProps = {
  track: TrackRowData;
  playbackTrack: PlaybackTrack;
  isCurrent: boolean;
  isPlaying: boolean;
  isLiked?: boolean;
  showDragHandle?: boolean;
  isDragging?: boolean;
  canRemove?: boolean;
  removeLabel?: string;
  removeReleaseLabel?: string;
  itemProps?: HTMLAttributes<HTMLLIElement>;
  onRemove?: () => void;
  onRemoveRelease?: () => void;
  onPlay: () => void;
  onToggle: () => void;
};

function DragHandleIcon() {
  return (
    <svg
      width="10"
      height="14"
      viewBox="0 0 10 14"
      fill="currentColor"
      className="text-on-surface-variant"
      aria-hidden
    >
      <circle cx="2.5" cy="2.5" r="1.25" />
      <circle cx="7.5" cy="2.5" r="1.25" />
      <circle cx="2.5" cy="7" r="1.25" />
      <circle cx="7.5" cy="7" r="1.25" />
      <circle cx="2.5" cy="11.5" r="1.25" />
      <circle cx="7.5" cy="11.5" r="1.25" />
    </svg>
  );
}

export function TrackRow({
  track,
  playbackTrack,
  isCurrent,
  isPlaying,
  isLiked,
  showDragHandle,
  isDragging,
  canRemove,
  removeLabel = "Remove from playlist",
  removeReleaseLabel,
  itemProps,
  onRemove,
  onRemoveRelease,
  onPlay,
  onToggle,
}: TrackRowProps) {
  const removeAction =
    canRemove && onRemove
      ? { label: removeLabel, onSelect: onRemove }
      : undefined;
  const removeReleaseAction =
    canRemove && onRemoveRelease && removeReleaseLabel
      ? { label: removeReleaseLabel, onSelect: onRemoveRelease }
      : undefined;

  const { onContextMenu, openMenuAt } = useTrackContextMenu(playbackTrack, track.hasAudio, {
    isLiked,
    remove: removeAction,
    removeRelease: removeReleaseAction,
  });
  const { onClick, queueAddPulsing } = usePlayableClick({
    tracks: [playbackTrack],
    hasAudio: track.hasAudio,
    onDefaultClick: onPlay,
  });

  const { className: itemClassName, ...restItemProps } = itemProps ?? {};

  return (
    <li
      {...restItemProps}
      className={cn(
        "group relative flex items-center justify-between gap-3 border-t border-transparent py-2 transition-colors",
        isCurrent && "text-primary",
        queueAddPulsing && "queue-add-pulse",
        isDragging && "opacity-50",
        showDragHandle && "cursor-grab active:cursor-grabbing",
        itemClassName,
      )}
      onContextMenu={onContextMenu}
    >
      {showDragHandle ? (
        <div className="flex w-4 shrink-0 items-center justify-center">
          <DragHandleIcon />
        </div>
      ) : null}
      <div className="flex w-8 shrink-0 items-center justify-center" data-no-drag>
        {isCurrent ? (
          <IconButton
            label={isPlaying ? "Pause" : "Play"}
            variant="tonal"
            size="sm"
            onClick={onToggle}
          >
            {isPlaying ? <PauseIcon /> : <PlayIcon />}
          </IconButton>
        ) : track.position !== undefined ? (
          <span className="w-full text-center tabular-nums opacity-70">{track.position}</span>
        ) : null}
      </div>
      <button
        type="button"
        data-no-drag
        onClick={onClick}
        disabled={!track.hasAudio}
        className="flex min-w-0 flex-1 flex-col text-left disabled:cursor-not-allowed disabled:opacity-50"
      >
        <Text variant="body-medium" className="truncate">
          {track.title}
        </Text>
        {(() => {
          const subtitle = formatTrackSubtitle(
            playbackTrack.artistName,
            playbackTrack.releaseTitle,
          );
          return subtitle ? (
            <Text variant="label-medium" className="truncate text-on-surface-variant">
              {subtitle}
            </Text>
          ) : null;
        })()}
      </button>
      <div className="flex items-center gap-2">
        <OverflowMenuButton
          label="Track options"
          data-no-drag
          className="opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100"
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();
            openMenuAt(event.clientX, event.clientY);
          }}
        />
        <Text variant="label-medium">{formatDuration(track.durationMs)}</Text>
      </div>
    </li>
  );
}
