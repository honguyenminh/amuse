"use client";

import { useCallback, useRef, useState, type PointerEvent as ReactPointerEvent } from "react";

const DRAG_THRESHOLD_PX = 6;
export const TRACK_ITEM_ID_ATTR = "data-track-item-id";
/** Insert after the last visible row. */
export const INSERT_AFTER_LAST = "__insert-after-last__";

function findInsertBeforeId(
  clientY: number,
  activeId: string | null,
): string | null {
  const rows = Array.from(
    document.querySelectorAll<HTMLElement>(`[${TRACK_ITEM_ID_ATTR}]`),
  ).filter((row) => row.getAttribute(TRACK_ITEM_ID_ATTR) !== activeId);

  if (rows.length === 0) return null;

  for (const row of rows) {
    const id = row.getAttribute(TRACK_ITEM_ID_ATTR);
    if (!id) continue;
    const rect = row.getBoundingClientRect();
    const midY = rect.top + rect.height / 2;
    if (clientY < midY) return id;
  }

  return INSERT_AFTER_LAST;
}

/** Maps a drag from `fromIndex` to the splice index after removal. */
export function computeReorderTargetIndex(
  fromIndex: number,
  insertBeforeIndex: number,
): number | null {
  if (insertBeforeIndex < 0) return null;
  if (insertBeforeIndex === fromIndex || insertBeforeIndex === fromIndex + 1) {
    return null;
  }
  if (fromIndex < insertBeforeIndex) return insertBeforeIndex - 1;
  return insertBeforeIndex;
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
  onCommit: (activeId: string, insertBeforeId: string) => void,
) {
  const [activeId, setActiveId] = useState<string | null>(null);
  const [insertBeforeId, setInsertBeforeId] = useState<string | null>(null);
  const pending = useRef<PendingDrag | null>(null);

  const reset = useCallback(() => {
    pending.current = null;
    setActiveId(null);
    setInsertBeforeId(null);
  }, []);

  const updateInsertBefore = useCallback(
    (clientY: number, draggingId: string | null) => {
      setInsertBeforeId(findInsertBeforeId(clientY, draggingId));
    },
    [],
  );

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

        const draggingId = activeId ?? drag.id;
        if (!activeId) {
          setActiveId(drag.id);
          drag.element.setPointerCapture(event.pointerId);
        }

        updateInsertBefore(event.clientY, draggingId);
      },
      onPointerUp: (event: ReactPointerEvent<HTMLElement>) => {
        const drag = pending.current;
        if (!enabled || !drag || drag.pointerId !== event.pointerId) return;

        const source = activeId ?? drag.id;
        const target =
          insertBeforeId ?? findInsertBeforeId(event.clientY, source);

        if (activeId && target) {
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
    [enabled, activeId, insertBeforeId, onCommit, reset, updateInsertBefore],
  );

  return { activeId, insertBeforeId, getItemProps, isDragging: activeId !== null };
}
