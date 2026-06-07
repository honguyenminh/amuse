"use client";

import { resolveApiUrl, WEB_CLIENT_HEADER } from "@/lib/api/config";
import type { TrackStreamRenditionDto } from "@/lib/api/types";
import {
  pruneDisjointAheadBuffer,
  resyncBufferingFromPlayhead,
  type DashActiveStream,
  type DashPlayerWithStream,
} from "./dashBufferSync";

export type ActiveRenditionInfo = {
  rendition: TrackStreamRenditionDto;
  bandwidthKbps: number | null;
};

type DashBitrateInfo = { id?: string | null; bandwidth?: number };
type DashTrackInfo = {
  id?: string | null;
  bitrateList?: Array<{ id?: string | null }>;
};

/**
 * How far behind the playhead dash.js may prune (seconds). Capped so disjoint seek
 * caches survive (e.g. 1:00–1:30 while playing at 3:00) without filling MSE quota.
 */
const MAX_RETAIN_BEHIND_SEC = 600;

/** Forward buffer targets — how far ahead to prefetch from the playhead. */
const FORWARD_BUFFER_TOP_SEC = 30;
const FORWARD_BUFFER_LONG_SEC = 60;

function retainBehindSeconds(durationSec: number | undefined): number {
  if (durationSec !== undefined && Number.isFinite(durationSec) && durationSec > 0) {
    return Math.min(Math.ceil(durationSec), MAX_RETAIN_BEHIND_SEC);
  }
  return MAX_RETAIN_BEHIND_SEC;
}

function bufferRetentionSettings(durationSec: number | undefined) {
  return {
    fastSwitchEnabled: true,
    bufferTimeDefault: 45,
    bufferTimeAtTopQuality: FORWARD_BUFFER_TOP_SEC,
    bufferTimeAtTopQualityLongForm: FORWARD_BUFFER_LONG_SEC,
    bufferToKeep: retainBehindSeconds(durationSec),
    initialBufferLevel: 15,
    avoidCurrentTimeRangePruning: true,
    bufferPruningInterval: 60,
  };
}

type DashPlayerInstance = {
  addRequestInterceptor: (
    interceptor: (request: { url: string; headers?: Record<string, string> }) => Promise<unknown>,
  ) => void;
  initialize: (audio: HTMLAudioElement, url: string, autoPlay: boolean, startTime?: number) => void;
  attachSource: (url: string | null, startTime?: number) => void;
  destroy: () => void;
  seek?: (time: number) => void;
  updateSettings?: (settings: object) => void;
  getActiveStream?: () => DashActiveStream | null;
  getBitrateInfoListFor?: (type: string) => DashBitrateInfo[];
  getTracksFor?: (type: string) => DashTrackInfo[];
  /** Returns average throughput in kbit/s (not bits/s). */
  getAverageThroughput?: (type: string) => number;
  getRawThroughputData?: (type: string) => unknown[];
  getBufferLength?: (type: string) => number;
  setCurrentTrack?: (track: DashTrackInfo) => void;
  setRepresentationForTypeById?: (type: string, id: string, forceReplace?: boolean) => void;
  on?: (event: string, handler: () => void) => void;
  off?: (event: string, handler: () => void) => void;
};

type DashSession = {
  destroy: () => Promise<void>;
  seek: (positionSec: number) => void;
  whenStreamReady: () => Promise<void>;
  setRendition: (rendition: TrackStreamRenditionDto) => void;
  getThroughputKbps: () => number | undefined;
  getBufferLengthSeconds: () => number | undefined;
  onActiveRenditionChange: (listener: (info: ActiveRenditionInfo | null) => void) => () => void;
  onBufferStall: (listener: () => void) => () => void;
};

type AudioBinding = {
  queue: Promise<void>;
  player: DashPlayerInstance | null;
  initialized: boolean;
  configured: boolean;
  activeSessionId: number;
  nextSessionId: number;
  removeListeners: (() => void) | null;
};

const bindings = new WeakMap<HTMLAudioElement, AudioBinding>();

