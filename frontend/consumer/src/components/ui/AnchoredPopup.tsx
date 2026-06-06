"use client";

import { cn } from "@/lib/cn";
import {
  computeAnchoredPosition,
  domRectToAnchorRect,
  type AnchorRect,
  type AnchoredAlign,
  type AnchoredPlacement,
} from "@/lib/ui/computeAnchoredPosition";
import {
  useEffect,
  useLayoutEffect,
  useRef,
  useState,
  type ReactNode,
  type RefObject,
} from "react";
import { createPortal } from "react-dom";

type AnchoredPopupProps = {
  open: boolean;
  onClose: () => void;
  anchorRef?: RefObject<HTMLElement | null>;
  anchorRect?: AnchorRect | null;
  children: ReactNode;
  className?: string;
  preferredPlacement?: AnchoredPlacement;
  align?: AnchoredAlign;
  offset?: number;
  viewportPadding?: number;
  closeOnScroll?: boolean;
  /** Bumps reposition when menu content changes (e.g. drill-down navigation). */
  layoutKey?: string | number;
  role?: string;
};

export function AnchoredPopup({
  open,
  onClose,
  anchorRef,
  anchorRect,
  children,
  className,
  preferredPlacement = "bottom",
  align = "start",
  offset = 8,
  viewportPadding = 8,
  closeOnScroll = false,
  layoutKey,
  role,
}: AnchoredPopupProps) {
  const popupRef = useRef<HTMLDivElement>(null);
  const [position, setPosition] = useState<ReturnType<typeof computeAnchoredPosition> | null>(
    null,
  );

  useLayoutEffect(() => {
    if (!open) {
      setPosition(null);
      return;
    }

    const updatePosition = () => {
      const popup = popupRef.current;
      if (!popup) {
        return;
      }

      const anchor =
        anchorRect ??
        (anchorRef?.current ? domRectToAnchorRect(anchorRef.current.getBoundingClientRect()) : null);
      if (!anchor) {
        return;
      }

      const popupRect = popup.getBoundingClientRect();
      setPosition(
        computeAnchoredPosition({
          anchor,
          popup: { width: popupRect.width, height: popupRect.height },
          viewport: {
            width: window.innerWidth,
            height: window.innerHeight,
          },
          preferredPlacement,
          align,
          offset,
          padding: viewportPadding,
        }),
      );
    };

    updatePosition();
    window.addEventListener("resize", updatePosition);
    if (closeOnScroll) {
      window.addEventListener("scroll", onClose, true);
    } else {
      window.addEventListener("scroll", updatePosition, true);
    }

    return () => {
      window.removeEventListener("resize", updatePosition);
      if (closeOnScroll) {
        window.removeEventListener("scroll", onClose, true);
      } else {
        window.removeEventListener("scroll", updatePosition, true);
      }
    };
  }, [
    open,
    anchorRef,
    anchorRect,
    preferredPlacement,
    align,
    offset,
    viewportPadding,
    closeOnScroll,
    layoutKey,
    onClose,
  ]);

  useEffect(() => {
    if (!open) {
      return;
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [open, onClose]);

  if (!open || typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <>
      <button
        type="button"
        className="fixed inset-0 z-40 cursor-default bg-transparent"
        aria-label="Close menu"
        onClick={onClose}
      />
      <div
        ref={popupRef}
        role={role}
        className={cn("fixed z-50 overflow-y-auto", className)}
        style={{
          top: position?.top ?? 0,
          left: position?.left ?? 0,
          maxHeight: position?.maxHeight,
          visibility: position ? "visible" : "hidden",
        }}
      >
        {children}
      </div>
    </>,
    document.body,
  );
}
