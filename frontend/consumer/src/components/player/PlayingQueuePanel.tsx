"use client";

import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { IconButton } from "@/components/ui/IconButton";
import { NowPlayingIcon } from "@/components/ui/PlaybackIcons";
import { Text } from "@/components/ui/Text";
import { useLikedTrackIds } from "@/lib/discovery/useLikedTrackIds";
import {
  computeReorderTargetIndex,
  INSERT_AFTER_LAST,
  TRACK_ITEM_ID_ATTR,
  useTrackDragReorder,
} from "@/lib/discovery/useTrackDragReorder";
import { cn } from "@/lib/cn";
import { TrackDropIndicator } from "@/components/ui/TrackDropIndicator";
import { formatDuration } from "@/lib/playback/formatDuration";
import { useTrackContextMenu } from "@/lib/playback/usePlaybackContextMenuHandlers";
import type { PlaybackTrack } from "@/lib/playback/types";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { useCallback, useMemo, useRef, useState, type PointerEvent as ReactPointerEvent, Fragment } from "react";

type PlayingQueuePanelProps = {
  expanded: boolean;
  onExpandedChange: (expanded: boolean) => void;
  className?: string;
};

type QueueRow = {
  track: PlaybackTrack;
  variant: "current" | "up-next";
  playOrderIndex: number;
};

