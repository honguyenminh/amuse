import { likeTrack } from "@/lib/api/discoveryClient";
import { ApiError } from "@/lib/api/types";
import type { PlaybackContextMenuItem } from "@/lib/playback/PlaybackContextMenuProvider";

export async function addTracksToLiked(trackIds: string[]): Promise<void> {
  for (const trackId of trackIds) {
    try {
      await likeTrack(trackId);
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

export function addToLikedMenuItem(trackIds: string[]): PlaybackContextMenuItem {
  const disabled = trackIds.length === 0;
  return {
    id: "add-to-liked",
    label: "Add to liked",
    disabled,
    onSelect: () => {
      void addTracksToLiked(trackIds);
    },
  };
}
