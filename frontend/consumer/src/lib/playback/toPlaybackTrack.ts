import type {
  GetReleaseDetailResponse,
  PlayableTrackDto,
  TrackResponse,
} from "@/lib/api/types";
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

export function fromPlayableTrackDto(track: PlayableTrackDto): PlaybackTrack {
  return {
    id: track.trackId,
    title: track.title,
    trackNumber: track.trackNumber,
    durationMs: track.durationMs,
    artistId: "",
    artistName: track.artistName,
    artistSlug: track.artistSlug,
    releaseId: track.releaseId,
    releaseTitle: track.releaseTitle,
    releaseSlug: track.releaseSlug,
    coverArtUrl: track.coverArtUrl,
  };
}

export function playableTracksFromDtos(tracks: PlayableTrackDto[]): PlaybackTrack[] {
  return tracks.filter((t) => t.hasAudio).map(fromPlayableTrackDto);
}
