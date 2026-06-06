"use client";

import { useCallback, useRef, useState, type PointerEvent as ReactPointerEvent } from "react";

const DRAG_THRESHOLD_PX = 6;
export const TRACK_ITEM_ID_ATTR = "data-track-item-id";

function findItemIdAt(x: number, y: number): string | null {
  for (const el of document.elementsFromPoint(x, y)) {
    const id = el.closest(`[${TRACK_ITEM_ID_ATTR}]`)?.getAttribute(TRACK_ITEM_ID_ATTR);
    if (id) return id;
  }
  return null;
}

type PendingDrag = {
  id: string;
  x: number;
  y: number;
  pointerId: number;
  element: HTMLElement;
};

export function useTrackDragReorder(
  enabled: boolean,
  onCommit: (activeId: string, overId: string) => void,
) {
  const [activeId, setActiveId] = useState<string | null>(null);
  const [overId, setOverId] = useState<string | null>(null);
  const pending = useRef<PendingDrag | null>(null);

  const reset = useCallback(() => {
    pending.current = null;
    setActiveId(null);
    setOverId(null);
  }, []);

  const getItemProps = useCallback(
    (itemId: string) => ({
      [TRACK_ITEM_ID_ATTR]: itemId,
      onPointerDown: (event: ReactPointerEvent<HTMLElement>) => {
        if (!enabled || event.button !== 0) return;
        if ((event.target as Element).closest("[data-no-drag]")) return;

        pending.current = {
          id: itemId,
          x: event.clientX,
          y: event.clientY,
          pointerId: event.pointerId,
          element: event.currentTarget,
        };
      },
      onPointerMove: (event: ReactPointerEvent<HTMLElement>) => {
        const drag = pending.current;
        if (!enabled || !drag || drag.pointerId !== event.pointerId) return;

        const distance = Math.hypot(
          event.clientX - drag.x,
          event.clientY - drag.y,
        );

        if (!activeId && distance < DRAG_THRESHOLD_PX) return;

        if (!activeId) {
          setActiveId(drag.id);
          drag.element.setPointerCapture(event.pointerId);
        }

        setOverId(findItemIdAt(event.clientX, event.clientY));
      },
      onPointerUp: (event: ReactPointerEvent<HTMLElement>) => {
        const drag = pending.current;
        if (!enabled || !drag || drag.pointerId !== event.pointerId) return;

        const source = activeId ?? drag.id;
        const target = overId ?? findItemIdAt(event.clientX, event.clientY);

        if (activeId && target && source !== target) {
          onCommit(source, target);
        }

        if (drag.element.hasPointerCapture(event.pointerId)) {
          drag.element.releasePointerCapture(event.pointerId);
        }
        reset();
      },
      onPointerCancel: () => {
        reset();
      },
    }),
    [enabled, activeId, overId, onCommit, reset],
  );

  return { activeId, overId, getItemProps, isDragging: activeId !== null };
}
