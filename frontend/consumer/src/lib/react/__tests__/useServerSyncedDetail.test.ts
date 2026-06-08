import { describe, expect, it } from "vitest";
import { resolveServerSyncedDisplay } from "@/lib/react/useServerSyncedDetail";

describe("resolveServerSyncedDisplay", () => {
  it("shows initial detail immediately when route key changes before sync effect", () => {
    const result = resolveServerSyncedDisplay({
      routeKey: "artist-b",
      resolvedKey: "artist-a",
      detail: { name: "Old Artist" },
      initialDetail: { name: "New Artist" },
    });

    expect(result.displayDetail).toEqual({ name: "New Artist" });
    expect(result.pending).toBe(false);
  });

  it("marks pending when neither synced nor initial detail exists", () => {
    const result = resolveServerSyncedDisplay({
      routeKey: "artist-b",
      resolvedKey: "artist-a",
      detail: null,
      initialDetail: undefined,
    });

    expect(result.displayDetail).toBeNull();
    expect(result.pending).toBe(true);
  });
});
