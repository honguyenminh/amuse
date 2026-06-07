import { readAudioDurationMs, readAudioPositionMs } from "./audioPosition";

export async function waitForAudioDuration(
  audio: HTMLAudioElement,
  timeoutMs = 15_000,
): Promise<boolean> {
  if (Number.isFinite(audio.duration) && audio.duration > 0) return true;

  return new Promise((resolve) => {
    const onReady = () => {
      if (Number.isFinite(audio.duration) && audio.duration > 0) {
        cleanup();
        resolve(true);
      }
    };
    const timer = setTimeout(() => {
      cleanup();
      resolve(false);
    }, timeoutMs);
    const cleanup = () => {
      clearTimeout(timer);
      audio.removeEventListener("loadedmetadata", onReady);
      audio.removeEventListener("durationchange", onReady);
    };
    audio.addEventListener("loadedmetadata", onReady);
    audio.addEventListener("durationchange", onReady);
    onReady();
  });
}

async function waitForAudioNearTime(
  audio: HTMLAudioElement,
  targetSec: number,
  toleranceSec = 0.75,
  timeoutMs = 1_500,
): Promise<void> {
  if (Math.abs(audio.currentTime - targetSec) <= toleranceSec) return;

  await new Promise<void>((resolve) => {
    const check = () => {
      if (Math.abs(audio.currentTime - targetSec) <= toleranceSec) {
        cleanup();
        resolve();
      }
    };
    const timer = setTimeout(() => {
      cleanup();
      resolve();
    }, timeoutMs);
    const cleanup = () => {
      clearTimeout(timer);
      audio.removeEventListener("seeked", check);
      audio.removeEventListener("timeupdate", check);
    };
    audio.addEventListener("seeked", check);
    audio.addEventListener("timeupdate", check);
    check();
  });
}

type RestorableDashSession = {
  seek: (positionSec: number) => void;
  whenStreamReady: () => Promise<void>;
};

/**
 * Seek progressive audio to a persisted position after reload.
 * DASH reloads should pass `startTime` into attachSource instead of calling this.
 */
export async function restorePlaybackPosition(
  audio: HTMLAudioElement,
  positionMs: number,
  dashSession: RestorableDashSession | null,
): Promise<{ positionMs: number; durationMs: number }> {
  const positionSec = Math.max(0, positionMs / 1000);
  if (positionSec <= 0) {
    return {
      positionMs: 0,
      durationMs: readAudioDurationMs(audio) ?? 0,
    };
  }

  if (dashSession) {
    await dashSession.whenStreamReady();
    dashSession.seek(positionSec);
    await waitForAudioNearTime(audio, positionSec);
    const actualMs = readAudioPositionMs(audio);
    return {
      positionMs: actualMs > 0 ? actualMs : positionMs,
      durationMs: readAudioDurationMs(audio) ?? 0,
    };
  }

  await waitForAudioDuration(audio);
  const durationSec =
    Number.isFinite(audio.duration) && audio.duration > 0 ? audio.duration : positionSec;
  const targetSec = Math.min(positionSec, durationSec);
  audio.currentTime = targetSec;
  await waitForAudioNearTime(audio, targetSec);
  const actualMs = readAudioPositionMs(audio);
  return {
    positionMs: actualMs > 0 ? actualMs : positionMs,
    durationMs: readAudioDurationMs(audio) ?? 0,
  };
}

/**
 * Apply reducer position to the media element right before resuming playback.
 * Never blocks on long seek confirmation waits — DASH uses its attach startTime.
 */
export function alignAudioToStatePosition(
  audio: HTMLAudioElement,
  positionMs: number,
  dashSession: RestorableDashSession | null,
): void {
  if (positionMs <= 0) return;
  const targetSec = positionMs / 1000;
  if (Math.abs(audio.currentTime - targetSec) <= 1) return;

  if (dashSession) {
    dashSession.seek(targetSec);
    return;
  }

  if (Number.isFinite(audio.duration) && audio.duration > 0) {
    audio.currentTime = Math.min(targetSec, audio.duration);
  }
}
