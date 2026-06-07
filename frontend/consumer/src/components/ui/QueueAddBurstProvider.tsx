"use client";

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";

const BURST_DURATION_MS = 550;

type Burst = {
  id: string;
  x: number;
  y: number;
};

type QueueAddBurstContextValue = {
  showBurstAt: (x: number, y: number) => void;
};

const QueueAddBurstContext = createContext<QueueAddBurstContextValue | null>(null);

function QueueAddBurstMarker({ x, y }: { x: number; y: number }) {
  return (
    <span
      aria-hidden
      className="pointer-events-none fixed z-[60]"
      style={{ left: x, top: y, transform: "translate(-50%, -50%)" }}
    >
      <span className="queue-add-burst flex size-10 items-center justify-center rounded-full bg-primary/20 text-primary">
        <svg
          width="20"
          height="20"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.5"
          strokeLinecap="round"
          aria-hidden
        >
          <line x1="12" y1="5" x2="12" y2="19" />
          <line x1="5" y1="12" x2="19" y2="12" />
        </svg>
      </span>
    </span>
  );
}

export function QueueAddBurstProvider({ children }: { children: ReactNode }) {
  const [bursts, setBursts] = useState<Burst[]>([]);
  const [portalReady, setPortalReady] = useState(false);

  useEffect(() => {
    setPortalReady(true);
  }, []);

  const showBurstAt = useCallback((x: number, y: number) => {
    const id = crypto.randomUUID();
    setBursts((current) => [...current, { id, x, y }]);
    window.setTimeout(() => {
      setBursts((current) => current.filter((burst) => burst.id !== id));
    }, BURST_DURATION_MS);
  }, []);

  const value = useMemo(() => ({ showBurstAt }), [showBurstAt]);

  return (
    <QueueAddBurstContext.Provider value={value}>
      {children}
      {portalReady
        ? createPortal(
            <>
              {bursts.map((burst) => (
                <QueueAddBurstMarker key={burst.id} x={burst.x} y={burst.y} />
              ))}
            </>,
            document.body,
          )
        : null}
    </QueueAddBurstContext.Provider>
  );
}

export function useQueueAddBurst(): QueueAddBurstContextValue {
  const context = useContext(QueueAddBurstContext);
  if (!context) {
    throw new Error("useQueueAddBurst must be used within QueueAddBurstProvider");
  }
  return context;
}
