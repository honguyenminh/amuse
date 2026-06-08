"use client";

import { PlaylistCoverArt } from "@/components/discovery/PlaylistCoverArt";
import { PlaylistMoreMenu } from "@/components/discovery/PlaylistMoreMenu";
import { PlaylistTrackList } from "@/components/discovery/PlaylistTrackList";
import { PlaylistVisibilityLabel } from "@/components/discovery/PlaylistVisibilityLabel";
import { AppShell } from "@/components/ui/AppShell";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { ConfirmDialog } from "@/components/ui/ConfirmDialog";
import { PlaylistFormDialog } from "@/components/ui/PlaylistFormDialog";
import { PageContent } from "@/components/ui/PageContent";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import {
  deletePlaylist,
  followPlaylist,
  forkPlaylist,
  getLikedPlaylist,
  getLikedPlayableTracks,
  getPlaylist,
  getPlaylistPlayableTracks,
  removeTrackFromPlaylist,
  reorderPlaylistItems,
  replacePlaylistShares,
  savePlaylist,
  unfollowPlaylist,
  unlikeTrack,
  unsavePlaylist,
  updatePlaylist,
} from "@/lib/api/discoveryClient";
import type { PlaylistDetailDto, PlayableTrackDto } from "@/lib/api/types";
import { ApiError } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import { playlistPath } from "@/lib/discovery/paths";
import {
  computeReorderTargetIndex,
  INSERT_AFTER_LAST,
} from "@/lib/discovery/useTrackDragReorder";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { playableTracksFromDtos } from "@/lib/playback/toPlaybackTrack";
import { usePageSeed } from "@/theme/ThemeProvider";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";

const EMPTY_PLAYLIST_ID = "00000000-0000-0000-0000-000000000000";

type PlaylistDetailViewProps = {
  playlistId?: string;
  mode?: "playlist" | "liked";
  /** When true, omits AppShell (e.g. inside library layout). */
  embedded?: boolean;
  initialPlaylist?: PlaylistDetailDto;
};

