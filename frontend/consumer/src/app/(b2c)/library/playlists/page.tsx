"use client";

import { PlaylistCard } from "@/components/discovery/PlaylistCard";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { createPlaylist, listLibraryPlaylists } from "@/lib/api/discoveryClient";
import type { PlaylistSummaryDto } from "@/lib/api/types";
import { ApiError } from "@/lib/api/types";
import { playlistPath } from "@/lib/discovery/paths";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

export default function LibraryPlaylistsPage() {
  const router = useRouter();
  const [playlists, setPlaylists] = useState<PlaylistSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);

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

  const onCreate = async () => {
    const title = window.prompt("Playlist name");
    if (!title?.trim()) return;
    setCreating(true);
    try {
      const created = await createPlaylist({
        title: title.trim(),
        visibility: "private",
      });
      router.push(playlistPath(created.id));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not create playlist");
    } finally {
      setCreating(false);
    }
  };

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3">
        <Text variant="title-large">Playlists</Text>
        <Button type="button" variant="outlined" disabled={creating} onClick={() => void onCreate()}>
          New playlist
        </Button>
      </div>

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

      {!loading && !error && playlists.length === 0 ? (
        <Card>
          <Text variant="body-medium" className="text-on-surface-variant">
            No playlists yet. Create one to start collecting tracks.
          </Text>
        </Card>
      ) : null}

      {!loading && playlists.length > 0 ? (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {playlists.map((playlist) => (
            <PlaylistCard key={playlist.id} playlist={playlist} />
          ))}
        </div>
      ) : null}
    </section>
  );
}
