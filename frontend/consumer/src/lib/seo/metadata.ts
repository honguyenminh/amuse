import type { Metadata } from "next";
import type {
  GetArtistDetailResponse,
  GetReleaseDetailResponse,
  GetReleaseGroupDetailResponse,
  PlaylistDetailDto,
} from "@/lib/api/types";
import {
  canonicalArtistUrl,
  canonicalPlaylistUrl,
  canonicalReleaseGroupUrl,
  canonicalReleaseUrl,
} from "./canonical";
import { excerptText } from "./excerpt";

const SITE_NAME = "Amuse";

function openGraphImage(url: string | null | undefined): Metadata["openGraph"] {
  if (!url) return undefined;
  return {
    images: [{ url, alt: "" }],
  };
}

export function artistMetadata(artist: GetArtistDetailResponse): Metadata {
  const title = `${artist.name} | ${SITE_NAME}`;
  const description =
    excerptText(artist.bio) ??
    `Listen to ${artist.name} on ${SITE_NAME}. Browse releases and discography.`;
  const image = artist.coverUrl ?? artist.avatarUrl ?? undefined;

  return {
    title,
    description,
    alternates: { canonical: canonicalArtistUrl(artist.slug) },
    openGraph: {
      title,
      description,
      type: "profile",
      ...openGraphImage(image),
    },
    twitter: {
      card: image ? "summary_large_image" : "summary",
      title,
      description,
      images: image ? [image] : undefined,
    },
  };
}

export function releaseMetadata(release: GetReleaseDetailResponse): Metadata {
  const title = `${release.title} — ${release.artistName} | ${SITE_NAME}`;
  const description =
    excerptText(release.description) ??
    `${release.title} by ${release.artistName}. ${release.tracks.length} track${
      release.tracks.length === 1 ? "" : "s"
    } on ${SITE_NAME}.`;

  return {
    title,
    description,
    alternates: {
      canonical: canonicalReleaseUrl(release.artistSlug, release.slug),
    },
    openGraph: {
      title,
      description,
      type: "music.album",
      ...openGraphImage(release.coverArtUrl),
    },
    twitter: {
      card: release.coverArtUrl ? "summary_large_image" : "summary",
      title,
      description,
      images: release.coverArtUrl ? [release.coverArtUrl] : undefined,
    },
  };
}

export function releaseGroupMetadata(group: GetReleaseGroupDetailResponse): Metadata {
  const title = `${group.title} — ${group.artistName} | ${SITE_NAME}`;
  const description =
    excerptText(group.description) ??
    `${group.title} by ${group.artistName}. ${group.releases.length} edition${
      group.releases.length === 1 ? "" : "s"
    } on ${SITE_NAME}.`;
  const cover = group.releases.find((edition) => edition.coverArtUrl)?.coverArtUrl ?? undefined;

  return {
    title,
    description,
    alternates: {
      canonical: canonicalReleaseGroupUrl(group.artistSlug, group.slug),
    },
    openGraph: {
      title,
      description,
      type: "music.album",
      ...openGraphImage(cover),
    },
    twitter: {
      card: cover ? "summary_large_image" : "summary",
      title,
      description,
      images: cover ? [cover] : undefined,
    },
  };
}

export function playlistMetadata(playlist: PlaylistDetailDto): Metadata {
  const title = `${playlist.title} | ${SITE_NAME}`;
  const description =
    excerptText(playlist.description) ??
    `Playlist on ${SITE_NAME} — ${playlist.items.length} track${
      playlist.items.length === 1 ? "" : "s"
    }.`;
  const isPublic = playlist.visibility === "public";

  return {
    title,
    description,
    robots: isPublic ? { index: true, follow: true } : { index: false, follow: false },
    alternates: isPublic ? { canonical: canonicalPlaylistUrl(playlist.id) } : undefined,
    openGraph: isPublic
      ? {
          title,
          description,
          type: "music.playlist",
        }
      : undefined,
    twitter: isPublic
      ? {
          card: "summary",
          title,
          description,
        }
      : undefined,
  };
}
