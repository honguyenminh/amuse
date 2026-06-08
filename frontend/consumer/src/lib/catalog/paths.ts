import { catalogHashtagPath as sharedCatalogHashtagPath } from "@amuse/catalog-text";
import type { ReleaseEditionSummary, ReleaseSummary } from "@/lib/api/types";

export function catalogArtistPath(artistSlug: string): string {
  return `/artist/${encodeURIComponent(artistSlug)}`;
}

type CatalogReleaseHrefOptions = {
  /** Shown in TopBar while release detail is still loading (client navigation). */
  title?: string;
};

function withReleaseTitle(path: string, title?: string): string {
  if (!title) {
    return path;
  }
  return `${path}?${new URLSearchParams({ title }).toString()}`;
}

export function catalogReleasePath(artistSlug: string, releaseSlug: string): string {
  return `/artist/${encodeURIComponent(artistSlug)}/release/${encodeURIComponent(releaseSlug)}`;
}

export function catalogReleaseHref(
  artistSlug: string,
  releaseSlug: string,
  options?: CatalogReleaseHrefOptions,
): string {
  return withReleaseTitle(catalogReleasePath(artistSlug, releaseSlug), options?.title);
}

export function catalogReleaseByIdHref(
  releaseId: string,
  options?: CatalogReleaseHrefOptions,
): string {
  return withReleaseTitle(`/release/${encodeURIComponent(releaseId)}`, options?.title);
}

export function catalogReleasePathFromSummary(release: ReleaseSummary): string {
  return catalogReleaseHref(release.artistSlug, release.slug, { title: release.title });
}

export function catalogReleasePathFromEdition(
  artistSlug: string,
  edition: ReleaseEditionSummary,
): string {
  return catalogReleaseHref(artistSlug, edition.slug, { title: edition.title });
}

export function catalogReleaseGroupPath(artistSlug: string, groupSlug: string): string {
  return `/artist/${encodeURIComponent(artistSlug)}/release-group/${encodeURIComponent(groupSlug)}`;
}

export function catalogHashtagPath(tag: string): string {
  return sharedCatalogHashtagPath(tag);
}
