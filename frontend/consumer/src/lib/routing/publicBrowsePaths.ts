/** Routes that must SSR for anonymous visitors and crawlers without waiting on auth. */
export function isPublicBrowsePath(pathname: string): boolean {
  return (
    pathname === "/home" ||
    pathname.startsWith("/artist/") ||
    pathname.startsWith("/release/") ||
    pathname.startsWith("/playlist/") ||
    pathname.startsWith("/search") ||
    pathname.startsWith("/hashtag/")
  );
}
