import { useEffect, useRef } from "react";
import type { PlaybackState, PlaybackTrack } from "./types";

const SEEK_SKIP_SEC = 10;

type MediaSessionHandlers = {
  play: () => void;
  pause: () => void;
  next: () => void;
  previous: () => void;
  seek: (positionMs: number) => void;
};

function artworkForTrack(track: PlaybackTrack): MediaImage[] {
  if (!track.coverArtUrl) return [];
  return [
    { src: track.coverArtUrl, sizes: "96x96", type: "image/jpeg" },
    { src: track.coverArtUrl, sizes: "256x256", type: "image/jpeg" },
    { src: track.coverArtUrl, sizes: "512x512", type: "image/jpeg" },
  ];
}

function clearActionHandlers() {
  if (!("mediaSession" in navigator)) return;
  const actions: MediaSessionAction[] = [
    "play",
    "pause",
    "previoustrack",
    "nexttrack",
    "seekbackward",
    "seekforward",
    "seekto",
    "stop",
  ];
  for (const action of actions) {
    try {
      navigator.mediaSession.setActionHandler(action, null);
    } catch {
      // Some platforms reject unsupported actions.
    }
  }
}

/**
 * Publishes now-playing metadata to the OS media overlay and routes system
 * transport controls back into the app playback context.
 */
export function useMediaSession(
  currentTrack: PlaybackTrack | null,
  state: PlaybackState,
  handlers: MediaSessionHandlers,
) {
  const { play, pause, next, previous, seek } = handlers;
  const positionMsRef = useRef(state.positionMs);
  const durationMsRef = useRef(state.durationMs);

  useEffect(() => {
    positionMsRef.current = state.positionMs;
    durationMsRef.current = state.durationMs;
  }, [state.positionMs, state.durationMs]);

  useEffect(() => {
    if (!("mediaSession" in navigator)) return;

    if (!currentTrack) {
      navigator.mediaSession.metadata = null;
      navigator.mediaSession.playbackState = "none";
      clearActionHandlers();
      return;
    }

    navigator.mediaSession.metadata = new MediaMetadata({
      title: currentTrack.title,
      artist: currentTrack.artistName,
      album: currentTrack.releaseTitle,
      artwork: artworkForTrack(currentTrack),
    });

    const onPlay = () => play();
    const onPause = () => pause();
    const onPrevious = () => previous();
    const onNext = () => next();
    const onSeekBackward = (details: MediaSessionActionDetails) => {
      const offsetSec = details.seekOffset ?? SEEK_SKIP_SEC;
      seek(Math.max(0, positionMsRef.current - offsetSec * 1000));
    };
    const onSeekForward = (details: MediaSessionActionDetails) => {
      const offsetSec = details.seekOffset ?? SEEK_SKIP_SEC;
      const duration =
        durationMsRef.current > 0 ? durationMsRef.current : Number.POSITIVE_INFINITY;
      seek(Math.min(duration, positionMsRef.current + offsetSec * 1000));
    };
    const onSeekTo = (details: MediaSessionActionDetails) => {
      if (details.seekTime == null) return;
      seek(details.seekTime * 1000);
    };
    const onStop = () => pause();

    navigator.mediaSession.setActionHandler("play", onPlay);
    navigator.mediaSession.setActionHandler("pause", onPause);
    navigator.mediaSession.setActionHandler("previoustrack", onPrevious);
    navigator.mediaSession.setActionHandler("nexttrack", onNext);
    navigator.mediaSession.setActionHandler("seekbackward", onSeekBackward);
    navigator.mediaSession.setActionHandler("seekforward", onSeekForward);
    navigator.mediaSession.setActionHandler("seekto", onSeekTo);
    navigator.mediaSession.setActionHandler("stop", onStop);

    return () => {
      clearActionHandlers();
    };
  }, [currentTrack, play, pause, next, previous, seek]);

  useEffect(() => {
    if (!("mediaSession" in navigator)) return;
    if (!currentTrack) return;
    navigator.mediaSession.playbackState = state.isPlaying ? "playing" : "paused";
  }, [currentTrack, state.isPlaying]);

  useEffect(() => {
    if (!("mediaSession" in navigator)) return;
    if (!currentTrack) return;
    if (!("setPositionState" in navigator.mediaSession)) return;

    const durationSec = state.durationMs > 0 ? state.durationMs / 1000 : 0;
    const positionSec = Math.min(
      durationSec > 0 ? durationSec : Number.POSITIVE_INFINITY,
      Math.max(0, state.positionMs / 1000),
    );

    try {
      navigator.mediaSession.setPositionState({
        duration: durationSec,
        playbackRate: 1,
        position: positionSec,
      });
    } catch {
      // Browsers may reject position updates before metadata is ready.
    }
  }, [currentTrack, state.positionMs, state.durationMs]);
}
