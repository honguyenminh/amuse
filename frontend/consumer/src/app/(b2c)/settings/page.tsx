"use client";

import { AppShell } from "@/components/ui/AppShell";
import { PageContent } from "@/components/ui/PageContent";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import {
  defaultPlaybackSettings,
  loadPlaybackSettings,
  savePlaybackSettings,
  type PlaybackSettings,
  type PreferredQuality,
} from "@/lib/playback/playbackSettings";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { useEffect, useState } from "react";

export default function SettingsPage() {
  const { setVolume, refreshPlaybackSettings } = usePlayback();
  const [settings, setSettings] = useState<PlaybackSettings>(defaultPlaybackSettings);

  useEffect(() => {
    setSettings(loadPlaybackSettings());
  }, []);

  const update = (partial: Partial<PlaybackSettings>) => {
    const next = savePlaybackSettings(partial);
    setSettings(next);
    if (partial.volume !== undefined) {
      setVolume(next.volume);
    }
    if (partial.volumeNormalization !== undefined) {
      refreshPlaybackSettings();
    }
  };

  return (
    <AppShell title="Settings" activePath="/settings">
      <PageContent width="settings">
        <Card>
          <Text variant="title-large">Playback</Text>
          <div className="mt-4 flex flex-col gap-4">
            <label className="flex items-center justify-between gap-4">
              <span>
                <Text variant="body-medium">Volume normalization</Text>
                <Text variant="label-small" className="text-on-surface-variant">
                  When on, playback gain is adjusted per track to about -14 LUFS using loudness
                  measured at upload. When off, hear the original levels.
                </Text>
              </span>
              <input
                type="checkbox"
                checked={settings.volumeNormalization}
                onChange={(e) => update({ volumeNormalization: e.target.checked })}
                className="h-5 w-5"
              />
            </label>

            <div className="flex flex-col gap-2">
              <Text variant="body-medium">Preferred quality</Text>
              <Text variant="label-small" className="text-on-surface-variant">
                Auto mode will not exceed this tier based on your network.
              </Text>
              <div className="flex flex-wrap gap-2">
                {(["lossless", "high", "medium", "low"] as PreferredQuality[]).map((tier) => (
                  <button
                    key={tier}
                    type="button"
                    onClick={() => update({ preferredQuality: tier })}
                    className={
                      settings.preferredQuality === tier
                        ? "rounded-md bg-primary-container px-3 py-1 text-on-primary-container"
                        : "rounded-md border border-outline px-3 py-1"
                    }
                  >
                    {tier.charAt(0).toUpperCase() + tier.slice(1)}
                  </button>
                ))}
              </div>
            </div>

            <div className="flex flex-col gap-2">
              <Text variant="body-medium">Streaming quality</Text>
              <select
                value={settings.qualityMode}
                onChange={(e) =>
                  update({
                    qualityMode: e.target.value as PlaybackSettings["qualityMode"],
                    manualRenditionId:
                      e.target.value === "auto" ? null : settings.manualRenditionId,
                  })
                }
                className="rounded-md border border-outline bg-surface px-3 py-2"
              >
                <option value="auto">Auto</option>
                <option value="manual">Manual</option>
              </select>
            </div>

            {settings.qualityMode === "manual" ? (
              <div className="flex flex-col gap-2">
                <Text variant="body-medium">Manual rendition</Text>
                <input
                  type="text"
                  value={settings.manualRenditionId ?? ""}
                  placeholder="e.g. opus-128"
                  onChange={(e) =>
                    update({
                      manualRenditionId: e.target.value || null,
                    })
                  }
                  className="rounded-md border border-outline bg-surface px-3 py-2"
                />
                <Text variant="label-small" className="text-on-surface-variant">
                  Pick a rendition from the now playing screen, or enter an id such as opus-128,
                  aac-256, flac-0.
                </Text>
              </div>
            ) : null}
          </div>
        </Card>
      </PageContent>
    </AppShell>
  );
}
