export type PreferredQuality = "lossless" | "high" | "medium" | "low";

export type PlaybackSettings = {
  version: 1;
  volume: number;
  volumeNormalization: boolean;
  preferredQuality: PreferredQuality;
  qualityMode: "auto" | "manual";
  manualRenditionId: string | null;
};

const STORAGE_KEY = "amuse.consumer.playbackSettings.v1";

export const defaultPlaybackSettings: PlaybackSettings = {
  version: 1,
  volume: 0.85,
  volumeNormalization: true,
  preferredQuality: "high",
  qualityMode: "auto",
  manualRenditionId: null,
};

function clampVolume(volume: number): number {
  return Math.max(0, Math.min(1, volume));
}

export function loadPlaybackSettings(): PlaybackSettings {
  if (typeof window === "undefined") return defaultPlaybackSettings;
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return defaultPlaybackSettings;
    const parsed = JSON.parse(raw) as Partial<PlaybackSettings>;
    return {
      ...defaultPlaybackSettings,
      ...parsed,
      version: 1,
      volume: clampVolume(parsed.volume ?? defaultPlaybackSettings.volume),
    };
  } catch {
    return defaultPlaybackSettings;
  }
}

export function savePlaybackSettings(partial: Partial<PlaybackSettings>): PlaybackSettings {
  const next = {
    ...loadPlaybackSettings(),
    ...partial,
    version: 1 as const,
    volume:
      partial.volume !== undefined
        ? clampVolume(partial.volume)
        : loadPlaybackSettings().volume,
  };
  if (typeof window !== "undefined") {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  }
  return next;
}
