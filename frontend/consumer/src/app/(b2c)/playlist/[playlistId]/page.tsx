import { PlaylistDetailView } from "@/components/discovery/PlaylistDetailView";

type PlaylistPageProps = {
  params: Promise<{ playlistId: string }>;
};

export default async function PlaylistPage({ params }: PlaylistPageProps) {
  const { playlistId } = await params;
  return <PlaylistDetailView playlistId={playlistId} />;
}
