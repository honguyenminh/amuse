import { describe, expect, it, vi } from "vitest";
import {
  alignAudioToStatePosition,
  restorePlaybackPosition,
  waitForAudioDuration,
} from "../restorePlaybackPosition";

describe("restorePlaybackPosition", () => {
  it("returns zero without seeking when position is zero", async () => {
    const audio = {
      duration: 180,
      currentTime: 0,
    } as HTMLAudioElement;

    const result = await restorePlaybackPosition(audio, 0, null);
    expect(result.positionMs).toBe(0);
    expect(audio.currentTime).toBe(0);
  });

  it("waits for metadata before seeking progressive audio", async () => {
    const audio = {
      duration: NaN,
      currentTime: 0,
      addEventListener: vi.fn((event: string, handler: () => void) => {
        if (event === "loadedmetadata") {
          queueMicrotask(() => {
            Object.defineProperty(audio, "duration", { value: 180, configurable: true });
            handler();
          });
        }
      }),
      removeEventListener: vi.fn(),
    } as unknown as HTMLAudioElement;

    Object.defineProperty(audio, "currentTime", {
      configurable: true,
      get() {
        return (audio as { _currentTime?: number })._currentTime ?? 0;
      },
      set(value: number) {
        (audio as { _currentTime?: number })._currentTime = value;
      },
    });

    const result = await restorePlaybackPosition(audio, 90_000, null);
    expect(result.positionMs).toBe(90_000);
    expect(audio.currentTime).toBe(90);
  });

  it("waits for dash stream readiness before seeking", async () => {
    const audio = {
      duration: 180,
      currentTime: 0,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    } as unknown as HTMLAudioElement;
    Object.defineProperty(audio, "currentTime", {
      configurable: true,
      get() {
        return (audio as { _currentTime?: number })._currentTime ?? 0;
      },
      set(value: number) {
        (audio as { _currentTime?: number })._currentTime = value;
      },
    });

    const seek = vi.fn((positionSec: number) => {
      audio.currentTime = positionSec;
    });
    const whenStreamReady = vi.fn(async () => undefined);

    await restorePlaybackPosition(audio, 45_000, {
      seek,
      whenStreamReady,
    });

    expect(whenStreamReady).toHaveBeenCalled();
    expect(seek).toHaveBeenCalledWith(45);
    expect(audio.currentTime).toBe(45);
  });
});

describe("alignAudioToStatePosition", () => {
  it("seeks progressive audio without blocking", () => {
    const audio = {
      duration: 180,
      currentTime: 0,
    } as HTMLAudioElement;
    Object.defineProperty(audio, "currentTime", {
      configurable: true,
      get() {
        return (audio as { _currentTime?: number })._currentTime ?? 0;
      },
      set(value: number) {
        (audio as { _currentTime?: number })._currentTime = value;
      },
    });

    alignAudioToStatePosition(audio, 60_000, null);
    expect(audio.currentTime).toBe(60);
  });
});

describe("waitForAudioDuration", () => {
  it("resolves immediately when duration is already known", async () => {
    const audio = { duration: 120 } as HTMLAudioElement;
    await expect(waitForAudioDuration(audio)).resolves.toBe(true);
  });
});
