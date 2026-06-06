import { addTrackToPlaylist } from "@/lib/api/discoveryClient";
import { ApiError } from "@/lib/api/types";
import type { PlaybackContextMenuItem } from "./PlaybackContextMenuProvider";

export async function addTracksToPlaylist(
  playlistId: string,
  trackIds: string[],
): Promise<void> {
  for (const trackId of trackIds) {
    try {
      await addTrackToPlaylist(playlistId, { trackId });
    } catch (error) {
      if (
        error instanceof ApiError
        && error.code === "discovery.playlist_track_duplicate"
      ) {
        continue;
      }
      throw error;
    }
  }
}

export function playlistPickerItems(
  playlists: { id: string; title: string }[],
  trackIds: string[],
): PlaybackContextMenuItem[] {
  if (playlists.length === 0) {
    return [
      {
        id: "no-playlists",
        label: "No playlists yet",
        disabled: true,
        onSelect: () => {},
      },
    ];
  }

  return playlists.map((playlist) => ({
    id: `playlist-${playlist.id}`,
    label: playlist.title,
    onSelect: () => {
      void addTracksToPlaylist(playlist.id, trackIds);
    },
  }));
}
