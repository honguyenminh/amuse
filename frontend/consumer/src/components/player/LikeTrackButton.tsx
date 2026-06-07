"use client";

import { IconButton } from "@/components/ui/IconButton";
import { useLikedTrackIds } from "@/lib/discovery/useLikedTrackIds";
import { useAuth } from "@/lib/auth/AuthProvider";
import { cn } from "@/lib/cn";
import Link from "next/link";
import { useCallback, useState } from "react";

type LikeTrackButtonProps = {
  trackId: string;
  size?: "sm" | "md";
  className?: string;
};

export function LikeTrackButton({ trackId, size = "md", className }: LikeTrackButtonProps) {
  const auth = useAuth();
  const { isLiked, toggleLike } = useLikedTrackIds();
  const [busy, setBusy] = useState(false);
  const liked = isLiked(trackId);

  const onToggle = useCallback(async () => {
    setBusy(true);
    try {
      await toggleLike(trackId);
    } catch {
      // keep optimistic revert inside provider
    } finally {
      setBusy(false);
    }
  }, [toggleLike, trackId]);

  if (!auth.isAuthenticated) {
    const next =
      typeof window !== "undefined"
        ? encodeURIComponent(window.location.pathname)
        : encodeURIComponent("/home");
    return (
      <Link
        href={`/login?next=${next}`}
        aria-label="Like track"
        className={cn(
          "inline-flex shrink-0 items-center justify-center rounded-full bg-transparent text-on-surface transition-colors hover:bg-surface-variant",
          size === "sm" ? "h-8 w-8" : "h-10 w-10",
          className,
        )}
      >
        <HeartIcon filled={false} />
      </Link>
    );
  }

  return (
    <IconButton
      label={liked ? "Unlike track" : "Like track"}
      variant={liked ? "tonal" : "ghost"}
      size={size}
      className={className}
      disabled={busy}
      onClick={() => void onToggle()}
    >
      <HeartIcon filled={liked} />
    </IconButton>
  );
}

function HeartIcon({ filled }: { filled: boolean }) {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 24 24"
      fill={filled ? "currentColor" : "none"}
      stroke="currentColor"
      strokeWidth="2"
      aria-hidden
      className={cn(filled && "text-primary")}
    >
      <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z" />
    </svg>
  );
}