function getBinding(audio: HTMLAudioElement): AudioBinding {
  let binding = bindings.get(audio);
  if (!binding) {
    binding = {
      queue: Promise.resolve(),
      player: null,
      initialized: false,
      configured: false,
      activeSessionId: 0,
      nextSessionId: 0,
      removeListeners: null,
    };
    bindings.set(audio, binding);
  }
  return binding;
}

function enqueue(audio: HTMLAudioElement, op: () => Promise<void>): Promise<void> {
  const binding = getBinding(audio);
  const run = binding.queue.catch(() => undefined).then(async () => {
    try {
      await op();
    } catch {
      // dash.js teardown/swap races must not surface to the UI
    }
  });
  binding.queue = run.catch(() => undefined);
  return run;
}

async function yieldToMediaElement(): Promise<void> {
  await new Promise<void>((resolve) => {
    requestAnimationFrame(() => requestAnimationFrame(() => resolve()));
  });
}

function configureDashPlayer(player: DashPlayerInstance, getToken: () => string | null): void {
  player.updateSettings?.({
    streaming: {
      abr: {
        autoSwitchBitrate: { audio: false },
      },
      buffer: bufferRetentionSettings(undefined),
      gaps: {
        jumpGaps: true,
        jumpLargeGaps: false,
        smallGapLimit: 1.5,
        threshold: 0.3,
        enableSeekFix: false,
      },
    },
  });

  player.addRequestInterceptor((request) => {
    const headers = (request.headers ??= {}) as Record<string, string>;
    const token = getToken();
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
    headers["X-Amuse-Client"] = WEB_CLIENT_HEADER;

    if (request.url.startsWith("/api/")) {
      request.url = resolveApiUrl(request.url);
    }

    return Promise.resolve(request);
  });
}

function safeInitialize(
  player: DashPlayerInstance,
  audio: HTMLAudioElement,
  manifestUrl: string,
  startTimeSec = 0,
): boolean {
  try {
    player.initialize(audio, manifestUrl, false, startTimeSec);
    return true;
  } catch {
    return false;
  }
}

/** dash.js destroy() calls reset() which touches the media element — defer off the hot path. */
function schedulePlayerDestroy(player: DashPlayerInstance): void {
  setTimeout(() => {
    try {
      player.destroy();
    } catch {
      // NotSupportedError is common on Web Audio–routed elements; playback already moved on.
    }
  }, 0);
}

async function ensureDashPlayer(
  getToken: () => string | null,
  binding: AudioBinding,
): Promise<DashPlayerInstance> {
  if (binding.player) {
    return binding.player;
  }

  const dashjs = await import("dashjs");
  const player = dashjs.MediaPlayer().create() as unknown as DashPlayerInstance;
  configureDashPlayer(player, getToken);
  binding.player = player;
  binding.configured = true;
  binding.initialized = false;
  return player;
}

/**
 * dash.js documents track changes as attachSource(newUrl) on the same player.
 * reset/destroy during skip is what triggers "Operation is not supported" here.
 */
function safeAttachSource(
  player: DashPlayerInstance,
  manifestUrl: string,
  startTimeSec = 0,
): void {
  try {
    player.attachSource(manifestUrl, startTimeSec);
    return;
  } catch {
    // Fall through to soft reset.
  }

  try {
    player.attachSource(null);
  } catch {
    // ignore
  }

  try {
    player.attachSource(manifestUrl, startTimeSec);
  } catch {
    // Swallowed by enqueue — attachDashToAudio may fail without surfacing DOM errors.
  }
}

