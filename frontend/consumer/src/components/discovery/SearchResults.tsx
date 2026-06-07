"use client";

import { PlaylistCoverArt } from "@/components/discovery/PlaylistCoverArt";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import type {
  PublicPlaylistSearchCardDto,
  SearchItemDto,
  SearchResponse,
} from "@/lib/api/types";
import {
  catalogArtistPath,
  catalogReleaseByIdHref,
  catalogReleaseHref,
} from "@/lib/catalog/paths";
import { playlistPath } from "@/lib/discovery/paths";
import Link from "next/link";
import type { ReactNode } from "react";

type SearchResultsProps = {
  data: SearchResponse;
  query: string;
};

export function SearchResults({ data, query }: SearchResultsProps) {
  const hasResults =
    data.verified.length > 0 ||
    data.unverified.length > 0 ||
    data.publicPlaylists.length > 0;

  if (!hasResults) {
    return (
      <Card>
        <Text variant="title-large">No results</Text>
        <Text variant="label-medium" className="text-on-surface-variant">
          Nothing matched &ldquo;{query}&rdquo;. Try another search.
        </Text>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-8">
      {data.verified.length > 0 ? (
        <SearchSection title="Verified">
          {data.verified.map((item) => (
            <SearchItemRow key={`verified-${item.kind}-${item.id}`} item={item} />
          ))}
        </SearchSection>
      ) : null}

      {data.unverified.length > 0 ? (
        <SearchSection title="Unverified">
          {data.unverified.map((item) => (
            <SearchItemRow key={`unverified-${item.kind}-${item.id}`} item={item} />
          ))}
        </SearchSection>
      ) : null}

      {data.publicPlaylists.length > 0 ? (
        <SearchSection title="Public playlists">
          {data.publicPlaylists.map((playlist) => (
            <PublicPlaylistRow key={playlist.id} playlist={playlist} />
          ))}
        </SearchSection>
      ) : null}
    </div>
  );
}

function SearchSection({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <section className="flex flex-col gap-3">
      <Text variant="title-large">{title}</Text>
      <Card>
        <ul className="flex flex-col divide-y divide-outline/40">{children}</ul>
      </Card>
    </section>
  );
}

function SearchItemRow({ item }: { item: SearchItemDto }) {
  const href = searchItemHref(item);
  const subtitle = item.subtitle ?? searchItemKindLabel(item.kind);

  return (
    <li>
      <Link
        href={href}
        className="flex items-center gap-3 py-3 transition-colors hover:text-primary"
      >
        {item.coverArtUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={item.coverArtUrl}
            alt=""
            className="size-12 rounded object-cover"
          />
        ) : (
          <div className="size-12 rounded bg-surface-container-high" />
        )}
        <div className="min-w-0 flex-1">
          <Text variant="body-medium" className="truncate">
            {item.title}
          </Text>
          <Text variant="label-medium" className="truncate text-on-surface-variant">
            {subtitle}
          </Text>
        </div>
      </Link>
    </li>
  );
}

function PublicPlaylistRow({ playlist }: { playlist: PublicPlaylistSearchCardDto }) {
  const ownerName = playlist.owner.displayName ?? "Unknown listener";

  return (
    <li>
      <Link
        href={playlistPath(playlist.id)}
        className="flex items-center gap-3 py-3 transition-colors hover:text-primary"
      >
        <PlaylistCoverArt coverArtUrls={playlist.coverArtUrls} variant="row" />
        <div className="min-w-0 flex-1">
          <Text variant="body-medium" className="truncate">
            {playlist.title}
          </Text>
          {playlist.description ? (
            <Text variant="body-small" className="line-clamp-1 text-on-surface-variant">
              {playlist.description}
            </Text>
          ) : null}
          <Text variant="label-medium" className="truncate text-on-surface-variant">
            {ownerName} · {playlist.trackCount} track
            {playlist.trackCount === 1 ? "" : "s"}
          </Text>
        </div>
      </Link>
    </li>
  );
}

function searchItemKindLabel(kind: string): string {
  switch (kind) {
    case "artist":
      return "Artist";
    case "release":
      return "Release";
    case "track":
      return "Track";
    default:
      return kind;
  }
}

function searchItemHref(item: SearchItemDto): string {
  switch (item.kind) {
    case "artist":
      if (item.artistSlug) return catalogArtistPath(item.artistSlug);
      return `/artist/${item.id}`;
    case "release":
      if (item.artistSlug && item.releaseSlug) {
        return catalogReleaseHref(item.artistSlug, item.releaseSlug, { title: item.title });
      }
      return catalogReleaseByIdHref(item.id, { title: item.title });
    case "track":
      if (item.artistSlug && item.releaseSlug) {
        return catalogReleaseHref(item.artistSlug, item.releaseSlug, { title: item.title });
      }
      if (item.releaseId) {
        return catalogReleaseByIdHref(item.releaseId, { title: item.title });
      }
      return "#";
    default:
      return "#";
  }
}
