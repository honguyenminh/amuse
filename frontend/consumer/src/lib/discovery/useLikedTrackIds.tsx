"use client";

import { getLikedPlayableTracks, likeTrack, unlikeTrack } from "@/lib/api/discoveryClient";
import { ApiError } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";

type LikedTracksContextValue = {
  isLiked: (trackId: string) => boolean;
  toggleLike: (trackId: string) => Promise<void>;
  loading: boolean;
};

const LikedTracksContext = createContext<LikedTracksContextValue | null>(null);

export function LikedTracksProvider({ children }: { children: ReactNode }) {
  const auth = useAuth();
  const [likedIds, setLikedIds] = useState<Set<string>>(() => new Set());
  const [loading, setLoading] = useState(false);
  const loadedRef = useRef(false);

  useEffect(() => {
    if (!auth.isAuthenticated) {
      setLikedIds(new Set());
      loadedRef.current = false;
      return;
    }

    let cancelled = false;
    setLoading(true);
    void getLikedPlayableTracks()
      .then((response) => {
        if (cancelled) return;
        setLikedIds(new Set(response.tracks.map((track) => track.trackId)));
        loadedRef.current = true;
      })
      .catch(() => {
        if (!cancelled) loadedRef.current = true;
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [auth.isAuthenticated]);

  const isLiked = useCallback((trackId: string) => likedIds.has(trackId), [likedIds]);

  const toggleLike = useCallback(
    async (trackId: string) => {
      if (!auth.isAuthenticated) return;
      const liked = likedIds.has(trackId);
      setLikedIds((prev) => {
        const next = new Set(prev);
        if (liked) next.delete(trackId);
        else next.add(trackId);
        return next;
      });
      try {
        if (liked) await unlikeTrack(trackId);
        else await likeTrack(trackId);
      } catch (error) {
        setLikedIds((prev) => {
          const next = new Set(prev);
          if (liked) next.add(trackId);
          else next.delete(trackId);
          return next;
        });
        if (error instanceof ApiError) throw error;
        throw new Error("Could not update liked status");
      }
    },
    [auth.isAuthenticated, likedIds],
  );

  const value = useMemo(
    () => ({ isLiked, toggleLike, loading }),
    [isLiked, toggleLike, loading],
  );

  return <LikedTracksContext.Provider value={value}>{children}</LikedTracksContext.Provider>;
}

export function useLikedTrackIds(): LikedTracksContextValue {
  const ctx = useContext(LikedTracksContext);
  if (!ctx) throw new Error("useLikedTrackIds must be used within LikedTracksProvider");
  return ctx;
}
