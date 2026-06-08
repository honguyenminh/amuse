import { PlaylistDetailView } from "@/components/discovery/PlaylistDetailView";
import {
  isNotFoundError,
  shouldFallbackToClientFetch,
} from "@/lib/api/errors";
import {
  getCachedPlaylist,
  getCachedPlaylistPlayableTracks,
} from "@/lib/api/discoveryServer";
import { playlistMetadata } from "@/lib/seo/metadata";
import type { Metadata } from "next";
import { notFound } from "next/navigation";

export const revalidate = 3600;

type PlaylistPageProps = {
  params: Promise<{ playlistId: string }>;
};

export async function generateMetadata({ params }: PlaylistPageProps): Promise<Metadata> {
  const { playlistId } = await params;
  try {
    const playlist = await getCachedPlaylist(playlistId);
    return playlistMetadata(playlist);
  } catch (error) {
    if (isNotFoundError(error)) return {};
    if (shouldFallbackToClientFetch(error)) {
      return { title: "Playlist | Amuse" };
    }
    throw error;
  }
}

export default async function PlaylistPage({ params }: PlaylistPageProps) {
  const { playlistId } = await params;

  let clientFallback = false;
  let playlist;
  let initialPlayableTracks;
  try {
    [playlist, { tracks: initialPlayableTracks }] = await Promise.all([
      getCachedPlaylist(playlistId),
      getCachedPlaylistPlayableTracks(playlistId),
    ]);
  } catch (error) {
    if (isNotFoundError(error)) notFound();
    if (shouldFallbackToClientFetch(error)) {
      clientFallback = true;
    } else {
      throw error;
    }
  }

  if (clientFallback) {
    return <PlaylistDetailView playlistId={playlistId} />;
  }

  return (
    <PlaylistDetailView
      playlistId={playlistId}
      initialPlaylist={playlist}
      initialPlayableTracks={initialPlayableTracks}
    />
  );
}
