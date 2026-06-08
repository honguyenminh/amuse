"use client";

import { useEffect, useState } from "react";

type UseServerSyncedDetailOptions<T> = {
  routeKey: string;
  initialDetail?: T;
  fetchDetail: () => Promise<T>;
};

type UseServerSyncedDetailResult<T> = {
  detail: T | null;
  pending: boolean;
  error: string | null;
};

export function resolveServerSyncedDisplay<T>({
  routeKey,
  resolvedKey,
  detail,
  initialDetail,
}: {
  routeKey: string;
  resolvedKey: string | null;
  detail: T | null;
  initialDetail?: T;
}) {
  const synced = resolvedKey === routeKey;
  const displayDetail = synced ? detail : (initialDetail ?? detail);
  const pending = !synced && !initialDetail;
  return { displayDetail, pending };
}

/**
 * Keeps client state aligned with SSR props on soft navigation while still
 * supporting client-only fetch when no server detail was provided.
 */
export function useServerSyncedDetail<T>({
  routeKey,
  initialDetail,
  fetchDetail,
}: UseServerSyncedDetailOptions<T>): UseServerSyncedDetailResult<T> {
  const [detail, setDetail] = useState<T | null>(initialDetail ?? null);
  const [resolvedKey, setResolvedKey] = useState<string | null>(
    initialDetail ? routeKey : null,
  );
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setError(null);

    if (initialDetail) {
      setDetail(initialDetail);
      setResolvedKey(routeKey);
      return;
    }

    let cancelled = false;
    setDetail(null);
    setResolvedKey(null);

    void fetchDetail()
      .then((response) => {
        if (!cancelled) {
          setDetail(response);
          setResolvedKey(routeKey);
        }
      })
      .catch((err: Error) => {
        if (!cancelled) setError(err.message);
      });

    return () => {
      cancelled = true;
    };
  }, [routeKey, initialDetail, fetchDetail]);

  const { displayDetail, pending } = resolveServerSyncedDisplay({
    routeKey,
    resolvedKey,
    detail,
    initialDetail,
  });

  return {
    detail: displayDetail,
    pending,
    error,
  };
}
