import { PlaylistDetailView } from "@/components/discovery/PlaylistDetailView";
import { isNotFoundError } from "@/lib/api/errors";
import { getCachedPlaylist } from "@/lib/api/discoveryServer";
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
    throw error;
  }
}

export default async function PlaylistPage({ params }: PlaylistPageProps) {
  const { playlistId } = await params;

  try {
    const playlist = await getCachedPlaylist(playlistId);
    return <PlaylistDetailView playlistId={playlistId} initialPlaylist={playlist} />;
  } catch (error) {
    if (isNotFoundError(error)) notFound();
    throw error;
  }
}
