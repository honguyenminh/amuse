"use client";

import { resolveApiUrl } from "@/lib/api/config";
import { getTrackStreamInfo } from "@/lib/api/catalogClient";
import {
  type TrackStreamLoudness,
  type TrackStreamRenditionDto,
} from "@/lib/api/types";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { useAuth } from "@/lib/auth/AuthProvider";
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
  useLayoutEffect,
  useMemo,
  useReducer,
  useRef,
  useState,
  type MutableRefObject,
  type ReactNode,
} from "react";
import {
  readAudioBufferedEndMs,
  readAudioDurationMs,
  readAudioPositionMs,
} from "./audioPosition";
import {
  attachDashToAudio,
  teardownDashAudio,
  type ActiveRenditionInfo,
} from "./dashPlayer";
import { loadPlaybackSettings, savePlaybackSettings } from "./playbackSettings";
import { getNetworkHints, readNavigatorConnection } from "./networkHints";
import {
  limitDowngradeToOneStep,
  renditionLadderIndex,
} from "./renditionLadder";
import { computeNormalizationGain } from "./normalizationGain";
import { alignAudioToStatePosition, restorePlaybackPosition } from "./restorePlaybackPosition";
import { selectRendition } from "./selectRendition";
import { createPlaybackOutput, type PlaybackOutput } from "./playbackOutput";
import { playbackReducer } from "./reducer";
import {
  clearPersistedQueue,
  loadPersistedQueue,
  savePersistedQueue,
  snapshotFromPlaybackState,
} from "./queuePersistence";
import {
  broadcastPlaybackClaim,
  broadcastQueueUpdated,
  getPlaybackTabId,
  holdsPlaybackLease,
  shouldIgnoreSyncMessage,
  subscribeQueueSync,
  type QueueSyncMessage,
} from "./queueTabSync";
import { useMediaSession } from "./useMediaSession";
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
  /** False until localStorage hydration (or confirmed empty) finishes on mount. */
  isQueueHydrated: boolean;
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
  nudgeVolume: (delta: number) => void;
  toggleMute: () => void;
  stop: () => void;
  setRepeat: (mode: RepeatMode) => void;
  toggleShuffle: () => void;
  addToQueue: (tracks: PlaybackTrack[]) => void;
  playNext: (tracks: PlaybackTrack[]) => void;
  moveToPlayNext: (trackId: string) => void;
  clearQueue: () => void;
  jumpToPlayOrderIndex: (playOrderIndex: number) => void;
  reorderPlayOrder: (fromPlayOrderIndex: number, toPlayOrderIndex: number) => void;
  streamRenditions: TrackStreamRenditionDto[];
  activeRendition: ActiveRenditionInfo | null;
  switchRendition: (renditionId: string) => void;
  refreshPlaybackSettings: () => void;
};

const PlaybackContext = createContext<PlaybackContextValue | null>(null);

/** Ignore stall/downgrade signals while a seek is settling. */
const SEEK_QUALITY_GRACE_MS = 5000;
/** Minimum gap between auto downgrades from real rebuffering. */
const STALL_DOWNGRADE_COOLDOWN_MS = 8000;
/** After a stall downgrade, block auto upgrades so the lower rung can buffer and play. */
const AUTO_UPGRADE_COOLDOWN_AFTER_STALL_MS = 45_000;
/** Require this much buffer ahead before attempting an auto upgrade. */
const AUTO_UPGRADE_MIN_BUFFER_SEC = 12;
/** Reset stall downgrade memory after stable playback for this long. */
const AUTO_STALL_MEMORY_DECAY_MS = 60_000;
const QUEUE_PERSIST_DEBOUNCE_MS = 400;

function queueStateChanged(prev: PlaybackState, next: PlaybackState): boolean {
  return (
    prev.queue !== next.queue ||
    prev.playOrder !== next.playOrder ||
    prev.playOrderIndex !== next.playOrderIndex ||
    prev.currentIndex !== next.currentIndex ||
    prev.shuffle !== next.shuffle ||
    prev.repeat !== next.repeat ||
    prev.isPlaying !== next.isPlaying ||
    prev.positionMs !== next.positionMs ||
    prev.durationMs !== next.durationMs
  );
}

