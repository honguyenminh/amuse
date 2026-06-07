import {
  completeAudioUpload,
  presignAudioUpload,
} from "@/lib/api/catalogClient";
import { ApiError } from "@/lib/api/types";

export type AudioUploadProgress = {
  loaded: number;
  total: number;
};

function titleFromFileName(fileName: string): string {
  const withoutExtension = fileName.replace(/\.[^.]+$/, "");
  return withoutExtension.replace(/[_-]+/g, " ").trim();
}

export function inferTrackTitle(file: File): string {
  const fromName = titleFromFileName(file.name);
  return fromName.length > 0 ? fromName : "Untitled track";
}

export async function inferAudioDurationMs(file: File): Promise<number> {
  const objectUrl = URL.createObjectURL(file);
  try {
    const durationSeconds = await new Promise<number>((resolve, reject) => {
      const audio = new Audio();
      audio.preload = "metadata";
      audio.onloadedmetadata = () => {
        if (!Number.isFinite(audio.duration) || audio.duration <= 0) {
          reject(new Error("Could not read audio duration."));
          return;
        }
        resolve(audio.duration);
      };
      audio.onerror = () => reject(new Error("Could not read audio file."));
      audio.src = objectUrl;
    });

    return Math.max(1, Math.round(durationSeconds * 1000));
  } finally {
    URL.revokeObjectURL(objectUrl);
  }
}

export async function uploadTrackAudioMaster(
  trackId: string,
  file: File,
  options?: {
    onProgress?: (progress: AudioUploadProgress) => void;
    signal?: AbortSignal;
  },
): Promise<void> {
  const contentType = file.type || "application/octet-stream";
  const presigned = await presignAudioUpload(trackId, {
    fileName: file.name,
    contentType,
  });

  if (options?.signal?.aborted) {
    throw new DOMException("Upload aborted.", "AbortError");
  }

  await uploadWithProgress(presigned.url, presigned.method, file, contentType, options);

  await completeAudioUpload(trackId, { key: presigned.key });
}

async function uploadWithProgress(
  url: string,
  method: string,
  file: File,
  contentType: string,
  options?: {
    onProgress?: (progress: AudioUploadProgress) => void;
    signal?: AbortSignal;
  },
): Promise<void> {
  if (options?.onProgress) {
    await xhrUpload(url, method, file, contentType, options.onProgress, options.signal);
    return;
  }

  const response = await fetch(url, {
    method,
    body: file,
    headers: { "Content-Type": contentType },
    signal: options?.signal,
  });

  if (!response.ok) {
    throw new Error(`Audio upload failed (${response.status}).`);
  }
}

function xhrUpload(
  url: string,
  method: string,
  file: File,
  contentType: string,
  onProgress: (progress: AudioUploadProgress) => void,
  signal?: AbortSignal,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open(method, url);
    xhr.setRequestHeader("Content-Type", contentType);

    const onAbort = () => {
      xhr.abort();
      reject(new DOMException("Upload aborted.", "AbortError"));
    };

    if (signal) {
      if (signal.aborted) {
        onAbort();
        return;
      }
      signal.addEventListener("abort", onAbort, { once: true });
    }

    xhr.upload.onprogress = (event) => {
      if (event.lengthComputable) {
        onProgress({ loaded: event.loaded, total: event.total });
      }
    };

    xhr.onload = () => {
      if (signal) {
        signal.removeEventListener("abort", onAbort);
      }
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve();
        return;
      }
      reject(new Error(`Audio upload failed (${xhr.status}).`));
    };

    xhr.onerror = () => {
      if (signal) {
        signal.removeEventListener("abort", onAbort);
      }
      reject(new Error("Audio upload failed due to a network error."));
    };

    xhr.send(file);
  });
}

export function formatUploadError(err: unknown): string {
  if (err instanceof ApiError) {
    return err.message;
  }
  if (err instanceof DOMException && err.name === "AbortError") {
    return "Upload cancelled.";
  }
  if (err instanceof Error) {
    return err.message;
  }
  return "Upload failed.";
}

export function formatDurationMs(ms: number): string {
  const totalSeconds = Math.floor(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

export function formatUploadProgress(progress: AudioUploadProgress): string {
  if (progress.total <= 0) {
    return "Uploading…";
  }
  const percent = Math.min(100, Math.round((progress.loaded / progress.total) * 100));
  return `Uploading… ${percent}%`;
}
