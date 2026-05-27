"use client";

import { resolveApiUrl } from "@/lib/api/config";
import { getTrackStreamInfo } from "@/lib/api/catalogClient";
import { ApiError } from "@/lib/api/types";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { useTheme } from "@/theme/ThemeProvider";
import {
  deterministicSeedFromString,
  extractSeedFromImage,
} from "@/theme/extractSeedFromImage";
import { useRouter } from "next/navigation";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useReducer,
  useRef,
  useState,
  type MutableRefObject,
  type ReactNode,
} from "react";
import { readAudioDurationMs, readAudioPositionMs } from "./audioPosition";
import { attachDashToAudio } from "./dashPlayer";
import { createPlaybackOutput, type PlaybackOutput } from "./playbackOutput";
import { playbackReducer } from "./reducer";
import { syncAudioTime } from "./syncAudioTime";
import {
  initialPlaybackState,
  type PlaybackState,
  type PlaybackTrack,
  type RepeatMode,
} from "./types";

type PlaybackContextValue = {
  state: PlaybackState;
  currentTrack: PlaybackTrack | null;
  audioRef: MutableRefObject<HTMLAudioElement | null>;
  playQueue: (tracks: PlaybackTrack[], startIndex?: number) => void;
  toggle: () => void;
  play: () => void;
  pause: () => void;
  next: () => void;
  previous: () => void;
  seek: (positionMs: number) => void;
  beginScrub: () => void;
  endScrub: (positionMs: number) => void;
  setVolume: (volume: number) => void;
  setRepeat: (mode: RepeatMode) => void;
  toggleShuffle: () => void;
};

const PlaybackContext = createContext<PlaybackContextValue | null>(null);

