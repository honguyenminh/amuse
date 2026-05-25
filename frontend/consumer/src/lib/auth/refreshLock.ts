let refreshPromise: Promise<string> | null = null;

export async function withRefreshLock(
  refreshFn: () => Promise<string>,
): Promise<string> {
  if (refreshPromise) {
    return refreshPromise;
  }

  refreshPromise = refreshFn().finally(() => {
    refreshPromise = null;
  });

  return refreshPromise;
}
