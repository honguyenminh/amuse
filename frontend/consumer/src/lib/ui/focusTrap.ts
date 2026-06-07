/**
 * Keeps keyboard focus inside `container`, defaulting to `focusTarget` when Tab
 * would leave or focus moves outside (e.g. underlying page).
 */
export function activateFocusTrap(
  container: HTMLElement,
  focusTarget: HTMLElement,
): () => void {
  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key !== "Tab") return;
    event.preventDefault();
    focusTarget.focus({ preventScroll: true });
  };

  const onFocusIn = (event: FocusEvent) => {
    const target = event.target;
    if (target instanceof Node && container.contains(target)) return;
    focusTarget.focus({ preventScroll: true });
  };

  document.addEventListener("keydown", onKeyDown, true);
  document.addEventListener("focusin", onFocusIn, true);

  return () => {
    document.removeEventListener("keydown", onKeyDown, true);
    document.removeEventListener("focusin", onFocusIn, true);
  };
}
