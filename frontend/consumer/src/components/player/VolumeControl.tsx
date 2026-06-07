"use client";

import { IconButton } from "@/components/ui/IconButton";
import { Slider } from "@/components/ui/Slider";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { savePlaybackSettings } from "@/lib/playback/playbackSettings";
import { cn } from "@/lib/cn";
import { useCallback, useLayoutEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";

function VolumeIcon({ muted, level }: { muted: boolean; level: number }) {
  if (muted || level === 0) {
    return (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" aria-hidden>
        <path d="M16.5 12c0-1.77-1.02-3.29-2.5-4.03v2.21l2.45 2.45c.03-.2.05-.41.05-.63zm2.5 0c0 .94-.2 1.82-.54 2.64l1.51 1.51C20.63 14.91 21 13.5 21 12c0-4.28-2.99-7.86-7-8.77v2.06c2.89.86 5 3.54 5 6.71zM4.27 3 3 4.27 7.73 9H3v6h4l5 5v-6.73l4.25 4.25c-.67.52-1.42.93-2.25 1.18v2.06c1.38-.31 2.63-.95 3.69-1.81L19.73 21 21 19.73l-9-9L4.27 3zM12 4 9.91 6.09 12 8.18V4z" />
      </svg>
    );
  }
  if (level < 0.5) {
    return (
      <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" aria-hidden>
        <path d="M18.5 12c0-1.77-1.02-3.29-2.5-4.03v8.05c1.48-.73 2.5-2.25 2.5-4.02zM5 9v6h4l5 5V4L9 9H5z" />
      </svg>
    );
  }
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="currentColor" aria-hidden>
      <path d="M3 9v6h4l5 5V4L7 9H3zm13.5 3c0-1.77-1.02-3.29-2.5-4.03v8.05c1.48-.73 2.5-2.25 2.5-4.02zM14 3.23v2.06c2.89.86 5 3.54 5 6.71s-2.11 5.85-5 6.71v2.06c4.01-.91 7-4.49 7-8.77s-2.99-7.86-7-8.77z" />
    </svg>
  );
}

type VolumeControlProps = {
  variant?: "compact" | "full";
  className?: string;
  /** Portal the slider popup to `document.body` (needed when overlapping layered mini-player chrome). */
  portalPopup?: boolean;
};

export function VolumeControl({
  variant = "full",
  className,
  portalPopup = false,
}: VolumeControlProps) {
  const { state, setVolume, toggleMute } = usePlayback();
  const muted = !Number.isFinite(state.volume) || state.volume === 0;
  const persistTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const anchorRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const [popupPosition, setPopupPosition] = useState<{ left: number; bottom: number } | null>(
    null,
  );
  const closeTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const persistVolume = useCallback((volume: number) => {
    if (persistTimerRef.current) clearTimeout(persistTimerRef.current);
    persistTimerRef.current = setTimeout(() => {
      savePlaybackSettings({ volume });
    }, 200);
  }, []);

  const onVolumeChange = useCallback(
    (value: number) => {
      if (!Number.isFinite(value)) return;
      const v = value / 100;
      setVolume(v);
      persistVolume(v);
    },
    [setVolume, persistVolume],
  );

  const onToggleMute = useCallback(() => {
    toggleMute();
  }, [toggleMute]);

  const updatePopupPosition = useCallback(() => {
    const anchor = anchorRef.current;
    if (!anchor) {
      return;
    }
    const rect = anchor.getBoundingClientRect();
    setPopupPosition({
      left: rect.left + rect.width / 2,
      bottom: window.innerHeight - rect.top + 8,
    });
  }, []);

  const openPopup = useCallback(() => {
    if (closeTimerRef.current) clearTimeout(closeTimerRef.current);
    setOpen(true);
    if (portalPopup) {
      updatePopupPosition();
    }
  }, [portalPopup, updatePopupPosition]);

  const keepPopupOpen = useCallback(() => {
    if (closeTimerRef.current) clearTimeout(closeTimerRef.current);
    setOpen(true);
  }, []);

  const scheduleClose = useCallback(() => {
    if (closeTimerRef.current) clearTimeout(closeTimerRef.current);
    closeTimerRef.current = setTimeout(() => setOpen(false), 180);
  }, []);

  useLayoutEffect(() => {
    if (!portalPopup || !open) {
      return;
    }
    updatePopupPosition();
    window.addEventListener("resize", updatePopupPosition);
    window.addEventListener("scroll", updatePopupPosition, true);
    return () => {
      window.removeEventListener("resize", updatePopupPosition);
      window.removeEventListener("scroll", updatePopupPosition, true);
    };
  }, [portalPopup, open, updatePopupPosition]);

  const popupVisible = open;
  const popupClassName = cn(
    "rounded-md border border-outline bg-surface px-2 py-3 shadow-lg transition-opacity duration-150",
    popupVisible ? "pointer-events-auto opacity-100" : "pointer-events-none opacity-0",
    !portalPopup &&
      "group-hover:pointer-events-auto group-hover:opacity-100 group-focus-within:pointer-events-auto group-focus-within:opacity-100",
    portalPopup
      ? "fixed z-50 -translate-x-1/2"
      : "absolute bottom-full left-1/2 z-30 mb-2 -translate-x-1/2",
  );

  const popup = (
    <div
      className={popupClassName}
      style={
        portalPopup && popupPosition
          ? { left: popupPosition.left, bottom: popupPosition.bottom }
          : undefined
      }
      onMouseEnter={openPopup}
      onMouseLeave={scheduleClose}
    >
      <Slider
        value={Math.round(state.volume * 100)}
        min={0}
        max={100}
        step={1}
        onChange={onVolumeChange}
        onScrubStart={keepPopupOpen}
        label="Volume"
        orientation="vertical"
        size={variant === "compact" ? "sm" : "md"}
      />
    </div>
  );

  return (
    <div
      ref={anchorRef}
      className={cn("group relative flex items-center", className)}
      onMouseEnter={openPopup}
      onMouseLeave={scheduleClose}
      onFocusCapture={openPopup}
      onBlurCapture={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node | null)) {
          scheduleClose();
        }
      }}
    >
      <IconButton
        label={muted ? "Unmute" : "Mute"}
        variant="ghost"
        size="sm"
        aria-expanded={open}
        onClick={onToggleMute}
      >
        <VolumeIcon muted={muted || state.volume === 0} level={state.volume} />
      </IconButton>
      {portalPopup && typeof document !== "undefined"
        ? createPortal(popup, document.body)
        : popup}
    </div>
  );
}
