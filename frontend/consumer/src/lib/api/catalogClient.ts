import { authFetch } from "@/lib/auth/authFetch";
import type {
  BrowseHomeResponse,
  GetAlbumDetailResponse,
  GetArtistDetailResponse,
} from "./types";

export function browseCatalogHome(): Promise<BrowseHomeResponse> {
  return authFetch<BrowseHomeResponse>("/api/v1/catalog/home", { method: "GET" });
}

export function getCatalogArtist(artistId: string): Promise<GetArtistDetailResponse> {
  return authFetch<GetArtistDetailResponse>(
    `/api/v1/catalog/artists/${encodeURIComponent(artistId)}`,
    { method: "GET" },
  );
}

export function getCatalogAlbum(albumId: string): Promise<GetAlbumDetailResponse> {
  return authFetch<GetAlbumDetailResponse>(
    `/api/v1/catalog/albums/${encodeURIComponent(albumId)}`,
    { method: "GET" },
  );
}