export function PlayingQueuePanel({
  expanded,
  onExpandedChange,
  className,
}: PlayingQueuePanelProps) {
  const { state, clearQueue, jumpToPlayOrderIndex, reorderPlayOrder } = usePlayback();
  const { isLiked } = useLikedTrackIds();
  const [clearOpen, setClearOpen] = useState(false);
  const suppressRowClickRef = useRef(false);

  const { current, upNext, played, reorderableRows, remainingCount } = useMemo(() => {
    if (state.currentIndex < 0 || state.playOrderIndex < 0) {
      return {
        current: null,
        upNext: [] as PlaybackTrack[],
        played: [] as PlaybackTrack[],
        reorderableRows: [] as QueueRow[],
        remainingCount: 0,
      };
    }
    const currentTrack = state.queue[state.currentIndex] ?? null;
    const playedTracks = state.playOrder
      .slice(0, state.playOrderIndex)
      .map((idx) => state.queue[idx])
      .filter((track): track is PlaybackTrack => track !== undefined);
    const upNextTracks = state.playOrder
      .slice(state.playOrderIndex + 1)
      .map((idx) => state.queue[idx])
      .filter((track): track is PlaybackTrack => track !== undefined);

    const rows: QueueRow[] = [];
    if (currentTrack) {
      rows.push({
        track: currentTrack,
        variant: "current",
        playOrderIndex: state.playOrderIndex,
      });
    }
    upNextTracks.forEach((track, index) => {
      rows.push({
        track,
        variant: "up-next",
        playOrderIndex: state.playOrderIndex + 1 + index,
      });
    });

    return {
      current: currentTrack,
      upNext: upNextTracks,
      played: playedTracks,
      reorderableRows: rows,
      remainingCount:
        state.playOrderIndex >= 0
          ? Math.max(0, state.playOrder.length - state.playOrderIndex)
          : 0,
    };
  }, [state.currentIndex, state.playOrder, state.playOrderIndex, state.queue]);

  const dragEnabled = expanded && reorderableRows.length > 1;

  const handleReorder = useCallback(
    (draggedId: string, insertBeforeId: string) => {
      const fromPlayOrderIndex = Number.parseInt(draggedId, 10);
      if (!Number.isFinite(fromPlayOrderIndex)) return;

      const insertBeforePlayOrderIndex =
        insertBeforeId === INSERT_AFTER_LAST
          ? state.playOrder.length
          : Number.parseInt(insertBeforeId, 10);
      if (!Number.isFinite(insertBeforePlayOrderIndex)) return;

      const toPlayOrderIndex = computeReorderTargetIndex(
        fromPlayOrderIndex,
        insertBeforePlayOrderIndex,
      );
      if (toPlayOrderIndex === null) return;

      suppressRowClickRef.current = true;
      reorderPlayOrder(fromPlayOrderIndex, toPlayOrderIndex);
    },
    [reorderPlayOrder, state.playOrder.length],
  );

  const { activeId, insertBeforeId, getItemProps, isDragging } = useTrackDragReorder(
    dragEnabled,
    handleReorder,
  );

  const handleRowSelect = useCallback(
    (playOrderIndex: number) => {
      if (suppressRowClickRef.current) {
        suppressRowClickRef.current = false;
        return;
      }
      jumpToPlayOrderIndex(playOrderIndex);
    },
    [jumpToPlayOrderIndex],
  );

  return (
    <>
      <section
        className={cn(
          "flex min-h-0 flex-col gap-2",
          expanded && "flex-1",
          className,
        )}
      >
        <div className="flex items-center justify-between gap-2">
          <div className="min-w-0">
            <Text variant="title-small" className="text-on-surface-variant">
              Queue{remainingCount > 0 ? ` (${remainingCount})` : ""}
            </Text>
            {dragEnabled ? (
              <Text variant="label-small" className="text-on-surface-variant/80">
                Drag to reorder
              </Text>
            ) : null}
          </div>
          <div className="flex items-center gap-1">
            <IconButton
              label={expanded ? "Collapse queue" : "Expand queue"}
              variant="ghost"
              size="sm"
              onClick={() => onExpandedChange(!expanded)}
            >
              <ChevronIcon expanded={expanded} />
            </IconButton>
            <IconButton
              label="Clear queue"
              variant="ghost"
              size="sm"
              onClick={() => setClearOpen(true)}
            >
              <ClearIcon />
            </IconButton>
          </div>
        </div>

        <div
          className={cn(
            "flex min-h-0 flex-col overflow-y-auto rounded-lg border border-outline/50",
            expanded ? "flex-1" : "max-h-60",
          )}
        >
          {expanded ? (
            <ol className={cn("flex flex-col", isDragging && "select-none touch-none")}>
              {reorderableRows.map((row) => {
                const rowId = String(row.playOrderIndex);
                const itemProps = dragEnabled ? getItemProps(rowId) : undefined;
                const showDropLine =
                  isDragging && insertBeforeId === rowId && activeId !== rowId;

                return (
                  <Fragment key={`${row.playOrderIndex}-${row.track.id}`}>
                    {showDropLine ? <TrackDropIndicator /> : null}
                    <QueueTrackRow
                      track={row.track}
                      variant={row.variant}
                      isPlaying={state.isPlaying}
                      isLiked={isLiked(row.track.id)}
                      playOrderIndex={row.playOrderIndex}
                      dragEnabled={dragEnabled}
                      isDragging={activeId === rowId}
                      itemProps={itemProps}
                      onSelect={handleRowSelect}
                    />
                  </Fragment>
                );
              })}
              {isDragging && insertBeforeId === INSERT_AFTER_LAST ? (
                <TrackDropIndicator />
              ) : null}
              {reorderableRows.length === 0 && (
                <Text variant="body-small" className="px-3 py-2 text-on-surface-variant">
                  End of queue.
                </Text>
              )}
            </ol>
          ) : (
            <ol className="flex flex-col">
              {current ? (
                <QueueTrackRow
                  track={current}
                  variant="current"
                  isPlaying={state.isPlaying}
                  isLiked={isLiked(current.id)}
                  playOrderIndex={state.playOrderIndex}
                  onSelect={handleRowSelect}
                />
              ) : null}
              {upNext.map((track, index) => (
                <QueueTrackRow
                  key={track.id}
                  track={track}
                  variant="up-next"
                  isLiked={isLiked(track.id)}
                  playOrderIndex={state.playOrderIndex + 1 + index}
                  onSelect={handleRowSelect}
                />
              ))}
              {upNext.length === 0 && (
                <Text variant="body-small" className="px-3 py-2 text-on-surface-variant">
                  End of queue.
                </Text>
              )}
            </ol>
          )}
        </div>

        {played.length > 0 ? (
          <details className="group rounded-lg border border-outline/50">
            <summary className="cursor-pointer list-none px-3 py-2 marker:content-none">
              <Text variant="label-medium" className="text-on-surface-variant">
                Played ({played.length})
              </Text>
            </summary>
            <ol className="flex max-h-48 flex-col overflow-y-auto border-t border-outline/50">
              {played.map((track, index) => (
                <QueueTrackRow
                  key={`played-${track.id}-${track.trackNumber}`}
                  track={track}
                  variant="played"
                  isLiked={isLiked(track.id)}
                  playOrderIndex={index}
                  onSelect={handleRowSelect}
                />
              ))}
            </ol>
          </details>
        ) : null}
      </section>

      <ConfirmDialog
        open={clearOpen}
        title="Clear queue?"
        description="This removes all tracks from the queue, including played history. This cannot be undone."
        confirmLabel="Clear queue"
        cancelLabel="Cancel"
        destructive
        onClose={() => setClearOpen(false)}
        onConfirm={() => {
          clearQueue();
          setClearOpen(false);
        }}
      />
    </>
  );
}

