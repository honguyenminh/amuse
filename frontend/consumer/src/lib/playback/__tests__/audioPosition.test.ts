import { describe, expect, it } from "vitest";
import { readAudioBufferedEndMs, readAudioPositionMs } from "../audioPosition";

describe("readAudioPositionMs", () => {
  it("rounds to nearest millisecond instead of flooring", () => {
    const audio = { currentTime: 12.5678 } as HTMLAudioElement;
    expect(readAudioPositionMs(audio)).toBe(12568);
  });
});

describe("readAudioBufferedEndMs", () => {
  it("returns the end of the range containing the playhead", () => {
    const audio = {
      currentTime: 12.5,
      buffered: {
        length: 1,
        start: (index: number) => (index === 0 ? 0 : 0),
        end: (index: number) => (index === 0 ? 45.25 : 0),
      },
    } as HTMLAudioElement;

    expect(readAudioBufferedEndMs(audio)).toBe(45250);
  });
});
