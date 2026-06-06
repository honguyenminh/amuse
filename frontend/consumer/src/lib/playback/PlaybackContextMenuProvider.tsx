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
  children?: PlaybackContextMenuItem[];
  onSelect: () => void;
};

type MenuLevel = {
  title?: string;
  items: PlaybackContextMenuItem[];
};

type MenuState = {
  x: number;
  y: number;
  stack: MenuLevel[];
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
    setMenu({ x, y, stack: [{ items }] });
  }, []);

  const pushLevel = useCallback((title: string, items: PlaybackContextMenuItem[]) => {
    setMenu((current) => {
      if (!current) return current;
      return { ...current, stack: [...current.stack, { title, items }] };
    });
  }, []);

  const popLevel = useCallback(() => {
    setMenu((current) => {
      if (!current || current.stack.length <= 1) return current;
      return { ...current, stack: current.stack.slice(0, -1) };
    });
  }, []);

  const value = useMemo(() => ({ openAt, close }), [openAt, close]);

  const currentLevel = menu?.stack[menu.stack.length - 1];
  const canGoBack = (menu?.stack.length ?? 0) > 1;

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
        layoutKey={menu?.stack.length ?? 0}
        className="min-w-[11rem] rounded-md border-2 border-outline bg-surface py-1 shadow-lg"
        role="menu"
      >
        {canGoBack ? (
          <button
            type="button"
            role="menuitem"
            className="flex w-full px-4 py-2 text-left text-body-medium transition-colors hover:bg-surface-variant"
            onClick={popLevel}
          >
            <Text variant="body-medium">← {currentLevel?.title ?? "Back"}</Text>
          </button>
        ) : null}
        {currentLevel?.items.map((item) => (
          <ContextMenuRow
            key={item.id}
            item={item}
            onClose={close}
            onOpenSubmenu={pushLevel}
          />
        )) ?? null}
      </AnchoredPopup>
    </PlaybackContextMenuContext.Provider>
  );
}

function ContextMenuRow({
  item,
  onClose,
  onOpenSubmenu,
}: {
  item: PlaybackContextMenuItem;
  onClose: () => void;
  onOpenSubmenu: (title: string, items: PlaybackContextMenuItem[]) => void;
}) {
  const hasChildren = (item.children?.length ?? 0) > 0;

  return (
    <button
      type="button"
      role="menuitem"
      aria-haspopup={hasChildren ? "menu" : undefined}
      disabled={item.disabled}
      className={cn(
        "flex w-full items-center justify-between gap-2 px-4 py-2 text-left text-body-medium transition-colors",
        item.disabled
          ? "cursor-not-allowed opacity-50"
          : "hover:bg-surface-variant",
      )}
      onClick={() => {
        if (item.disabled) return;
        if (hasChildren && item.children) {
          onOpenSubmenu(item.label, item.children);
          return;
        }
        item.onSelect();
        onClose();
      }}
    >
      <Text variant="body-medium">{item.label}</Text>
      {hasChildren ? (
        <span aria-hidden className="text-on-surface-variant">
          ›
        </span>
      ) : null}
    </button>
  );
}

export function usePlaybackContextMenu(): PlaybackContextMenuContextValue {
  const ctx = useContext(PlaybackContextMenuContext);
  if (!ctx) {
    throw new Error("usePlaybackContextMenu must be used within PlaybackContextMenuProvider");
  }
  return ctx;
}
