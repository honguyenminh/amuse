export function formatTrackSubtitle(
  artistName?: string | null,
  releaseTitle?: string | null,
): string | null {
  const artist = artistName?.trim();
  const release = releaseTitle?.trim();
  if (artist && release) return `${artist} · ${release}`;
  if (artist) return artist;
  if (release) return release;
  return null;
}
