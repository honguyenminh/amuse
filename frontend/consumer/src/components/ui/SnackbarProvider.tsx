"use client";

import { Text } from "@/components/ui/Text";
import { cn } from "@/lib/cn";
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

type SnackbarMessage = {
  id: string;
  text: string;
};

type SnackbarContextValue = {
  showSnackbar: (text: string) => void;
};

const SnackbarContext = createContext<SnackbarContextValue | null>(null);

const SNACKBAR_DURATION_MS = 3200;

export function SnackbarProvider({ children }: { children: ReactNode }) {
  const [messages, setMessages] = useState<SnackbarMessage[]>([]);
  const [portalReady, setPortalReady] = useState(false);

  useEffect(() => {
    setPortalReady(true);
  }, []);

  const showSnackbar = useCallback((text: string) => {
    const id = crypto.randomUUID();
    setMessages((current) => [...current, { id, text }]);
    window.setTimeout(() => {
      setMessages((current) => current.filter((message) => message.id !== id));
    }, SNACKBAR_DURATION_MS);
  }, []);

  const value = useMemo(() => ({ showSnackbar }), [showSnackbar]);

  return (
    <SnackbarContext.Provider value={value}>
      {children}
      {portalReady
        ? createPortal(
            <div
              aria-live="polite"
              className="pointer-events-none fixed bottom-24 left-1/2 z-50 flex w-full max-w-md -translate-x-1/2 flex-col items-center gap-2 px-4"
            >
              {messages.map((message) => (
                <div
                  key={message.id}
                  className={cn(
                    "snackbar-enter w-full rounded-lg border-2 border-outline bg-surface px-4 py-3 text-on-surface shadow-lg backdrop-blur-sm supports-[backdrop-filter]:bg-surface/95",
                  )}
                  role="status"
                >
                  <Text variant="body-medium">{message.text}</Text>
                </div>
              ))}
            </div>,
            document.body,
          )
        : null}
    </SnackbarContext.Provider>
  );
}

export function useSnackbar(): SnackbarContextValue {
  const context = useContext(SnackbarContext);
  if (!context) {
    throw new Error("useSnackbar must be used within SnackbarProvider");
  }
  return context;
}
