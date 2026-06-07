"use client";

import { KeyCombo } from "@/components/keyboard/KeyCap";
import { Text } from "@/components/ui/Text";
import { cn } from "@/lib/cn";
import {
  SHORTCUT_DEFINITIONS,
  SHORTCUT_GROUPS,
  type ShortcutKeyPart,
} from "@/lib/keyboard/shortcutDefs";
import { useKeyboardShortcuts } from "@/lib/keyboard/KeyboardShortcutsContext";
import { activateFocusTrap } from "@/lib/ui/focusTrap";
import { createSmoothScrollAnimator } from "@/lib/ui/smoothScrollAnimator";
import {
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type KeyboardEvent as ReactKeyboardEvent,
} from "react";

const HELP_EXIT_MS = 320;

function arrowScrollStep(content: HTMLElement): number {
  return Math.max(112, Math.round(content.clientHeight * 0.34));
}

function modKeyLabel(): string {
  if (typeof navigator === "undefined") return "Ctrl";
  return /Mac|iPhone|iPad|iPod/.test(navigator.platform) ? "⌘" : "Ctrl";
}

function displayKeys(keys: ShortcutKeyPart[]): string[][] {
  const mod = modKeyLabel();
  return keys.map((part) => {
    const items = Array.isArray(part) ? part : [part];
    return items.map((key) => (key === "Ctrl" ? mod : key));
  });
}

