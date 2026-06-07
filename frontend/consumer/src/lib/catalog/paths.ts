import { catalogHashtagPath as sharedCatalogHashtagPath } from "@amuse/catalog-text";
import type { ReleaseEditionSummary, ReleaseSummary } from "@/lib/api/types";

export function catalogArtistPath(artistSlug: string): string {
  return `/artist/${encodeURIComponent(artistSlug)}`;
}

export function catalogReleasePath(artistSlug: string, releaseSlug: string): string {
  return `/artist/${encodeURIComponent(artistSlug)}/release/${encodeURIComponent(releaseSlug)}`;
}

export function catalogReleaseHref(artistSlug: string, releaseSlug: string): string {
  return catalogReleasePath(artistSlug, releaseSlug);
}

export function catalogReleaseByIdHref(releaseId: string): string {
  return `/release/${encodeURIComponent(releaseId)}`;
}

export function catalogReleasePathFromSummary(release: ReleaseSummary): string {
  return catalogReleasePath(release.artistSlug, release.slug);
}

export function catalogReleasePathFromEdition(
  artistSlug: string,
  edition: ReleaseEditionSummary,
): string {
  return catalogReleasePath(artistSlug, edition.slug);
}

export function catalogReleaseGroupPath(artistSlug: string, groupSlug: string): string {
  return `/artist/${encodeURIComponent(artistSlug)}/release-group/${encodeURIComponent(groupSlug)}`;
}

export function catalogHashtagPath(tag: string): string {
  return sharedCatalogHashtagPath(tag);
}
