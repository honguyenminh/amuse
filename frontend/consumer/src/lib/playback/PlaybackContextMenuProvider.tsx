"use client";

import { AnchoredPopup } from "@/components/ui/AnchoredPopup";
import { Text } from "@/components/ui/Text";
import { cn } from "@/lib/cn";
import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";

export type PlaybackContextMenuItem = {
  id: string;
  label: string;
  disabled?: boolean;
  onSelect: () => void;
};

type MenuState = {
  x: number;
  y: number;
  items: PlaybackContextMenuItem[];
};

type PlaybackContextMenuContextValue = {
  openAt: (x: number, y: number, items: PlaybackContextMenuItem[]) => void;
  close: () => void;
};

const PlaybackContextMenuContext = createContext<PlaybackContextMenuContextValue | null>(
  null,
);

export function PlaybackContextMenuProvider({ children }: { children: ReactNode }) {
  const [menu, setMenu] = useState<MenuState | null>(null);

  const close = useCallback(() => setMenu(null), []);

  const openAt = useCallback((x: number, y: number, items: PlaybackContextMenuItem[]) => {
    if (items.length === 0) return;
    setMenu({ x, y, items });
  }, []);

  const value = useMemo(() => ({ openAt, close }), [openAt, close]);

  return (
    <PlaybackContextMenuContext.Provider value={value}>
      {children}
      <AnchoredPopup
        open={menu !== null}
        onClose={close}
        anchorRect={
          menu
            ? { left: menu.x, top: menu.y, width: 0, height: 0 }
            : null
        }
        preferredPlacement="bottom"
        align="start"
        closeOnScroll
        className="min-w-[11rem] rounded-md border-2 border-outline bg-surface py-1 shadow-lg"
        role="menu"
      >
        {menu?.items.map((item) => (
          <button
            key={item.id}
            type="button"
            role="menuitem"
            disabled={item.disabled}
            className={cn(
              "flex w-full px-4 py-2 text-left text-body-medium transition-colors",
              item.disabled
                ? "cursor-not-allowed opacity-50"
                : "hover:bg-surface-variant",
            )}
            onClick={() => {
              if (item.disabled) return;
              item.onSelect();
              close();
            }}
          >
            <Text variant="body-medium">{item.label}</Text>
          </button>
        )) ?? null}
      </AnchoredPopup>
    </PlaybackContextMenuContext.Provider>
  );
}

export function usePlaybackContextMenu(): PlaybackContextMenuContextValue {
  const ctx = useContext(PlaybackContextMenuContext);
  if (!ctx) {
    throw new Error("usePlaybackContextMenu must be used within PlaybackContextMenuProvider");
  }
  return ctx;
}