function isPositionTickOnly(prev: PlaybackState, next: PlaybackState): boolean {
  return (
    prev.queue === next.queue &&
    prev.playOrder === next.playOrder &&
    prev.playOrderIndex === next.playOrderIndex &&
    prev.currentIndex === next.currentIndex &&
    prev.shuffle === next.shuffle &&
    prev.repeat === next.repeat &&
    prev.isPlaying === next.isPlaying &&
    prev.durationMs === next.durationMs &&
    prev.positionMs !== next.positionMs
  );
}

export function PlaybackProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(playbackReducer, undefined, initialStateFromSettings);
  const outputRef = useRef<PlaybackOutput | null>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const dashSessionRef = useRef<Awaited<ReturnType<typeof attachDashToAudio>> | null>(null);
  const trackLoadGenerationRef = useRef(0);
  const trackLoadChainRef = useRef(Promise.resolve());
  const lastLoadedTrackIdRef = useRef<string | null>(null);
  const streamRenditionsRef = useRef<TrackStreamRenditionDto[]>([]);
  const streamIsOwnerRef = useRef(false);
  const [streamRenditions, setStreamRenditions] = useState<TrackStreamRenditionDto[]>([]);
  const [activeRendition, setActiveRendition] = useState<ActiveRenditionInfo | null>(null);
  const activeRenditionRef = useRef<ActiveRenditionInfo | null>(null);
  const lastAutoSwitchAtRef = useRef(0);
  const lastStallHandledAtRef = useRef(0);
  const autoStallDowngradesRef = useRef(0);
  const autoUpgradeBlockedUntilRef = useRef(0);
  const seekGraceUntilRef = useRef(0);
  const lastStreamLoudnessRef = useRef<TrackStreamLoudness | null>(null);
  const isPlayingRef = useRef(false);
  const isScrubbingRef = useRef(false);
  const volumeBeforeMuteRef = useRef(0.85);
  const [isQueueHydrated, setIsQueueHydrated] = useState(false);
  const restorePendingRef = useRef(false);
  const applyingRemoteSyncRef = useRef(false);
  const lastAppliedUpdatedAtRef = useRef(0);
  const leaseRef = useRef<{ activeTabId: string | null; isPlaying: boolean }>({
    activeTabId: null,
    isPlaying: false,
  });
  const pendingRestorePositionMsRef = useRef<number | null>(null);
  const suppressAudioPositionSyncRef = useRef(false);
  const persistTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const stateRef = useRef(state);
  const prevPersistStateRef = useRef(state);
  const localTabIdRef = useRef<string>("");
  const { setPlayingSeed, setPaused } = useTheme();
  const router = useRouter();
  const auth = useAuth();

  const currentTrack =
    state.currentIndex >= 0 ? (state.queue[state.currentIndex] ?? null) : null;
  const resetDashSession = useCallback(async () => {
    dashSessionRef.current = null;
    streamRenditionsRef.current = [];
    streamIsOwnerRef.current = false;
    setStreamRenditions([]);
    setActiveRendition(null);
    const audio = audioRef.current;
    if (audio) {
      await teardownDashAudio(audio);
    }
  }, []);

  const applyNormalizationGain = useCallback((loudness: TrackStreamLoudness | null) => {
    lastStreamLoudnessRef.current = loudness;
    const settings = loadPlaybackSettings();
    const gain = computeNormalizationGain(loudness, settings.volumeNormalization);
    outputRef.current?.setNormalizationGain(gain);
  }, []);

  const refreshPlaybackSettings = useCallback(() => {
    applyNormalizationGain(lastStreamLoudnessRef.current);
  }, [applyNormalizationGain]);


  isPlayingRef.current = state.isPlaying;
  stateRef.current = state;

  const applyRemoteSnapshot = useCallback(
    (snapshot: NonNullable<ReturnType<typeof loadPersistedQueue>>, fromColdLoad = false) => {
      const tabId = localTabIdRef.current || getPlaybackTabId();
      const holdsLease =
        !fromColdLoad &&
        holdsPlaybackLease(snapshot.activeTabId, tabId) &&
        snapshot.isPlaying;
      const isPlaying = holdsLease;

      leaseRef.current = fromColdLoad
        ? { activeTabId: null, isPlaying: false }
        : { activeTabId: snapshot.activeTabId, isPlaying: holdsLease };

      applyingRemoteSyncRef.current = true;
      pendingRestorePositionMsRef.current = snapshot.positionMs;
      isPlayingRef.current = isPlaying;
      if (fromColdLoad) {
        restorePendingRef.current = true;
      }

      dispatch({
        type: "restoreState",
        snapshot,
        isPlaying,
      });

      prevPersistStateRef.current = {
        ...stateRef.current,
        queue: snapshot.queue,
        playOrder: snapshot.playOrder,
        playOrderIndex: snapshot.playOrderIndex,
        currentIndex: snapshot.currentIndex,
        positionMs: snapshot.positionMs,
        durationMs: snapshot.durationMs,
        shuffle: snapshot.shuffle,
        repeat: snapshot.repeat,
        isPlaying,
      };

      if (!isPlaying) {
        outputRef.current?.pauseImmediate();
      }

      lastAppliedUpdatedAtRef.current = snapshot.updatedAt;
      applyingRemoteSyncRef.current = false;
    },
    [],
  );

  const flushPersistQueue = useCallback((claimPlayback = false) => {
    const tabId = localTabIdRef.current || getPlaybackTabId();
    const current = stateRef.current;
    const updatedAt = Date.now();

    if (current.queue.length === 0) {
      if (restorePendingRef.current) return;
      clearPersistedQueue();
      lastAppliedUpdatedAtRef.current = updatedAt;
      broadcastQueueUpdated(tabId, updatedAt);
      leaseRef.current = { activeTabId: null, isPlaying: false };
      return;
    }

    if (claimPlayback || current.isPlaying) {
      leaseRef.current = { activeTabId: tabId, isPlaying: current.isPlaying };
    } else if (holdsPlaybackLease(leaseRef.current.activeTabId, tabId)) {
      leaseRef.current = { activeTabId: tabId, isPlaying: false };
    }

    const snapshot = snapshotFromPlaybackState(current, leaseRef.current, updatedAt);
    if (!snapshot) return;

    savePersistedQueue(snapshot);
    lastAppliedUpdatedAtRef.current = updatedAt;

    if (claimPlayback || (current.isPlaying && holdsPlaybackLease(leaseRef.current.activeTabId, tabId))) {
      broadcastPlaybackClaim(tabId, updatedAt);
    } else {
      broadcastQueueUpdated(tabId, updatedAt);
    }
  }, []);

  const schedulePersistQueue = useCallback(
    (claimPlayback = false) => {
      if (persistTimerRef.current) clearTimeout(persistTimerRef.current);
      persistTimerRef.current = setTimeout(() => {
        flushPersistQueue(claimPlayback);
      }, QUEUE_PERSIST_DEBOUNCE_MS);
    },
    [flushPersistQueue],
  );

  useLayoutEffect(() => {
    localTabIdRef.current = getPlaybackTabId();
    const snapshot = loadPersistedQueue();
    if (snapshot) {
      applyRemoteSnapshot(snapshot, true);
    } else {
      setIsQueueHydrated(true);
    }
  }, [applyRemoteSnapshot]);

  useEffect(() => {
    const unsubscribe = subscribeQueueSync((message: QueueSyncMessage) => {
      const tabId = localTabIdRef.current;
      if (shouldIgnoreSyncMessage(message, tabId, lastAppliedUpdatedAtRef.current)) {
        return;
      }
      const remote = loadPersistedQueue();
      if (!remote) {
        if (restorePendingRef.current || !isQueueHydrated) return;
        applyingRemoteSyncRef.current = true;
        dispatch({ type: "clear" });
        leaseRef.current = { activeTabId: null, isPlaying: false };
        isPlayingRef.current = false;
        lastAppliedUpdatedAtRef.current = message.updatedAt;
        applyingRemoteSyncRef.current = false;
        outputRef.current?.pauseImmediate();
        return;
      }
      applyRemoteSnapshot(remote);
    });

    return () => {
      unsubscribe();
      if (persistTimerRef.current) clearTimeout(persistTimerRef.current);
    };
  }, [applyRemoteSnapshot, isQueueHydrated]);

  useEffect(() => {
    if (state.queue.length > 0 && restorePendingRef.current) {
      restorePendingRef.current = false;
      setIsQueueHydrated(true);
    }
  }, [state.queue.length]);

  useEffect(() => {
    if (!isQueueHydrated || applyingRemoteSyncRef.current) return;

    const prev = prevPersistStateRef.current;
    if (!queueStateChanged(prev, state)) return;

    if (isPositionTickOnly(prev, state)) {
      const tabId = localTabIdRef.current;
      if (!holdsPlaybackLease(leaseRef.current.activeTabId, tabId) || !state.isPlaying) {
        prevPersistStateRef.current = state;
        return;
      }
      schedulePersistQueue(false);
    } else {
      if (persistTimerRef.current) clearTimeout(persistTimerRef.current);
      flushPersistQueue(state.isPlaying);
    }
    prevPersistStateRef.current = state;
  }, [state, isQueueHydrated, schedulePersistQueue, flushPersistQueue]);

  useEffect(() => {
    const flushOnPageHide = () => {
      if (persistTimerRef.current) {
        clearTimeout(persistTimerRef.current);
        persistTimerRef.current = null;
      }
      if (isQueueHydrated && !applyingRemoteSyncRef.current) {
        flushPersistQueue(false);
      }
    };
    window.addEventListener("pagehide", flushOnPageHide);
    return () => window.removeEventListener("pagehide", flushOnPageHide);
  }, [isQueueHydrated, flushPersistQueue]);

  const markSeekGrace = useCallback(() => {
    seekGraceUntilRef.current = Date.now() + SEEK_QUALITY_GRACE_MS;
  }, []);

  const isSeekQualityGraceActive = useCallback(() => {
    if (isScrubbingRef.current) return true;
    const audio = audioRef.current;
    if (audio?.seeking) return true;
    return Date.now() < seekGraceUntilRef.current;
  }, []);

  const applyPositionToAudio = useCallback((positionMs: number, resume: boolean) => {
    const audio = audioRef.current;
    const output = outputRef.current;
    const dashSession = dashSessionRef.current;
    if (!audio) return;

    const positionSec = positionMs / 1000;

    if (dashSession) {
      if (Math.abs(audio.currentTime - positionSec) < 0.001) {
        if (resume && audio.paused) void audio.play().catch(() => undefined);
        return;
      }
      markSeekGrace();
      output?.pauseImmediate();
      dashSession.seek(positionSec);
      if (resume) void audio.play().catch(() => undefined);
      return;
    }

    if (!Number.isFinite(audio.duration)) return;

    const target = Math.max(0, Math.min(positionSec, audio.duration));
    if (Math.abs(audio.currentTime - target) < 0.001) {
      if (resume && audio.paused) void audio.play().catch(() => undefined);
      return;
    }

    markSeekGrace();
    output?.pauseImmediate();
    audio.currentTime = target;

    if (resume) {
      void audio.play().catch(() => undefined);
    }
  }, [markSeekGrace]);

  useEffect(() => {
    if (outputRef.current) return;
    const output = createPlaybackOutput({ disableWebAudio: true });
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
      if (suppressAudioPositionSyncRef.current) return;
      const audioMs = readAudioPositionMs(audio);
      const stateMs = stateRef.current.positionMs;
      // DASH/progressive load can fire metadata at 0:00 while reducer still holds a restored seek.
      if (
        !isPlayingRef.current &&
        stateMs > 1000 &&
        audioMs < 1000 &&
        stateMs - audioMs > 1000
      ) {
        return;
      }
      dispatch({
        type: "tick",
        positionMs: audioMs,
        durationMs: readAudioDurationMs(audio),
      });
    });

  }, []);

  useEffect(() => {
    outputRef.current?.setVolume(state.volume);
  }, [state.volume]);

  useEffect(() => {
    if (state.volume > 0) volumeBeforeMuteRef.current = state.volume;
  }, [state.volume]);

  useEffect(() => {
    activeRenditionRef.current = activeRendition;
  }, [activeRendition]);

  const buildAutoHints = useCallback(() => {
    const dashSession = dashSessionRef.current;
    return getNetworkHints({
      throughputKbps: dashSession?.getThroughputKbps(),
      stallDowngradeSteps: autoStallDowngradesRef.current,
      isOwner: streamIsOwnerRef.current,
    });
  }, []);

  const maybeDecayStallMemory = useCallback(() => {
    const dashSession = dashSessionRef.current;
    if (!dashSession) return;

    const bufferSec = dashSession.getBufferLengthSeconds() ?? 0;
    const now = Date.now();
    const sinceStall = now - lastStallHandledAtRef.current;
    if (
      autoStallDowngradesRef.current > 0 &&
      sinceStall >= AUTO_STALL_MEMORY_DECAY_MS &&
      bufferSec >= AUTO_UPGRADE_MIN_BUFFER_SEC
    ) {
      autoStallDowngradesRef.current = 0;
      autoUpgradeBlockedUntilRef.current = 0;
    }
  }, []);

  const handlePlaybackStall = useCallback(() => {
    if (isSeekQualityGraceActive()) return;

    const dashSession = dashSessionRef.current;
    const renditions = streamRenditionsRef.current;
    const settings = loadPlaybackSettings();
    if (!dashSession || renditions.length === 0 || settings.qualityMode !== "auto") return;

    const now = Date.now();
    if (now - lastStallHandledAtRef.current < STALL_DOWNGRADE_COOLDOWN_MS) return;

    const current = activeRenditionRef.current?.rendition;
    if (!current) return;

    autoStallDowngradesRef.current += 1;
    const chosen = selectRendition(renditions, settings, {
      ...buildAutoHints(),
      stallDowngradeSteps: autoStallDowngradesRef.current,
    });
    if (!chosen) return;

    const next = limitDowngradeToOneStep(current, chosen, renditions);
    if (next.id === current.id) return;

    lastStallHandledAtRef.current = now;
    lastAutoSwitchAtRef.current = now;
    autoUpgradeBlockedUntilRef.current = now + AUTO_UPGRADE_COOLDOWN_AFTER_STALL_MS;
    dashSession.setRendition(next);
  }, [buildAutoHints, isSeekQualityGraceActive]);

  const applyAutoRenditionIfNeeded = useCallback((force = false) => {
    if (isSeekQualityGraceActive()) return;

    const dashSession = dashSessionRef.current;
    const renditions = streamRenditionsRef.current;
    if (!dashSession || renditions.length === 0) return;

    const settings = loadPlaybackSettings();
    if (settings.qualityMode !== "auto") return;

    maybeDecayStallMemory();

    const now = Date.now();
    if (now < autoUpgradeBlockedUntilRef.current) return;
    if (!force && now - lastAutoSwitchAtRef.current < 3000) return;

    const bufferSec = dashSession.getBufferLengthSeconds() ?? 0;
    if (bufferSec < AUTO_UPGRADE_MIN_BUFFER_SEC) return;

    const chosen = selectRendition(renditions, settings, buildAutoHints());
    if (!chosen) return;

    const current = activeRenditionRef.current?.rendition;
    if (current && renditionLadderIndex(chosen) <= renditionLadderIndex(current)) return;

    lastAutoSwitchAtRef.current = now;
    dashSession.setRendition(chosen);
  }, [buildAutoHints, isSeekQualityGraceActive, maybeDecayStallMemory]);

  useEffect(() => {
    const audio = audioRef.current;
    if (!audio) return;

    if (!currentTrack) {
      trackLoadGenerationRef.current += 1;
      trackLoadChainRef.current = trackLoadChainRef.current
        .catch(() => undefined)
        .then(async () => {
          await resetDashSession();
          outputRef.current?.pauseImmediate();
          audio.removeAttribute("src");
        });
      lastLoadedTrackIdRef.current = null;
      return;
    }

    // Wait for session restore; stale access tokens are refreshed inside authFetch.
    if (!auth.isReady || !auth.isAuthenticated) {
      return;
    }

    if (lastLoadedTrackIdRef.current === currentTrack.id) return;

    const trackId = currentTrack.id;
    const generation = (trackLoadGenerationRef.current += 1);
    lastLoadedTrackIdRef.current = trackId;
    lastStreamLoudnessRef.current = null;
    lastAutoSwitchAtRef.current = 0;
    lastStallHandledAtRef.current = 0;
    autoStallDowngradesRef.current = 0;
    autoUpgradeBlockedUntilRef.current = 0;
    seekGraceUntilRef.current = 0;

    const isStaleLoad = () => trackLoadGenerationRef.current !== generation;

    trackLoadChainRef.current = trackLoadChainRef.current
      .catch(() => undefined)
      .then(async () => {
        if (isStaleLoad()) return;

        try {
          const info = await getTrackStreamInfo(trackId);
          if (isStaleLoad()) return;

          dashSessionRef.current = null;
          outputRef.current?.pauseImmediate();

          const playbackUrl = resolveApiUrl(info.url);
          const isDash =
            info.contentType === "application/dash+xml" ||
            playbackUrl.toLowerCase().endsWith(".mpd");

          streamRenditionsRef.current = info.renditions ?? [];
          streamIsOwnerRef.current = info.isOwner;
          setStreamRenditions(info.renditions ?? []);
          applyNormalizationGain(info.loudness);

          if (isDash) {
            if (isStaleLoad()) return;

            const settings = loadPlaybackSettings();
            const chosen = selectRendition(info.renditions ?? [], settings, {
              ...getNetworkHints(),
              isOwner: info.isOwner,
            });
            const restoreMs = pendingRestorePositionMsRef.current;
            const startTimeSec =
              restoreMs !== null && restoreMs > 0 ? restoreMs / 1000 : 0;
            const dashSession = await attachDashToAudio(
              audio,
              playbackUrl,
              getAccessToken,
              chosen,
              startTimeSec,
            );
            if (isStaleLoad()) {
              await dashSession.destroy();
              return;
            }
            dashSessionRef.current = dashSession;
            dashSession.onActiveRenditionChange(setActiveRendition);
            dashSession.onBufferStall(() => {
              handlePlaybackStall();
            });
          } else {
            if (isStaleLoad()) return;
            await teardownDashAudio(audio);
            audio.crossOrigin = "anonymous";
            audio.src = playbackUrl;
          }

          if (isStaleLoad()) return;

          const restoreMs = pendingRestorePositionMsRef.current;
          if (restoreMs !== null && restoreMs > 0) {
            suppressAudioPositionSyncRef.current = true;
            try {
              if (dashSessionRef.current) {
                pendingRestorePositionMsRef.current = null;
                dispatch({
                  type: "tick",
                  positionMs: restoreMs,
                  durationMs:
                    currentTrack.durationMs > 0
                      ? currentTrack.durationMs
                      : (readAudioDurationMs(audio) ?? restoreMs),
                });
              } else {
                const restored = await restorePlaybackPosition(audio, restoreMs, null);
                pendingRestorePositionMsRef.current = null;
                dispatch({
                  type: "tick",
                  positionMs: restored.positionMs,
                  durationMs: restored.durationMs,
                });
              }
            } finally {
              suppressAudioPositionSyncRef.current = false;
            }
          } else {
            pendingRestorePositionMsRef.current = null;
            audio.currentTime = 0;
          }

          if (isPlayingRef.current) {
            await outputRef.current?.playSmooth();
          }
        } catch {
          if (isStaleLoad()) return;
          dispatch({ type: "pause" });
          isPlayingRef.current = false;
          lastLoadedTrackIdRef.current = null;
        }
      });
  }, [
    currentTrack,
    auth.isReady,
    auth.isAuthenticated,
    resetDashSession,
    handlePlaybackStall,
    applyNormalizationGain,
  ]);

  useEffect(
    () => () => {
      void resetDashSession();
    },
    [resetDashSession],
  );

  useEffect(() => {
    const output = outputRef.current;
    const audio = audioRef.current;
    if (!output || !audio?.src) return;
    if (state.isPlaying) {
      alignAudioToStatePosition(
        audio,
        stateRef.current.positionMs,
        dashSessionRef.current,
      );
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

  useEffect(() => {
    if (!currentTrack) return;

    // navigator.connection exists in Chromium only; Firefox relies on measured throughput.
    const connection = readNavigatorConnection();
    const handleNetworkChange = () => applyAutoRenditionIfNeeded();

    if (connection?.addEventListener) {
      connection.addEventListener("change", handleNetworkChange);
    }
    window.addEventListener("online", handleNetworkChange);
    window.addEventListener("offline", handleNetworkChange);

    const audio = audioRef.current;
    const onSeeked = () => {
      seekGraceUntilRef.current = Math.max(
        seekGraceUntilRef.current,
        Date.now() + SEEK_QUALITY_GRACE_MS,
      );
    };
    audio?.addEventListener("seeked", onSeeked);

    const intervalId = window.setInterval(() => {
      if (isPlayingRef.current) {
        applyAutoRenditionIfNeeded();
      }
    }, 2000);

    return () => {
      if (connection?.removeEventListener) {
        connection.removeEventListener("change", handleNetworkChange);
      }
      window.removeEventListener("online", handleNetworkChange);
      window.removeEventListener("offline", handleNetworkChange);
      audio?.removeEventListener("seeked", onSeeked);
      window.clearInterval(intervalId);
    };
  }, [currentTrack, applyAutoRenditionIfNeeded]);

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
      if (!auth.isReady) return;
      if (!auth.isAuthenticated) {
        const next = encodeURIComponent(
          typeof window !== "undefined" ? window.location.pathname : "/home",
        );
        router.push(`/login?next=${next}`);
        return;
      }
      outputRef.current?.prime();
      isPlayingRef.current = true;
      dispatch({ type: "playQueue", tracks, startIndex });
      schedulePersistQueue(true);
    },
    [auth.isReady, auth.isAuthenticated, router, schedulePersistQueue],
  );

  const play = useCallback(() => {
    outputRef.current?.prime();
    isPlayingRef.current = true;
    dispatch({ type: "play" });
    schedulePersistQueue(true);
  }, [schedulePersistQueue]);
  const pause = useCallback(() => {
    syncPositionFromAudio();
    isPlayingRef.current = false;
    dispatch({ type: "pause" });
    schedulePersistQueue(false);
  }, [syncPositionFromAudio, schedulePersistQueue]);
  const toggle = useCallback(() => {
    if (isPlayingRef.current) {
      syncPositionFromAudio();
      isPlayingRef.current = false;
      dispatch({ type: "toggle" });
      schedulePersistQueue(false);
    } else {
      outputRef.current?.prime();
      isPlayingRef.current = true;
      dispatch({ type: "toggle" });
      schedulePersistQueue(true);
    }
  }, [syncPositionFromAudio, schedulePersistQueue]);
  const next = useCallback(() => {
    dispatch({ type: "next" });
    schedulePersistQueue(false);
  }, [schedulePersistQueue]);
  const previous = useCallback(() => {
    if (state.currentIndex < 0) return;
    const restartCurrent = state.positionMs > 3000;
    dispatch({ type: "previous" });
    if (restartCurrent) {
      applyPositionToAudio(0, isPlayingRef.current);
    }
    schedulePersistQueue(false);
  }, [state.currentIndex, state.positionMs, applyPositionToAudio, schedulePersistQueue]);

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

  const persistVolume = useCallback((volume: number) => {
    savePlaybackSettings({ volume });
  }, []);

  const nudgeVolume = useCallback(
    (delta: number) => {
      const next = Math.max(0, Math.min(1, state.volume + delta));
      if (next > 0) volumeBeforeMuteRef.current = next;
      dispatch({ type: "setVolume", volume: next });
      persistVolume(next);
    },
    [state.volume, persistVolume],
  );

  const toggleMute = useCallback(() => {
    const volume = Number.isFinite(state.volume) ? state.volume : 0;
    if (volume === 0) {
      const restored = volumeBeforeMuteRef.current || 0.85;
      dispatch({ type: "setVolume", volume: restored });
      persistVolume(restored);
      return;
    }
    volumeBeforeMuteRef.current = volume;
    dispatch({ type: "setVolume", volume: 0 });
    persistVolume(0);
  }, [state.volume, persistVolume]);

  const stop = useCallback(() => {
    syncPositionFromAudio();
    isPlayingRef.current = false;
    dispatch({ type: "pause" });
  }, [syncPositionFromAudio]);
  const setRepeat = useCallback(
    (mode: RepeatMode) => {
      dispatch({ type: "setRepeat", mode });
      schedulePersistQueue(false);
    },
    [schedulePersistQueue],
  );
  const toggleShuffle = useCallback(() => {
    dispatch({ type: "toggleShuffle" });
    schedulePersistQueue(false);
  }, [schedulePersistQueue]);

  const requireAuthForQueue = useCallback(() => {
    if (!auth.isReady) return false;
    if (auth.isAuthenticated) {
      outputRef.current?.prime();
      return true;
    }
    const next = encodeURIComponent(
      typeof window !== "undefined" ? window.location.pathname : "/home",
    );
    router.push(`/login?next=${next}`);
    return false;
  }, [auth.isReady, auth.isAuthenticated, router]);

  const addToQueue = useCallback(
    (tracks: PlaybackTrack[]) => {
      if (tracks.length === 0 || !requireAuthForQueue()) return;
      dispatch({ type: "appendToQueue", tracks });
      schedulePersistQueue(false);
    },
    [requireAuthForQueue, schedulePersistQueue],
  );

  const playNext = useCallback(
    (tracks: PlaybackTrack[]) => {
      if (tracks.length === 0 || !requireAuthForQueue()) return;
      dispatch({ type: "insertPlayNext", tracks });
      schedulePersistQueue(false);
    },
    [requireAuthForQueue, schedulePersistQueue],
  );

  const moveToPlayNext = useCallback(
    (trackId: string) => {
      if (!requireAuthForQueue()) return;
      dispatch({ type: "moveToPlayNext", trackId });
      schedulePersistQueue(false);
    },
    [requireAuthForQueue, schedulePersistQueue],
  );

  const clearQueue = useCallback(() => {
    syncPositionFromAudio();
    isPlayingRef.current = false;
    void resetDashSession();
    dispatch({ type: "clear" });
    leaseRef.current = { activeTabId: null, isPlaying: false };
    prevPersistStateRef.current = {
      ...initialPlaybackState,
      volume: stateRef.current.volume,
    };
    clearPersistedQueue();
    const updatedAt = Date.now();
    lastAppliedUpdatedAtRef.current = updatedAt;
    broadcastQueueUpdated(localTabIdRef.current || getPlaybackTabId(), updatedAt);
  }, [syncPositionFromAudio, resetDashSession]);

  useEffect(() => {
    if (!auth.isReady || auth.isAuthenticated) return;
    if (stateRef.current.queue.length === 0 && !loadPersistedQueue()) return;

    syncPositionFromAudio();
    isPlayingRef.current = false;
    void resetDashSession();
    dispatch({ type: "clear" });
    leaseRef.current = { activeTabId: null, isPlaying: false };
    prevPersistStateRef.current = {
      ...initialPlaybackState,
      volume: stateRef.current.volume,
    };
    clearPersistedQueue();
    pendingRestorePositionMsRef.current = null;
    lastLoadedTrackIdRef.current = null;
    const updatedAt = Date.now();
    lastAppliedUpdatedAtRef.current = updatedAt;
    broadcastQueueUpdated(localTabIdRef.current || getPlaybackTabId(), updatedAt);
  }, [auth.isReady, auth.isAuthenticated, syncPositionFromAudio, resetDashSession]);

  const jumpToPlayOrderIndex = useCallback(
    (playOrderIndex: number) => {
      outputRef.current?.prime();
      isPlayingRef.current = true;
      dispatch({ type: "jumpToPlayOrderIndex", playOrderIndex });
      schedulePersistQueue(true);
    },
    [schedulePersistQueue],
  );

  const reorderPlayOrder = useCallback(
    (fromPlayOrderIndex: number, toPlayOrderIndex: number) => {
      dispatch({ type: "reorderPlayOrder", fromPlayOrderIndex, toPlayOrderIndex });
      schedulePersistQueue(false);
    },
    [schedulePersistQueue],
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

  useMediaSession(currentTrack, state, {
    play,
    pause,
    next,
    previous,
    seek,
  });

  const value = useMemo<PlaybackContextValue>(
    () => ({
      state,
      isQueueHydrated,
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
      nudgeVolume,
      toggleMute,
      stop,
      setRepeat,
      toggleShuffle,
      addToQueue,
      playNext,
      moveToPlayNext,
      clearQueue,
      jumpToPlayOrderIndex,
      reorderPlayOrder,
      streamRenditions,
      activeRendition,
      switchRendition,
      refreshPlaybackSettings,
    }),
    [
      state,
      isQueueHydrated,
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
      nudgeVolume,
      toggleMute,
      stop,
      setRepeat,
      toggleShuffle,
      addToQueue,
      playNext,
      moveToPlayNext,
      clearQueue,
      jumpToPlayOrderIndex,
      reorderPlayOrder,
      streamRenditions,
      activeRendition,
      switchRendition,
      refreshPlaybackSettings,
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
  }, [audioRef, state.isPlaying, state.currentIndex, state.positionMs]);

  return state.isPlaying ? playheadMs : state.positionMs;
}

/** Buffered playhead extent for seek bars (same ms scale as duration). */
export function usePlaybackBufferedEnd(): number {
  const { state, audioRef } = usePlayback();
  const [bufferedMs, setBufferedMs] = useState(0);

  useEffect(() => {
    const audio = audioRef.current;
    if (!audio || state.currentIndex < 0) {
      setBufferedMs(0);
      return;
    }

    const update = () => {
      setBufferedMs(readAudioBufferedEndMs(audio));
    };

    update();
    audio.addEventListener("progress", update);
    audio.addEventListener("loadedmetadata", update);
    audio.addEventListener("durationchange", update);
    audio.addEventListener("seeked", update);

    let raf = 0;
    const loop = () => {
      update();
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);

    return () => {
      audio.removeEventListener("progress", update);
      audio.removeEventListener("loadedmetadata", update);
      audio.removeEventListener("durationchange", update);
      audio.removeEventListener("seeked", update);
      cancelAnimationFrame(raf);
    };
  }, [audioRef, state.currentIndex, state.isPlaying]);

  return bufferedMs;
}
