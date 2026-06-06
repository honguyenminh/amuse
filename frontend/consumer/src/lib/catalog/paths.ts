import type { ReleaseEditionSummary, ReleaseSummary } from "@/lib/api/types";

export function catalogArtistPath(artistSlug: string): string {
  return `/artist/${encodeURIComponent(artistSlug)}`;
}

export function catalogReleasePath(artistSlug: string, releaseSlug: string): string {
  return `/artist/${encodeURIComponent(artistSlug)}/release/${encodeURIComponent(releaseSlug)}`;
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