export function PlaybackProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(playbackReducer, initialPlaybackState);
  const outputRef = useRef<PlaybackOutput | null>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const dashSessionRef = useRef<{ destroy: () => void } | null>(null);
  const lastLoadedTrackIdRef = useRef<string | null>(null);
  const isPlayingRef = useRef(false);
  const isScrubbingRef = useRef(false);
  const { setPlayingSeed, setPaused } = useTheme();
  const router = useRouter();

  const currentTrack = state.currentIndex >= 0 ? state.queue[state.currentIndex] ?? null : null;
  const resetDashSession = useCallback(() => {
    dashSessionRef.current?.destroy();
    dashSessionRef.current = null;
  }, []);


  isPlayingRef.current = state.isPlaying;

  const applyPositionToAudio = useCallback((positionMs: number, resume: boolean) => {
    const audio = audioRef.current;
    const output = outputRef.current;
    if (!audio || !Number.isFinite(audio.duration)) return;
    syncAudioTime(audio, positionMs / 1000, resume, () => output?.pauseImmediate());
  }, []);

  useEffect(() => {
    if (outputRef.current) return;
    const output = createPlaybackOutput();
    outputRef.current = output;
    audioRef.current = output.audio;
    const audio = output.audio;

    audio.addEventListener("timeupdate", () => {
      // Ignore late timeupdates while paused (e.g. during gain fade before pause()).
      if (isScrubbingRef.current || !isPlayingRef.current) return;
      dispatch({
        type: "tick",
        positionMs: readAudioPositionMs(audio),
        durationMs: readAudioDurationMs(audio),
      });
    });
    audio.addEventListener("ended", () => dispatch({ type: "trackEnded" }));
    audio.addEventListener("loadedmetadata", () => {
      dispatch({
        type: "tick",
        positionMs: readAudioPositionMs(audio),
        durationMs: readAudioDurationMs(audio),
      });
    });

  }, []);

  useEffect(() => {
    outputRef.current?.setVolume(state.volume);
  }, [state.volume]);

  useEffect(() => {
    const audio = audioRef.current;
    if (!audio) return;
    if (!currentTrack) {
      resetDashSession();
      outputRef.current?.pauseImmediate();
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
        resetDashSession();
        outputRef.current?.pauseImmediate();

        const playbackUrl = resolveApiUrl(info.url);
        const isDash =
          info.contentType === "application/dash+xml" ||
          playbackUrl.toLowerCase().endsWith(".mpd");

        if (isDash) {
          const dashSession = await attachDashToAudio(audio, playbackUrl, getAccessToken);
          if (cancelled || lastLoadedTrackIdRef.current !== currentTrack.id) {
            dashSession.destroy();
            return;
          }
          dashSessionRef.current = dashSession;
        } else {
          audio.crossOrigin = "anonymous";
          audio.src = playbackUrl;
        }

        audio.currentTime = 0;
        if (isPlayingRef.current) {
          await outputRef.current?.playSmooth();
        }
      } catch (error) {
        dispatch({ type: "pause" });
        if (
          error instanceof ApiError &&
          (error.status === 401 || error.code === "auth.not_authenticated")
        ) {
          dispatch({ type: "clear" });
          lastLoadedTrackIdRef.current = null;
          const next = encodeURIComponent(
            typeof window !== "undefined" ? window.location.pathname : "/home",
          );
          router.push(`/login?next=${next}`);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [currentTrack?.id, router, resetDashSession]);

  useEffect(
    () => () => {
      resetDashSession();
    },
    [resetDashSession],
  );

  useEffect(() => {
    const output = outputRef.current;
    const audio = audioRef.current;
    if (!output || !audio?.src) return;
    if (state.isPlaying) {
      void output.playSmooth();
    } else {
      void output.pauseSmooth();
    }
  }, [state.isPlaying]);

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

  useEffect(() => {
    setPaused(!state.isPlaying && state.currentIndex >= 0);
  }, [state.isPlaying, state.currentIndex, setPaused]);

  const syncPositionFromAudio = useCallback(() => {
    const audio = audioRef.current;
    if (!audio?.src) return;
    dispatch({
      type: "tick",
      positionMs: readAudioPositionMs(audio),
      durationMs: readAudioDurationMs(audio),
    });
  }, []);

  const playQueue = useCallback(
    (tracks: PlaybackTrack[], startIndex = 0) => {
      if (!getAccessToken()) {
        const next = encodeURIComponent(
          typeof window !== "undefined" ? window.location.pathname : "/home",
        );
        router.push(`/login?next=${next}`);
        return;
      }
      outputRef.current?.prime();
      isPlayingRef.current = true;
      dispatch({ type: "playQueue", tracks, startIndex });
    },
    [router],
  );

  const play = useCallback(() => {
    outputRef.current?.prime();
    isPlayingRef.current = true;
    dispatch({ type: "play" });
  }, []);
  const pause = useCallback(() => {
    syncPositionFromAudio();
    isPlayingRef.current = false;
    dispatch({ type: "pause" });
  }, [syncPositionFromAudio]);
  const toggle = useCallback(() => {
    if (isPlayingRef.current) {
      syncPositionFromAudio();
      isPlayingRef.current = false;
    } else {
      outputRef.current?.prime();
      isPlayingRef.current = true;
    }
    dispatch({ type: "toggle" });
  }, [syncPositionFromAudio]);
  const next = useCallback(() => dispatch({ type: "next" }), []);
  const previous = useCallback(() => {
    if (state.currentIndex < 0) return;
    const restartCurrent = state.positionMs > 3000;
    dispatch({ type: "previous" });
    if (restartCurrent) {
      applyPositionToAudio(0, isPlayingRef.current);
    }
  }, [state.currentIndex, state.positionMs, applyPositionToAudio]);

  const seek = useCallback(
    (positionMs: number) => {
      const ms = Math.max(0, positionMs);
      dispatch({ type: "seek", positionMs: ms });
      applyPositionToAudio(ms, isPlayingRef.current);
    },
    [applyPositionToAudio],
  );

  const beginScrub = useCallback(() => {
    isScrubbingRef.current = true;
  }, []);

  const endScrub = useCallback(
    (positionMs: number) => {
      isScrubbingRef.current = false;
      const ms = Math.max(0, positionMs);
      dispatch({ type: "seek", positionMs: ms });
      applyPositionToAudio(ms, isPlayingRef.current);
    },
    [applyPositionToAudio],
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
      audioRef,
      playQueue,
      toggle,
      play,
      pause,
      next,
      previous,
      seek,
      beginScrub,
      endScrub,
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
      beginScrub,
      endScrub,
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

/**
 * Playhead position for sliders. While playing, sampled every frame from the
 * audio element. While paused, follows reducer `positionMs` so seek / previous
 * update the bar immediately without waiting for `timeupdate`.
 */
export function usePlaybackPosition(): number {
  const { state, audioRef } = usePlayback();
  const [playheadMs, setPlayheadMs] = useState(state.positionMs);
  const resumeFromPauseRef = useRef(true);

  useEffect(() => {
    if (state.isPlaying) return;
    resumeFromPauseRef.current = true;
    setPlayheadMs(state.positionMs);
  }, [state.isPlaying, state.positionMs, state.currentIndex]);

  useEffect(() => {
    if (!state.isPlaying) return;
    const audio = audioRef.current;
    if (!audio) return;

    if (resumeFromPauseRef.current) {
      resumeFromPauseRef.current = false;
      setPlayheadMs(state.positionMs);
    } else {
      setPlayheadMs(readAudioPositionMs(audio));
    }

    let raf = 0;
    const loop = () => {
      setPlayheadMs(readAudioPositionMs(audio));
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, [audioRef, state.isPlaying, state.currentIndex]);

  return state.isPlaying ? playheadMs : state.positionMs;
}
