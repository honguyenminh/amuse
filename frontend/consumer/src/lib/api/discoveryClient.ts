import { authFetch } from "@/lib/auth/authFetch";
import { publicFetch } from "./publicFetch";
import type { SearchKind } from "@/lib/discovery/searchKinds";
import { appendSearchKindsParams } from "@/lib/discovery/searchKinds";
import type {
  AddPlaylistItemRequest,
  AddPlaylistItemResponse,
  CreatePlaylistRequest,
  ForkPlaylistRequest,
  LikedTracksResponse,
  PlayableTracksResponse,
  PlaylistDetailDto,
  PlaylistListResponse,
  ReplacePlaylistSharesRequest,
  ReorderPlaylistItemsRequest,
  SavedReleasesResponse,
  SearchResponse,
  UpdatePlaylistRequest,
} from "./types";

const BASE = "/api/v1/discovery";

function enc(id: string): string {
  return encodeURIComponent(id);
}

// Browse / read — anonymous-friendly; auth token sent when present for personalization.
export function searchDiscovery(
  q: string,
  options?: { pageSize?: number; kinds?: SearchKind[] },
): Promise<SearchResponse> {
  const params = new URLSearchParams({ q });
  if (options?.pageSize !== undefined) params.set("pageSize", String(options.pageSize));
  appendSearchKindsParams(params, options?.kinds);
  return publicFetch<SearchResponse>(`${BASE}/search?${params}`, { method: "GET" });
}

export function getPlaylist(playlistId: string): Promise<PlaylistDetailDto> {
  return publicFetch<PlaylistDetailDto>(`${BASE}/playlists/${enc(playlistId)}`, {
    method: "GET",
  });
}

export function getPlaylistPlayableTracks(playlistId: string): Promise<PlayableTracksResponse> {
  return publicFetch<PlayableTracksResponse>(
    `${BASE}/playables/playlist/${enc(playlistId)}/tracks`,
    { method: "GET" },
  );
}

export function getReleasePlayableTracks(releaseId: string): Promise<PlayableTracksResponse> {
  return publicFetch<PlayableTracksResponse>(
    `${BASE}/playables/release/${enc(releaseId)}/tracks`,
    { method: "GET" },
  );
}

// Library — authenticated listener only.
export function listLibraryPlaylists(): Promise<PlaylistListResponse> {
  return authFetch<PlaylistListResponse>(`${BASE}/library/playlists`, { method: "GET" });
}

export function listLibraryLiked(): Promise<LikedTracksResponse> {
  return authFetch<LikedTracksResponse>(`${BASE}/library/liked`, { method: "GET" });
}

export function getLikedPlaylist(): Promise<PlaylistDetailDto> {
  return authFetch<PlaylistDetailDto>(`${BASE}/liked`, { method: "GET" });
}

export function getLikedPlayableTracks(): Promise<PlayableTracksResponse> {
  return authFetch<PlayableTracksResponse>(`${BASE}/playables/liked/tracks`, {
    method: "GET",
  });
}

export function listLibraryReleases(): Promise<SavedReleasesResponse> {
  return authFetch<SavedReleasesResponse>(`${BASE}/library/releases`, { method: "GET" });
}

export function listMyPlaylists(): Promise<PlaylistListResponse> {
  return authFetch<PlaylistListResponse>(`${BASE}/playlists/mine`, { method: "GET" });
}

// Playlist mutations.
export function createPlaylist(request: CreatePlaylistRequest): Promise<PlaylistDetailDto> {
  return authFetch<PlaylistDetailDto>(`${BASE}/playlists`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updatePlaylist(
  playlistId: string,
  request: UpdatePlaylistRequest,
): Promise<PlaylistDetailDto> {
  return authFetch<PlaylistDetailDto>(`${BASE}/playlists/${enc(playlistId)}`, {
    method: "PATCH",
    body: JSON.stringify(request),
  });
}

export function deletePlaylist(playlistId: string): Promise<void> {
  return authFetch<void>(`${BASE}/playlists/${enc(playlistId)}`, { method: "DELETE" });
}

export function addTrackToPlaylist(
  playlistId: string,
  request: AddPlaylistItemRequest,
): Promise<AddPlaylistItemResponse> {
  return authFetch<AddPlaylistItemResponse>(`${BASE}/playlists/${enc(playlistId)}/items`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function removeTrackFromPlaylist(playlistId: string, itemId: string): Promise<void> {
  return authFetch<void>(`${BASE}/playlists/${enc(playlistId)}/items/${enc(itemId)}`, {
    method: "DELETE",
  });
}

export function removeReleaseFromPlaylist(
  playlistId: string,
  releaseId: string,
): Promise<void> {
  return authFetch<void>(
    `${BASE}/playlists/${enc(playlistId)}/releases/${enc(releaseId)}`,
    { method: "DELETE" },
  );
}

export function reorderPlaylistItems(
  playlistId: string,
  request: ReorderPlaylistItemsRequest,
): Promise<void> {
  return authFetch<void>(`${BASE}/playlists/${enc(playlistId)}/items/reorder`, {
    method: "PATCH",
    body: JSON.stringify(request),
  });
}

export function replacePlaylistShares(
  playlistId: string,
  request: ReplacePlaylistSharesRequest,
): Promise<PlaylistDetailDto> {
  return authFetch<PlaylistDetailDto>(`${BASE}/playlists/${enc(playlistId)}/shares`, {
    method: "PUT",
    body: JSON.stringify(request),
  });
}

export function forkPlaylist(
  playlistId: string,
  request: ForkPlaylistRequest,
): Promise<PlaylistDetailDto> {
  return authFetch<PlaylistDetailDto>(`${BASE}/playlists/${enc(playlistId)}/fork`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function savePlaylist(playlistId: string): Promise<void> {
  return authFetch<void>(`${BASE}/playlists/${enc(playlistId)}/save`, { method: "POST" });
}

export function unsavePlaylist(playlistId: string): Promise<void> {
  return authFetch<void>(`${BASE}/playlists/${enc(playlistId)}/save`, { method: "DELETE" });
}

export function followPlaylist(playlistId: string): Promise<void> {
  return authFetch<void>(`${BASE}/playlists/${enc(playlistId)}/follow`, { method: "POST" });
}

export function unfollowPlaylist(playlistId: string): Promise<void> {
  return authFetch<void>(`${BASE}/playlists/${enc(playlistId)}/follow`, { method: "DELETE" });
}

// Track likes.
export function likeTrack(trackId: string): Promise<void> {
  return authFetch<void>(`${BASE}/liked/${enc(trackId)}`, { method: "PUT" });
}

export function unlikeTrack(trackId: string): Promise<void> {
  return authFetch<void>(`${BASE}/liked/${enc(trackId)}`, { method: "DELETE" });
}

// Saved releases.
export function saveRelease(releaseId: string): Promise<void> {
  return authFetch<void>(`${BASE}/library/releases/${enc(releaseId)}`, { method: "PUT" });
}

export function unsaveRelease(releaseId: string): Promise<void> {
  return authFetch<void>(`${BASE}/library/releases/${enc(releaseId)}`, { method: "DELETE" });
}
