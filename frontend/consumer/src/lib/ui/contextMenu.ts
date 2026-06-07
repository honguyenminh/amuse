import type { MouseEvent as ReactMouseEvent } from "react";

/** Alt+right-click keeps the browser context menu (user override). */
export function allowNativeContextMenu(event: Pick<MouseEvent, "altKey">): boolean {
  return event.altKey;
}

/** Suppress the browser menu and run the app menu opener unless Alt is held. */
export function consumeAppContextMenu(
  event: Pick<ReactMouseEvent, "altKey" | "preventDefault">,
  open: () => void,
): void {
  if (allowNativeContextMenu(event)) return;
  event.preventDefault();
  open();
}

export function dispatchContextMenuAt(target: EventTarget, clientX: number, clientY: number): MouseEvent {
  const event = new MouseEvent("contextmenu", {
    bubbles: true,
    cancelable: true,
    view: window,
    clientX,
    clientY,
    button: 2,
    buttons: 2,
  });
  target.dispatchEvent(event);
  return event;
}

/** Re-dispatch contextmenu to whatever is under the cursor (overlay must already be gone). */
export function redispatchContextMenuAt(clientX: number, clientY: number): MouseEvent | null {
  const target = document.elementFromPoint(clientX, clientY);
  if (!target) {
    return null;
  }
  return dispatchContextMenuAt(target, clientX, clientY);
}

/**
 * Close an open menu instantly, then re-dispatch contextmenu on the next frame so
 * the new target can open with a fresh enter animation.
 */
export function forwardContextMenuAfterInstantClose(
  clientX: number,
  clientY: number,
  onInstantClose: () => void,
): void {
  onInstantClose();
  requestAnimationFrame(() => {
    redispatchContextMenuAt(clientX, clientY);
  });
}

export function handlePopupBackdropContextMenu(
  event: ReactMouseEvent,
  onInstantClose: () => void,
): void {
  if (allowNativeContextMenu(event)) return;
  event.preventDefault();
  event.stopPropagation();
  forwardContextMenuAfterInstantClose(event.clientX, event.clientY, onInstantClose);
}

export function suppressBrowserContextMenu(event: ReactMouseEvent): void {
  if (allowNativeContextMenu(event)) return;
  event.preventDefault();
}
