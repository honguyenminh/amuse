import { describe, expect, it } from "vitest";
import { readAudioPositionMs } from "../audioPosition";

describe("readAudioPositionMs", () => {
  it("rounds to nearest millisecond instead of flooring", () => {
    const audio = { currentTime: 12.5678 } as HTMLAudioElement;
    expect(readAudioPositionMs(audio)).toBe(12568);
  });
});
