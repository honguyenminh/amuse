"use client";

import { resolveApiUrl, WEB_CLIENT_HEADER } from "@/lib/api/config";

type DashSession = {
  destroy: () => void;
};

export async function attachDashToAudio(
  audio: HTMLAudioElement,
  manifestUrl: string,
  getToken: () => string | null,
): Promise<DashSession> {
  const dashjs = await import("dashjs");
  const player = dashjs.MediaPlayer().create();

  // dash.js 5.x: RequestModifier was removed; use addRequestInterceptor instead.
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

  player.initialize(audio, manifestUrl, false);

  return {
    destroy: () => player.reset(),
  };
}
