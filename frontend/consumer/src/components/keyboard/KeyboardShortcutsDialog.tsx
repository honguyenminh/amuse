"use client";

import { KeyCombo } from "@/components/keyboard/KeyCap";
import { Text } from "@/components/ui/Text";
import {
  SHORTCUT_DEFINITIONS,
  SHORTCUT_GROUPS,
  type ShortcutKeyPart,
} from "@/lib/keyboard/shortcutDefs";
import { useKeyboardShortcuts } from "@/lib/keyboard/KeyboardShortcutsContext";
import { useEffect, useId } from "react";

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

  useEffect(() => {
    if (!helpOpen) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
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
  }, [helpOpen, closeHelp]);

  if (!helpOpen) return null;

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center p-4 sm:p-8">
      <div
        className="shortcut-help-backdrop absolute inset-0 bg-background/35 backdrop-blur-xl"
        onClick={closeHelp}
        aria-hidden
      />
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="shortcut-help-panel relative z-10 w-full max-w-2xl overflow-hidden rounded-3xl border-2 border-outline/60 bg-surface/85 shadow-2xl backdrop-blur-md"
      >
        <div className="border-b-2 border-outline/40 px-6 py-5 sm:px-8">
          <h2 id={titleId} className="text-headline-small text-on-surface">
            Keyboard shortcuts
          </h2>
          <Text variant="body-medium" className="mt-1 text-on-surface-variant">
            Use {modKeyLabel()} on this device (⌘ on Mac). Press {modKeyLabel()} + / anytime to
            open this panel. Esc to close.
          </Text>
        </div>

        <div className="max-h-[min(70vh,32rem)] overflow-y-auto px-6 py-5 sm:px-8">
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
                  {rows.map((row, index) => (
                    <li
                      key={row.id}
                      className="shortcut-help-row flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-outline/30 bg-surface-variant/35 px-4 py-3"
                      style={{ animationDelay: `${index * 45}ms` }}
                    >
                      <Text variant="body-medium">{row.label}</Text>
                      <div className="flex flex-wrap items-center gap-3">
                        {displayKeys(row.keys).map((combo, comboIndex) => (
                          <KeyCombo key={`${row.id}-${comboIndex}`} keys={combo} />
                        ))}
                      </div>
                    </li>
                  ))}
                </ul>
              </section>
            );
          })}
        </div>
      </div>
    </div>
  );
}
