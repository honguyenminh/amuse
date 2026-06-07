"use client";

import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { usePathname, useRouter } from "next/navigation";

type KeyboardShortcutsContextValue = {
  helpOpen: boolean;
  openHelp: () => void;
  closeHelp: () => void;
  toggleHelp: () => void;
  registerSearchInput: (element: HTMLInputElement | null) => void;
  focusSearch: () => void;
};

const KeyboardShortcutsContext = createContext<KeyboardShortcutsContextValue | null>(null);

export function KeyboardShortcutsProvider({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [helpOpen, setHelpOpen] = useState(false);
  const searchInputRef = useRef<HTMLInputElement | null>(null);

  const openHelp = useCallback(() => setHelpOpen(true), []);
  const closeHelp = useCallback(() => setHelpOpen(false), []);
  const toggleHelp = useCallback(() => setHelpOpen((open) => !open), []);

  const registerSearchInput = useCallback((element: HTMLInputElement | null) => {
    searchInputRef.current = element;
  }, []);

  const focusSearch = useCallback(() => {
    if (pathname === "/search" && searchInputRef.current) {
      searchInputRef.current.focus();
      searchInputRef.current.select();
      return;
    }
    router.push("/search?focus=1");
  }, [pathname, router]);

  const value = useMemo(
    () => ({
      helpOpen,
      openHelp,
      closeHelp,
      toggleHelp,
      registerSearchInput,
      focusSearch,
    }),
    [helpOpen, openHelp, closeHelp, toggleHelp, registerSearchInput, focusSearch],
  );

  return (
    <KeyboardShortcutsContext.Provider value={value}>
      {children}
    </KeyboardShortcutsContext.Provider>
  );
}

export function useKeyboardShortcuts(): KeyboardShortcutsContextValue {
  const ctx = useContext(KeyboardShortcutsContext);
  if (!ctx) {
    throw new Error("useKeyboardShortcuts must be used within KeyboardShortcutsProvider");
  }
  return ctx;
}
