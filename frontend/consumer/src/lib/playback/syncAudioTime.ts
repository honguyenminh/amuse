/**
 * Seek the media element with minimal audible glitch.
 * Pause → jump → optionally resume avoids many browsers popping on in-place seeks.
 *
 * Uses `currentTime` (not `fastSeek`) so the playhead stays at sub-second precision.
 */
export function syncAudioTime(
  audio: HTMLAudioElement,
  positionSec: number,
  resume: boolean,
  pauseImmediate: (audio: HTMLAudioElement) => void = (el) => el.pause(),
): void {
  if (!Number.isFinite(audio.duration) || audio.duration <= 0) return;

  const target = Math.max(0, Math.min(positionSec, audio.duration));
  if (Math.abs(audio.currentTime - target) < 0.001) {
    if (resume && audio.paused) void audio.play().catch(() => undefined);
    return;
  }

  pauseImmediate(audio);
  audio.currentTime = target;

  if (resume) {
    void audio.play().catch(() => undefined);
  }
}
