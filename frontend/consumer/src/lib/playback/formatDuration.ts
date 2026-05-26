/**
 * Formats a millisecond duration as `m:ss` (or `h:mm:ss` for tracks an hour or longer).
 * Returns "0:00" for invalid input so the UI never shows NaN.
 */
export function formatDuration(ms: number): string {
  if (!Number.isFinite(ms) || ms <= 0) return "0:00";
  const totalSeconds = Math.floor(ms / 1000);
  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;
  const ss = seconds.toString().padStart(2, "0");
  if (hours > 0) {
    return `${hours}:${minutes.toString().padStart(2, "0")}:${ss}`;
  }
  return `${minutes}:${ss}`;
}
