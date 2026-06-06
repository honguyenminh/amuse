"use client";

import { getCatalogRelease } from "@/lib/api/catalogClient";
import { useCallback } from "react";
import { usePlayback } from "./PlaybackContext";
import type { PlaybackContextMenuItem } from "./PlaybackContextMenuProvider";
import { usePlaybackContextMenu } from "./PlaybackContextMenuProvider";
import { playableTracksFromRelease } from "./toPlaybackTrack";
import type { PlaybackTrack } from "./types";

function trackContextMenuItems(
  track: PlaybackTrack,
  hasAudio: boolean,
  addToQueue: (tracks: PlaybackTrack[]) => void,
  playNext: (tracks: PlaybackTrack[]) => void,
): PlaybackContextMenuItem[] {
  return [
    {
      id: "add-to-queue",
      label: "Add to queue",
      disabled: !hasAudio,
      onSelect: () => addToQueue([track]),
    },
    {
      id: "play-next",
      label: "Play next",
      disabled: !hasAudio,
      onSelect: () => playNext([track]),
    },
  ];
}

const loadingItems: PlaybackContextMenuItem[] = [
  {
    id: "loading",
    label: "Loading…",
    disabled: true,
    onSelect: () => {},
  },
];

export function useTrackContextMenu(track: PlaybackTrack, hasAudio: boolean) {
  const { addToQueue, playNext } = usePlayback();
  const { openAt } = usePlaybackContextMenu();

  return useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      openAt(
        e.clientX,
        e.clientY,
        trackContextMenuItems(track, hasAudio, addToQueue, playNext),
      );
    },
    [track, hasAudio, addToQueue, playNext, openAt],
  );
}

export function useReleaseContextMenu(releaseId: string) {
  const { addToQueue, playNext } = usePlayback();
  const { openAt } = usePlaybackContextMenu();

  return useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      const x = e.clientX;
      const y = e.clientY;
      openAt(x, y, loadingItems);

      void getCatalogRelease(releaseId)
        .then((release) => {
          const tracks = playableTracksFromRelease(release);
          const disabled = tracks.length === 0;
          openAt(x, y, [
            {
              id: "add-to-queue",
              label: "Add to queue",
              disabled,
              onSelect: () => addToQueue(tracks),
            },
            {
              id: "play-next",
              label: "Play next",
              disabled,
              onSelect: () => playNext(tracks),
            },
          ]);
        })
        .catch(() => {
          openAt(x, y, [
            {
              id: "error",
              label: "Could not load release",
              disabled: true,
              onSelect: () => {},
            },
          ]);
        });
    },
    [releaseId, addToQueue, playNext, openAt],
  );
}
