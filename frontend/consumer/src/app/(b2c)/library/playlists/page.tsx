"use client";

import { PlaylistCard } from "@/components/discovery/PlaylistCard";
import { PlaylistFiltersDialog } from "@/components/discovery/PlaylistFiltersDialog";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { PlaylistFormDialog } from "@/components/ui/PlaylistFormDialog";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { createPlaylist, listLibraryPlaylists } from "@/lib/api/discoveryClient";
import type { PlaylistSummaryDto } from "@/lib/api/types";
import { ApiError } from "@/lib/api/types";
import {
  activePlaylistLibraryFilterCount,
  applyPlaylistLibraryFilters,
  DEFAULT_PLAYLIST_LIBRARY_FILTERS,
  isDefaultPlaylistLibraryFilters,
  playlistLibraryFilterSummary,
  type PlaylistLibraryFilters,
} from "@/lib/discovery/playlistLibraryFilters";
import { playlistPath } from "@/lib/discovery/paths";
import { cn } from "@/lib/cn";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";

function FilterIcon({ className }: { className?: string }) {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      className={className}
      aria-hidden
    >
      <path d="M4 6h16M7 12h10M10 18h4" />
    </svg>
  );
}

export default function LibraryPlaylistsPage() {
  const router = useRouter();
  const [playlists, setPlaylists] = useState<PlaylistSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [createDialogOpen, setCreateDialogOpen] = useState(false);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [filters, setFilters] = useState<PlaylistLibraryFilters>(
    DEFAULT_PLAYLIST_LIBRARY_FILTERS,
  );

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    return listLibraryPlaylists()
      .then((response) => setPlaylists(response.playlists))
      .catch((err: Error) => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const filteredPlaylists = useMemo(
    () => applyPlaylistLibraryFilters(playlists, filters),
    [playlists, filters],
  );

  const filtersActive = !isDefaultPlaylistLibraryFilters(filters);
  const activeFilterCount = activePlaylistLibraryFilterCount(filters);
  const filterSummary = playlistLibraryFilterSummary(filters);

  const onCreate = async ({ title, description }: { title: string; description: string }) => {
    setCreating(true);
    try {
      const created = await createPlaylist({
        title,
        visibility: "private",
        ...(description ? { description } : {}),
      });
      router.push(playlistPath(created.id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not create playlist");
    } finally {
      setCreating(false);
    }
  };

  const emptyMessage = filtersActive
    ? "No playlists match these filters."
    : "No playlists yet. Create one, follow a public playlist, or save one to your library.";

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Text variant="title-large">Playlists</Text>
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant="outlined"
            className="inline-flex items-center gap-2"
            onClick={() => setFiltersOpen(true)}
            aria-label={
              filtersActive
                ? `Filters active: ${filterSummary}. Open filter options`
                : "Open playlist filters"
            }
          >
            <FilterIcon />
            Filters
            {filtersActive ? (
              <span
                className={cn(
                  "inline-flex min-w-5 items-center justify-center rounded-full",
                  "bg-primary px-1.5 py-0.5 text-label-small text-on-primary",
                )}
              >
                {activeFilterCount}
              </span>
            ) : null}
          </Button>
          <Button
            type="button"
            variant="outlined"
            disabled={creating}
            onClick={() => setCreateDialogOpen(true)}
          >
            New playlist
          </Button>
        </div>
      </div>

      {filtersActive ? (
        <Text variant="label-medium" className="text-on-surface-variant">
          Showing: {filterSummary}
        </Text>
      ) : null}

      <PlaylistFiltersDialog
        open={filtersOpen}
        filters={filters}
        onClose={() => setFiltersOpen(false)}
        onApply={setFilters}
      />

      <PlaylistFormDialog
        open={createDialogOpen}
        title="New playlist"
        confirmLabel="Create"
        confirmDisabled={creating}
        onClose={() => setCreateDialogOpen(false)}
        onConfirm={(values) => {
          setCreateDialogOpen(false);
          void onCreate(values);
        }}
      />

      {loading ? (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {Array.from({ length: 6 }, (_, i) => (
            <Skeleton key={i} className="aspect-square w-full rounded-md" />
          ))}
        </div>
      ) : null}

      {error ? (
        <Card>
          <Text variant="label-medium">{error}</Text>
        </Card>
      ) : null}

      {!loading && !error && filteredPlaylists.length === 0 ? (
        <Card>
          <Text variant="body-medium" className="text-on-surface-variant">
            {emptyMessage}
          </Text>
          {filtersActive ? (
            <Button
              type="button"
              variant="text"
              className="mt-3 self-start px-0"
              onClick={() => setFilters(DEFAULT_PLAYLIST_LIBRARY_FILTERS)}
            >
              Clear filters
            </Button>
          ) : null}
        </Card>
      ) : null}

      {!loading && filteredPlaylists.length > 0 ? (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {filteredPlaylists.map((playlist) => (
            <PlaylistCard key={playlist.id} playlist={playlist} />
          ))}
        </div>
      ) : null}
    </section>
  );
}
