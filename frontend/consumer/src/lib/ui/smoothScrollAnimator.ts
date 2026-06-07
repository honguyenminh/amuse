const DEFAULT_DURATION_MS = 140;

export type SmoothScrollAnimator = {
  scrollBy: (delta: number) => void;
  scrollTo: (top: number) => void;
  jumpTo: (top: number) => void;
  cancel: () => void;
};

function easeOutCubic(progress: number): number {
  return 1 - (1 - progress) ** 3;
}

export function createSmoothScrollAnimator(
  element: HTMLElement,
  options?: { durationMs?: number; instant?: boolean },
): SmoothScrollAnimator {
  const durationMs = options?.durationMs ?? DEFAULT_DURATION_MS;
  const instant = options?.instant ?? false;

  let rafId = 0;
  let targetTop = 0;
  let animStartTop = 0;
  let animStartTime = 0;

  const maxScroll = () => Math.max(0, element.scrollHeight - element.clientHeight);
  const clamp = (top: number) => Math.max(0, Math.min(maxScroll(), top));

  const cancel = () => {
    if (rafId) {
      cancelAnimationFrame(rafId);
      rafId = 0;
    }
  };

  const tick = (now: number) => {
    const progress = Math.min(1, (now - animStartTime) / durationMs);
    element.scrollTop =
      animStartTop + (targetTop - animStartTop) * easeOutCubic(progress);
    if (progress < 1) {
      rafId = requestAnimationFrame(tick);
      return;
    }
    rafId = 0;
    element.scrollTop = targetTop;
  };

  const startAnimation = (nextTarget: number) => {
    targetTop = clamp(nextTarget);
    animStartTop = element.scrollTop;
    animStartTime = performance.now();
    if (instant || durationMs <= 0) {
      cancel();
      element.scrollTop = targetTop;
      return;
    }
    if (!rafId) {
      rafId = requestAnimationFrame(tick);
    }
  };

  const scrollBy = (delta: number) => {
    if (instant || durationMs <= 0) {
      startAnimation(element.scrollTop + delta);
      return;
    }
    if (rafId) {
      targetTop = clamp(targetTop + delta);
      animStartTop = element.scrollTop;
      animStartTime = performance.now();
      return;
    }
    startAnimation(element.scrollTop + delta);
  };

  const scrollTo = (top: number) => {
    cancel();
    startAnimation(top);
  };

  const jumpTo = (top: number) => {
    cancel();
    targetTop = clamp(top);
    element.scrollTop = targetTop;
  };

  return { scrollBy, scrollTo, jumpTo, cancel };
}
