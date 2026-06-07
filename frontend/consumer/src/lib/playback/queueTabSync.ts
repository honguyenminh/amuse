import { QUEUE_STORAGE_KEY } from "./queuePersistence";

const TAB_ID_KEY = "amuse.consumer.playbackTabId";
const CHANNEL_NAME = "amuse.consumer.playbackQueue.v1";

export type QueueSyncMessage =
  | { type: "queue-updated"; tabId: string; updatedAt: number }
  | { type: "playback-claim"; tabId: string; updatedAt: number };

export type QueueSyncListener = (message: QueueSyncMessage) => void;

let cachedTabId: string | null = null;

export function getPlaybackTabId(): string {
  if (cachedTabId) return cachedTabId;
  if (typeof window === "undefined") return "ssr";

  try {
    const existing = window.sessionStorage.getItem(TAB_ID_KEY);
    if (existing) {
      cachedTabId = existing;
      return existing;
    }
    const id = crypto.randomUUID();
    window.sessionStorage.setItem(TAB_ID_KEY, id);
    cachedTabId = id;
    return id;
  } catch {
    cachedTabId = crypto.randomUUID();
    return cachedTabId;
  }
}

/** Resets module-level tab id cache for unit tests. */
export function resetPlaybackTabIdForTests(): void {
  cachedTabId = null;
}

function createChannel(): BroadcastChannel | null {
  if (typeof window === "undefined" || typeof BroadcastChannel === "undefined") {
    return null;
  }
  try {
    return new BroadcastChannel(CHANNEL_NAME);
  } catch {
    return null;
  }
}

export function broadcastQueueSync(message: QueueSyncMessage): void {
  const channel = createChannel();
  if (!channel) return;
  try {
    channel.postMessage(message);
  } finally {
    channel.close();
  }
}

export function broadcastQueueUpdated(tabId: string, updatedAt: number): void {
  broadcastQueueSync({ type: "queue-updated", tabId, updatedAt });
}

export function broadcastPlaybackClaim(tabId: string, updatedAt: number): void {
  broadcastQueueSync({ type: "playback-claim", tabId, updatedAt });
}

export function subscribeQueueSync(listener: QueueSyncListener): () => void {
  if (typeof window === "undefined") return () => undefined;

  const channel = createChannel();

  const onChannelMessage = (event: MessageEvent<QueueSyncMessage>) => {
    if (!event.data || typeof event.data !== "object") return;
    listener(event.data);
  };

  const onStorage = (event: StorageEvent) => {
    if (event.key !== QUEUE_STORAGE_KEY || !event.newValue) return;
    try {
      const parsed = JSON.parse(event.newValue) as { updatedAt?: number };
      if (typeof parsed.updatedAt !== "number") return;
      listener({
        type: "queue-updated",
        tabId: "",
        updatedAt: parsed.updatedAt,
      });
    } catch {
      // ignore malformed storage payloads
    }
  };

  channel?.addEventListener("message", onChannelMessage);
  window.addEventListener("storage", onStorage);

  return () => {
    channel?.removeEventListener("message", onChannelMessage);
    channel?.close();
    window.removeEventListener("storage", onStorage);
  };
}

export function shouldIgnoreSyncMessage(
  message: QueueSyncMessage,
  localTabId: string,
  lastAppliedUpdatedAt: number,
): boolean {
  if (message.updatedAt <= lastAppliedUpdatedAt) return true;
  if (message.tabId && message.tabId === localTabId) return true;
  return false;
}

export function holdsPlaybackLease(activeTabId: string | null, localTabId: string): boolean {
  return activeTabId !== null && activeTabId === localTabId;
}