async function loadManifest(
  audio: HTMLAudioElement,
  manifestUrl: string,
  getToken: () => string | null,
  binding: AudioBinding,
  startTimeSec = 0,
): Promise<DashPlayerInstance> {
  let player = await ensureDashPlayer(getToken, binding);

  try {
    audio.pause();
  } catch {
    // ignore
  }

  if (!binding.initialized) {
    if (!safeInitialize(player, audio, manifestUrl, startTimeSec)) {
      await yieldToMediaElement();
      if (!safeInitialize(player, audio, manifestUrl, startTimeSec)) {
        schedulePlayerDestroy(player);
        binding.player = null;
        binding.configured = false;
        await yieldToMediaElement();
        player = await ensureDashPlayer(getToken, binding);
        if (!safeInitialize(player, audio, manifestUrl, startTimeSec)) {
          throw new Error("dash.js failed to initialize");
        }
      }
    }
    binding.initialized = true;
    return player;
  }

  safeAttachSource(player, manifestUrl, startTimeSec);
  return player;
}

/** Fully tears down any dash.js player bound to this audio element. */
export function teardownDashAudio(audio: HTMLAudioElement): Promise<void> {
  return enqueue(audio, async () => {
    const binding = getBinding(audio);
    binding.activeSessionId = 0;
    binding.removeListeners?.();
    binding.removeListeners = null;
    const player = binding.player;
    binding.player = null;
    binding.initialized = false;
    binding.configured = false;
    if (!player) return;

    try {
      audio.pause();
    } catch {
      // ignore
    }
    schedulePlayerDestroy(player);
  });
}

function findAudioTrackForRendition(
  player: DashPlayerInstance,
  rendition: TrackStreamRenditionDto,
): DashTrackInfo | null {
  const tracks = player.getTracksFor?.("audio") ?? [];
  const byRepresentation = tracks.find((track) =>
    (track.bitrateList ?? []).some((entry) => String(entry.id) === rendition.representationId),
  );
  if (byRepresentation) return byRepresentation;

  return (
    tracks.find(
      (track) =>
        track.id === rendition.adaptationSetId ||
        track.id === rendition.codec ||
        String(track.id).toLowerCase() === rendition.codec,
    ) ?? null
  );
}

