import { describe, expect, it } from "vitest";
import { computeAnchoredPosition } from "../computeAnchoredPosition";

const viewport = { width: 400, height: 300 };

describe("computeAnchoredPosition", () => {
  it("opens below the anchor when there is enough space", () => {
    const result = computeAnchoredPosition({
      anchor: { top: 40, left: 20, width: 120, height: 32 },
      popup: { width: 160, height: 120 },
      viewport,
    });

    expect(result.placement).toBe("bottom");
    expect(result.top).toBe(80);
    expect(result.left).toBe(20);
    expect(result.maxHeight).toBeUndefined();
  });

  it("flips above the anchor when there is not enough space below", () => {
    const result = computeAnchoredPosition({
      anchor: { top: 220, left: 20, width: 120, height: 32 },
      popup: { width: 160, height: 120 },
      viewport,
    });

    expect(result.placement).toBe("top");
    expect(result.top).toBe(92);
    expect(result.left).toBe(20);
  });

  it("aligns to the end and shifts into the viewport", () => {
    const result = computeAnchoredPosition({
      anchor: { top: 40, left: 320, width: 60, height: 32 },
      popup: { width: 160, height: 80 },
      viewport,
      align: "end",
    });

    expect(result.left).toBe(220);
  });

  it("constrains height when the popup is taller than the available space", () => {
    const result = computeAnchoredPosition({
      anchor: { top: 180, left: 20, width: 120, height: 32 },
      popup: { width: 160, height: 200 },
      viewport,
    });

    expect(result.placement).toBe("bottom");
    expect(result.maxHeight).toBe(72);
  });
});
