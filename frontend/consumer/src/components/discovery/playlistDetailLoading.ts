/** Logged-in listeners need a client fetch so isOwned/isSaved flags are accurate. */
export function shouldClientRefreshPlaylistDetail(
  isAuthenticated: boolean,
  isLikedMode: boolean,
): boolean {
  return isAuthenticated || isLikedMode;
}
