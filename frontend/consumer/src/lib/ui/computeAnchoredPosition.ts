export type AnchorRect = {
  top: number;
  left: number;
  width: number;
  height: number;
};

export type PopupSize = {
  width: number;
  height: number;
};

export type ViewportSize = {
  width: number;
  height: number;
};

export type AnchoredPlacement = "top" | "bottom";

export type AnchoredAlign = "start" | "end";

export type ComputeAnchoredPositionOptions = {
  anchor: AnchorRect;
  popup: PopupSize;
  viewport: ViewportSize;
  preferredPlacement?: AnchoredPlacement;
  align?: AnchoredAlign;
  offset?: number;
  padding?: number;
};

export type AnchoredPosition = {
  top: number;
  left: number;
  maxHeight?: number;
  placement: AnchoredPlacement;
};

export function computeAnchoredPosition(
  options: ComputeAnchoredPositionOptions,
): AnchoredPosition {
  const {
    anchor,
    popup,
    viewport,
    preferredPlacement = "bottom",
    align = "start",
    offset = 8,
    padding = 8,
  } = options;

  const spaceBelow = Math.max(
    0,
    viewport.height - padding - (anchor.top + anchor.height + offset),
  );
  const spaceAbove = Math.max(0, anchor.top - offset - padding);

  const placement: AnchoredPlacement = (() => {
    if (preferredPlacement === "bottom") {
      if (spaceBelow >= popup.height) {
        return "bottom";
      }
      if (spaceAbove > spaceBelow && spaceAbove >= popup.height) {
        return "top";
      }
      return "bottom";
    }

    if (spaceAbove >= popup.height) {
      return "top";
    }
    if (spaceBelow > spaceAbove && spaceBelow >= popup.height) {
      return "bottom";
    }
    return "top";
  })();

  let top = 0;
  let maxHeight: number | undefined;

  if (placement === "bottom") {
    top = anchor.top + anchor.height + offset;
    if (popup.height > spaceBelow) {
      maxHeight = spaceBelow;
    }
  } else {
    const idealTop = anchor.top - offset - popup.height;
    if (idealTop >= padding) {
      top = idealTop;
    } else {
      top = padding;
      maxHeight = spaceAbove;
    }
    if (popup.height > spaceAbove) {
      maxHeight = spaceAbove;
    }
  }

  let left =
    align === "end"
      ? anchor.left + anchor.width - popup.width
      : anchor.left;

  if (left + popup.width > viewport.width - padding) {
    left = Math.max(padding, viewport.width - padding - popup.width);
  }
  if (left < padding) {
    left = padding;
  }

  return {
    top,
    left,
    maxHeight: maxHeight && maxHeight > 0 ? maxHeight : undefined,
    placement,
  };
}

export function domRectToAnchorRect(rect: DOMRect): AnchorRect {
  return {
    top: rect.top,
    left: rect.left,
    width: rect.width,
    height: rect.height,
  };
}
