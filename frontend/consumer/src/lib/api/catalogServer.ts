import "server-only";

import { cache } from "react";
import { isCatalogGuid } from "@/lib/catalog/guid";
import { PUBLIC_PAGE_REVALIDATE_SECONDS } from "./revalidate";
import { serverPublicFetch } from "./serverPublicFetch";
import type {
  GetArtistDetailResponse,
  GetReleaseDetailResponse,
  GetReleaseGroupDetailResponse,
} from "./types";

const cacheOpts = (tags: string[]) => ({
  revalidate: PUBLIC_PAGE_REVALIDATE_SECONDS,
  tags,
});

function enc(value: string): string {
  return encodeURIComponent(value);
}

export function fetchCatalogArtistById(artistId: string): Promise<GetArtistDetailResponse> {
  return serverPublicFetch<GetArtistDetailResponse>(
    `/api/v1/catalog/artists/${enc(artistId)}`,
    { method: "GET" },
    cacheOpts([`artist:id:${artistId}`]),
  );
}

export function fetchCatalogArtistBySlug(artistSlug: string): Promise<GetArtistDetailResponse> {
  return serverPublicFetch<GetArtistDetailResponse>(
    `/api/v1/catalog/artists/by-slug/${enc(artistSlug)}`,
    { method: "GET" },
    cacheOpts([`artist:slug:${artistSlug}`]),
  );
}

export function fetchCatalogArtist(artistKey: string): Promise<GetArtistDetailResponse> {
  if (isCatalogGuid(artistKey)) {
    return fetchCatalogArtistById(artistKey);
  }
  return fetchCatalogArtistBySlug(artistKey);
}

export const getCachedCatalogArtist = cache(fetchCatalogArtist);

export function fetchCatalogRelease(releaseId: string): Promise<GetReleaseDetailResponse> {
  return serverPublicFetch<GetReleaseDetailResponse>(
    `/api/v1/catalog/releases/${enc(releaseId)}`,
    { method: "GET" },
    cacheOpts([`release:id:${releaseId}`]),
  );
}

export const getCachedCatalogRelease = cache(fetchCatalogRelease);

export function fetchCatalogReleaseBySlugs(
  artistSlug: string,
  releaseSlug: string,
): Promise<GetReleaseDetailResponse> {
  return serverPublicFetch<GetReleaseDetailResponse>(
    `/api/v1/catalog/artists/${enc(artistSlug)}/releases/${enc(releaseSlug)}`,
    { method: "GET" },
    cacheOpts([`release:slug:${artistSlug}/${releaseSlug}`]),
  );
}

export const getCachedCatalogReleaseBySlugs = cache(fetchCatalogReleaseBySlugs);

export function fetchCatalogReleaseGroupBySlugs(
  artistSlug: string,
  groupSlug: string,
): Promise<GetReleaseGroupDetailResponse> {
  return serverPublicFetch<GetReleaseGroupDetailResponse>(
    `/api/v1/catalog/artists/${enc(artistSlug)}/release-groups/${enc(groupSlug)}`,
    { method: "GET" },
    cacheOpts([`release-group:slug:${artistSlug}/${groupSlug}`]),
  );
}

export const getCachedCatalogReleaseGroupBySlugs = cache(fetchCatalogReleaseGroupBySlugs);

export type SitemapEntryDto =
  | {
      type: "artist";
      artistSlug: string;
      lastModified: string;
    }
  | {
      type: "release";
      artistSlug: string;
      releaseSlug: string;
      lastModified: string;
    }
  | {
      type: "releaseGroup";
      artistSlug: string;
      groupSlug: string;
      lastModified: string;
    }
  | {
      type: "playlist";
      playlistId: string;
      lastModified: string;
    };

export type SitemapResponse = {
  entries: SitemapEntryDto[];
  nextCursor: string | null;
};

export function fetchCatalogSitemap(
  cursor?: string,
  pageSize = 5000,
): Promise<SitemapResponse> {
  const params = new URLSearchParams({ pageSize: String(pageSize) });
  if (cursor) params.set("cursor", cursor);
  return serverPublicFetch<SitemapResponse>(
    `/api/v1/catalog/sitemap?${params}`,
    { method: "GET" },
    { revalidate: 86400, tags: ["sitemap"] },
  );
}
