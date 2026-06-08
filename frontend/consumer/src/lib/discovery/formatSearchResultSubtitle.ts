export function searchItemKindLabel(kind: string): string {
  switch (kind) {
    case "artist":
      return "Artist";
    case "release":
      return "Release";
    case "track":
      return "Track";
    case "playlist":
      return "Playlist";
    default:
      return kind.length > 0 ? kind.charAt(0).toUpperCase() + kind.slice(1) : kind;
  }
}

export function formatSearchItemSubtitle(kind: string, subtitle: string | null): string {
  const type = searchItemKindLabel(kind);
  const detail = subtitle?.trim();
  if (!detail) return type;
  return `${type} · ${detail}`;
}

export function formatPlaylistSearchSubtitle(ownerName: string, trackCount: number): string {
  const tracks = `${trackCount} track${trackCount === 1 ? "" : "s"}`;
  return `Playlist · ${ownerName} · ${tracks}`;
}
