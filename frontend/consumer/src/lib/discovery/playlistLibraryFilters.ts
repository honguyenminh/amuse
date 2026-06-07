import type { PlaylistSummaryDto } from "@/lib/api/types";

export type PlaylistOwnershipFilter = "all" | "mine" | "following";
export type PlaylistVisibilityFilter = "all" | "public" | "private";

export type PlaylistLibraryFilters = {
  ownership: PlaylistOwnershipFilter;
  visibility: PlaylistVisibilityFilter;
};

export const DEFAULT_PLAYLIST_LIBRARY_FILTERS: PlaylistLibraryFilters = {
  ownership: "all",
  visibility: "all",
};

export function isDefaultPlaylistLibraryFilters(filters: PlaylistLibraryFilters): boolean {
  return (
    filters.ownership === DEFAULT_PLAYLIST_LIBRARY_FILTERS.ownership &&
    filters.visibility === DEFAULT_PLAYLIST_LIBRARY_FILTERS.visibility
  );
}

export function activePlaylistLibraryFilterCount(filters: PlaylistLibraryFilters): number {
  let count = 0;
  if (filters.ownership !== "all") count += 1;
  if (filters.visibility !== "all") count += 1;
  return count;
}

export function playlistLibraryFilterSummary(filters: PlaylistLibraryFilters): string {
  const parts: string[] = [];
  if (filters.ownership === "mine") parts.push("Mine");
  if (filters.ownership === "following") parts.push("Following");
  if (filters.visibility === "public") parts.push("Public");
  if (filters.visibility === "private") parts.push("Private");
  return parts.join(" · ");
}

export function applyPlaylistLibraryFilters(
  playlists: PlaylistSummaryDto[],
  filters: PlaylistLibraryFilters,
): PlaylistSummaryDto[] {
  return playlists.filter((playlist) => {
    if (filters.ownership === "mine" && !playlist.isOwned) return false;
    if (filters.ownership === "following" && !playlist.isFollowed) return false;
    if (filters.visibility === "public" && playlist.visibility !== "public") return false;
    if (filters.visibility === "private" && playlist.visibility !== "private") return false;
    return true;
  });
}
