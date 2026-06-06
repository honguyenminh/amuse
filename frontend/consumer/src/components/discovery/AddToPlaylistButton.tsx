"use client";

import { AnchoredPopup } from "@/components/ui/AnchoredPopup";
import { Button } from "@/components/ui/Button";
import { Text } from "@/components/ui/Text";
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

export function AddToPlaylistButton({ trackIds, disabled = false }: AddToPlaylistButtonProps) {
  const auth = useAuth();
  const anchorRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [playlists, setPlaylists] = useState<{ id: string; title: string }[]>([]);
  const [addingTo, setAddingTo] = useState<string | null>(null);

  const loadAndOpen = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await listMyPlaylists();
      const owned = response.playlists
        .filter((playlist) => playlist.isOwned)
        .map((playlist) => ({ id: playlist.id, title: playlist.title }));
      setPlaylists(owned);
      setOpen(true);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not load playlists");
    } finally {
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
          disabled={disabled || loading || trackIds.length === 0}
          onClick={() => void loadAndOpen()}
        >
          {loading ? "Loading…" : "Add to playlist"}
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
        className="min-w-[12rem] rounded-md border-2 border-outline bg-surface py-1 shadow-lg"
        role="menu"
      >
        {playlists.length === 0 ? (
          <Text variant="body-medium" className="px-4 py-2 text-on-surface-variant">
            No playlists yet
          </Text>
        ) : (
          playlists.map((playlist) => (
            <button
              key={playlist.id}
              type="button"
              role="menuitem"
              disabled={addingTo !== null}
              className="flex w-full px-4 py-2 text-left text-body-medium hover:bg-surface-variant disabled:opacity-50"
              onClick={() => void onSelectPlaylist(playlist.id)}
            >
              {addingTo === playlist.id ? `Adding to ${playlist.title}…` : playlist.title}
            </button>
          ))
        )}
        {error && open ? (
          <Text variant="label-medium" className="px-4 py-2 text-error">
            {error}
          </Text>
        ) : null}
      </AnchoredPopup>
    </>
  );
}
