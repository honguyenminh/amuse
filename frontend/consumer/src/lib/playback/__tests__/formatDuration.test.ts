import { describe, expect, it } from "vitest";
import { formatDuration } from "../formatDuration";

describe("formatDuration", () => {
  it("returns 0:00 for non-positive or non-finite values", () => {
    expect(formatDuration(0)).toBe("0:00");
    expect(formatDuration(-1000)).toBe("0:00");
    expect(formatDuration(NaN)).toBe("0:00");
    expect(formatDuration(Number.POSITIVE_INFINITY)).toBe("0:00");
  });

  it("formats sub-minute durations", () => {
    expect(formatDuration(1_000)).toBe("0:01");
    expect(formatDuration(45_000)).toBe("0:45");
  });

  it("formats minute durations with zero-padded seconds", () => {
    expect(formatDuration(60_000)).toBe("1:00");
    expect(formatDuration(125_000)).toBe("2:05");
    expect(formatDuration(599_999)).toBe("9:59");
  });

  it("switches to h:mm:ss for durations >= 1 hour", () => {
    expect(formatDuration(3_600_000)).toBe("1:00:00");
    expect(formatDuration(3_725_000)).toBe("1:02:05");
  });
});