export function PlaylistDetailView({
  playlistId,
  mode = "playlist",
  embedded = false,
  initialPlaylist,
}: PlaylistDetailViewProps) {
  const isLikedMode = mode === "liked";
  const auth = useAuth();
  const router = useRouter();
  const { state, currentTrack, playQueue, toggle } = usePlayback();

  const [playlist, setPlaylist] = useState<PlaylistDetailDto | null>(
    initialPlaylist ?? null,
  );
  const [playableByTrackId, setPlayableByTrackId] = useState<
    Map<string, PlayableTrackDto>
  >(new Map());
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const [makePrivateDialogOpen, setMakePrivateDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [editDetailsOpen, setEditDetailsOpen] = useState(false);
  const [editShares, setEditShares] = useState(false);
  const [sharesDraft, setSharesDraft] = useState("");
  const [reorderMode, setReorderMode] = useState(false);

  const load = useCallback(async () => {
    setError(null);
    const [detail, playables] = isLikedMode
      ? await Promise.all([getLikedPlaylist(), getLikedPlayableTracks()])
      : await Promise.all([
          getPlaylist(playlistId!),
          getPlaylistPlayableTracks(playlistId!),
        ]);
    setPlaylist(detail);
    setSharesDraft((detail.shareEmails ?? []).join("\n"));
    setPlayableByTrackId(new Map(playables.tracks.map((t) => [t.trackId, t])));
  }, [isLikedMode, playlistId]);

  const loadPlayables = useCallback(async () => {
    const playables = isLikedMode
      ? await getLikedPlayableTracks()
      : await getPlaylistPlayableTracks(playlistId!);
    setPlayableByTrackId(new Map(playables.tracks.map((t) => [t.trackId, t])));
  }, [isLikedMode, playlistId]);

  useEffect(() => {
    let cancelled = false;
    setError(null);

    const hasInitialDetail =
      initialPlaylist && !isLikedMode && initialPlaylist.id === playlistId;

    if (hasInitialDetail) {
      setPlaylist(initialPlaylist);
      setSharesDraft((initialPlaylist.shareEmails ?? []).join("\n"));
      void loadPlayables().catch((err: Error) => {
        if (!cancelled) setError(err.message);
      });
    } else {
      setPlaylist(null);
      setPlayableByTrackId(new Map());
      void load().catch((err: Error) => {
        if (!cancelled) setError(err.message);
      });
    }

    return () => {
      cancelled = true;
    };
  }, [load, loadPlayables, isLikedMode, initialPlaylist, playlistId]);

  useEffect(() => {
    setReorderMode(false);
  }, [playlistId, isLikedMode]);

  usePageSeed(null);

  const runAction = useCallback(
    async (action: () => Promise<void>) => {
      setBusy(true);
      setActionError(null);
      try {
        await action();
        await load();
      } catch (err) {
        setActionError(err instanceof ApiError ? err.message : "Action failed");
      } finally {
        setBusy(false);
      }
    },
    [load],
  );

  const activePlaylistId = playlist?.id ?? playlistId ?? "";
  const canEditTracks =
    playlist?.isOwned === true && activePlaylistId !== EMPTY_PLAYLIST_ID;
  const handleReorder = useCallback(
    (draggedItemId: string, insertBeforeId: string) => {
      if (!playlist || !canEditTracks) return;
      const sorted = [...playlist.items].sort((a, b) => a.position - b.position);
      const fromIndex = sorted.findIndex((item) => item.itemId === draggedItemId);
      if (fromIndex < 0) return;

      const insertBeforeIndex =
        insertBeforeId === INSERT_AFTER_LAST
          ? sorted.length
          : sorted.findIndex((item) => item.itemId === insertBeforeId);
      if (insertBeforeIndex < 0) return;

      const toIndex = computeReorderTargetIndex(fromIndex, insertBeforeIndex);
      if (toIndex === null) return;

      void runAction(async () => {
        await reorderPlaylistItems(activePlaylistId, {
          itemId: draggedItemId,
          newPosition: toIndex + 1,
        });
      });
    },
    [playlist, canEditTracks, runAction, activePlaylistId],
  );

  const removeTrack = useCallback(
    (item: PlaylistDetailDto["items"][number]) => {
      if (!canEditTracks) return;
      void runAction(async () => {
        if (isLikedMode) {
          await unlikeTrack(item.trackId);
        } else {
          await removeTrackFromPlaylist(activePlaylistId, item.itemId);
        }
      });
    },
    [canEditTracks, runAction, isLikedMode, activePlaylistId],
  );

  const playbackTracks = useMemo(
    () => playableTracksFromDtos([...playableByTrackId.values()]),
    [playableByTrackId],
  );

  const playAll = useCallback(() => {
    if (playbackTracks.length > 0) playQueue(playbackTracks, 0);
  }, [playbackTracks, playQueue]);

  const playFromTrack = useCallback(
    (trackId: string) => {
      const idx = playbackTracks.findIndex((t) => t.id === trackId);
      if (idx < 0) return;
      playQueue(playbackTracks, idx);
    },
    [playbackTracks, playQueue],
  );

  const isPlayingThisPlaylist =
    currentTrack !== null &&
    playlist !== null &&
    playlist.items.some((item) => item.trackId === currentTrack.id);

  const onFork = () => {
    void runAction(async () => {
      const forked = await forkPlaylist(playlistId!);
      router.push(playlistPath(forked.id));
    });
  };

  const onToggleSave = () => {
    if (!playlist) return;
    void runAction(async () => {
      if (playlist.isSaved) {
        await unsavePlaylist(playlistId!);
      } else {
        await savePlaylist(playlistId!);
      }
    });
  };

  const onToggleFollow = () => {
    if (!playlist) return;
    void runAction(async () => {
      if (playlist.isFollowed) {
        await unfollowPlaylist(playlistId!);
      } else {
        await followPlaylist(playlistId!);
      }
    });
  };

  const onToggleVisibility = () => {
    if (!playlist || !canEditTracks) return;
    if (playlist.visibility === "public") {
      setMakePrivateDialogOpen(true);
      return;
    }
    void runAction(async () => {
      await updatePlaylist(activePlaylistId, { visibility: "public" });
    });
  };

  const onConfirmMakePrivate = () => {
    setMakePrivateDialogOpen(false);
    void runAction(async () => {
      await updatePlaylist(activePlaylistId, { visibility: "private" });
    });
  };

  const onSaveShares = () => {
    const emails = sharesDraft
      .split(/[\n,;]+/)
      .map((e) => e.trim())
      .filter(Boolean);
    void runAction(async () => {
      await replacePlaylistShares(activePlaylistId, { emails });
      setEditShares(false);
    });
  };

  const onDelete = () => {
    setDeleteDialogOpen(true);
  };

  const onConfirmDelete = () => {
    setDeleteDialogOpen(false);
    void runAction(async () => {
      await deletePlaylist(activePlaylistId);
      router.push("/library/playlists");
    });
  };

  const onConfirmEditDetails = ({
    title,
    description,
  }: {
    title: string;
    description: string;
  }) => {
    setEditDetailsOpen(false);
    void runAction(async () => {
      await updatePlaylist(activePlaylistId, {
        title,
        description,
      });
    });
  };

  const coverArtUrls = useMemo(() => {
    if (!playlist) return [];
    const seen = new Set<string>();
    const urls: string[] = [];
    for (const item of [...playlist.items].sort((a, b) => a.position - b.position)) {
      if (!item.coverArtUrl || seen.has(item.coverArtUrl)) continue;
      seen.add(item.coverArtUrl);
      urls.push(item.coverArtUrl);
      if (urls.length >= 3) break;
    }
    return urls;
  }, [playlist]);

  const ownerName = playlist?.owner?.displayName ?? "Unknown listener";
  const chromeTitle = isLikedMode ? "Liked" : (playlist?.title ?? "Playlist");

  const content = (
    <>
      {error ? (
        <Card>
          <Text variant="title-large">Could not load playlist</Text>
          <Text variant="label-medium">{error}</Text>
        </Card>
      ) : null}

      {!playlist && !error ? <PlaylistSkeleton /> : null}

      {playlist ? (
        <>
          <Card>
              <div className="flex items-start gap-2">
                <div className="flex min-w-0 flex-1 flex-col gap-4 sm:flex-row sm:items-start">
                <PlaylistCoverArt coverArtUrls={coverArtUrls} variant="hero" />
                <div className="flex min-w-0 flex-1 flex-col gap-1">
                  <PlaylistVisibilityLabel
                    visibility={playlist.visibility}
                    prefix={isLikedMode ? "Liked · " : undefined}
                  />
                  <Text variant="headline-medium">{playlist.title}</Text>
                  {!isLikedMode && playlist.owner && !playlist.isOwned ? (
                    <Text variant="title-medium">{ownerName}</Text>
                  ) : null}
                  {!isLikedMode && playlist.description ? (
                    <Text variant="body-medium" className="text-on-surface-variant">
                      {playlist.description}
                    </Text>
                  ) : null}
                  <Text variant="label-medium">
                    {playlist.items.length} track{playlist.items.length === 1 ? "" : "s"}
                  </Text>
                  {!isLikedMode && playlist.forkedFromPlaylistId ? (
                    <Link
                      href={playlistPath(playlist.forkedFromPlaylistId)}
                      className="text-label-medium text-primary underline"
                    >
                      Forked from original playlist
                    </Link>
                  ) : null}
                  <div className="mt-3 flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="primary"
                      onClick={isPlayingThisPlaylist && state.isPlaying ? toggle : playAll}
                      disabled={playbackTracks.length === 0}
                    >
                      {isPlayingThisPlaylist && state.isPlaying ? "Pause" : "Play all"}
                    </Button>

                    {!isLikedMode && !playlist.isOwned && auth.isAuthenticated ? (
                      <>
                        <Button
                          type="button"
                          variant="outlined"
                          disabled={busy}
                          onClick={onToggleSave}
                        >
                          {playlist.isSaved ? "Unsave" : "Save"}
                        </Button>
                        {playlist.visibility === "public" ? (
                          <Button
                            type="button"
                            variant="outlined"
                            disabled={busy}
                            onClick={onToggleFollow}
                          >
                            {playlist.isFollowed ? "Unfollow" : "Follow"}
                          </Button>
                        ) : null}
                        <Button
                          type="button"
                          variant="outlined"
                          disabled={busy}
                          onClick={onFork}
                        >
                          Fork
                        </Button>
                      </>
                    ) : null}
                  </div>
                  {actionError ? (
                    <Text variant="label-medium" className="mt-2 text-error">
                      {actionError}
                    </Text>
                  ) : null}
                </div>
                </div>
                {playlist.isOwned && auth.isAuthenticated ? (
                  <PlaylistMoreMenu
                    reorderMode={reorderMode}
                    onReorderModeChange={setReorderMode}
                    visibility={playlist.visibility}
                    canEdit={canEditTracks}
                    isDeletable={playlist.isDeletable}
                    busy={busy}
                    onToggleVisibility={onToggleVisibility}
                    onEditShares={() => setEditShares(true)}
                    onEditDetails={() => setEditDetailsOpen(true)}
                    showEditDetails={!isLikedMode}
                    onDelete={onDelete}
                  />
                ) : null}
              </div>
            </Card>

            {editShares && playlist.isOwned && playlist.visibility === "private" && canEditTracks ? (
              <Card>
                <Text variant="title-large">Share with emails</Text>
                <Text variant="label-medium" className="mt-1 text-on-surface-variant">
                  One email per line, or comma-separated.
                </Text>
                <textarea
                  className="mt-2 min-h-24 w-full rounded-md border-2 border-outline bg-background p-2 text-body-medium"
                  value={sharesDraft}
                  onChange={(e) => setSharesDraft(e.target.value)}
                />
                <Button type="button" className="mt-2" disabled={busy} onClick={onSaveShares}>
                  Save shares
                </Button>
              </Card>
            ) : null}

            <Card>
              <div className="flex items-center justify-between gap-2">
                <Text variant="title-large">Tracks</Text>
                {reorderMode ? (
                  <Text variant="label-medium" className="text-on-surface-variant">
                    Drag tracks to reorder
                  </Text>
                ) : null}
              </div>
              <div className="mt-2">
                <PlaylistTrackList
                  items={playlist.items}
                  playableByTrackId={playableByTrackId}
                  reorderMode={reorderMode}
                  canEdit={canEditTracks}
                  isLikedMode={isLikedMode}
                  currentTrackId={currentTrack?.id ?? null}
                  isPlaying={state.isPlaying}
                  onReorder={handleReorder}
                  onRemove={removeTrack}
                  onPlayTrack={playFromTrack}
                  onToggle={toggle}
                />
              </div>
            </Card>
        </>
      ) : null}
    </>
  );

  const dialog = (
    <>
      <ConfirmDialog
        open={makePrivateDialogOpen}
        title="Make playlist private?"
        destructive
        confirmLabel="Make private"
        confirmDisabled={busy}
        onClose={() => setMakePrivateDialogOpen(false)}
        onConfirm={onConfirmMakePrivate}
        description={
          isLikedMode ? (
            <Text variant="body-medium">
              Your liked collection will only be visible to you and people you share it with.
            </Text>
          ) : (
            <>
              <Text variant="body-medium">
                Making a public playlist private has lasting effects for people who engaged with
                it:
              </Text>
              <ul className="list-disc space-y-1 pl-5 text-body-medium">
                <li>All forks are cut loose — they become standalone playlists, no longer linked to this one.</li>
                <li>All followers are removed and will stop receiving updates.</li>
                <li>People who saved it to their library can still access it.</li>
              </ul>
              <Text variant="label-medium">
                This is generally discouraged unless you have a good reason.
              </Text>
            </>
          )
        }
      />
      <ConfirmDialog
        open={deleteDialogOpen}
        title="Delete playlist?"
        destructive
        confirmLabel="Delete"
        confirmDisabled={busy}
        onClose={() => setDeleteDialogOpen(false)}
        onConfirm={onConfirmDelete}
        description={
          <Text variant="body-medium">This cannot be undone.</Text>
        }
      />
      <PlaylistFormDialog
        open={editDetailsOpen}
        title="Edit playlist"
        initialTitle={playlist?.title ?? ""}
        initialDescription={playlist?.description ?? ""}
        confirmLabel="Save"
        confirmDisabled={busy}
        onClose={() => setEditDetailsOpen(false)}
        onConfirm={onConfirmEditDetails}
      />
    </>
  );

  if (embedded) {
    return (
      <>
        <div className="flex flex-col gap-4">{content}</div>
        {dialog}
      </>
    );
  }

  return (
    <AppShell title={chromeTitle} activePath={isLikedMode ? "/library/liked" : "/library"}>
      <PageContent gap="4">{content}</PageContent>
      {dialog}
    </AppShell>
  );
}

function PlaylistSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <Card>
        <Skeleton className="h-8 w-2/3" />
        <Skeleton className="mt-2 h-5 w-1/2" />
        <Skeleton className="mt-4 h-10 w-24" />
      </Card>
      <Card>
        <Skeleton className="mb-3 h-6 w-24" />
        <div className="flex flex-col gap-2">
          {Array.from({ length: 5 }, (_, i) => (
            <Skeleton key={i} className="h-6 w-full" />
          ))}
        </div>
      </Card>
    </div>
  );
}
