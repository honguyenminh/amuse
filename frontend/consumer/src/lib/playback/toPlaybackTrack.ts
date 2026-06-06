import type { GetReleaseDetailResponse, TrackResponse } from "@/lib/api/types";
import type { PlaybackTrack } from "./types";

export function toPlaybackTrack(
  track: TrackResponse,
  release: Pick<
    GetReleaseDetailResponse,
    | "id"
    | "slug"
    | "title"
    | "artistId"
    | "artistName"
    | "artistSlug"
    | "coverArtUrl"
  >,
): PlaybackTrack {
  return {
    id: track.id,
    title: track.title,
    trackNumber: track.trackNumber,
    durationMs: track.durationMs,
    artistId: release.artistId,
    artistName: release.artistName,
    artistSlug: release.artistSlug,
    releaseId: release.id,
    releaseTitle: release.title,
    releaseSlug: release.slug,
    coverArtUrl: release.coverArtUrl,
  };
}

export function playableTracksFromRelease(
  release: GetReleaseDetailResponse,
): PlaybackTrack[] {
  return release.tracks.filter((t) => t.hasAudio).map((t) => toPlaybackTrack(t, release));
}
