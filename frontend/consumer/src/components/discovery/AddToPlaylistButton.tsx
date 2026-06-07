"use client";

import { AnchoredPopup } from "@/components/ui/AnchoredPopup";
import { Button } from "@/components/ui/Button";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { cn } from "@/lib/cn";
import { listMyPlaylists } from "@/lib/api/discoveryClient";
import { ApiError } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import { addTracksToPlaylist } from "@/lib/playback/playlistContextMenu";
import Link from "next/link";
import { useCallback, useRef, useState } from "react";

type AddToPlaylistButtonProps = {
  trackIds: string[];
  disabled?: boolean;
};

function PlaylistMenuSkeleton() {
  return (
    <div className="py-1" aria-hidden>
      {Array.from({ length: 4 }, (_, index) => (
        <div key={index} className="px-4 py-2">
          <Skeleton className="h-5 w-3/4" />
        </div>
      ))}
    </div>
  );
}

export function AddToPlaylistButton({ trackIds, disabled = false }: AddToPlaylistButtonProps) {
  const auth = useAuth();
  const anchorRef = useRef<HTMLDivElement>(null);
  const loadingRef = useRef(false);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [playlists, setPlaylists] = useState<{ id: string; title: string }[]>([]);
  const [addingTo, setAddingTo] = useState<string | null>(null);

  const loadAndOpen = useCallback(async () => {
    if (loadingRef.current) {
      return;
    }

    loadingRef.current = true;
    setOpen(true);
    setLoading(true);
    setError(null);
    setPlaylists([]);
    try {
      const response = await listMyPlaylists();
      const owned = response.playlists
        .filter((playlist) => playlist.isOwned)
        .map((playlist) => ({ id: playlist.id, title: playlist.title }));
      setPlaylists(owned);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load playlists");
    } finally {
      loadingRef.current = false;
      setLoading(false);
    }
  }, []);

  const onSelectPlaylist = useCallback(
    async (playlistId: string) => {
      setAddingTo(playlistId);
      setError(null);
      try {
        await addTracksToPlaylist(playlistId, trackIds);
        setOpen(false);
      } catch (err) {
        setError(err instanceof ApiError ? err.message : "Could not add to playlist");
      } finally {
        setAddingTo(null);
      }
    },
    [trackIds],
  );

  const loadedContent =
    error && playlists.length === 0 ? (
      <Text variant="body-medium" className="px-4 py-2 text-error">
        {error}
      </Text>
    ) : playlists.length === 0 ? (
      <Text variant="body-medium" className="px-4 py-2 text-on-surface-variant">
        No playlists yet
      </Text>
    ) : (
      <>
        {playlists.map((playlist) => {
          const isAdding = addingTo === playlist.id;
          return (
            <button
              key={playlist.id}
              type="button"
              role="menuitem"
              disabled={addingTo !== null}
              aria-busy={isAdding}
              className={cn(
                "flex w-full px-4 py-2 text-left text-body-medium transition-opacity duration-150 hover:bg-surface-variant disabled:opacity-50",
                isAdding && "opacity-60",
              )}
              onClick={() => void onSelectPlaylist(playlist.id)}
            >
              {playlist.title}
            </button>
          );
        })}
        {error ? (
          <Text variant="label-medium" className="px-4 py-2 text-error">
            {error}
          </Text>
        ) : null}
      </>
    );

  if (!auth.isAuthenticated) {
    return (
      <Link href="/login">
        <Button type="button" variant="outlined" disabled={disabled}>
          Add to playlist
        </Button>
      </Link>
    );
  }

  return (
    <>
      <div ref={anchorRef} className="flex flex-col gap-1">
        <Button
          type="button"
          variant="outlined"
          disabled={disabled || trackIds.length === 0}
          aria-busy={loading}
          aria-expanded={open}
          onClick={() => void loadAndOpen()}
        >
          Add to playlist
        </Button>
        {error && !open ? (
          <span className="text-label-medium text-error">{error}</span>
        ) : null}
      </div>

      <AnchoredPopup
        open={open}
        onClose={() => setOpen(false)}
        anchorRef={anchorRef}
        preferredPlacement="bottom"
        align="start"
        layoutKey={loading ? "loading" : playlists.length}
        className="min-w-[12rem] rounded-md border-2 border-outline bg-surface shadow-lg"
        role="menu"
      >
        <div className="grid [&>*]:col-start-1 [&>*]:row-start-1">
          <div
            aria-hidden={!loading}
            className={cn(
              "transition-opacity duration-200 ease-out motion-reduce:transition-none",
              loading ? "opacity-100" : "pointer-events-none opacity-0",
            )}
          >
            <PlaylistMenuSkeleton />
          </div>
          <div
            aria-hidden={loading}
            className={cn(
              "py-1 transition-opacity duration-200 ease-out motion-reduce:transition-none",
              loading ? "pointer-events-none opacity-0" : "opacity-100",
            )}
          >
            {loadedContent}
          </div>
        </div>
      </AnchoredPopup>
    </>
  );
}
