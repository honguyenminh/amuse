import { describe, expect, it } from "vitest";
import { nextRepeatMode, repeatButtonVariant, repeatModeLabel } from "../repeatMode";

describe("repeatMode", () => {
  it("cycles off → queue → one → off", () => {
    expect(nextRepeatMode("off")).toBe("queue");
    expect(nextRepeatMode("queue")).toBe("one");
    expect(nextRepeatMode("one")).toBe("off");
  });

  it("labels each mode for accessibility", () => {
    expect(repeatModeLabel("off")).toBe("Repeat off");
    expect(repeatModeLabel("queue")).toBe("Repeat queue");
    expect(repeatModeLabel("one")).toBe("Repeat current song");
  });

  it("maps each mode to a button variant", () => {
    expect(repeatButtonVariant("off")).toBe("ghost");
    expect(repeatButtonVariant("queue")).toBe("tonal");
    expect(repeatButtonVariant("one")).toBe("tertiary-tonal");
  });
});
