/** Matches backend `PlaylistTitle.MaxLength`. */
export const MAX_PLAYLIST_TITLE_LENGTH = 200;

const FORK_SUFFIX = " (fork)";

export function defaultForkPlaylistTitle(sourceTitle: string): string {
  const trimmed = sourceTitle.trim();
  if (trimmed.length + FORK_SUFFIX.length <= MAX_PLAYLIST_TITLE_LENGTH) {
    return `${trimmed}${FORK_SUFFIX}`;
  }
  return `${trimmed.slice(0, MAX_PLAYLIST_TITLE_LENGTH - FORK_SUFFIX.length)}${FORK_SUFFIX}`;
}
