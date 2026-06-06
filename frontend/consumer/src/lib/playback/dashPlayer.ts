"use client";

import { resolveApiUrl, WEB_CLIENT_HEADER } from "@/lib/api/config";
import type { TrackStreamRenditionDto } from "@/lib/api/types";

export type ActiveRenditionInfo = {
  rendition: TrackStreamRenditionDto;
  bandwidthKbps: number | null;
};

type DashSession = {
  destroy: () => void;
  setRendition: (rendition: TrackStreamRenditionDto) => void;
  onActiveRenditionChange: (listener: (info: ActiveRenditionInfo | null) => void) => () => void;
};

type DashPlayerInstance = {
  addRequestInterceptor: (
    interceptor: (request: { url: string; headers?: Record<string, string> }) => Promise<unknown>,
  ) => void;
  initialize: (audio: HTMLAudioElement, url: string, autoPlay: boolean) => void;
  reset: () => void;
  getBitrateInfoListFor?: (type: string) => Array<{ id?: string | null; bandwidth?: number }>;
  setRepresentationForTypeById?: (type: string, id: string) => void;
  on?: (event: string, handler: () => void) => void;
};

export async function attachDashToAudio(
  audio: HTMLAudioElement,
  manifestUrl: string,
  getToken: () => string | null,
  initialRendition?: TrackStreamRenditionDto | null,
): Promise<DashSession> {
  const dashjs = await import("dashjs");
  const player = dashjs.MediaPlayer().create() as unknown as DashPlayerInstance;

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
  let active: ActiveRenditionInfo | null = null;

  const notify = () => {
    for (const listener of listeners) listener(active);
  };

  const updateActiveFromPlayer = () => {
    const list = player.getBitrateInfoListFor?.("audio") ?? [];
    const current = list[0];
    if (!current) return;
    const bandwidth = current.bandwidth ?? null;
    if (initialRendition && active?.rendition.id === initialRendition.id) {
      active = {
        rendition: initialRendition,
        bandwidthKbps: bandwidth ? Math.round(bandwidth / 1000) : initialRendition.bitrateKbps,
      };
    } else if (initialRendition) {
      active = {
        rendition: initialRendition,
        bandwidthKbps: bandwidth ? Math.round(bandwidth / 1000) : initialRendition.bitrateKbps,
      };
    }
    notify();
  };

  const setRendition = (rendition: TrackStreamRenditionDto) => {
    if (player.setRepresentationForTypeById) {
      player.setRepresentationForTypeById("audio", rendition.representationId);
    }
    active = { rendition, bandwidthKbps: rendition.bitrateKbps };
    notify();
  };

  player.initialize(audio, manifestUrl, false);

  if (initialRendition) {
    setRendition(initialRendition);
  }

  player.on?.("representationSwitch", updateActiveFromPlayer);
  player.on?.("streamActivated", updateActiveFromPlayer);

  return {
    destroy: () => player.reset(),
    setRendition,
    onActiveRenditionChange: (listener) => {
      listeners.add(listener);
      listener(active);
      return () => listeners.delete(listener);
    },
  };
}
