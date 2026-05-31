export function safeReturnPath(
  value: string | null | undefined,
  fallback = "/dashboard",
): string {
  if (!value || !value.startsWith("/") || value.startsWith("//")) {
    return fallback;
  }
  return value;
}

export function readReturnPath(
  searchParams: Pick<URLSearchParams, "get">,
  fallback = "/dashboard",
): string {
  return safeReturnPath(
    searchParams.get("next") ?? searchParams.get("returnTo"),
    fallback,
  );
}
