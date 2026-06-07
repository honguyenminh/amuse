import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  broadcastQueueUpdated,
  getPlaybackTabId,
  holdsPlaybackLease,
  resetPlaybackTabIdForTests,
  shouldIgnoreSyncMessage,
  subscribeQueueSync,
} from "../queueTabSync";

class MockBroadcastChannel {
  static instances: MockBroadcastChannel[] = [];
  onmessage: ((event: MessageEvent) => void) | null = null;

  constructor(_name: string) {
    MockBroadcastChannel.instances.push(this);
  }

  postMessage(data: unknown) {
    for (const channel of MockBroadcastChannel.instances) {
      channel.onmessage?.({ data } as MessageEvent);
    }
  }

  addEventListener(type: string, listener: (event: MessageEvent) => void) {
    if (type === "message") this.onmessage = listener;
  }

  removeEventListener(type: string) {
    if (type === "message") this.onmessage = null;
  }

  close() {}
}

function installBrowserMocks() {
  const session = new Map<string, string>();
  const sessionStorage = {
    getItem: (key: string) => session.get(key) ?? null,
    setItem: (key: string, value: string) => {
      session.set(key, value);
    },
    removeItem: (key: string) => {
      session.delete(key);
    },
    clear: () => session.clear(),
  };
  vi.stubGlobal("sessionStorage", sessionStorage);
  vi.stubGlobal("BroadcastChannel", MockBroadcastChannel);
  vi.stubGlobal("window", {
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    sessionStorage,
  });
  vi.stubGlobal("crypto", { randomUUID: () => "test-tab-id" });
}

describe("queueTabSync", () => {
  beforeEach(() => {
    vi.unstubAllGlobals();
    MockBroadcastChannel.instances = [];
    resetPlaybackTabIdForTests();
    installBrowserMocks();
  });

  it("returns a stable tab id per session", () => {
    const a = getPlaybackTabId();
    const b = getPlaybackTabId();
    expect(a).toBe(b);
    expect(a).toBe("test-tab-id");
  });

  it("ignores stale and echo messages", () => {
    const tabId = getPlaybackTabId();
    expect(
      shouldIgnoreSyncMessage({ type: "queue-updated", tabId, updatedAt: 5 }, tabId, 10),
    ).toBe(true);
    expect(
      shouldIgnoreSyncMessage({ type: "queue-updated", tabId: "other", updatedAt: 4 }, tabId, 10),
    ).toBe(true);
    expect(
      shouldIgnoreSyncMessage({ type: "queue-updated", tabId: "other", updatedAt: 11 }, tabId, 10),
    ).toBe(false);
  });

  it("detects playback lease holder", () => {
    const tabId = getPlaybackTabId();
    expect(holdsPlaybackLease(tabId, tabId)).toBe(true);
    expect(holdsPlaybackLease("other", tabId)).toBe(false);
    expect(holdsPlaybackLease(null, tabId)).toBe(false);
  });

  it("delivers broadcast messages to subscribers", () => {
    const listener = vi.fn();
    const unsubscribe = subscribeQueueSync(listener);
    broadcastQueueUpdated("remote-tab", 123);
    expect(listener).toHaveBeenCalledWith({
      type: "queue-updated",
      tabId: "remote-tab",
      updatedAt: 123,
    });
    unsubscribe();
  });
});
