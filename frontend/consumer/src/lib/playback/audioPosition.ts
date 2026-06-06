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

/** End of the buffered range that contains the current playhead, in ms. */
export function readAudioBufferedEndMs(audio: HTMLAudioElement): number {
  const current = audio.currentTime;
  if (!Number.isFinite(current) || current < 0) return 0;

  const { buffered } = audio;
  for (let i = 0; i < buffered.length; i++) {
    if (buffered.start(i) <= current && current <= buffered.end(i)) {
      return Math.round(buffered.end(i) * 1000);
    }
  }

  return Math.round(current * 1000);
}