export function KeyboardShortcutsDialog() {
  const { helpOpen, closeHelp } = useKeyboardShortcuts();
  const titleId = useId();
  const dialogRef = useRef<HTMLDivElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);
  const scrollerRef = useRef<ReturnType<typeof createSmoothScrollAnimator> | null>(null);
  const previouslyFocusedRef = useRef<HTMLElement | null>(null);
  const [visible, setVisible] = useState(false);
  const [exiting, setExiting] = useState(false);
  const [enterActive, setEnterActive] = useState(false);

  useEffect(() => {
    if (helpOpen) {
      setVisible(true);
      setExiting(false);
      return;
    }
    if (!visible) return;
    setExiting(true);
    const timer = window.setTimeout(() => {
      setVisible(false);
      setExiting(false);
    }, HELP_EXIT_MS);
    return () => window.clearTimeout(timer);
  }, [helpOpen, visible]);

  useLayoutEffect(() => {
    if (!visible || exiting) {
      setEnterActive(false);
      return;
    }

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (reducedMotion) {
      setEnterActive(true);
      return;
    }

    setEnterActive(false);
    let outer = 0;
    let inner = 0;
    outer = requestAnimationFrame(() => {
      inner = requestAnimationFrame(() => setEnterActive(true));
    });
    return () => {
      cancelAnimationFrame(outer);
      cancelAnimationFrame(inner);
    };
  }, [visible, exiting]);

  useEffect(() => {
    if (!visible) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !exiting) {
        event.preventDefault();
        closeHelp();
      }
    };
    document.addEventListener("keydown", onKey);
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = prev;
    };
  }, [visible, exiting, closeHelp]);

  useEffect(() => {
    if (!visible) {
      previouslyFocusedRef.current?.focus?.();
      previouslyFocusedRef.current = null;
      return;
    }

    const dialog = dialogRef.current;
    const content = contentRef.current;
    if (!dialog || !content) return;

    return activateFocusTrap(dialog, content);
  }, [visible]);

  useEffect(() => {
    if (!visible || exiting) return;

    const content = contentRef.current;
    if (!content) return;

    const active = document.activeElement;
    if (active instanceof HTMLElement) {
      previouslyFocusedRef.current = active;
    }

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const scroller = createSmoothScrollAnimator(content, {
      durationMs: reducedMotion ? 0 : 140,
      instant: reducedMotion,
    });
    scrollerRef.current = scroller;
    scroller.jumpTo(0);

    const frame = requestAnimationFrame(() => {
      content.focus({ preventScroll: true });
    });
    return () => {
      cancelAnimationFrame(frame);
      scroller.cancel();
      scrollerRef.current = null;
    };
  }, [visible, exiting]);

  const onContentKeyDown = (event: ReactKeyboardEvent<HTMLDivElement>) => {
    const content = contentRef.current;
    const scroller = scrollerRef.current;
    if (!content || !scroller) return;

    const arrowStep = arrowScrollStep(content);
    const pageStep = content.clientHeight;
    const maxScroll = Math.max(0, content.scrollHeight - content.clientHeight);

    switch (event.key) {
      case "ArrowDown":
        event.preventDefault();
        scroller.scrollBy(arrowStep);
        break;
      case "ArrowUp":
        event.preventDefault();
        scroller.scrollBy(-arrowStep);
        break;
      case "PageDown":
        event.preventDefault();
        scroller.scrollBy(pageStep);
        break;
      case "PageUp":
        event.preventDefault();
        scroller.scrollBy(-pageStep);
        break;
      case "Home":
        event.preventDefault();
        scroller.scrollTo(0);
        break;
      case "End":
        event.preventDefault();
        scroller.scrollTo(maxScroll);
        break;
      default:
        break;
    }
  };

  const requestClose = () => {
    if (!exiting) closeHelp();
  };

  if (!visible) return null;

  const showEnter = enterActive && !exiting;
  const showIdle = !enterActive && !exiting;
  let rowIndex = 0;

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center p-4 sm:p-8">
      <div
        className={cn(
          "absolute inset-0 bg-background/35 backdrop-blur-xl",
          exiting && "shortcut-help-backdrop-exit",
          showEnter && "shortcut-help-backdrop",
          showIdle && "shortcut-help-backdrop-idle",
        )}
        onClick={requestClose}
        aria-hidden
      />
      <div
        ref={dialogRef}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className={cn(
          "relative z-10 w-full max-w-2xl overflow-hidden rounded-3xl border-2 border-outline/60 bg-surface/85 shadow-2xl backdrop-blur-md",
          exiting && "shortcut-help-panel-exit",
          showEnter && "shortcut-help-panel",
          showIdle && "shortcut-help-panel-idle",
        )}
      >
        <div className="border-b-2 border-outline/40 px-6 py-5 sm:px-8">
          <h2 id={titleId} className="text-headline-small text-on-surface">
            Keyboard shortcuts
          </h2>
          <Text variant="body-medium" className="mt-1 text-on-surface-variant">
            Use {modKeyLabel()} on this device (⌘ on Mac). Press {modKeyLabel()} + / to toggle this
            panel (Esc also closes). Alt + click adds playable items to the queue; Alt + right-click
            shows the browser menu instead of the app menu.
          </Text>
        </div>

        <div
          ref={contentRef}
          tabIndex={-1}
          aria-label="Shortcut list"
          onKeyDown={onContentKeyDown}
          className="shortcut-help-scroll max-h-[min(70vh,32rem)] overflow-y-auto px-6 py-5 sm:px-8"
        >
          {SHORTCUT_GROUPS.map((group) => {
            const rows = SHORTCUT_DEFINITIONS.filter((item) => item.group === group);
            if (rows.length === 0) return null;
            return (
              <section key={group} className="mb-6 last:mb-0">
                <Text
                  variant="label-large"
                  className="mb-3 text-on-surface-variant uppercase tracking-wider"
                >
                  {group}
                </Text>
                <ul className="flex flex-col gap-2">
                  {rows.map((row) => {
                    const staggerIndex = rowIndex++;
                    return (
                    <li
                      key={row.id}
                      className={cn(
                        "flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-outline/30 bg-surface-variant/35 px-4 py-3",
                        exiting && "shortcut-help-row-exit",
                        showEnter && "shortcut-help-row",
                        showIdle && "shortcut-help-row-idle",
                      )}
                      style={
                        showEnter ? { animationDelay: `${staggerIndex * 45}ms` } : undefined
                      }
                    >
                      <Text variant="body-medium">{row.label}</Text>
                      <div className="flex flex-wrap items-center gap-3">
                        {displayKeys(row.keys).map((combo, comboIndex) => (
                          <KeyCombo key={`${row.id}-${comboIndex}`} keys={combo} />
                        ))}
                      </div>
                    </li>
                    );
                  })}
                </ul>
              </section>
            );
          })}
        </div>
      </div>
    </div>
  );
}
