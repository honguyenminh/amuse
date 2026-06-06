import { describe, expect, it } from "vitest";
import {
  bufferAheadSeconds,
  getContinuousBufferEnd,
  hasBufferedRangeAfter,
} from "../dashBufferSync";

function mockBuffered(ranges: Array<[number, number]>): TimeRanges {
  return {
    length: ranges.length,
    start: (index: number) => ranges[index]![0],
    end: (index: number) => ranges[index]![1],
  } as TimeRanges;
}

describe("dashBufferSync", () => {
  it("reads the contiguous buffer end at the playhead", () => {
    const audio = { buffered: mockBuffered([[60, 62], [180, 210]]) } as HTMLAudioElement;
    expect(getContinuousBufferEnd(audio, 61)).toBe(62);
    expect(bufferAheadSeconds(audio, 61)).toBe(1);
  });

  it("detects disjoint buffered ranges ahead of the playhead", () => {
    const audio = { buffered: mockBuffered([[60, 62], [180, 210]]) } as HTMLAudioElement;
    expect(hasBufferedRangeAfter(audio, 62)).toBe(true);
    expect(hasBufferedRangeAfter(audio, 210)).toBe(false);
  });
});
