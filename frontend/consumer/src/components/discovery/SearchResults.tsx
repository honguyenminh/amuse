"use client";

import { PlaylistCoverArt } from "@/components/discovery/PlaylistCoverArt";
import { UnverifiedSellerBadge } from "@/components/catalog/UnverifiedSellerBadge";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import type { SearchResponse, SearchResultItem } from "@/lib/api/types";
import {
  catalogArtistPath,
  catalogReleaseByIdHref,
  catalogReleaseHref,
} from "@/lib/catalog/paths";
import {
  formatPlaylistSearchSubtitle,
  formatSearchItemSubtitle,
} from "@/lib/discovery/formatSearchResultSubtitle";
import { playlistPath } from "@/lib/discovery/paths";
import {
  filteredKindsSummary,
  isAllSearchKindsSelected,
  type SearchKind,
} from "@/lib/discovery/searchKinds";
import {
  usePlaylistContextMenu,
  useReleaseContextMenu,
} from "@/lib/playback/usePlaybackContextMenuHandlers";
import Link from "next/link";
import type { MouseEvent } from "react";

type SearchResultsProps = {
  data: SearchResponse;
  query: string;
  selectedKinds: SearchKind[];
};

export function SearchResults({ data, query, selectedKinds }: SearchResultsProps) {
  const hasResults = data.items.length > 0;
  const filtersActive = !isAllSearchKindsSelected(selectedKinds);

  if (!hasResults) {
    return (
      <Card>
        <Text variant="title-large">No results</Text>
        <Text variant="label-medium" className="text-on-surface-variant">
          {filtersActive
            ? `Nothing matched "${query}" for ${filteredKindsSummary(selectedKinds).toLowerCase()}. Try other filters or another search.`
            : `Nothing matched "${query}". Try another search.`}
        </Text>
      </Card>
    );
  }

  return (
    <Card className="px-4 py-0">
      <ul className="flex flex-col divide-y divide-outline/40">
        {data.items.map((item) => (
          <SearchResultRow key={`${item.kind}-${item.id}`} item={item} />
        ))}
      </ul>
    </Card>
  );
}

function SearchResultRow({ item }: { item: SearchResultItem }) {
  if (item.kind === "playlist") {
    return <SearchPlaylistRow item={item} />;
  }

  if (item.kind === "release") {
    return <SearchReleaseRow item={item} />;
  }

  return <SearchCatalogRow item={item} />;
}

function SearchReleaseRow({ item }: { item: SearchResultItem }) {
  const onContextMenu = useReleaseContextMenu(item.id);
  return <SearchCatalogRow item={item} onContextMenu={onContextMenu} />;
}

function SearchCatalogRow({
  item,
  onContextMenu,
}: {
  item: SearchResultItem;
  onContextMenu?: (event: MouseEvent) => void;
}) {
  const href = searchItemHref(item);
  const subtitle = formatSearchItemSubtitle(item.kind, item.subtitle);

  return (
    <li>
      <Link
        href={href}
        className="flex items-center gap-3 py-3 transition-colors hover:text-primary"
        onContextMenu={onContextMenu}
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
          <div className="flex min-w-0 flex-wrap items-center gap-2">
            <Text variant="body-medium" className="truncate">
              {item.title}
            </Text>
            <UnverifiedSellerBadge trustTier={item.trustTier} />
          </div>
          <Text variant="label-medium" className="truncate text-on-surface-variant">
            {subtitle}
          </Text>
        </div>
      </Link>
    </li>
  );
}

function SearchPlaylistRow({ item }: { item: SearchResultItem }) {
  const onContextMenu = usePlaylistContextMenu(item.id);
  const ownerName = item.owner?.displayName ?? "Unknown listener";
  const trackCount = item.trackCount ?? 0;

  return (
    <li>
      <Link
        href={playlistPath(item.id)}
        className="flex items-center gap-3 py-3 transition-colors hover:text-primary"
        onContextMenu={onContextMenu}
      >
        <PlaylistCoverArt coverArtUrls={item.coverArtUrls ?? []} variant="row" />
        <div className="min-w-0 flex-1">
          <Text variant="body-medium" className="truncate">
            {item.title}
          </Text>
          {item.description ? (
            <Text variant="body-small" className="line-clamp-1 text-on-surface-variant">
              {item.description}
            </Text>
          ) : null}
          <Text variant="label-medium" className="truncate text-on-surface-variant">
            {formatPlaylistSearchSubtitle(ownerName, trackCount)}
          </Text>
        </div>
      </Link>
    </li>
  );
}

function searchItemHref(item: SearchResultItem): string {
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
