import type { PlaybackTrack } from "./types";

export function countNewQueueTracks(
  tracks: PlaybackTrack[],
  queue: PlaybackTrack[],
): PlaybackTrack[] {
  const existing = new Set(queue.map((track) => track.id));
  return tracks.filter((track) => !existing.has(track.id));
}

export function queueAddSnackbarMessage(
  newTracks: PlaybackTrack[],
  options?: { releaseTitle?: string },
): string {
  if (newTracks.length === 0) return "Already in queue";
  if (newTracks.length === 1) return `Added “${newTracks[0]!.title}” to queue`;
  if (options?.releaseTitle) {
    return `Added “${options.releaseTitle}” to queue (${newTracks.length} tracks)`;
  }
  return `Added ${newTracks.length} tracks to queue`;
}
