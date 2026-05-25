import { describe, expect, it } from "vitest";
import { withRefreshLock } from "../refreshLock";

describe("withRefreshLock", () => {
  it("deduplicates concurrent refresh calls", async () => {
    let calls = 0;
    const refresh = () =>
      withRefreshLock(async () => {
        calls += 1;
        await new Promise((r) => setTimeout(r, 20));
        return "token";
      });

    const [a, b] = await Promise.all([refresh(), refresh()]);
    expect(calls).toBe(1);
    expect(a).toBe("token");
    expect(b).toBe("token");
  });
});
