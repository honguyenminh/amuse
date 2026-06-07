"use client";

import { AnchoredPopup } from "@/components/ui/AnchoredPopup";
import { Text } from "@/components/ui/Text";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import {
  loadPlaybackSettings,
  savePlaybackSettings,
  type PlaybackSettings,
} from "@/lib/playback/playbackSettings";
import { formatRenditionLabel } from "@/lib/playback/selectRendition";
import { useCallback, useRef, useState } from "react";

export function QualityPicker() {
  const { streamRenditions, activeRendition, switchRendition } = usePlayback();
  const [open, setOpen] = useState(false);
  const [settings, setSettings] = useState<PlaybackSettings>(() => loadPlaybackSettings());
  const triggerRef = useRef<HTMLButtonElement>(null);

  const setMode = useCallback((mode: PlaybackSettings["qualityMode"]) => {
    const next = savePlaybackSettings({
      qualityMode: mode,
      manualRenditionId: mode === "auto" ? null : settings.manualRenditionId,
    });
    setSettings(next);
  }, [settings.manualRenditionId]);

  if (!activeRendition && streamRenditions.length === 0) return null;

  const label = activeRendition
    ? formatRenditionLabel(activeRendition.rendition)
    : "Select quality";

  return (
    <div className="mt-3 flex flex-col items-center gap-1">
      <button
        ref={triggerRef}
        type="button"
        onClick={() => setOpen((value) => !value)}
        className="rounded-md border border-outline px-3 py-1 text-label-small text-on-surface-variant hover:bg-surface-variant"
      >
        {settings.qualityMode === "auto" ? "Auto · " : ""}
        {label}
      </button>
      <AnchoredPopup
        open={open}
        onClose={() => setOpen(false)}
        anchorRef={triggerRef}
        preferredPlacement="bottom"
        align="start"
        className="min-w-[14rem] rounded-md border-2 border-outline bg-surface py-1 shadow-lg"
      >
        <button
          type="button"
          className="flex w-full px-4 py-2 text-left hover:bg-surface-variant"
          onClick={() => {
            setMode("auto");
            setOpen(false);
          }}
        >
          <Text variant="body-medium">Auto</Text>
        </button>
        {streamRenditions.map((rendition) => (
          <button
            key={rendition.id}
            type="button"
            className="flex w-full px-4 py-2 text-left hover:bg-surface-variant"
            onClick={() => {
              switchRendition(rendition.id);
              const next = savePlaybackSettings({
                qualityMode: "manual",
                manualRenditionId: rendition.id,
              });
              setSettings(next);
              setOpen(false);
            }}
          >
            <Text variant="body-medium">{formatRenditionLabel(rendition)}</Text>
          </button>
        ))}
      </AnchoredPopup>
    </div>
  );
}