function createDashSession(
  audio: HTMLAudioElement,
  player: DashPlayerInstance,
  initialRendition: TrackStreamRenditionDto | null | undefined,
  sessionId: number,
): DashSession {
  const listeners = new Set<(info: ActiveRenditionInfo | null) => void>();
  const stallListeners = new Set<() => void>();
  let active: ActiveRenditionInfo | null = null;
  let streamReady = false;
  let pendingRendition: TrackStreamRenditionDto | null = initialRendition ?? null;
  let destroyed = false;

  const applyBufferRetentionForDuration = (durationSec: number) => {
    player.updateSettings?.({
      streaming: {
        buffer: bufferRetentionSettings(durationSec),
      },
    });
  };

  const notify = () => {
    for (const listener of listeners) listener(active);
  };

  const notifyStall = () => {
    if (destroyed) return;
    resyncBufferingFromPlayhead(player as DashPlayerWithStream, audio);
    for (const listener of stallListeners) listener();
  };

  const onPlaybackSeeked = () => {
    if (destroyed) return;
    void pruneDisjointAheadBuffer(
      player as DashPlayerWithStream,
      audio,
      audio.currentTime,
    );
    resyncBufferingFromPlayhead(player as DashPlayerWithStream, audio);
  };

  const applyRendition = (rendition: TrackStreamRenditionDto) => {
    const track = findAudioTrackForRendition(player, rendition);
    if (track && player.setCurrentTrack) {
      player.setCurrentTrack(track);
    }
    if (player.setRepresentationForTypeById) {
      player.setRepresentationForTypeById("audio", rendition.representationId, true);
    }
    active = { rendition, bandwidthKbps: rendition.bitrateKbps };
    notify();
  };

  const setRendition = (rendition: TrackStreamRenditionDto) => {
    if (destroyed) return;
    pendingRendition = rendition;
    if (!streamReady) return;
    applyRendition(rendition);
  };

  const onStreamActivated = () => {
    if (destroyed) return;
    streamReady = true;
    if (pendingRendition) {
      applyRendition(pendingRendition);
    }
  };

  const onRepresentationSwitch = () => {
    if (destroyed || !active) return;
    const list = player.getBitrateInfoListFor?.("audio") ?? [];
    const current = list[0];
    if (!current) return;
    const bandwidth = current.bandwidth ?? null;
    active = {
      rendition: active.rendition,
      bandwidthKbps: bandwidth ? Math.round(bandwidth / 1000) : active.rendition.bitrateKbps,
    };
    notify();
  };

  const onDurationAvailable = () => {
    if (destroyed) return;
    if (Number.isFinite(audio.duration) && audio.duration > 0) {
      applyBufferRetentionForDuration(audio.duration);
    }
  };

  const removePlayerListeners = () => {
    audio.removeEventListener("loadedmetadata", onDurationAvailable);
    audio.removeEventListener("durationchange", onDurationAvailable);
    player.off?.("streamActivated", onStreamActivated);
    player.off?.("representationSwitch", onRepresentationSwitch);
    player.off?.("playbackSeeked", onPlaybackSeeked);
    player.off?.("bufferStalled", notifyStall);
    player.off?.("playbackStalled", notifyStall);
  };

  audio.addEventListener("loadedmetadata", onDurationAvailable);
  audio.addEventListener("durationchange", onDurationAvailable);
  player.on?.("streamActivated", onStreamActivated);
  player.on?.("representationSwitch", onRepresentationSwitch);
  player.on?.("playbackSeeked", onPlaybackSeeked);
  player.on?.("bufferStalled", notifyStall);
  player.on?.("playbackStalled", notifyStall);
  onDurationAvailable();

  const binding = getBinding(audio);
  binding.removeListeners = removePlayerListeners;

  return {
    destroy: () =>
      enqueue(audio, async () => {
        if (binding.activeSessionId !== sessionId || destroyed) return;
        destroyed = true;
        binding.activeSessionId = 0;
        removePlayerListeners();
        if (binding.removeListeners === removePlayerListeners) {
          binding.removeListeners = null;
        }
      }),
    seek: (positionSec: number) => {
      if (destroyed) return;
      player.seek?.(positionSec);
    },
    whenStreamReady: (): Promise<void> => {
      if (destroyed || streamReady) return Promise.resolve();
      return new Promise((resolve) => {
        const onActivated = () => {
          player.off?.("streamActivated", onActivated);
          resolve();
        };
        player.on?.("streamActivated", onActivated);
      });
    },
    setRendition,
    getThroughputKbps: () => {
      if (destroyed) return undefined;
      const samples = player.getRawThroughputData?.("audio") ?? [];
      if (samples.length < 2) return undefined;

      const kbps = player.getAverageThroughput?.("audio");
      if (kbps === undefined || !Number.isFinite(kbps) || kbps <= 0) return undefined;
      return Math.round(kbps);
    },
    getBufferLengthSeconds: () => {
      if (destroyed) return undefined;
      const seconds = player.getBufferLength?.("audio");
      return seconds !== undefined && Number.isFinite(seconds) ? seconds : undefined;
    },
    onActiveRenditionChange: (listener) => {
      listeners.add(listener);
      listener(active);
      return () => listeners.delete(listener);
    },
    onBufferStall: (listener) => {
      stallListeners.add(listener);
      return () => listeners.delete(listener);
    },
  };
}

export async function attachDashToAudio(
  audio: HTMLAudioElement,
  manifestUrl: string,
  getToken: () => string | null,
  initialRendition?: TrackStreamRenditionDto | null,
  startTimeSec = 0,
): Promise<DashSession> {
  const binding = getBinding(audio);
  const sessionId = ++binding.nextSessionId;
  let session: DashSession | null = null;

  await enqueue(audio, async () => {
    binding.removeListeners?.();
    binding.removeListeners = null;

    const player = await loadManifest(audio, manifestUrl, getToken, binding, startTimeSec);
    binding.activeSessionId = sessionId;
    session = createDashSession(audio, player, initialRendition, sessionId);
  });

  if (!session) {
    throw new Error("Failed to attach dash.js player");
  }

  return session;
}
