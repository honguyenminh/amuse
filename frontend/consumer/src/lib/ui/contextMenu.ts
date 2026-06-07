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

/**
 * Close an open popup/menu, then re-dispatch contextmenu to whatever is now under
 * the cursor so track/release handlers can open the app menu instead of the browser.
 */
export function forwardContextMenuAfterClose(
  clientX: number,
  clientY: number,
  onClose: () => void,
): void {
  onClose();
  requestAnimationFrame(() => {
    const target = document.elementFromPoint(clientX, clientY);
    if (!target) return;
    target.dispatchEvent(
      new MouseEvent("contextmenu", {
        bubbles: true,
        cancelable: true,
        view: window,
        clientX,
        clientY,
        button: 2,
        buttons: 2,
      }),
    );
  });
}

export function handlePopupBackdropContextMenu(
  event: ReactMouseEvent,
  onClose: () => void,
): void {
  if (allowNativeContextMenu(event)) return;
  event.preventDefault();
  event.stopPropagation();
  forwardContextMenuAfterClose(event.clientX, event.clientY, onClose);
}

export function suppressBrowserContextMenu(event: ReactMouseEvent): void {
  if (allowNativeContextMenu(event)) return;
  event.preventDefault();
}
