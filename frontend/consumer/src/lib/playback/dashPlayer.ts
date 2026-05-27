"use client";

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

  player.extend(
    "RequestModifier",
    () => ({
      modifyRequestURL: (url: string) => url,
      modifyRequestHeader: (xhr: XMLHttpRequest) => {
        const token = getToken();
        if (token) xhr.setRequestHeader("Authorization", `Bearer ${token}`);
        return xhr;
      },
    }),
    true,
  );

  player.initialize(audio, manifestUrl, false);

  return {
    destroy: () => player.reset(),
  };
}

