/** Playhead in ms from the media element (nearest millisecond, not floored). */
export function readAudioPositionMs(audio: HTMLAudioElement): number {
  const t = audio.currentTime;
  if (!Number.isFinite(t) || t < 0) return 0;
  return Math.round(t * 1000);
}

export function readAudioDurationMs(audio: HTMLAudioElement): number | undefined {
  const d = audio.duration;
  if (!Number.isFinite(d) || d <= 0) return undefined;
  return Math.round(d * 1000);
}
