"use client";

import { getTrackStreamInfo } from "@/lib/api/catalogClient";
import { useTheme } from "@/theme/ThemeProvider";
import {
  deterministicSeedFromString,
  extractSeedFromImage,
} from "@/theme/extractSeedFromImage";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useReducer,
  useRef,
  type ReactNode,
} from "react";
import { playbackReducer } from "./reducer";
import {
  initialPlaybackState,
  type PlaybackState,
  type PlaybackTrack,
  type RepeatMode,
} from "./types";

type PlaybackContextValue = {
  state: PlaybackState;
  currentTrack: PlaybackTrack | null;
  playQueue: (tracks: PlaybackTrack[], startIndex?: number) => void;
  toggle: () => void;
  play: () => void;
  pause: () => void;
  next: () => void;
  previous: () => void;
  seek: (positionMs: number) => void;
  setVolume: (volume: number) => void;
  setRepeat: (mode: RepeatMode) => void;
  toggleShuffle: () => void;
};

const PlaybackContext = createContext<PlaybackContextValue | null>(null);

export function PlaybackProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(playbackReducer, initialPlaybackState);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const lastLoadedTrackIdRef = useRef<string | null>(null);
  const { setPlayingSeed, setPaused } = useTheme();

  const currentTrack = state.currentIndex >= 0 ? state.queue[state.currentIndex] ?? null : null;

  // Lazily create the single audio element on the client only.
  useEffect(() => {
    if (audioRef.current) return;
    const audio = new Audio();
    audio.preload = "metadata";
    audio.volume = initialPlaybackState.volume;

    audio.addEventListener("timeupdate", () => {
      dispatch({
        type: "tick",
        positionMs: Math.floor(audio.currentTime * 1000),
        durationMs: Number.isFinite(audio.duration) ? Math.floor(audio.duration * 1000) : undefined,
      });
    });
    audio.addEventListener("ended", () => dispatch({ type: "trackEnded" }));
    audio.addEventListener("loadedmetadata", () => {
      dispatch({
        type: "tick",
        positionMs: 0,
        durationMs: Number.isFinite(audio.duration) ? Math.floor(audio.duration * 1000) : undefined,
      });
    });

    audioRef.current = audio;
  }, []);

  // Load the current track's stream URL when the queue position changes.
  useEffect(() => {
    const audio = audioRef.current;
    if (!audio) return;
    if (!currentTrack) {
      audio.pause();
      audio.removeAttribute("src");
      lastLoadedTrackIdRef.current = null;
      return;
    }
    if (lastLoadedTrackIdRef.current === currentTrack.id) return;
    lastLoadedTrackIdRef.current = currentTrack.id;

    let cancelled = false;
    void (async () => {
      try {
        const info = await getTrackStreamInfo(currentTrack.id);
        if (cancelled || lastLoadedTrackIdRef.current !== currentTrack.id) return;
        audio.src = info.url;
        audio.currentTime = 0;
        if (state.isPlaying) {
          await audio.play().catch(() => undefined);
        }
      } catch {
        // Stream URL fetch failed; pause until the user retries.
        dispatch({ type: "pause" });
      }
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- isPlaying changes drive a different effect.
  }, [currentTrack?.id]);

  // Drive play/pause from reducer state to the DOM element.
  useEffect(() => {
    const audio = audioRef.current;
    if (!audio) return;
    if (state.isPlaying && audio.src) {
      void audio.play().catch(() => undefined);
    } else {
      audio.pause();
    }
  }, [state.isPlaying]);

  // Sync volume.
  useEffect(() => {
    if (audioRef.current) audioRef.current.volume = state.volume;
  }, [state.volume]);

  // Theme bridge: feed the current cover into the playing seed precedence.
  useEffect(() => {
    if (!currentTrack) {
      setPlayingSeed(null);
      return;
    }
    const url = currentTrack.coverArtUrl;
    const fallback = url
      ? deterministicSeedFromString(url)
      : deterministicSeedFromString(currentTrack.id);
    setPlayingSeed(fallback);

    if (!url) return;
    let cancelled = false;
    void extractSeedFromImage(url).then((extracted) => {
      if (!cancelled && extracted) setPlayingSeed(extracted);
    });
    return () => {
      cancelled = true;
    };
  }, [currentTrack, setPlayingSeed]);

  // Theme bridge: reflect paused state for the de-saturated palette.
  useEffect(() => {
    setPaused(!state.isPlaying && state.currentIndex >= 0);
  }, [state.isPlaying, state.currentIndex, setPaused]);

  // Seek to externally-dispatched positions (e.g. from the scrubber).
  useEffect(() => {
    const audio = audioRef.current;
    if (!audio || !Number.isFinite(audio.duration)) return;
    const desired = state.positionMs / 1000;
    if (Math.abs(audio.currentTime - desired) > 0.5) {
      audio.currentTime = desired;
    }
  }, [state.positionMs]);

  const playQueue = useCallback(
    (tracks: PlaybackTrack[], startIndex = 0) =>
      dispatch({ type: "playQueue", tracks, startIndex }),
    [],
  );
  const play = useCallback(() => dispatch({ type: "play" }), []);
  const pause = useCallback(() => dispatch({ type: "pause" }), []);
  const toggle = useCallback(() => dispatch({ type: "toggle" }), []);
  const next = useCallback(() => dispatch({ type: "next" }), []);
  const previous = useCallback(() => dispatch({ type: "previous" }), []);
  const seek = useCallback(
    (positionMs: number) => dispatch({ type: "seek", positionMs }),
    [],
  );
  const setVolume = useCallback(
    (volume: number) => dispatch({ type: "setVolume", volume }),
    [],
  );
  const setRepeat = useCallback(
    (mode: RepeatMode) => dispatch({ type: "setRepeat", mode }),
    [],
  );
  const toggleShuffle = useCallback(() => dispatch({ type: "toggleShuffle" }), []);

  const value = useMemo<PlaybackContextValue>(
    () => ({
      state,
      currentTrack,
      playQueue,
      toggle,
      play,
      pause,
      next,
      previous,
      seek,
      setVolume,
      setRepeat,
      toggleShuffle,
    }),
    [
      state,
      currentTrack,
      playQueue,
      toggle,
      play,
      pause,
      next,
      previous,
      seek,
      setVolume,
      setRepeat,
      toggleShuffle,
    ],
  );

  return <PlaybackContext.Provider value={value}>{children}</PlaybackContext.Provider>;
}

export function usePlayback(): PlaybackContextValue {
  const ctx = useContext(PlaybackContext);
  if (!ctx) throw new Error("usePlayback must be used within PlaybackProvider");
  return ctx;
}
