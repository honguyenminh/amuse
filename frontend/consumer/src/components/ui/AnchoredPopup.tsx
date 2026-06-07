"use client";

import { cn } from "@/lib/cn";
import { handlePopupBackdropContextMenu, suppressBrowserContextMenu } from "@/lib/ui/contextMenu";
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

const POPUP_EXIT_MS = 90;

type PopupPosition = ReturnType<typeof computeAnchoredPosition>;

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
  const skipExitRef = useRef(false);
  const cachedChildrenRef = useRef<ReactNode>(null);
  const cachedPositionRef = useRef<PopupPosition | null>(null);
  const [visible, setVisible] = useState(false);
  const [exiting, setExiting] = useState(false);
  const [position, setPosition] = useState<PopupPosition | null>(null);

  if (open) {
    cachedChildrenRef.current = children;
  }

  useLayoutEffect(() => {
    if (open) {
      setVisible(true);
      setExiting(false);
      return;
    }

    if (!visible || exiting) {
      return;
    }

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (skipExitRef.current || reducedMotion) {
      skipExitRef.current = false;
      setVisible(false);
      setExiting(false);
      setPosition(null);
      cachedPositionRef.current = null;
      return;
    }

    if (position) {
      cachedPositionRef.current = position;
    }
    setExiting(true);
  }, [open, visible, exiting, position]);

  useEffect(() => {
    if (!exiting) {
      return;
    }

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    const timer = window.setTimeout(
      () => {
        setVisible(false);
        setExiting(false);
        setPosition(null);
        cachedPositionRef.current = null;
      },
      reducedMotion ? 0 : POPUP_EXIT_MS,
    );

    return () => window.clearTimeout(timer);
  }, [exiting]);

  useLayoutEffect(() => {
    if (!visible || exiting || !open) {
      if (!visible) {
        setPosition(null);
      }
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
      const nextPosition = computeAnchoredPosition({
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
      });
      setPosition(nextPosition);
      cachedPositionRef.current = nextPosition;
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
    visible,
    exiting,
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
    if (!open || exiting) {
      return;
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };
    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [open, exiting, onClose]);

  if (!visible || typeof document === "undefined") {
    return null;
  }

  const displayPosition = exiting ? cachedPositionRef.current : position;
  const displayChildren = open ? children : cachedChildrenRef.current;
  const isPositioned = displayPosition !== null;

  const requestClose = () => {
    if (exiting) {
      return;
    }
    onClose();
  };

  const requestInstantClose = () => {
    if (exiting) {
      return;
    }
    skipExitRef.current = true;
    onClose();
  };

  return createPortal(
    <>
      <div
        className="fixed inset-0 z-40 cursor-default bg-transparent"
        aria-hidden
        onClick={requestClose}
        onContextMenu={(event) => handlePopupBackdropContextMenu(event, requestInstantClose)}
      />
      <div
        ref={popupRef}
        role={role}
        className={cn(
          "fixed z-50 overflow-y-auto",
          exiting
            ? "anchored-popup-exit"
            : isPositioned
              ? "anchored-popup-enter"
              : null,
          className,
        )}
        onContextMenu={suppressBrowserContextMenu}
        style={{
          top: displayPosition?.top ?? 0,
          left: displayPosition?.left ?? 0,
          maxHeight: displayPosition?.maxHeight,
          visibility: isPositioned ? "visible" : "hidden",
          pointerEvents: exiting ? "none" : undefined,
        }}
      >
        {displayChildren}
      </div>
    </>,
    document.body,
  );
}
