"use client";

import { resolveApiUrl } from "@/lib/api/config";
import { getTrackStreamInfo } from "@/lib/api/catalogClient";
import { ApiError, type TrackStreamRenditionDto } from "@/lib/api/types";
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
import { attachDashToAudio, type ActiveRenditionInfo } from "./dashPlayer";
import { loadPlaybackSettings, savePlaybackSettings } from "./playbackSettings";
import { getNetworkHints } from "./networkHints";
import { selectRendition } from "./selectRendition";
import { createPlaybackOutput, type PlaybackOutput } from "./playbackOutput";
import { playbackReducer } from "./reducer";
import { syncAudioTime } from "./syncAudioTime";
import {
  initialPlaybackState,
  type PlaybackState,
  type PlaybackTrack,
  type RepeatMode,
} from "./types";

function initialStateFromSettings(): PlaybackState {
  const settings = loadPlaybackSettings();
  return { ...initialPlaybackState, volume: settings.volume };
}

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
  addToQueue: (tracks: PlaybackTrack[]) => void;
  playNext: (tracks: PlaybackTrack[]) => void;
  streamRenditions: TrackStreamRenditionDto[];
  activeRendition: ActiveRenditionInfo | null;
  switchRendition: (renditionId: string) => void;
};

const PlaybackContext = createContext<PlaybackContextValue | null>(null);

export function PlaybackProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(playbackReducer, undefined, initialStateFromSettings);
  const outputRef = useRef<PlaybackOutput | null>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const dashSessionRef = useRef<Awaited<ReturnType<typeof attachDashToAudio>> | null>(null);
  const lastLoadedTrackIdRef = useRef<string | null>(null);
  const streamRenditionsRef = useRef<TrackStreamRenditionDto[]>([]);
  const [streamRenditions, setStreamRenditions] = useState<TrackStreamRenditionDto[]>([]);
  const [activeRendition, setActiveRendition] = useState<ActiveRenditionInfo | null>(null);
  const activeRenditionRef = useRef<ActiveRenditionInfo | null>(null);
  const isPlayingRef = useRef(false);
  const isScrubbingRef = useRef(false);
  const { setPlayingSeed, setPaused } = useTheme();
  const router = useRouter();

  const currentTrack =
    state.currentIndex >= 0 ? (state.queue[state.currentIndex] ?? null) : null;
  const resetDashSession = useCallback(() => {
    dashSessionRef.current?.destroy();
    dashSessionRef.current = null;
    streamRenditionsRef.current = [];
    setStreamRenditions([]);
    setActiveRendition(null);
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
    activeRenditionRef.current = activeRendition;
  }, [activeRendition]);

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

        streamRenditionsRef.current = info.renditions ?? [];
        setStreamRenditions(info.renditions ?? []);

        if (isDash) {
          const settings = loadPlaybackSettings();
          const chosen = selectRendition(info.renditions ?? [], settings, getNetworkHints());
          const dashSession = await attachDashToAudio(
            audio,
            playbackUrl,
            getAccessToken,
            chosen,
          );
          if (cancelled || lastLoadedTrackIdRef.current !== currentTrack.id) {
            dashSession.destroy();
            return;
          }
          dashSessionRef.current = dashSession;
          dashSession.onActiveRenditionChange(setActiveRendition);
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

  const applyAutoRenditionIfNeeded = useCallback(() => {
    const dashSession = dashSessionRef.current;
    const renditions = streamRenditionsRef.current;
    if (!dashSession || renditions.length === 0) return;

    const settings = loadPlaybackSettings();
    if (settings.qualityMode !== "auto") return;

    const chosen = selectRendition(renditions, settings, getNetworkHints());
    if (!chosen) return;

    if (activeRenditionRef.current?.rendition.id === chosen.id) return;
    dashSession.setRendition(chosen);
  }, []);

  useEffect(() => {
    if (!currentTrack) return;

    const connection = (
      navigator as Navigator & {
        connection?: EventTarget;
      }
    ).connection;
    const handleNetworkChange = () => applyAutoRenditionIfNeeded();

    if (connection) {
      connection.addEventListener("change", handleNetworkChange);
    }
    window.addEventListener("online", handleNetworkChange);
    window.addEventListener("offline", handleNetworkChange);

    const intervalId = window.setInterval(() => {
      if (isPlayingRef.current) {
        applyAutoRenditionIfNeeded();
      }
    }, 5000);

    return () => {
      if (connection) {
        connection.removeEventListener("change", handleNetworkChange);
      }
      window.removeEventListener("online", handleNetworkChange);
      window.removeEventListener("offline", handleNetworkChange);
      window.clearInterval(intervalId);
    };
  }, [currentTrack?.id, applyAutoRenditionIfNeeded]);

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

  const requireAuthForQueue = useCallback(() => {
    if (getAccessToken()) {
      outputRef.current?.prime();
      return true;
    }
    const next = encodeURIComponent(
      typeof window !== "undefined" ? window.location.pathname : "/home",
    );
    router.push(`/login?next=${next}`);
    return false;
  }, [router]);

  const addToQueue = useCallback(
    (tracks: PlaybackTrack[]) => {
      if (tracks.length === 0 || !requireAuthForQueue()) return;
      dispatch({ type: "appendToQueue", tracks });
    },
    [requireAuthForQueue],
  );

  const playNext = useCallback(
    (tracks: PlaybackTrack[]) => {
      if (tracks.length === 0 || !requireAuthForQueue()) return;
      dispatch({ type: "insertPlayNext", tracks });
    },
    [requireAuthForQueue],
  );

  const switchRendition = useCallback((renditionId: string) => {
    const rendition = streamRenditionsRef.current.find((r) => r.id === renditionId);
    if (!rendition) return;
    savePlaybackSettings({
      qualityMode: "manual",
      manualRenditionId: renditionId,
    });
    dashSessionRef.current?.setRendition(rendition);
  }, []);

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
      addToQueue,
      playNext,
      streamRenditions,
      activeRendition,
      switchRendition,
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
      addToQueue,
      playNext,
      streamRenditions,
      activeRendition,
      switchRendition,
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
  const prevPositionMsRef = useRef(state.positionMs);
  const seekHoldMsRef = useRef<number | null>(null);

  useEffect(() => {
    if (state.isPlaying) return;
    resumeFromPauseRef.current = true;
    seekHoldMsRef.current = null;
    setPlayheadMs(state.positionMs);
  }, [state.isPlaying, state.positionMs, state.currentIndex]);

  useEffect(() => {
    if (!state.isPlaying) return;

    const prev = prevPositionMsRef.current;
    prevPositionMsRef.current = state.positionMs;
    const delta = Math.abs(state.positionMs - prev);
    if (delta > 500) {
      seekHoldMsRef.current = state.positionMs;
      setPlayheadMs(state.positionMs);
    }
  }, [state.positionMs, state.isPlaying]);

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
      const fromAudio = readAudioPositionMs(audio);
      const hold = seekHoldMsRef.current;
      if (hold !== null) {
        if (Math.abs(fromAudio - hold) <= 500) {
          seekHoldMsRef.current = null;
          setPlayheadMs(fromAudio);
        } else {
          setPlayheadMs(hold);
        }
      } else {
        setPlayheadMs(fromAudio);
      }
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, [audioRef, state.isPlaying, state.currentIndex]);

  return state.isPlaying ? playheadMs : state.positionMs;
}
