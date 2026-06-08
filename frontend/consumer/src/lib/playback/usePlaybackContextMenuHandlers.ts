"use client";

import { getCatalogRelease } from "@/lib/api/catalogClient";
import {
  getLikedPlayableTracks,
  getPlaylistPlayableTracks,
  likeTrack,
  listMyPlaylists,
  unlikeTrack,
} from "@/lib/api/discoveryClient";
import { ApiError } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import { consumeAppContextMenu } from "@/lib/ui/contextMenu";
import { useCallback } from "react";
import type { PlaybackContextMenuItem } from "./PlaybackContextMenuProvider";
import { usePlaybackContextMenu } from "./PlaybackContextMenuProvider";
import { addToLikedMenuItem } from "@/lib/discovery/likedContextMenu";
import { playlistPickerItems } from "./playlistContextMenu";
import { playableTracksFromDtos, playableTracksFromRelease } from "./toPlaybackTrack";
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

type TrackContextMenuOptions = {
  isLiked?: boolean;
  remove?: TrackRemoveAction;
  removeRelease?: TrackRemoveAction;
  /** Hides "Add to queue" and makes "Play next" reorder within the existing queue. */
  inQueue?: boolean;
};

function trackContextMenuItems(
  track: PlaybackTrack,
  hasAudio: boolean,
  isAuthenticated: boolean,
  isLiked: boolean | undefined,
  addToQueue: (tracks: PlaybackTrack[]) => void,
  playNext: (tracks: PlaybackTrack[]) => void,
  moveToPlayNext: (trackId: string) => void,
  removeFromQueue: (trackId: string) => void,
  playlistChildren: PlaybackContextMenuItem[],
  options?: Pick<TrackContextMenuOptions, "inQueue" | "remove" | "removeRelease">,
): PlaybackContextMenuItem[] {
  const items: PlaybackContextMenuItem[] = [];

  if (!options?.inQueue) {
    items.push({
      id: "add-to-queue",
      label: "Add to queue",
      disabled: !hasAudio,
      onSelect: () => addToQueue([track]),
    });
  }

  items.push({
    id: "play-next",
    label: "Play next",
    disabled: !hasAudio,
    onSelect: () =>
      options?.inQueue ? moveToPlayNext(track.id) : playNext([track]),
  });

  if (options?.inQueue) {
    items.push({
      id: "remove-from-queue",
      label: "Remove from queue",
      onSelect: () => removeFromQueue(track.id),
    });
  }

  if (options?.remove) {
    items.push({
      id: "remove-track",
      label: options.remove.label,
      onSelect: options.remove.onSelect,
    });
  }

  if (options?.removeRelease) {
    items.push({
      id: "remove-release",
      label: options.removeRelease.label,
      onSelect: options.removeRelease.onSelect,
    });
  }

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
  options?: TrackContextMenuOptions,
) {
  const auth = useAuth();
  const { addToQueue, playNext, moveToPlayNext, removeFromQueue } = usePlayback();
  const { openAt } = usePlaybackContextMenu();

  const openMenuAt = useCallback(
    (x: number, y: number) => {
      const menuOptions = options
        ? {
            inQueue: options.inQueue,
            remove: options.remove,
            removeRelease: options.removeRelease,
          }
        : undefined;

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
            moveToPlayNext,
            removeFromQueue,
            [],
            menuOptions,
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
              moveToPlayNext,
              removeFromQueue,
              playlistPickerItems(owned, [track.id]),
              menuOptions,
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
      options,
      auth.isAuthenticated,
      addToQueue,
      playNext,
      moveToPlayNext,
      removeFromQueue,
      openAt,
    ],
  );

  const onContextMenu = useCallback(
    (e: React.MouseEvent) => {
      consumeAppContextMenu(e, () => openMenuAt(e.clientX, e.clientY));
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
      consumeAppContextMenu(e, () => {
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
      });
    },
    [releaseId, auth.isAuthenticated, addToQueue, playNext, openAt],
  );
}

type PlaylistContextMenuOptions = {
  isLikedMode?: boolean;
};

export function usePlaylistContextMenu(
  playlistId: string,
  options?: PlaylistContextMenuOptions,
) {
  const auth = useAuth();
  const { addToQueue, playNext } = usePlayback();
  const { openAt } = usePlaybackContextMenu();

  return useCallback(
    (e: React.MouseEvent) => {
      consumeAppContextMenu(e, () => {
        const x = e.clientX;
        const y = e.clientY;
        openAt(x, y, loadingItems);

        const tracksPromise = options?.isLikedMode
          ? getLikedPlayableTracks().then((response) =>
              playableTracksFromDtos(response.tracks),
            )
          : getPlaylistPlayableTracks(playlistId).then((response) =>
              playableTracksFromDtos(response.tracks),
            );

        const playlistsPromise = auth.isAuthenticated
          ? ownedPlaylistsForPicker()
          : Promise.resolve([]);

        void Promise.all([tracksPromise, playlistsPromise])
          .then(([tracks, owned]) => {
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
                label: "Could not load playlist",
                disabled: true,
                onSelect: () => {},
              },
            ]);
          });
      });
    },
    [playlistId, options?.isLikedMode, auth.isAuthenticated, addToQueue, playNext, openAt],
  );
}
