import {
  catalogArtistPath,
  catalogReleaseGroupPath,
  catalogReleasePath,
} from "@/lib/catalog/paths";
import { playlistPath } from "@/lib/discovery/paths";
import { absoluteUrl } from "./siteUrl";

export function canonicalArtistUrl(artistSlug: string): string {
  return absoluteUrl(catalogArtistPath(artistSlug));
}

export function canonicalReleaseUrl(artistSlug: string, releaseSlug: string): string {
  return absoluteUrl(catalogReleasePath(artistSlug, releaseSlug));
}

export function canonicalReleaseGroupUrl(artistSlug: string, groupSlug: string): string {
  return absoluteUrl(catalogReleaseGroupPath(artistSlug, groupSlug));
}

export function canonicalPlaylistUrl(playlistId: string): string {
  return absoluteUrl(playlistPath(playlistId));
}
