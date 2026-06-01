import { formatDateTime } from "@/lib/format/dateTime";

/** Display a catalog release instant in the viewer's local timezone (date + time). */
export function formatReleaseDateTime(iso: string): string {
  return formatDateTime(iso);
}

/** Value for `<input type="datetime-local" />` from an ISO-8601 instant. */
export function toLocalDatetimeInput(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return "";
  }
  const local = new Date(date);
  local.setMinutes(local.getMinutes() - local.getTimezoneOffset());
  return local.toISOString().slice(0, 16);
}

/** Default `datetime-local` value for "now" in the viewer's timezone. */
export function defaultLocalDatetimeInput(date: Date = new Date()): string {
  const local = new Date(date);
  local.setMinutes(local.getMinutes() - local.getTimezoneOffset());
  return local.toISOString().slice(0, 16);
}

/**
 * Converts a `datetime-local` value (no timezone) to UTC ISO for the API.
 * The input is interpreted as the user's local wall-clock time.
 */
export function toReleaseDateIso(localDatetime: string): string {
  const parsed = new Date(localDatetime);
  if (Number.isNaN(parsed.getTime())) {
    throw new Error("Invalid release date.");
  }
  return parsed.toISOString();
}

/** Whether the release instant is still in the future (same instant comparison as the API). */
export function isReleaseDateInFuture(releaseDateIso: string): boolean {
  const releaseMs = new Date(releaseDateIso).getTime();
  if (Number.isNaN(releaseMs)) {
    return false;
  }
  return releaseMs > Date.now();
}

/** Short timezone label for helper copy, e.g. "GMT+7" or "PDT". */
export function getLocalTimeZoneLabel(): string {
  const parts = new Intl.DateTimeFormat(undefined, {
    timeZoneName: "short",
  }).formatToParts(new Date());
  return parts.find((part) => part.type === "timeZoneName")?.value ?? "local time";
}

export const RELEASE_DATE_TIME_HELPER = (() => {
  const zone = getLocalTimeZoneLabel();
  return `Use your ${zone} local timezone`;
})();
