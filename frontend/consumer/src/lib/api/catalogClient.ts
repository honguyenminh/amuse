import { authFetch } from "@/lib/auth/authFetch";
import { publicFetch } from "./publicFetch";
import type {
  BrowseHomeResponse,
  GetArtistDetailResponse,
  GetReleaseDetailResponse,
  TrackStreamInfoResponse,
} from "./types";

// Browse endpoints — anonymous-friendly. Use `publicFetch` so we don't force
// a login before we even know what the visitor wants to look at.
export function browseCatalogHome(): Promise<BrowseHomeResponse> {
  return publicFetch<BrowseHomeResponse>("/api/v1/catalog/home", { method: "GET" });
}

export function getCatalogArtist(artistId: string): Promise<GetArtistDetailResponse> {
  return publicFetch<GetArtistDetailResponse>(
    `/api/v1/catalog/artists/${encodeURIComponent(artistId)}`,
    { method: "GET" },
  );
}

export function getCatalogRelease(releaseId: string): Promise<GetReleaseDetailResponse> {
  return publicFetch<GetReleaseDetailResponse>(
    `/api/v1/catalog/releases/${encodeURIComponent(releaseId)}`,
    { method: "GET" },
  );
}

// Streaming is gated: the signed URL is only handed out to authenticated
// listeners. `authFetch` will refresh once on 401; if that still fails the
// playback layer redirects to /login.
export function getTrackStreamInfo(trackId: string): Promise<TrackStreamInfoResponse> {
  return authFetch<TrackStreamInfoResponse>(
    `/api/v1/catalog/tracks/${encodeURIComponent(trackId)}/stream-info`,
    { method: "GET" },
  );
}
