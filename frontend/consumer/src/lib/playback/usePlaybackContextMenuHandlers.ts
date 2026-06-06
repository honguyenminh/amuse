"use client";

import { getCatalogRelease } from "@/lib/api/catalogClient";
import { likeTrack, listMyPlaylists, unlikeTrack } from "@/lib/api/discoveryClient";
import { ApiError } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import { useCallback } from "react";
import type { PlaybackContextMenuItem } from "./PlaybackContextMenuProvider";
import { usePlaybackContextMenu } from "./PlaybackContextMenuProvider";
import { addToLikedMenuItem } from "@/lib/discovery/likedContextMenu";
import { playlistPickerItems } from "./playlistContextMenu";
import { playableTracksFromRelease } from "./toPlaybackTrack";
import type { PlaybackTrack } from "./types";
import { usePlayback } from "./PlaybackContext";

function addToPlaylistItem(
  playlistChildren: PlaybackContextMenuItem[],
  isAuthenticated: boolean,
): PlaybackContextMenuItem {
  if (!isAuthenticated) {
    return {
      id: "add-to-playlist",
      label: "Add to playlist…",
      disabled: true,
      onSelect: () => {},
    };
  }

  return {
    id: "add-to-playlist",
    label: "Add to playlist…",
    children: playlistChildren,
    onSelect: () => {},
  };
}

type TrackRemoveAction = {
  label: string;
  onSelect: () => void;
};

function trackContextMenuItems(
  track: PlaybackTrack,
  hasAudio: boolean,
  isAuthenticated: boolean,
  isLiked: boolean | undefined,
  addToQueue: (tracks: PlaybackTrack[]) => void,
  playNext: (tracks: PlaybackTrack[]) => void,
  playlistChildren: PlaybackContextMenuItem[],
  remove?: TrackRemoveAction,
): PlaybackContextMenuItem[] {
  const items: PlaybackContextMenuItem[] = [
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

  if (!isAuthenticated) {
    items.push({
      id: "like",
      label: "Like",
      disabled: true,
      onSelect: () => {},
    });
  } else {
    items.push({
      id: isLiked ? "unlike" : "like",
      label: isLiked ? "Unlike" : "Like",
      onSelect: () => {
        void (isLiked ? unlikeTrack(track.id) : likeTrack(track.id));
      },
    });
  }

  items.push(addToPlaylistItem(playlistChildren, isAuthenticated));
  if (isAuthenticated) {
    items.push(addToLikedMenuItem([track.id]));
  }

  if (remove) {
    items.push({
      id: "remove-track",
      label: remove.label,
      onSelect: remove.onSelect,
    });
  }

  return items;
}

function releaseContextMenuItems(
  tracks: PlaybackTrack[],
  isAuthenticated: boolean,
  addToQueue: (tracks: PlaybackTrack[]) => void,
  playNext: (tracks: PlaybackTrack[]) => void,
  playlistChildren: PlaybackContextMenuItem[],
): PlaybackContextMenuItem[] {
  const disabled = tracks.length === 0;
  const trackIds = tracks.map((track) => track.id);

  return [
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
    addToPlaylistItem(playlistChildren, isAuthenticated && !disabled),
    ...(isAuthenticated ? [addToLikedMenuItem(trackIds)] : []),
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

async function ownedPlaylistsForPicker() {
  const response = await listMyPlaylists();
  return response.playlists.filter((playlist) => playlist.isOwned);
}

export function useTrackContextMenu(
  track: PlaybackTrack,
  hasAudio: boolean,
  options?: { isLiked?: boolean; remove?: TrackRemoveAction },
) {
  const auth = useAuth();
  const { addToQueue, playNext } = usePlayback();
  const { openAt } = usePlaybackContextMenu();

  const openMenuAt = useCallback(
    (x: number, y: number) => {
      if (!auth.isAuthenticated) {
        openAt(
          x,
          y,
          trackContextMenuItems(
            track,
            hasAudio,
            false,
            options?.isLiked,
            addToQueue,
            playNext,
            [],
            options?.remove,
          ),
        );
        return;
      }

      openAt(x, y, loadingItems);

      void ownedPlaylistsForPicker()
        .then((owned) => {
          openAt(
            x,
            y,
            trackContextMenuItems(
              track,
              hasAudio,
              true,
              options?.isLiked,
              addToQueue,
              playNext,
              playlistPickerItems(owned, [track.id]),
              options?.remove,
            ),
          );
        })
        .catch((error: unknown) => {
          const message =
            error instanceof ApiError ? error.message : "Could not load playlists";
          openAt(x, y, [
            {
              id: "playlist-error",
              label: message,
              disabled: true,
              onSelect: () => {},
            },
          ]);
        });
    },
    [
      track,
      hasAudio,
      options?.isLiked,
      options?.remove,
      auth.isAuthenticated,
      addToQueue,
      playNext,
      openAt,
    ],
  );

  const onContextMenu = useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      openMenuAt(e.clientX, e.clientY);
    },
    [openMenuAt],
  );

  return { onContextMenu, openMenuAt };
}

export function useReleaseContextMenu(releaseId: string) {
  const auth = useAuth();
  const { addToQueue, playNext } = usePlayback();
  const { openAt } = usePlaybackContextMenu();

  return useCallback(
    (e: React.MouseEvent) => {
      e.preventDefault();
      const x = e.clientX;
      const y = e.clientY;
      openAt(x, y, loadingItems);

      const releasePromise = getCatalogRelease(releaseId);
      const playlistsPromise = auth.isAuthenticated
        ? ownedPlaylistsForPicker()
        : Promise.resolve([]);

      void Promise.all([releasePromise, playlistsPromise])
        .then(([release, owned]) => {
          const tracks = playableTracksFromRelease(release);
          const trackIds = tracks.map((track) => track.id);
          openAt(
            x,
            y,
            releaseContextMenuItems(
              tracks,
              auth.isAuthenticated,
              addToQueue,
              playNext,
              playlistPickerItems(owned, trackIds),
            ),
          );
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
    [releaseId, auth.isAuthenticated, addToQueue, playNext, openAt],
  );
}