type QueueTrackRowProps = {
  track: PlaybackTrack;
  variant: "current" | "up-next" | "played";
  isPlaying?: boolean;
  isLiked: boolean;
  playOrderIndex: number;
  dragEnabled?: boolean;
  isDragging?: boolean;
  itemProps?: Record<string, unknown>;
  onSelect: (playOrderIndex: number) => void;
};

function QueueTrackRow({
  track,
  variant,
  isPlaying = false,
  isLiked,
  playOrderIndex,
  dragEnabled = false,
  isDragging = false,
  itemProps,
  onSelect,
}: QueueTrackRowProps) {
  const { onContextMenu } = useTrackContextMenu(track, true, { isLiked, inQueue: true });

  const isCurrent = variant === "current";
  const currentTextTone = isCurrent
    ? isPlaying
      ? "text-on-primary-container"
      : "text-on-secondary-container"
    : null;

  const {
    onPointerDown,
    onPointerMove,
    onPointerUp,
    onPointerCancel,
    ...restItemProps
  } = (itemProps ?? {}) as {
    onPointerDown?: (event: ReactPointerEvent<HTMLDivElement>) => void;
    onPointerMove?: (event: ReactPointerEvent<HTMLDivElement>) => void;
    onPointerUp?: (event: ReactPointerEvent<HTMLDivElement>) => void;
    onPointerCancel?: () => void;
  };

  return (
    <li
      {...restItemProps}
      {...(dragEnabled ? { [TRACK_ITEM_ID_ATTR]: String(playOrderIndex) } : {})}
      className={cn(
        "flex items-center gap-2 border-b border-outline/50 px-3 py-2 last:border-b-0",
        !isCurrent && "hover:bg-surface-variant/50",
        isCurrent &&
          isPlaying &&
          "bg-primary-container text-on-primary-container hover:opacity-95",
        isCurrent &&
          !isPlaying &&
          "bg-secondary-container text-on-secondary-container hover:opacity-95",
        variant === "played" && "opacity-70",
        isDragging && "opacity-50",
      )}
      onContextMenu={onContextMenu}
    >
      {dragEnabled ? (
        <div
          className="flex w-4 shrink-0 cursor-grab items-center justify-center active:cursor-grabbing"
          aria-hidden
          onPointerDown={onPointerDown}
          onPointerMove={onPointerMove}
          onPointerUp={onPointerUp}
          onPointerCancel={onPointerCancel}
        >
          <DragHandleIcon />
        </div>
      ) : null}
      <button
        type="button"
        data-no-drag
        className="flex min-w-0 flex-1 cursor-pointer items-center justify-between gap-3 bg-transparent text-left"
        onClick={() => onSelect(playOrderIndex)}
      >
        <div className="flex min-w-0 flex-1 items-center gap-2">
          {isCurrent && isPlaying ? (
            <NowPlayingIcon className="size-4 shrink-0" />
          ) : null}
          <div className="flex min-w-0 flex-col">
            <Text variant="body-medium" className={cn("truncate", currentTextTone)}>
              {track.title}
            </Text>
            <Text
              variant="label-small"
              className={cn(
                "truncate",
                currentTextTone ?? "text-on-surface-variant",
              )}
            >
              {track.artistName}
            </Text>
          </div>
        </div>
        <Text
          variant="label-small"
          className={cn(
            "shrink-0 tabular-nums",
            currentTextTone ?? "text-on-surface-variant",
          )}
        >
          {formatDuration(track.durationMs)}
        </Text>
      </button>
    </li>
  );
}

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

function ChevronIcon({ expanded }: { expanded: boolean }) {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill="currentColor"
      aria-hidden
      className={cn("transition-transform", expanded && "rotate-180")}
    >
      <path d="M7.41 8.59 12 13.17l4.59-4.58L18 10l-6 6-6-6z" />
    </svg>
  );
}

function ClearIcon() {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" aria-hidden>
      <path d="M19 6.41 17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z" />
    </svg>
  );
}
