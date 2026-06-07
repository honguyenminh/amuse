import { isCatalogGuid } from "@/lib/catalog/guid";
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

export function getCatalogArtistById(artistId: string): Promise<GetArtistDetailResponse> {
  return publicFetch<GetArtistDetailResponse>(
    `/api/v1/catalog/artists/${encodeURIComponent(artistId)}`,
    { method: "GET" },
  );
}

export function getCatalogArtistBySlug(artistSlug: string): Promise<GetArtistDetailResponse> {
  return publicFetch<GetArtistDetailResponse>(
    `/api/v1/catalog/artists/by-slug/${encodeURIComponent(artistSlug)}`,
    { method: "GET" },
  );
}

/** Resolves a roster artist by GUID or public slug. */
export function getCatalogArtist(artistKey: string): Promise<GetArtistDetailResponse> {
  if (isCatalogGuid(artistKey)) {
    return getCatalogArtistById(artistKey);
  }
  return getCatalogArtistBySlug(artistKey);
}

export function getCatalogRelease(releaseId: string): Promise<GetReleaseDetailResponse> {
  return publicFetch<GetReleaseDetailResponse>(
    `/api/v1/catalog/releases/${encodeURIComponent(releaseId)}`,
    { method: "GET" },
  );
}

export function getCatalogReleaseBySlugs(
  artistSlug: string,
  releaseSlug: string,
): Promise<GetReleaseDetailResponse> {
  return publicFetch<GetReleaseDetailResponse>(
    `/api/v1/catalog/artists/${encodeURIComponent(artistSlug)}/releases/${encodeURIComponent(releaseSlug)}`,
    { method: "GET" },
  );
}

// Streaming is gated: the signed URL is only handed out to authenticated
// listeners. `authFetch` refreshes stale access tokens; only a failed refresh
// surfaces as unauthenticated.
export function getTrackStreamInfo(trackId: string): Promise<TrackStreamInfoResponse> {
  return authFetch<TrackStreamInfoResponse>(
    `/api/v1/catalog/tracks/${encodeURIComponent(trackId)}/stream-info`,
    { method: "GET" },
  );
}
