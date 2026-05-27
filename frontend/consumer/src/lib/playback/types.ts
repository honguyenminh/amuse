export type PlaybackTrack = {
  id: string;
  title: string;
  trackNumber: number;
  durationMs: number;
  artistId: string;
  artistName: string;
  releaseId: string;
  releaseTitle: string;
  coverArtUrl: string | null;
};

export type RepeatMode = "off" | "queue" | "one";

export type PlaybackState = {
  queue: PlaybackTrack[];
  currentIndex: number;
  isPlaying: boolean;
  positionMs: number;
  durationMs: number;
  volume: number;
  repeat: RepeatMode;
  shuffle: boolean;
};

export const initialPlaybackState: PlaybackState = {
  queue: [],
  currentIndex: -1,
  isPlaying: false,
  positionMs: 0,
  durationMs: 0,
  volume: 0.85,
  repeat: "off",
  shuffle: false,
};

export type PlaybackAction =
  | { type: "playQueue"; tracks: PlaybackTrack[]; startIndex?: number }
  | { type: "play" }
  | { type: "pause" }
  | { type: "toggle" }
  | { type: "next" }
  | { type: "previous" }
  | { type: "seek"; positionMs: number }
  | { type: "tick"; positionMs: number; durationMs?: number }
  | { type: "trackEnded" }
  | { type: "setVolume"; volume: number }
  | { type: "setRepeat"; mode: RepeatMode }
  | { type: "toggleShuffle" }
  | { type: "clear" };
