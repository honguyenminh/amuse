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
  initialize: (audio: HTMLAudioElement, url: string, autoPlay: boolean) => void;
  reset: () => void;
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
  destroy: () => void;
  seek: (positionSec: number) => void;
  setRendition: (rendition: TrackStreamRenditionDto) => void;
  getThroughputKbps: () => number | undefined;
  getBufferLengthSeconds: () => number | undefined;
  onActiveRenditionChange: (listener: (info: ActiveRenditionInfo | null) => void) => () => void;
  onBufferStall: (listener: () => void) => () => void;
};

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

export async function attachDashToAudio(
  audio: HTMLAudioElement,
  manifestUrl: string,
  getToken: () => string | null,
  initialRendition?: TrackStreamRenditionDto | null,
): Promise<DashSession> {
  const dashjs = await import("dashjs");
  const player = dashjs.MediaPlayer().create() as unknown as DashPlayerInstance;

  player.updateSettings?.({
    streaming: {
      abr: {
        autoSwitchBitrate: { audio: false },
      },
      buffer: bufferRetentionSettings(undefined),
      gaps: {
        jumpGaps: true,
        // VoD: disjoint cached ranges (e.g. 1:00–1:02 and 3:00+) must not skip — resume buffering.
        jumpLargeGaps: false,
        smallGapLimit: 1.5,
        threshold: 0.3,
        // Keep false so dash does not advance the scheduler across intentional buffer gaps.
        enableSeekFix: false,
      },
    },
  });

  const applyBufferRetentionForDuration = (durationSec: number) => {
    player.updateSettings?.({
      streaming: {
        buffer: bufferRetentionSettings(durationSec),
      },
    });
  };

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

  const listeners = new Set<(info: ActiveRenditionInfo | null) => void>();
  const stallListeners = new Set<() => void>();
  let active: ActiveRenditionInfo | null = null;
  let streamReady = false;
  let pendingRendition: TrackStreamRenditionDto | null = initialRendition ?? null;

  const notify = () => {
    for (const listener of listeners) listener(active);
  };

  const notifyStall = () => {
    resyncBufferingFromPlayhead(player as DashPlayerWithStream, audio);
    for (const listener of stallListeners) listener();
  };

  const onPlaybackSeeked = () => {
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
    pendingRendition = rendition;
    if (!streamReady) return;
    applyRendition(rendition);
  };

  const onStreamActivated = () => {
    streamReady = true;
    if (pendingRendition) {
      applyRendition(pendingRendition);
    }
  };

  const onRepresentationSwitch = () => {
    const list = player.getBitrateInfoListFor?.("audio") ?? [];
    const current = list[0];
    if (!current || !active) return;
    const bandwidth = current.bandwidth ?? null;
    active = {
      rendition: active.rendition,
      bandwidthKbps: bandwidth ? Math.round(bandwidth / 1000) : active.rendition.bitrateKbps,
    };
    notify();
  };

  const onDurationAvailable = () => {
    if (Number.isFinite(audio.duration) && audio.duration > 0) {
      applyBufferRetentionForDuration(audio.duration);
    }
  };

  player.initialize(audio, manifestUrl, false);
  audio.addEventListener("loadedmetadata", onDurationAvailable);
  audio.addEventListener("durationchange", onDurationAvailable);
  player.on?.("streamActivated", onStreamActivated);
  player.on?.("representationSwitch", onRepresentationSwitch);
  player.on?.("playbackSeeked", onPlaybackSeeked);
  player.on?.("bufferStalled", notifyStall);
  player.on?.("playbackStalled", notifyStall);
  onDurationAvailable();

  return {
    destroy: () => {
      audio.removeEventListener("loadedmetadata", onDurationAvailable);
      audio.removeEventListener("durationchange", onDurationAvailable);
      player.off?.("streamActivated", onStreamActivated);
      player.off?.("representationSwitch", onRepresentationSwitch);
      player.off?.("playbackSeeked", onPlaybackSeeked);
      player.off?.("bufferStalled", notifyStall);
      player.off?.("playbackStalled", notifyStall);
      player.reset();
    },
    seek: (positionSec: number) => {
      player.seek?.(positionSec);
    },
    setRendition,
    getThroughputKbps: () => {
      const samples = player.getRawThroughputData?.("audio") ?? [];
      if (samples.length < 2) return undefined;

      const kbps = player.getAverageThroughput?.("audio");
      if (kbps === undefined || !Number.isFinite(kbps) || kbps <= 0) return undefined;
      return Math.round(kbps);
    },
    getBufferLengthSeconds: () => {
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
      return () => stallListeners.delete(listener);
    },
  };
}
