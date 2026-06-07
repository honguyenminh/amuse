import { describe, expect, it } from "vitest";
import { hasModKey } from "../isEditableTarget";

describe("hasModKey", () => {
  it("accepts ctrl or meta as the platform modifier", () => {
    expect(
      hasModKey({ ctrlKey: true, metaKey: false } as KeyboardEvent),
    ).toBe(true);
    expect(
      hasModKey({ ctrlKey: false, metaKey: true } as KeyboardEvent),
    ).toBe(true);
    expect(
      hasModKey({ ctrlKey: false, metaKey: false } as KeyboardEvent),
    ).toBe(false);
  });
});
