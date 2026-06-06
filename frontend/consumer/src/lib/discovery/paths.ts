export function playlistPath(playlistId: string): string {
  return `/playlist/${encodeURIComponent(playlistId)}`;
}

export const libraryPath = "/library";
export const libraryPlaylistsPath = "/library/playlists";
export const libraryLikedPath = "/library/liked";
export const libraryAlbumsPath = "/library/albums";

export function searchPath(query?: string): string {
  if (!query?.trim()) return "/search";
  return `/search?q=${encodeURIComponent(query.trim())}`;
}
