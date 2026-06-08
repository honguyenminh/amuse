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

type FetchState<T> = {
  routeKey: string;
  detail: T | null;
  error: string | null;
  status: "loading" | "done" | "error";
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
  const [fetchState, setFetchState] = useState<FetchState<T> | null>(null);

  const needsFetch = initialDetail == null;

  useEffect(() => {
    if (!needsFetch) {
      return;
    }

    let cancelled = false;

    void fetchDetail()
      .then((response) => {
        if (!cancelled) {
          setFetchState({
            routeKey,
            detail: response,
            error: null,
            status: "done",
          });
        }
      })
      .catch((err: Error) => {
        if (!cancelled) {
          setFetchState({
            routeKey,
            detail: null,
            error: err.message,
            status: "error",
          });
        }
      });

    return () => {
      cancelled = true;
    };
  }, [routeKey, needsFetch, fetchDetail]);

  if (initialDetail != null) {
    return { detail: initialDetail, pending: false, error: null };
  }

  const synced = fetchState?.routeKey === routeKey;
  const pending =
    fetchState == null || !synced || fetchState.status === "loading";

  return {
    detail:
      synced && fetchState?.status === "done" ? fetchState.detail : null,
    pending,
    error:
      synced && fetchState?.status === "error" ? fetchState.error : null,
  };
}
